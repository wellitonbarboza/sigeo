using Microsoft.EntityFrameworkCore;
using QualyHub.Application.Comum;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;

namespace QualyHub.Application.Servicos;

/// <summary>
/// Plataforma: administra as construtoras (tenants), os usuários entre construtoras (vínculos que usam o login da conta principal),
/// o acesso de suporte e a auditoria da plataforma. Só para contas marcadas como administrador da plataforma.
/// Lê e grava ignorando o filtro de empresa; tudo que muda fica em <see cref="RegistroPlataforma"/>.
/// </summary>
public sealed class PlataformaServico(IFabricaSistema sistema, IUsuarioAtual usuario, TimeProvider relogio)
{
    public const string FuncaoSuporte = "Suporte da plataforma";
    public static readonly TimeSpan DuracaoSuporte = TimeSpan.FromHours(4);

    private IQualyHubDb Db(Guid? empresa = null) => sistema.Criar(empresa ?? Guid.Empty, "Plataforma");

    /// <summary>Conta principal do usuário logado (o vínculo aponta para ela).</summary>
    private async Task<Usuario> ContaAsync(IQualyHubDb db, CancellationToken ct)
    {
        var id = usuario.UsuarioId ?? throw new RegraDeNegocioException("Entre no sistema.");
        var u = await db.Usuarios.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new RegraDeNegocioException("Usuário não encontrado.");
        return u.ContaId is { } c ? await db.Usuarios.IgnoreQueryFilters().AsNoTracking().FirstAsync(x => x.Id == c, ct) : u;
    }

    private async Task<Usuario> ExigirAsync(IQualyHubDb db, CancellationToken ct)
    {
        var c = await ContaAsync(db, ct);
        if (!c.AdministradorPlataforma) throw new RegraDeNegocioException("Só o administrador da plataforma acessa esta área.");
        return c;
    }

    public async Task<bool> EhAdministradorAsync(CancellationToken ct = default)
    {
        await using var db = Db();
        return usuario.UsuarioId is not null && (await ContaAsync(db, ct)).AdministradorPlataforma;
    }

    /// <summary>Marca a conta logada como administradora da plataforma (o código da plataforma é conferido antes, no endpoint).</summary>
    public async Task AtivarAdministradorAsync(CancellationToken ct = default)
    {
        await using var db = Db();
        var c = await ContaAsync(db, ct);
        var u = await db.Usuarios.IgnoreQueryFilters().FirstAsync(x => x.Id == c.Id, ct);
        u.AdministradorPlataforma = true;
        Registrar(db, c.Nome, "Ativou administrador da plataforma", null, null, c.Login);
        await db.SaveChangesAsync(ct);
    }

    private void Registrar(IQualyHubDb db, string quem, string acao, Guid? empresaId, string? empresa, string? detalhe) =>
        db.RegistrosPlataforma.Add(new RegistroPlataforma { Quando = relogio.GetUtcNow(), Quem = quem, Acao = acao, EmpresaId = empresaId, Empresa = empresa, Detalhe = detalhe });

    // ───────── Construtoras ─────────

    public sealed record LinhaConstrutora(Guid Id, string? Codigo, string RazaoSocial, string NomeFantasia, string Cnpj, string NivelSiac, bool Ativa, string Modelo,
        int Obras, int Usuarios, string? Administrador, DateTimeOffset? UltimoAcesso, DateTimeOffset? CriadaEm);

