using Microsoft.EntityFrameworkCore;
using Groundkey.Domain.Cadastros;
using Groundkey.Domain.Common;
using Groundkey.Domain.Documentos;
using Groundkey.Domain.Estrutura;

namespace Groundkey.Application.Servicos;

/// <summary>
/// Configuração do sistema: parâmetros, listas, matriz de documentos exigidos, regras de notificação,
/// permissões do SGQ, usuários, alçadas e delegações. Senha e login ficam no <see cref="ContaServico"/>.
/// </summary>
public sealed class SistemaServico(IFabricaDb fabrica, TimeProvider relogio)
{
    private const string PrefixoPesoAvaliacao = "AVAL.PESO";
    private IUsuarioAtual Usuario => fabrica.Usuario;

    // ───────────── Parâmetros, listas, documentos exigidos e notificações (só Administrador) ─────────────

    /// <summary>Altera o valor de um parâmetro. Para os pesos da avaliação, devolve a soma atual (a tela avisa se não fechar 1).</summary>
    public async Task<decimal?> SalvarParametroAsync(Guid parametroId, decimal valor, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await Permissoes.ExigirAsync(db, Usuario, "alterar parâmetros");
        var p = await db.Parametros.FirstOrDefaultAsync(x => x.Id == parametroId, ct) ?? throw new RegraDeNegocioException("Parâmetro não encontrado.");
        ValidacaoException.Se(ValidarParametro(p.Chave, valor));
        p.Valor = valor;
        await db.SaveChangesAsync(ct);
        // Faixa de alçada / nº de propostas exigidas valem nas cotações em andamento.
        if (await SincronizacaoCadastros.PropostasExigidasAsync(db, ct) > 0) await db.SaveChangesAsync(ct);
        if (!p.Chave.StartsWith(PrefixoPesoAvaliacao)) return null;
        return await db.Parametros.Where(x => x.Chave.StartsWith(PrefixoPesoAvaliacao)).SumAsync(x => x.Valor, ct);
    }

    public Task SalvarItemListaAsync(ItemLista item, CancellationToken ct = default) =>
        SalvarConfiguracaoAsync(item, item.Nome, "alterar listas", ct);

    public Task SalvarTipoDocumentoAsync(TipoDocumentoExigido tipo, CancellationToken ct = default) =>
        SalvarConfiguracaoAsync(tipo, tipo.Nome, "alterar a matriz de documentos exigidos", ct);

    public Task SalvarRegraNotificacaoAsync(RegraNotificacao regra, CancellationToken ct = default) =>
        SalvarConfiguracaoAsync(regra, regra.Evento, "alterar notificações", ct);

