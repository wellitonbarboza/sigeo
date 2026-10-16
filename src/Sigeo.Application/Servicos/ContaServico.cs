using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Servicos;

/// <summary>Hash de senha PBKDF2-HMAC-SHA512 (210.000 iterações, sal de 16 bytes). Formato: pbkdf2-sha512$iter$sal$hash.</summary>
public static class Senhas
{
    private const int Iteracoes = 210_000;
    private const string Prefixo = "pbkdf2-sha512";
    // Usado para gastar o mesmo tempo quando o login não existe (não revela quais logins são válidos).
    private static readonly string HashFicticio = Gerar("senha-ficticia-para-tempo-constante-1");

    public static string Gerar(string senha)
    {
        var sal = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, sal, Iteracoes, HashAlgorithmName.SHA512, 32);
        return $"{Prefixo}${Iteracoes}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string senha, string? armazenado)
    {
        var p = (armazenado ?? HashFicticio).Split('$');
        if (p.Length != 4 || p[0] != Prefixo || !int.TryParse(p[1], out var it)) { Verificar(senha, HashFicticio); return false; }
        var esperado = Convert.FromBase64String(p[3]);
        var calculado = Rfc2898DeriveBytes.Pbkdf2(senha, Convert.FromBase64String(p[2]), it, HashAlgorithmName.SHA512, esperado.Length);
        return armazenado is not null && CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }

    /// <summary>Senha provisória legível (sem 0/O, 1/l/I), 12 caracteres com letras e números.</summary>
    public static string GerarProvisoria()
    {
        const string letras = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ", numeros = "23456789";
        const string todos = letras + numeros;
        while (true)
        {
            var s = new string(Enumerable.Range(0, 12).Select(_ => todos[RandomNumberGenerator.GetInt32(todos.Length)]).ToArray());
            if (s.Any(char.IsLetter) && s.Count(char.IsDigit) >= 2) return s;
        }
    }
}

public sealed record ResultadoLogin(Usuario? Usuario, string? EmpresaNome, bool TrocarSenha, string? Erro);

