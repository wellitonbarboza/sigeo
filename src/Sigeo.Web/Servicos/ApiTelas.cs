using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Abstracoes;
using Sigeo.Application.Consultas;
using Sigeo.Application.Servicos;
using Sigeo.Domain.Common;

namespace Sigeo.Web.Servicos;

/// <summary>
/// API das telas que rodam no navegador (WebAssembly). Mesmo site e mesmo cookie de login das telas do servidor:
/// exige usuário interno (política padrão), a empresa vem das claims e a obra é conferida em cada consulta.
/// Proteção contra requisição forjada de outro site: cookie SameSite=Lax (não vai em POST de outro domínio) e corpo JSON.
/// </summary>
public static class ApiTelas
{
    /// <summary>Painéis mudam pouco: 5 min por empresa/filtro poupa o banco (e deixa o Azure SQL pausar). "Atualizar" força nova leitura.</summary>
    private static readonly TimeSpan ValidadePainel = TimeSpan.FromMinutes(5);

    public static void Mapear(WebApplication app)
    {
        var api = app.MapGroup("/api/telas").RequireAuthorization().AddEndpointFilter<FiltroProblemDetails>();

        api.MapGet("/obras", (PaineisConsultas q, CancellationToken ct) => q.ObrasAsync(ct));

        // Acesso por tela do usuário (menu e bloqueio das telas do navegador).
        api.MapGet("/acesso", (AcessoServico a, CancellationToken ct) => a.MapaAsync(ct));

        // Contador de mensagens não lidas do menu (telas que rodam no navegador).
        api.MapGet("/mensagens/nao-lidas", (MensagensServico m, CancellationToken ct) => m.NaoLidasAsync(ct));

        api.MapGet("/painel", async (int? meses, Guid? obra, bool? novo, PaineisConsultas q, CacheCompartilhado cache, IUsuarioAtual u, CancellationToken ct) =>
        {
            q.ConferirObra(obra);
            var chave = $"painel:{meses ?? 6}:{obra}";
            if (novo == true) cache.Invalidar(u.EmpresaId, chave);
            return await cache.ObterAsync(u.EmpresaId, chave, ValidadePainel, () => q.IndicadoresAsync(meses ?? 6, obra, ct));
        });

        api.MapGet("/sst/painel", async (Guid? obra, bool? novo, PaineisConsultas q, CacheCompartilhado cache, IUsuarioAtual u, CancellationToken ct) =>
        {
            q.ConferirObra(obra);
            var chave = $"sst:{obra}";
            if (novo == true) cache.Invalidar(u.EmpresaId, chave);
            return await cache.ObterAsync(u.EmpresaId, chave, ValidadePainel, () => q.SstAsync(obra, ct));
        });

        api.MapGet("/obra/{obra:guid}/torres", (Guid obra, PaineisConsultas q, CancellationToken ct) => q.TorresAsync(obra, ct));

        api.MapGet("/obra/{obra:guid}/mapa-torre/{torre:guid}", async (Guid obra, Guid torre, PaineisConsultas q, CancellationToken ct) =>
            await q.MapaTorreAsync(obra, torre, ct) is { } m ? Results.Ok(m) : Results.NotFound()).Tela("obra/mapa-torre");

        // Orçamento por local (Engenharia)
        api.MapGet("/obra/{obra:guid}/orcamento", (Guid obra, EngenhariaConsultas q, CancellationToken ct) => q.OrcamentoAsync(obra, ct)).Tela("engenharia/orcamento");

        api.MapPost("/obra/{obra:guid}/orcamento/gerar", (Guid obra, PaineisConsultas p, OrcamentoServico orc, CancellationToken ct) =>
        {
            p.ConferirObra(obra);
            return orc.GerarAsync(obra, ct);
        }).Tela("engenharia/orcamento");

        api.MapPost("/orcamento/celula/{id:guid}", async (Guid id, EdicaoCelula e, OrcamentoServico orc, CancellationToken ct) =>
        {
            if (e.Quantidade < 0) throw new Sigeo.Domain.Aprovacoes.RegraDeNegocioException("Informe uma quantidade maior ou igual a zero.");
            await orc.EditarCelulaAsync(id, e.Quantidade, e.Travar, ct);
            return Results.NoContent();
        }).Tela("engenharia/orcamento");

        api.MapPost("/obra/{obra:guid}/orcamento/demanda", (Guid obra, PedidoDemanda d, PaineisConsultas p, OrcamentoServico orc, CancellationToken ct) =>
        {
            p.ConferirObra(obra);
            return orc.DemandaAsync(obra, d.Composicoes, d.Locais, d.Tipo, ct: ct);
        }).Tela("engenharia/orcamento");

        // A requisição é montada com a demanda recalculada aqui (a tela manda só a escolha, nunca as quantidades).
        api.MapPost("/obra/{obra:guid}/orcamento/requisicao", async (Guid obra, PedidoDemanda d, PaineisConsultas p, OrcamentoServico orc,
            RequisicoesServico reqs, Catalogos cat, IFabricaDb fabrica, TimeProvider relogio, CancellationToken ct) =>
        {
            p.ConferirObra(obra);
            if (d.Composicoes.Count == 0 || d.Locais.Count == 0) throw new Sigeo.Domain.Aprovacoes.RegraDeNegocioException("Escolha ao menos um serviço e um local.");
            var demanda = await orc.DemandaAsync(obra, d.Composicoes, d.Locais, d.Tipo, ct: ct);
            string? grupo;
            await using (var db = fabrica.CriarLeitura())
                grupo = await db.Composicoes.Where(c => c.Id == d.Composicoes[0]).Select(c => c.Grupo).FirstOrDefaultAsync(ct);
            var id = await reqs.CriarDoOrcamentoAsync(obra, d.Tipo, demanda, grupo, await cat.ListaAsync(TipoLista.Atividade),
                DateOnly.FromDateTime(relogio.GetLocalNow().Date), ct);
            return Results.Ok(new { id });
        }).Tela("requisicoes");
    }
}

/// <summary>
/// Endpoint de uma tela: confere se o usuário vê a tela (módulo contratado + perfil de acesso) e marca a tela da requisição,
/// para a barreira de gravação conferir "Incluir/Editar" ou "Excluir" nela.
/// </summary>
public static class TelaEndpoint
{
    public static RouteHandlerBuilder Tela(this RouteHandlerBuilder b, string rota) => b.AddEndpointFilter(async (ctx, next) =>
    {
        var http = ctx.HttpContext;
        http.Items[Sigeo.Web.Seguranca.ContextoTelaWeb.Chave] = rota;
        var mapa = await http.RequestServices.GetRequiredService<AcessoServico>().MapaAsync(http.RequestAborted);
        AcessoServico.Conferir(mapa, rota, Sigeo.Contratos.AcaoTela.Ver);
        return await next(ctx);
    });
}