    private async Task SalvarConfiguracaoAsync<T>(T item, string? nome, string acao, CancellationToken ct) where T : EntidadeAuditavel
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ValidacaoException(["Informe o nome."]);
        await using var db = fabrica.Criar();
        await Permissoes.ExigirAsync(db, Usuario, acao);
        await Grafo.SalvarAsync(db, item);
        await db.SaveChangesAsync(ct);
    }

    // ───────────── Permissões de documentos do SGQ (Qualidade/Administrador) ─────────────

    public async Task SalvarPermissaoAsync(PermissaoDocumento regra, CancellationToken ct = default)
    {
        ValidacaoException.Se(ValidarPermissao(regra));
        await using var db = fabrica.Criar();
        await Permissoes.ExigirAsync(db, Usuario, "alterar permissões de documentos", PerfilUsuario.Qualidade);
        await Grafo.SalvarAsync(db, regra);
        // O log de documentos é a evidência do SGQ de quem mudou o acesso — vai na mesma gravação da regra.
        db.EventosDocumento.Add(EventoPermissao(regra.Escopo == EscopoPermissao.Documento ? regra.AlvoId : null, $"Regra {regra.Escopo} salva"));
        await db.SaveChangesAsync(ct);
    }

    public async Task ExcluirPermissaoAsync(Guid regraId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await Permissoes.ExigirAsync(db, Usuario, "alterar permissões de documentos", PerfilUsuario.Qualidade);
        var r = await db.Permissoes.FirstOrDefaultAsync(x => x.Id == regraId, ct) ?? throw new RegraDeNegocioException("Regra não encontrada.");
        db.Permissoes.Remove(r); // exclusão lógica pelo interceptor de auditoria
        db.EventosDocumento.Add(EventoPermissao(r.Escopo == EscopoPermissao.Documento ? r.AlvoId : null, "Regra excluída"));
        await db.SaveChangesAsync(ct);
    }

    private EventoDocumento EventoPermissao(Guid? documentoId, string detalhe) => new()
    {
        EmpresaId = Usuario.EmpresaId, DataHora = relogio.GetUtcNow(), UsuarioId = Usuario.UsuarioId, UsuarioNome = Usuario.Nome,
        Evento = EventoDocumentoTipo.PermissaoAlterada, DocumentoId = documentoId, Detalhe = detalhe, Origem = Usuario.Origem
    };

    // ───────────── Usuários, alçadas e delegações (só Administrador) ─────────────

    /// <summary>Cria ou altera o usuário (obras e limites juntos). Login e e-mail são normalizados e conferidos como únicos.</summary>
    public async Task<Guid> SalvarUsuarioAsync(Usuario u, CancellationToken ct = default)
    {
        u.Login = PoliticaSenha.NormalizarLogin(u.Login);
        u.Email = (u.Email ?? "").Trim().ToLowerInvariant();
        ValidacaoException.Se(ValidarUsuario(u, Usuario.UsuarioId));
        await using var db = fabrica.Criar();
        await Permissoes.ExigirAsync(db, Usuario, "alterar usuários");
        if (await db.Usuarios.AnyAsync(x => x.Email == u.Email && x.Id != u.Id, ct)) throw new RegraDeNegocioException("E-mail já cadastrado.");
        // O login entra por qualquer empresa (tela de login não sabe a empresa): por isso a unicidade ignora o filtro de empresa.
        if (u.ContaId is null && await db.Usuarios.IgnoreQueryFilters().AnyAsync(x => x.Login == u.Login && x.Id != u.Id && !x.Excluido, ct))
            throw new RegraDeNegocioException("Login já está em uso.");
        foreach (var o in u.Obras) o.Obra = null!;
        if (u.AcessoTodasObras) u.Obras.Clear();
        await Grafo.SalvarAsync(db, u, nameof(Domain.Estrutura.Usuario.Obras), nameof(Domain.Estrutura.Usuario.LimitesAprovacao));
        await db.SaveChangesAsync(ct);
        // Perfil, desativação ou troca do usuário valem nas aprovações em andamento (aprovador nominal inativo → substituto/perfil).
        if (await AprovacaoServico.RecalcularPendentesAsync(db, x => true, ct) > 0) await db.SaveChangesAsync(ct);
        AcessoServico.Invalidar(); // perfil do usuário muda o acesso por tela na hora
        AcessoObrasAoVivo.Definir(u.Id, u.VeTodasObras, u.Obras.Select(o => o.ObraId)); // e as obras liberadas também
        return u.Id;
    }

    /// <summary>
    /// Acesso por obra: marca quem acessa a obra (usuários com "todas as obras" ou perfil Administrador/Diretoria não mudam).
    /// </summary>
    public async Task DefinirAcessoObraAsync(Guid obraId, IReadOnlyDictionary<Guid, bool> usuarios, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await Permissoes.ExigirAsync(db, Usuario, "alterar usuários");
        var ids = usuarios.Keys.ToList();
        var lista = await db.Usuarios.Include(u => u.Obras).Where(u => ids.Contains(u.Id)).ToListAsync(ct);
        foreach (var u in lista.Where(u => !u.VeTodasObras))
        {
            var tem = u.Obras.FirstOrDefault(o => o.ObraId == obraId);
            if (usuarios[u.Id] && tem is null)
            {
                var novo = new Domain.Estrutura.UsuarioObra { UsuarioId = u.Id, ObraId = obraId };
                db.UsuariosObras.Add(novo); // Id já gerado: adicionar pelo DbSet (pela coleção o EF trataria como existente)
                u.Obras.Add(novo);
            }
            else if (!usuarios[u.Id] && tem is not null) { u.Obras.Remove(tem); db.Remove(tem); }
        }
        await db.SaveChangesAsync(ct);
        foreach (var u in lista) AcessoObrasAoVivo.Definir(u.Id, u.VeTodasObras, u.Obras.Select(o => o.ObraId));
    }

    public async Task SalvarAlcadaAsync(Alcada a, CancellationToken ct = default)
    {
        ValidacaoException.Se(ValidarAlcada(a));
        await using var db = fabrica.Criar();
        await Permissoes.ExigirAsync(db, Usuario, "alterar alçadas");
        await Grafo.SalvarAsync(db, a);
        await db.SaveChangesAsync(ct);
        // Aprovações em andamento dessa entidade refazem as etapas ainda não decididas.
        if (await AprovacaoServico.RecalcularPendentesAsync(db, x => x.Entidade == a.Entidade, ct) > 0) await db.SaveChangesAsync(ct);
    }

    public async Task SalvarDelegacaoAsync(DelegacaoAprovacao d, CancellationToken ct = default)
    {
        ValidacaoException.Se(ValidarDelegacao(d));
        await using var db = fabrica.Criar();
        await Permissoes.ExigirAsync(db, Usuario, "alterar delegações de outros usuários");
        await Grafo.SalvarAsync(db, d);
        await db.SaveChangesAsync(ct);
    }

    public async Task RevogarDelegacaoAsync(Guid delegacaoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await Permissoes.ExigirAsync(db, Usuario, "revogar delegações");
        var d = await db.DelegacoesAprovacao.FirstOrDefaultAsync(x => x.Id == delegacaoId, ct) ?? throw new RegraDeNegocioException("Delegação não encontrada.");
        if (d.Revogada) return;
        d.Revogada = true; d.RevogadaEm = relogio.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// O próprio usuário delega as aprovações dele (férias, ausência). Qualquer perfil pode — mas só em nome próprio:
    /// o titular é sempre o usuário logado, nunca o que veio da tela.
    /// </summary>
    public async Task DelegarMinhasAprovacoesAsync(DelegacaoAprovacao d, CancellationToken ct = default)
    {
        d.TitularUsuarioId = Usuario.UsuarioId ?? throw new RegraDeNegocioException("Usuário não identificado.");
        ValidacaoException.Se(ValidarDelegacao(d));
        await using var db = fabrica.Criar();
        db.DelegacoesAprovacao.Add(d);
        await db.SaveChangesAsync(ct);
    }

    // ───────────── Validações puras (cobertas por testes) ─────────────

    public static IReadOnlyList<string> ValidarParametro(string chave, decimal valor)
    {
        var e = new List<string>();
        if (valor < 0) e.Add("Valor não pode ser negativo.");
        else if (chave.StartsWith(PrefixoPesoAvaliacao) && valor > 1) e.Add("Peso vai de 0 a 1 (ex.: 0,25).");
        return e;
    }

    public static IReadOnlyList<string> ValidarPermissao(PermissaoDocumento p)
    {
        var e = new List<string>();
        if (p.AlvoId == Guid.Empty) e.Add("Informe a pasta ou documento.");
        if (p.Perfil is null && p.UsuarioId is null) e.Add("Informe o perfil ou o usuário.");
        if (p.Perfil is not null && p.UsuarioId is not null) e.Add("Escolha perfil OU usuário.");
        return e;
    }

    /// <summary>Espera login e e-mail já normalizados.</summary>
    public static IReadOnlyList<string> ValidarUsuario(Usuario u, Guid? usuarioLogadoId)
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(u.Nome)) e.Add("Informe o nome.");
        if (string.IsNullOrWhiteSpace(u.Email) || !u.Email.Contains('@')) e.Add("Informe um e-mail válido.");
        // Vínculo de outra construtora entra com o login da conta principal: não tem login próprio.
        if (u.ContaId is null) e.AddRange(PoliticaSenha.ValidarLogin(u.Login));
        else u.Login = "";
        if (u.LimitesAprovacao.Any(l => l.ValorMaximo < 0)) e.Add("Limite não pode ser negativo.");
        // Evita a empresa ficar sem administrador por engano do próprio administrador.
        if (u.Id == usuarioLogadoId && u.Perfil != PerfilUsuario.Administrador) e.Add("Você não pode remover o próprio perfil de Administrador.");
        return e;
    }

    public static IReadOnlyList<string> ValidarAlcada(Alcada a)
    {
        var e = new List<string>();
        if (a.Ordem < 1) e.Add("Ordem deve ser 1 ou maior.");
        if (a.ValorMinimo is { } mi && a.ValorMaximo is { } ma && ma < mi) e.Add("Valor até menor que valor a partir de.");
        return e;
    }

    public static IReadOnlyList<string> ValidarDelegacao(DelegacaoAprovacao d)
    {
        var e = new List<string>();
        if (d.TitularUsuarioId == Guid.Empty || d.SubstitutoUsuarioId == Guid.Empty) e.Add("Informe titular e substituto.");
        else if (d.TitularUsuarioId == d.SubstitutoUsuarioId) e.Add("Titular e substituto devem ser diferentes.");
        if (d.Fim <= d.Inicio) e.Add("Fim deve ser posterior ao início.");
        if (string.IsNullOrWhiteSpace(d.Motivo)) e.Add("Informe o motivo.");
        return e;
    }
}