/// <summary>Login por usuário e senha, bloqueio por tentativas, troca de senha e senha provisória (administrador).</summary>
public sealed class ContaServico(IFabricaSistema sistema, IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    public const string ErroGenerico = "Login ou senha inválidos.";

    public async Task<ResultadoLogin> EntrarAsync(string? login, string? senha, CancellationToken ct = default)
    {
        var l = PoliticaSenha.NormalizarLogin(login);
        await using var db = sistema.Criar(Guid.Empty, "Login");
        var u = l.Length == 0 ? null : await db.Usuarios.IgnoreQueryFilters().Include(x => x.Obras)
            .FirstOrDefaultAsync(x => (x.Login == l || x.Email == l) && x.ContaId == null && x.Ativo && !x.Excluido, ct);
        var c = u is null ? null : await db.Credenciais.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.UsuarioId == u.Id && !x.Excluido, ct);
        var agora = relogio.GetUtcNow();
        if (u is null || c is null) { Senhas.Verificar(senha ?? "", null); return new(null, null, false, ErroGenerico); }
        if (c.BloqueadoAte > agora) { Senhas.Verificar(senha ?? "", null); return new(null, null, false, "Acesso bloqueado por excesso de tentativas. Tente novamente mais tarde ou peça ao administrador para desbloquear."); }

        if (!Senhas.Verificar(senha ?? "", c.SenhaHash))
        {
            c.TentativasFalhas++;
            if (c.TentativasFalhas >= PoliticaSenha.MaxTentativas) { c.BloqueadoAte = agora + PoliticaSenha.TempoBloqueio; c.TentativasFalhas = 0; }
            await db.SaveChangesAsync(ct);
            return new(null, null, false, ErroGenerico);
        }
        c.TentativasFalhas = 0; c.BloqueadoAte = null; c.UltimoAcessoEm = agora;
        await db.SaveChangesAsync(ct);
        var emp = await db.Empresas.IgnoreQueryFilters().Where(e => e.Id == u.EmpresaId).Select(e => e.NomeFantasia).FirstAsync(ct);
        return new(u, emp, c.TrocarSenhaNoProximoAcesso, null);
    }

    public sealed record Acesso(Guid UsuarioId, Guid EmpresaId, string Empresa, Domain.Common.PerfilUsuario Perfil, bool Principal, DateTimeOffset? SuporteAte);

    /// <summary>
    /// Construtoras que a conta acessa: o cadastro principal e os vínculos em outras construtoras (ativos, construtora ativa,
    /// suporte dentro do prazo). A principal vem primeiro.
    /// </summary>
    public async Task<List<Acesso>> AcessosAsync(Guid contaId, CancellationToken ct = default)
    {
        await using var db = sistema.Criar(Guid.Empty, "Login");
        var agora = relogio.GetUtcNow();
        var us = await db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .Where(u => (u.Id == contaId || u.ContaId == contaId) && u.Ativo && !u.Excluido && (u.SuporteAte == null || u.SuporteAte > agora))
            .Select(u => new { u.Id, u.EmpresaId, u.Perfil, u.ContaId, u.SuporteAte }).ToListAsync(ct);
        var emp = us.Select(u => u.EmpresaId).Distinct().ToList();
        var nomes = await db.Empresas.AsNoTracking().Where(e => emp.Contains(e.Id) && e.Ativa).ToDictionaryAsync(e => e.Id, e => e.NomeFantasia, ct);
        return us.Where(u => nomes.ContainsKey(u.EmpresaId))
            .Select(u => new Acesso(u.Id, u.EmpresaId, nomes[u.EmpresaId], u.Perfil, u.ContaId == null, u.SuporteAte))
            .OrderByDescending(a => a.Principal).ThenBy(a => a.Empresa).ToList();
    }

    /// <summary>Cadastro (com obras) do acesso escolhido — só se pertencer à conta e estiver válido.</summary>
    public async Task<(Usuario Usuario, string Empresa, bool AdministradorPlataforma, int Acessos)?> AcessoAsync(Guid contaId, Guid usuarioId, CancellationToken ct = default)
    {
        var acessos = await AcessosAsync(contaId, ct);
        var a = acessos.FirstOrDefault(x => x.UsuarioId == usuarioId);
        if (a is null) return null;
        await using var db = sistema.Criar(Guid.Empty, "Login");
        var u = await db.Usuarios.IgnoreQueryFilters().AsNoTracking().Include(x => x.Obras).FirstAsync(x => x.Id == usuarioId, ct);
        var plataforma = await db.Usuarios.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == contaId).Select(x => x.AdministradorPlataforma).FirstOrDefaultAsync(ct);
        return (u, a.Empresa, plataforma, acessos.Count);
    }

    /// <summary>O próprio usuário troca a senha (exige a atual).</summary>
    public async Task AlterarSenhaAsync(Guid usuarioId, string? atual, string? nova, string? confirmacao, CancellationToken ct = default)
    {
        await using var db = sistema.Criar(Guid.Empty, "Conta");
        var u = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == usuarioId && x.Ativo && !x.Excluido, ct) ?? throw new RegraDeNegocioException("Usuário inativo.");
        var c = await db.Credenciais.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && !x.Excluido, ct) ?? throw new RegraDeNegocioException("Usuário sem senha definida.");
        var e = new List<string>();
        if (!Senhas.Verificar(atual ?? "", c.SenhaHash)) e.Add("Senha atual incorreta.");
        if (nova != confirmacao) e.Add("A confirmação não confere com a nova senha.");
        if (nova == atual) e.Add("A nova senha precisa ser diferente da atual.");
        e.AddRange(PoliticaSenha.Validar(nova, u.Login, u.Nome));
        ValidacaoException.Se(e);
        c.SenhaHash = Senhas.Gerar(nova!);
        c.TrocarSenhaNoProximoAcesso = false; c.SenhaAlteradaEm = relogio.GetUtcNow(); c.TentativasFalhas = 0; c.BloqueadoAte = null;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Administrador gera uma senha provisória (troca obrigatória no próximo acesso). Retorna a senha — mostrar uma única vez.</summary>
    public async Task<string> DefinirSenhaProvisoriaAsync(Guid usuarioId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAdministradorAsync(db, ct);
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == usuarioId, ct) ?? throw new RegraDeNegocioException("Usuário não encontrado.");
        if (string.IsNullOrWhiteSpace(u.Login)) throw new RegraDeNegocioException("Defina e salve o login do usuário antes de gerar a senha.");
        var senha = Senhas.GerarProvisoria();
        var c = await db.Credenciais.FirstOrDefaultAsync(x => x.UsuarioId == usuarioId, ct);
        if (c is null) db.Credenciais.Add(c = new CredencialUsuario { UsuarioId = usuarioId, EmpresaId = u.EmpresaId });
        c.SenhaHash = Senhas.Gerar(senha);
        c.TrocarSenhaNoProximoAcesso = true; c.TentativasFalhas = 0; c.BloqueadoAte = null; c.SenhaAlteradaEm = relogio.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return senha;
    }

    public async Task DesbloquearAsync(Guid usuarioId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAdministradorAsync(db, ct);
        var c = await db.Credenciais.FirstOrDefaultAsync(x => x.UsuarioId == usuarioId, ct) ?? throw new RegraDeNegocioException("Usuário sem senha definida.");
        c.TentativasFalhas = 0; c.BloqueadoAte = null;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Situação de acesso por usuário (para a tela de usuários): sem senha, troca pendente, bloqueado, último acesso.</summary>
    public async Task<Dictionary<Guid, CredencialUsuario>> SituacaoAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        return (await db.Credenciais.AsNoTracking().ToListAsync(ct)).ToDictionary(c => c.UsuarioId);
    }

    /// <summary>Senha definida pelo próprio administrador na configuração inicial ou no primeiro acesso protegido por código.</summary>
    public static void DefinirSenha(ISigeoDb db, Usuario u, string senha, DateTimeOffset agora) =>
        db.Credenciais.Add(new CredencialUsuario
        {
            UsuarioId = u.Id, EmpresaId = u.EmpresaId, SenhaHash = Senhas.Gerar(senha), TrocarSenhaNoProximoAcesso = false, SenhaAlteradaEm = agora
        });

    /// <summary>
    /// Primeiro acesso do administrador em um banco que já existia antes do login por senha:
    /// exige o código de configuração (Key Vault) e só vale para administrador ainda sem senha.
    /// </summary>
    public async Task DefinirSenhaAdministradorAsync(string? loginOuEmail, string? senha, string? confirmacao, CancellationToken ct = default)
    {
        var l = PoliticaSenha.NormalizarLogin(loginOuEmail);
        await using var db = sistema.Criar(Guid.Empty, "PrimeiroAcesso");
        var u = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => (x.Login == l || x.Email == l) && x.Ativo && !x.Excluido && x.Perfil == PerfilUsuario.Administrador, ct);
        if (u is null || await db.Credenciais.IgnoreQueryFilters().AnyAsync(x => x.UsuarioId == u.Id && !x.Excluido, ct))
            throw new RegraDeNegocioException("Não há administrador sem senha com esse login/e-mail.");
        var e = new List<string>();
        if (senha != confirmacao) e.Add("A confirmação não confere com a senha.");
        e.AddRange(PoliticaSenha.Validar(senha, u.Login, u.Nome));
        ValidacaoException.Se(e);
        DefinirSenha(db, u, senha!, relogio.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    private async Task ExigirAdministradorAsync(ISigeoDb db, CancellationToken ct)
    {
        var perfil = usuario.UsuarioId is { } id ? await db.Usuarios.Where(x => x.Id == id).Select(x => (PerfilUsuario?)x.Perfil).FirstOrDefaultAsync(ct) : null;
        if (perfil != PerfilUsuario.Administrador) throw new RegraDeNegocioException("Somente o administrador gerencia senhas.");
    }
}
