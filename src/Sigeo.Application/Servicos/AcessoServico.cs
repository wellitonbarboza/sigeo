using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Sigeo.Contratos;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Servicos;

/// <summary>Tela em que o usuário está agindo (rota relativa, ex.: "pedidos"). Null fora de tela (rotinas, login, API sem tela).</summary>
public interface IContextoTela
{
    string? Rota { get; }
}

public sealed class SemContextoTela : IContextoTela
{
    public string? Rota => null;
}

/// <summary>
/// Acesso por tela: a plataforma libera telas para a construtora; o perfil de acesso (com exceções por usuário) diz o que
/// cada usuário vê e faz em cada tela. Administrador da construtora vê e faz tudo o que foi liberado (não se tranca para fora).
/// O mapa fica em memória por pouco tempo e é refeito a cada mudança — vale sem sair e entrar de novo.
/// </summary>
public sealed class AcessoServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    private static long _versao;
    private static readonly ConcurrentDictionary<(Guid Empresa, Guid Usuario), (long Versao, DateTimeOffset Ate, MapaAcesso Mapa)> Cache = new();
    private static readonly TimeSpan Validade = TimeSpan.FromMinutes(5);

    /// <summary>Algo mudou (perfil, exceção, telas da construtora, perfil do usuário): os mapas em memória são refeitos.</summary>
    public static void Invalidar() => Interlocked.Increment(ref _versao);

    public static MapaAcesso Completo()
    {
        var telas = CatalogoTelas.Telas.Values.ToDictionary(t => t.Rota, t => t.Acoes, StringComparer.OrdinalIgnoreCase);
        return new MapaAcesso(true, telas, []);
    }

    /// <summary>Mapa do usuário logado. Rotina de sistema (sem usuário) pode tudo.</summary>
    public async Task<MapaAcesso> MapaAsync(CancellationToken ct = default)
    {
        if (usuario.UsuarioId is not { } uid) return Completo();
        var chave = (usuario.EmpresaId, uid);
        var v = Interlocked.Read(ref _versao);
        var agora = relogio.GetUtcNow();
        if (Cache.TryGetValue(chave, out var c) && c.Versao == v && c.Ate > agora) return c.Mapa;
        await using var db = fabrica.CriarLeitura();
        var m = await CalcularAsync(db, uid, ct);
        Cache[chave] = (v, agora + Validade, m);
        return m;
    }

    public static async Task<MapaAcesso> CalcularAsync(ISigeoDb db, Guid usuarioId, CancellationToken ct = default)
    {
        var u = await db.Usuarios.AsNoTracking().Where(x => x.Id == usuarioId)
            .Select(x => new { x.Perfil, x.PerfilAcessoId, x.ExcecoesAcesso }).FirstOrDefaultAsync(ct);
        if (u is null) return MapaAcesso.Vazio;
        var bloqueadas = PlataformaServico.Bloqueadas(await db.Empresas.AsNoTracking().Where(e => e.Id == db.EmpresaAtualId).Select(e => e.TelasBloqueadas).FirstOrDefaultAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var admin = u.Perfil == PerfilUsuario.Administrador;
        var regras = admin ? null : await RegrasDoUsuarioAsync(db, u.Perfil, u.PerfilAcessoId, ct);
        var exc = admin ? [] : CatalogoTelas.LerExcecoes(u.ExcecoesAcesso);
        var telas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var nao = new List<string>();
        foreach (var t in CatalogoTelas.Telas.Values)
        {
            if (bloqueadas.Contains(t.Rota)) { nao.Add(t.Rota); continue; }
            var a = admin ? t.Acoes : CatalogoTelas.Aplicar(regras!.GetValueOrDefault(t.Rota), exc.GetValueOrDefault(t.Rota), t.Acoes);
            if (a.Length > 0) telas[t.Rota] = a;
        }
        return new MapaAcesso(admin, telas, nao);
    }

    /// <summary>Regras do perfil de acesso do usuário: o escolhido; senão o padrão do perfil dele (gravado ou o do sistema).</summary>
    private static async Task<Dictionary<string, string>> RegrasDoUsuarioAsync(ISigeoDb db, PerfilUsuario perfil, Guid? perfilAcessoId, CancellationToken ct)
    {
        var gravado = perfilAcessoId is { } pid ? await db.PerfisAcesso.AsNoTracking().Where(p => p.Id == pid).Select(p => new { p.Regras, p.CatalogoVersao, p.Base }).FirstOrDefaultAsync(ct) : null;
        gravado ??= await db.PerfisAcesso.AsNoTracking().Where(p => p.Base == perfil).Select(p => new { p.Regras, p.CatalogoVersao, p.Base }).FirstOrDefaultAsync(ct);
        return gravado is null ? PerfisPadrao.Regras(perfil) : CatalogoTelas.Atualizar(CatalogoTelas.LerRegras(gravado.Regras), gravado.CatalogoVersao, gravado.Base ?? perfil);
    }

    public static string Recusa(SituacaoTela s, TelaSistema t, char acao) => s == SituacaoTela.NaoContratada
        ? $"Módulo não contratado: “{t.Nome}” não está liberado para esta construtora."
        : $"Seu perfil de acesso não permite {AcaoTela.Verbo(acao)} em “{t.Nome}”.";

    /// <summary>Confere a ação na tela para o usuário logado (rotina de sistema passa).</summary>
    public async Task ExigirAsync(string rota, char acao, CancellationToken ct = default)
    {
        if (usuario.UsuarioId is null) return;
        Conferir(await MapaAsync(ct), rota, acao);
    }

    public static void Conferir(MapaAcesso m, string rota, char acao)
    {
        var t = CatalogoTelas.Telas[rota];
        if (m.NaoContratadas.Contains(t.Rota, StringComparer.OrdinalIgnoreCase)) throw new RegraDeNegocioException(Recusa(SituacaoTela.NaoContratada, t, acao));
        if (!m.Pode(t.Rota, acao)) throw new RegraDeNegocioException(Recusa(SituacaoTela.SemPermissao, t, acao));
    }

    // ───────── Administração (Sistema › Perfis de Acesso) — só o administrador da construtora ─────────

    public sealed record LinhaPerfil(Guid Id, string Nome, PerfilUsuario? Base, Dictionary<string, string> Regras, int Usuarios);
    public sealed record LinhaUsuario(Guid Id, string Nome, string Login, PerfilUsuario Perfil, string? Funcao, bool Ativo, Guid? PerfilAcessoId, string? Excecoes);

    private async Task<bool> EhAdminAsync(ISigeoDb db, CancellationToken ct) =>
        usuario.UsuarioId is not { } id || await db.Usuarios.AsNoTracking().AnyAsync(u => u.Id == id && u.Perfil == PerfilUsuario.Administrador, ct);

    private async Task ExigirAdminAsync(ISigeoDb db) => await Permissoes.ExigirAsync(db, usuario, "alterar perfis de acesso");

    /// <summary>Perfis de acesso da construtora. Na primeira vez grava os padrões (um por perfil de usuário), iguais aos do sistema.</summary>
    public async Task<List<LinhaPerfil>> PerfisAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var existentes = await db.PerfisAcesso.Where(p => p.Base != null).Select(p => p.Base!.Value).ToListAsync(ct);
        var faltam = Enum.GetValues<PerfilUsuario>().Where(p => p != PerfilUsuario.Administrador && !existentes.Contains(p)).ToList();
        if (faltam.Count > 0 && await EhAdminAsync(db, ct))
        {
            var nomes = await db.PerfisAcesso.Select(p => p.Nome).ToListAsync(ct);
            foreach (var p in faltam)
            {
                var nome = Rotulo(p);
                if (nomes.Contains(nome, StringComparer.OrdinalIgnoreCase)) nome += " (padrão)";
                db.PerfisAcesso.Add(new PerfilAcesso { Nome = nome, Base = p, Regras = CatalogoTelas.EscreverRegras(PerfisPadrao.Regras(p)), CatalogoVersao = CatalogoTelas.Versao });
            }
            await db.SaveChangesAsync(ct);
        }
        var lista = await db.PerfisAcesso.AsNoTracking().OrderBy(p => p.Base == null).ThenBy(p => p.Nome).ToListAsync(ct);
        var usuarios = await db.Usuarios.AsNoTracking().Where(u => u.Ativo).Select(u => new { u.Perfil, u.PerfilAcessoId }).ToListAsync(ct);
        return lista.Select(p => new LinhaPerfil(p.Id, p.Nome, p.Base, CatalogoTelas.Atualizar(CatalogoTelas.LerRegras(p.Regras), p.CatalogoVersao, p.Base),
            usuarios.Count(u => u.Perfil != PerfilUsuario.Administrador && (u.PerfilAcessoId == p.Id || (u.PerfilAcessoId == null && p.Base == u.Perfil))))).ToList();
    }

    public static string Rotulo(PerfilUsuario p) => p switch
    {
        PerfilUsuario.Administrador => "Administrador", PerfilUsuario.Diretoria => "Diretoria", PerfilUsuario.Engenharia => "Engenharia",
        PerfilUsuario.Qualidade => "Qualidade", PerfilUsuario.Suprimentos => "Suprimentos", PerfilUsuario.Financeiro => "Financeiro",
        PerfilUsuario.Campo => "Campo", PerfilUsuario.Terceiro => "Terceiro", PerfilUsuario.Almoxarifado => "Almoxarifado",
        PerfilUsuario.Seguranca => "Segurança", _ => p.ToString()
    };

    /// <summary>Grava nome e matriz do perfil (só telas do catálogo; ações limitadas às que existem na tela).</summary>
    public async Task SalvarPerfilAsync(Guid id, string nome, IReadOnlyDictionary<string, string> regras, CancellationToken ct = default)
    {
        nome = (nome ?? "").Trim();
        if (nome.Length == 0) throw new ValidacaoException(["Informe o nome do perfil."]);
        await using var db = fabrica.Criar();
        await ExigirAdminAsync(db);
        var p = await db.PerfisAcesso.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new RegraDeNegocioException("Perfil não encontrado.");
        if (await db.PerfisAcesso.AnyAsync(x => x.Id != id && x.Nome == nome, ct)) throw new RegraDeNegocioException("Já existe perfil com esse nome.");
        p.Nome = nome;
        p.Regras = CatalogoTelas.EscreverRegras(Limpar(regras)); p.CatalogoVersao = CatalogoTelas.Versao;
        await db.SaveChangesAsync(ct);
        Invalidar();
    }

    private static Dictionary<string, string> Limpar(IReadOnlyDictionary<string, string> regras) =>
        regras.Where(r => CatalogoTelas.Telas.ContainsKey(r.Key))
              .ToDictionary(r => CatalogoTelas.Telas[r.Key].Rota, r => AcaoTela.Normalizar(r.Value, CatalogoTelas.Telas[r.Key].Acoes), StringComparer.OrdinalIgnoreCase);

    public async Task<Guid> DuplicarPerfilAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAdminAsync(db);
        var p = await db.PerfisAcesso.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new RegraDeNegocioException("Perfil não encontrado.");
        var nomes = (await db.PerfisAcesso.Select(x => x.Nome).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var nome = $"{p.Nome} (cópia)";
        for (var i = 2; nomes.Contains(nome); i++) nome = $"{p.Nome} (cópia {i})";
        var n = new PerfilAcesso { Nome = nome.Length > 80 ? nome[..80] : nome, Regras = p.Regras };
        db.PerfisAcesso.Add(n);
        await db.SaveChangesAsync(ct);
        return n.Id;
    }

    /// <summary>Volta o perfil padrão às regras do sistema.</summary>
    public async Task RestaurarPadraoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAdminAsync(db);
        var p = await db.PerfisAcesso.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new RegraDeNegocioException("Perfil não encontrado.");
        if (p.Base is not { } b) throw new RegraDeNegocioException("Só perfil padrão volta ao padrão do sistema.");
        p.Regras = CatalogoTelas.EscreverRegras(PerfisPadrao.Regras(b)); p.CatalogoVersao = CatalogoTelas.Versao;
        await db.SaveChangesAsync(ct);
        Invalidar();
    }

    public async Task ExcluirPerfilAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAdminAsync(db);
        var p = await db.PerfisAcesso.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new RegraDeNegocioException("Perfil não encontrado.");
        if (p.Base is not null) throw new RegraDeNegocioException("Perfil padrão não pode ser excluído (é o de quem não tem perfil escolhido). Ajuste as permissões dele.");
        if (await db.Usuarios.AnyAsync(u => u.PerfilAcessoId == id, ct)) throw new RegraDeNegocioException("Há usuários com este perfil. Troque o perfil deles antes de excluir.");
        db.PerfisAcesso.Remove(p);
        await db.SaveChangesAsync(ct);
        Invalidar();
    }

    public async Task<List<LinhaUsuario>> UsuariosAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Usuarios.AsNoTracking().OrderByDescending(u => u.Ativo).ThenBy(u => u.Nome)
            .Select(u => new LinhaUsuario(u.Id, u.Nome, u.Login, u.Perfil, u.Funcao, u.Ativo, u.PerfilAcessoId, u.ExcecoesAcesso)).ToListAsync(ct);
    }

    /// <summary>Perfil de acesso e exceções de um usuário nesta construtora (vale na hora, sem sair e entrar).</summary>
    public async Task SalvarAcessoUsuarioAsync(Guid usuarioId, Guid? perfilAcessoId, IReadOnlyDictionary<string, Dictionary<char, bool>> excecoes, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAdminAsync(db);
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == usuarioId, ct) ?? throw new RegraDeNegocioException("Usuário não encontrado.");
        if (perfilAcessoId is { } pid && !await db.PerfisAcesso.AnyAsync(p => p.Id == pid, ct)) throw new RegraDeNegocioException("Perfil de acesso não encontrado.");
        u.PerfilAcessoId = perfilAcessoId;
        var limpas = excecoes.Where(e => CatalogoTelas.Telas.ContainsKey(e.Key))
            .ToDictionary(e => CatalogoTelas.Telas[e.Key].Rota, e => e.Value.Where(a => CatalogoTelas.Telas[e.Key].Acoes.Contains(a.Key)).ToDictionary(a => a.Key, a => a.Value))
            .Where(e => e.Value.Count > 0).ToDictionary(e => e.Key, e => e.Value);
        var texto = CatalogoTelas.EscreverExcecoes(limpas);
        if (texto.Length > 2000) throw new RegraDeNegocioException("Exceções demais para um usuário: crie um perfil de acesso para ele.");
        u.ExcecoesAcesso = texto.Length > 0 ? texto : null;
        await db.SaveChangesAsync(ct);
        Invalidar();
    }
}