    public async Task<List<LinhaConstrutora>> ConstrutorasAsync(CancellationToken ct = default)
    {
        await using var db = Db();
        await ExigirAsync(db, ct);
        var emps = await db.Empresas.AsNoTracking().OrderBy(e => e.Codigo).ThenBy(e => e.NomeFantasia).ToListAsync(ct);
        var obras = await db.Obras.IgnoreQueryFilters().Where(o => !o.Excluido).GroupBy(o => o.EmpresaId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);
        var usuarios = await db.Usuarios.IgnoreQueryFilters().AsNoTracking().Where(u => !u.Excluido && u.Ativo).Select(u => new { u.Id, u.EmpresaId, u.Nome, u.Perfil, u.ContaId, u.SuporteAte }).ToListAsync(ct);
        var acessos = await db.Credenciais.IgnoreQueryFilters().Where(c => !c.Excluido && c.UltimoAcessoEm != null).Select(c => new { c.UsuarioId, c.UltimoAcessoEm }).ToListAsync(ct);
        var ultimo = acessos.ToDictionary(a => a.UsuarioId, a => a.UltimoAcessoEm);
        return emps.Select(e =>
        {
            var us = usuarios.Where(u => u.EmpresaId == e.Id && u.SuporteAte == null).ToList();
            var adm = us.Where(u => u.Perfil == PerfilUsuario.Administrador).OrderBy(u => u.ContaId != null).Select(u => u.Nome).FirstOrDefault();
            var ult = us.Select(u => ultimo.GetValueOrDefault(u.ContaId ?? u.Id)).Where(d => d != null).DefaultIfEmpty(null).Max();
            var est = EstruturaLocais.De(e.ModeloEstrutura, e.NomesNiveis);
            var modelo = EstruturaLocais.Modelos.FirstOrDefault(m => m.Modelo.Modelo == est.Modelo).Titulo ?? "Personalizado";
            return new LinhaConstrutora(e.Id, e.Codigo, e.RazaoSocial, e.NomeFantasia, e.Cnpj, e.NivelSiac, e.Ativa, modelo, obras.GetValueOrDefault(e.Id), us.Count, adm, ult, e.CriadaEm);
        }).ToList();
    }

