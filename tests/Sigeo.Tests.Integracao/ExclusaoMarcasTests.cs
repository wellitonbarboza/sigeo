using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Application.Abstracoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Tests.Integracao;

/// <summary>Exclusão genérica (em uso bloqueia, livre exclui) e marcas normalizadas pelo cadastro.</summary>
[Collection(ColecaoBanco.Nome)]
public sealed class ExclusaoMarcasTests(BancoFixture f)
{
    [FatoComBanco]
    public async Task Exclui_cadastro_livre_e_bloqueia_em_uso_e_normaliza_marca()
    {
        var livre = await Dados.CriarProdutoAsync(f, "Produto livre ficticio", 1m);
        var usado = await Dados.CriarProdutoAsync(f, "Produto usado ficticio", 2m);
        var obra = await Dados.CriarObraAsync(f);
        await using (var db = f.Fabrica.Criar()) { db.Marcas.Add(new Marca { Nome = "Marca Ficticia" }); await db.SaveChangesAsync(); }
        var r = new Requisicao
        {
            ObraId = obra, Tipo = TipoRequisicao.Material, Atividade = "Teste", DataEntrega = DateOnly.FromDateTime(DateTime.Today.AddDays(20)),
            Itens = [new RequisicaoItem { ProdutoId = usado.Id, Descricao = usado.Nome, Unidade = "KG", Quantidade = 5, Marca = "  marca   FICTÍCIA" }]
        };
        await f.Requisicoes.SalvarRequisicaoAsync(r);
        await using (var db = f.Fabrica.CriarLeitura())
            Assert.Equal("Marca Ficticia", (await db.RequisicaoItens.SingleAsync(i => i.RequisicaoId == r.Id)).Marca); // grafia do cadastro

        IExclusao exc = new Sigeo.Infrastructure.Servicos.Exclusao(f.Fabrica, f.Usuario);
        Assert.Contains(await exc.ImpedimentosAsync(typeof(Produto), usado.Id), m => m.Contains("requisição"));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => exc.ExcluirAsync(typeof(Produto), usado.Id, null));
        Assert.Empty(await exc.ImpedimentosAsync(typeof(Produto), livre.Id));
        await exc.ExcluirAsync(typeof(Produto), livre.Id, null);
        await using (var db = f.Fabrica.CriarLeitura())
        {
            Assert.False(await db.Produtos.AnyAsync(p => p.Id == livre.Id));
            Assert.True(await db.Produtos.IgnoreQueryFilters().AnyAsync(p => p.Id == livre.Id && p.Excluido)); // exclusão lógica
        }
        // Requisição enviada não é excluída (regra de situação), rascunho sim.
        Assert.Empty((await exc.ImpedimentosAsync(typeof(Requisicao), r.Id)).Where(x => x.Contains("enviada")));
    }

    [FatoComBanco]
    public async Task Acesso_por_obra_grava_e_vale_na_hora()
    {
        var obra = await Dados.CriarObraAsync(f);
        var u = new Usuario { Login = "campo" + Dados.Sufixo().ToLowerInvariant(), Nome = "Campo Ficticio", Email = $"c{Dados.Sufixo()}@exemplo.com", Perfil = PerfilUsuario.Campo };
        var d = new Usuario { Login = "dir" + Dados.Sufixo().ToLowerInvariant(), Nome = "Diretor Ficticio", Email = $"d{Dados.Sufixo()}@exemplo.com", Perfil = PerfilUsuario.Diretoria };
        await using (var db = f.Fabrica.Criar()) { db.Usuarios.AddRange(u, d); await db.SaveChangesAsync(); }
        var srv = new SistemaServico(f.Fabrica, f.Relogio);
        await srv.DefinirAcessoObraAsync(obra, new Dictionary<Guid, bool> { [u.Id] = true, [d.Id] = true });
        await using (var db = f.Fabrica.CriarLeitura())
        {
            Assert.True(await db.UsuariosObras.AnyAsync(x => x.UsuarioId == u.Id && x.ObraId == obra));
            Assert.False(await db.UsuariosObras.AnyAsync(x => x.UsuarioId == d.Id)); // Diretoria vê todas: não precisa marcar
        }
        Assert.True(AcessoObrasAoVivo.Obter(u.Id, out var todas, out var obras) && !todas && obras.Contains(obra));
        Assert.True(AcessoObrasAoVivo.Obter(d.Id, out var todasD, out _) && todasD);
        await srv.DefinirAcessoObraAsync(obra, new Dictionary<Guid, bool> { [u.Id] = false });
        Assert.True(AcessoObrasAoVivo.Obter(u.Id, out _, out var depois) && !depois.Contains(obra));
    }
}