    public async Task<Empresa> ConstrutoraAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = Db();
        await ExigirAsync(db, ct);
        return await db.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct) ?? throw new RegraDeNegocioException("Construtora não encontrada.");
    }

    private static List<string> ValidarEmpresa(Empresa e, EstruturaLocais est)
    {
        var r = new List<string>();
        if (string.IsNullOrWhiteSpace(e.RazaoSocial)) r.Add("Informe a razão social.");
        if (e.NivelSiac is not ("A" or "B")) r.Add("Nível SiAC: A ou B.");
        r.AddRange(est.Validar());
        return r;
    }

    private static async Task ConferirCnpjAsync(IQualyHubDb db, Empresa e, CancellationToken ct)
    {
        var doc = new string((e.Cnpj ?? "").Where(char.IsDigit).ToArray());
        if (doc.Length == 0) return;
        var outros = await db.Empresas.AsNoTracking().Where(x => x.Id != e.Id).Select(x => x.Cnpj).ToListAsync(ct);
        if (outros.Any(c => new string((c ?? "").Where(char.IsDigit).ToArray()) == doc)) throw new RegraDeNegocioException("Já existe construtora com este CNPJ.");
    }

    /// <summary>Altera os dados da construtora, suspende/reativa e muda o modelo e os nomes dos níveis de local.</summary>
    public async Task SalvarConstrutoraAsync(Empresa e, EstruturaLocais estrutura, CancellationToken ct = default)
    {
        ValidacaoException.Se(ValidarEmpresa(e, estrutura));
        await using var db = Db();
        var quem = await ExigirAsync(db, ct);
        var atual = await db.Empresas.FirstOrDefaultAsync(x => x.Id == e.Id, ct) ?? throw new RegraDeNegocioException("Construtora não encontrada.");
        await ConferirCnpjAsync(db, e, ct);
        if (!e.Ativa && usuario.EmpresaId == e.Id) throw new RegraDeNegocioException("Você está dentro desta construtora: troque para outra antes de suspendê-la.");
        var mudou = new List<string>();
        if (atual.Ativa != e.Ativa) mudou.Add(e.Ativa ? "reativada" : "suspensa");
        if (atual.ModeloEstrutura != estrutura.Modelo || atual.NomesNiveis != estrutura.Serializar()) mudou.Add($"estrutura: {string.Join(" › ", estrutura.Niveis)}");
        atual.Codigo = e.Codigo?.Trim(); atual.RazaoSocial = e.RazaoSocial.Trim(); atual.NomeFantasia = string.IsNullOrWhiteSpace(e.NomeFantasia) ? e.RazaoSocial.Trim() : e.NomeFantasia.Trim();
        atual.Cnpj = (e.Cnpj ?? "").Trim(); atual.NivelSiac = e.NivelSiac; atual.Ativa = e.Ativa; atual.EnderecoEntregaPadrao = e.EnderecoEntregaPadrao;
        atual.ModeloEstrutura = estrutura.Modelo; atual.NomesNiveis = estrutura.Serializar();
        var bloqAntes = Bloqueadas(atual.TelasBloqueadas);
        var bloq = Bloqueadas(e.TelasBloqueadas);
        var bloqueou = bloq.Except(bloqAntes).ToList(); var liberou = bloqAntes.Except(bloq).ToList();
        if (bloqueou.Count > 0) mudou.Add("bloqueou: " + string.Join(", ", bloqueou.Select(NomeTela)));
        if (liberou.Count > 0) mudou.Add("liberou: " + string.Join(", ", liberou.Select(NomeTela)));
        atual.TelasBloqueadas = bloq.Count > 0 ? string.Join(';', bloq.Order(StringComparer.Ordinal)) : null;
        Registrar(db, quem.Nome, e.Ativa ? "Alterou construtora" : "Suspendeu construtora", atual.Id, atual.NomeFantasia, mudou.Count > 0 ? string.Join("; ", mudou) : null);
        await db.SaveChangesAsync(ct);
        AcessoServico.Invalidar();
    }

    /// <summary>Telas bloqueadas válidas (do catálogo e de módulo que a plataforma pode desligar).</summary>
    public static List<string> Bloqueadas(string? texto) =>
        (texto ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(r => Contratos.CatalogoTelas.Telas.TryGetValue(r, out var t) && !Contratos.CatalogoTelas.Modulos.First(m => m.Id == t.Modulo).Fixo)
            .Select(r => Contratos.CatalogoTelas.Telas[r].Rota).Distinct().ToList();

    private static string NomeTela(string rota) => Contratos.CatalogoTelas.Telas.TryGetValue(rota, out var t) ? t.Nome : rota;

    public sealed record NovaConstrutora(Empresa Empresa, EstruturaLocais Estrutura, string AdminNome, string AdminEmail, string AdminLogin,
        string? ObraCodigo, string? ObraNome, string? ObraCidade, string? ObraUf, string? ObraEndereco);

    /// <summary>Resultado: senha provisória do administrador (mostrar uma única vez) ou nula quando o login já existia (vira vínculo).</summary>
    public sealed record ConstrutoraCriada(Guid EmpresaId, string? SenhaProvisoria, bool Vinculado);

    /// <summary>
    /// Cria a construtora com a carga padrão, o administrador e (opcional) a primeira obra. Login que já existe em outra construtora
    /// recebe um vínculo (mesma senha); login novo recebe senha provisória com troca obrigatória no primeiro acesso.
    /// </summary>
    public async Task<ConstrutoraCriada> CriarConstrutoraAsync(NovaConstrutora n, CancellationToken ct = default)
    {
        var e = n.Empresa;
        var erros = ValidarEmpresa(e, n.Estrutura);
        var login = PoliticaSenha.NormalizarLogin(n.AdminLogin);
        if (string.IsNullOrWhiteSpace(n.AdminNome)) erros.Add("Informe o nome do administrador.");
        if (string.IsNullOrWhiteSpace(n.AdminEmail) || !n.AdminEmail.Contains('@')) erros.Add("Informe o e-mail do administrador.");
        erros.AddRange(PoliticaSenha.ValidarLogin(login));
        var comObra = !string.IsNullOrWhiteSpace(n.ObraCodigo) || !string.IsNullOrWhiteSpace(n.ObraNome);
        if (comObra && (string.IsNullOrWhiteSpace(n.ObraCodigo) || string.IsNullOrWhiteSpace(n.ObraNome) || string.IsNullOrWhiteSpace(n.ObraEndereco)))
            erros.Add("Primeira obra: informe código, nome e endereço de entrega (ou deixe os três em branco).");
        ValidacaoException.Se(erros);
        string quem;
        await using (var db = Db())
        {
            quem = (await ExigirAsync(db, ct)).Nome;
            await ConferirCnpjAsync(db, e, ct);
        }
        e.RazaoSocial = e.RazaoSocial.Trim();
        e.NomeFantasia = string.IsNullOrWhiteSpace(e.NomeFantasia) ? e.RazaoSocial : e.NomeFantasia.Trim();
        e.Cnpj = (e.Cnpj ?? "").Trim(); e.Codigo = e.Codigo?.Trim(); e.Ativa = true; e.CriadaEm = relogio.GetUtcNow();
        e.ModeloEstrutura = n.Estrutura.Modelo; e.NomesNiveis = n.Estrutura.Serializar();
        // Módulos adicionais (Financeiro, Incorporação) nascem bloqueados: a plataforma libera quando a construtora contratar.
        e.TelasBloqueadas = string.Join(';', Bloqueadas(e.TelasBloqueadas).Union(Contratos.CatalogoTelas.RotasAdicionais).Order(StringComparer.Ordinal));
        await using var tdb = Db(e.Id);
        var existente = await tdb.Usuarios.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(u => u.Login == login && u.ContaId == null && !u.Excluido, ct);
        tdb.Empresas.Add(e);
        var admin = new Usuario
        {
            EmpresaId = e.Id, Nome = n.AdminNome.Trim(), Email = n.AdminEmail.Trim().ToLowerInvariant(), Perfil = PerfilUsuario.Administrador,
            Funcao = "Coordenador da Qualidade", AcessoTodasObras = true, Login = existente is null ? login : "", ContaId = existente?.Id
        };
        tdb.Usuarios.Add(admin);
        string? senha = null;
        if (existente is null)
        {
            senha = Senhas.GerarProvisoria();
            tdb.Credenciais.Add(new CredencialUsuario { UsuarioId = admin.Id, EmpresaId = e.Id, SenhaHash = Senhas.Gerar(senha), TrocarSenhaNoProximoAcesso = true, SenhaAlteradaEm = relogio.GetUtcNow() });
        }
        if (comObra)
            tdb.Obras.Add(new Obra { EmpresaId = e.Id, Codigo = n.ObraCodigo!.Trim(), Nome = n.ObraNome!.Trim(), Cidade = n.ObraCidade?.Trim(), Uf = n.ObraUf?.Trim().ToUpperInvariant(), EnderecoEntrega = n.ObraEndereco!.Trim(), Status = StatusObra.EmExecucao });
        Registrar(tdb, quem, "Criou construtora", e.Id, e.NomeFantasia,
            $"Estrutura {string.Join(" › ", n.Estrutura.Niveis)} · administrador {admin.Nome} ({(existente is null ? login : "vínculo da conta " + existente.Login)})" + (comObra ? $" · obra {n.ObraCodigo}" : ""));
        await tdb.SaveChangesAsync(ct);
        await Carga.CargaPadrao.AplicarSeNovaVersaoAsync(tdb, e.Id);
        return new ConstrutoraCriada(e.Id, senha, existente is not null);
    }

    // ───────── Usuários e acessos entre construtoras ─────────

    public sealed record AcessoConta(Guid UsuarioId, Guid EmpresaId, string Empresa, PerfilUsuario Perfil, bool TodasObras, bool Ativo, bool Principal, DateTimeOffset? SuporteAte);
    public sealed record LinhaConta(Guid Id, string Nome, string Login, string Email, bool Ativo, bool AdministradorPlataforma, DateTimeOffset? UltimoAcesso, bool Bloqueado,
        IReadOnlyList<AcessoConta> Acessos);

    public async Task<List<LinhaConta>> ContasAsync(CancellationToken ct = default)
    {
        await using var db = Db();
        await ExigirAsync(db, ct);
        var agora = relogio.GetUtcNow();
        var us = await db.Usuarios.IgnoreQueryFilters().AsNoTracking().Where(u => !u.Excluido).ToListAsync(ct);
        var emp = await db.Empresas.AsNoTracking().ToDictionaryAsync(e => e.Id, e => e.NomeFantasia, ct);
        var cred = await db.Credenciais.IgnoreQueryFilters().AsNoTracking().Where(c => !c.Excluido).ToDictionaryAsync(c => c.UsuarioId, ct);
        var vinc = us.Where(u => u.ContaId != null).ToLookup(u => u.ContaId!.Value);
        AcessoConta A(Usuario u) => new(u.Id, u.EmpresaId, emp.GetValueOrDefault(u.EmpresaId, "?"), u.Perfil, u.AcessoTodasObras, u.Ativo, u.ContaId == null, u.SuporteAte);
        return us.Where(u => u.ContaId == null && u.Login != "").OrderBy(u => u.Nome).Select(u =>
        {
            var c = cred.GetValueOrDefault(u.Id);
            return new LinhaConta(u.Id, u.Nome, u.Login, u.Email, u.Ativo, u.AdministradorPlataforma, c?.UltimoAcessoEm, c?.BloqueadoAte > agora,
                new[] { A(u) }.Concat(vinc[u.Id].Where(v => v.SuporteAte == null || v.SuporteAte > agora).OrderBy(v => emp.GetValueOrDefault(v.EmpresaId)).Select(A)).ToList());
        }).ToList();
    }

    /// <summary>Dá (ou altera) o acesso da conta a uma construtora: perfil e todas as obras; o administrador dela ajusta obras e alçadas depois.</summary>
    public async Task SalvarAcessoAsync(Guid contaId, Guid empresaId, PerfilUsuario perfil, bool ativo, CancellationToken ct = default)
    {
        await using var db = Db(empresaId);
        var quem = await ExigirAsync(db, ct);
        var conta = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == contaId && u.ContaId == null && !u.Excluido, ct) ?? throw new RegraDeNegocioException("Conta não encontrada.");
        var empresa = await db.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == empresaId, ct) ?? throw new RegraDeNegocioException("Construtora não encontrada.");
        if (conta.EmpresaId == empresaId)
        {
            if (conta.Id == quem.Id && (!ativo || perfil != PerfilUsuario.Administrador))
                throw new RegraDeNegocioException("Você não pode retirar o próprio acesso de administrador na construtora principal.");
            conta.Perfil = perfil; conta.Ativo = ativo;
        }
        else
        {
            var v = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.ContaId == contaId && u.EmpresaId == empresaId && !u.Excluido && u.SuporteAte == null, ct);
            if (v is null)
            {
                v = new Usuario { EmpresaId = empresaId, ContaId = contaId, Login = "", Nome = conta.Nome, Email = conta.Email, AcessoTodasObras = true, Funcao = conta.Funcao };
                db.Usuarios.Add(v);
            }
            v.Perfil = perfil; v.Ativo = ativo;
        }
        Registrar(db, quem.Nome, ativo ? "Deu acesso" : "Retirou acesso", empresaId, empresa.NomeFantasia, $"{conta.Nome} ({conta.Login}) como {perfil}");
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Bloqueia (ou libera) a conta: sem a conta principal ativa, nenhum vínculo entra.</summary>
    public async Task BloquearContaAsync(Guid contaId, bool bloquear, CancellationToken ct = default)
    {
        await using var db = Db();
        var quem = await ExigirAsync(db, ct);
        if (contaId == quem.Id && bloquear) throw new RegraDeNegocioException("Você não pode bloquear a própria conta.");
        var u = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == contaId && x.ContaId == null, ct) ?? throw new RegraDeNegocioException("Conta não encontrada.");
        u.Ativo = !bloquear;
        var c = await db.Credenciais.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.UsuarioId == contaId && !x.Excluido, ct);
        if (c is not null && !bloquear) { c.BloqueadoAte = null; c.TentativasFalhas = 0; }
        Registrar(db, quem.Nome, bloquear ? "Bloqueou conta" : "Liberou conta", u.EmpresaId, null, $"{u.Nome} ({u.Login})");
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Senha provisória da conta (troca obrigatória no próximo acesso). Retorna a senha — mostrar uma única vez.</summary>
    public async Task<string> SenhaProvisoriaAsync(Guid contaId, CancellationToken ct = default)
    {
        await using var db = Db();
        var quem = await ExigirAsync(db, ct);
        var u = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == contaId && x.ContaId == null && !x.Excluido, ct) ?? throw new RegraDeNegocioException("Conta não encontrada.");
        var senha = Senhas.GerarProvisoria();
        var c = await db.Credenciais.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.UsuarioId == contaId && !x.Excluido, ct);
        if (c is null) db.Credenciais.Add(c = new CredencialUsuario { UsuarioId = contaId, EmpresaId = u.EmpresaId });
        c.SenhaHash = Senhas.Gerar(senha); c.TrocarSenhaNoProximoAcesso = true; c.TentativasFalhas = 0; c.BloqueadoAte = null; c.SenhaAlteradaEm = relogio.GetUtcNow();
        Registrar(db, quem.Nome, "Gerou senha provisória", u.EmpresaId, null, $"{u.Nome} ({u.Login})");
        await db.SaveChangesAsync(ct);
        return senha;
    }

    /// <summary>
    /// Acesso de suporte: vínculo temporário (4 h) do administrador da plataforma na construtora, como Administrador, com o motivo
    /// registrado. Retorna o cadastro a usar na troca de construtora.
    /// </summary>
    public async Task<Guid> SuporteAsync(Guid empresaId, string motivo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 5) throw new ValidacaoException(["Informe o motivo do acesso de suporte (fica na auditoria)."]);
        await using var db = Db(empresaId);
        var quem = await ExigirAsync(db, ct);
        var empresa = await db.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == empresaId, ct) ?? throw new RegraDeNegocioException("Construtora não encontrada.");
        if (!empresa.Ativa) throw new RegraDeNegocioException("Construtora suspensa: reative antes de acessar.");
        var ate = relogio.GetUtcNow() + DuracaoSuporte;
        if (quem.EmpresaId == empresaId) throw new RegraDeNegocioException("Esta é a construtora do seu cadastro principal: use \"Trocar construtora\".");
        var v = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.ContaId == quem.Id && u.EmpresaId == empresaId && !u.Excluido && u.SuporteAte != null, ct);
        if (v is null)
        {
            v = new Usuario { EmpresaId = empresaId, ContaId = quem.Id, Login = "", Nome = quem.Nome, Email = quem.Email, Perfil = PerfilUsuario.Administrador, AcessoTodasObras = true, Funcao = FuncaoSuporte };
            db.Usuarios.Add(v);
        }
        v.Ativo = true; v.SuporteAte = ate;
        Registrar(db, quem.Nome, "Entrou como suporte", empresaId, empresa.NomeFantasia, $"Motivo: {motivo.Trim()} · até {ate.ToLocalTime():dd/MM HH:mm}");
        await db.SaveChangesAsync(ct);
        return v.Id;
    }

    public async Task<List<RegistroPlataforma>> RegistrosAsync(int max = 500, CancellationToken ct = default)
    {
        await using var db = Db();
        await ExigirAsync(db, ct);
        return await db.RegistrosPlataforma.AsNoTracking().OrderByDescending(r => r.Quando).Take(max).ToListAsync(ct);
    }
}
