using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Common;
using Groundkey.Domain.Estrutura;

namespace Groundkey.Tests.Integracao;

/// <summary>
/// Plataforma no banco real: só o administrador da plataforma entra; nova construtora (carga padrão, administrador com senha provisória,
/// estrutura de locais); acesso da mesma conta a outra construtora (login único); suspensão; suporte temporário; auditoria.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public sealed class PlataformaTests(BancoFixture f)
{
    private sealed class Logado(Guid empresa, Guid id, string nome) : IUsuarioAtual
    {
        public Guid EmpresaId => empresa;
        public Guid? UsuarioId => id;
        public string Nome => nome;
        public bool AcessoTodasObras => true;
        public IReadOnlyCollection<Guid> ObrasPermitidas { get; } = [];
        public string Origem => "Teste";
        public string? CorrelationId => null;
    }

    private async Task<Usuario> ContaAsync(string nome, bool plataforma, string senha = "Senha#Ficticia1")
    {
        var u = new Usuario { Login = $"{nome.Replace(" ", "").ToLowerInvariant()}.{Guid.NewGuid():N}"[..28], Nome = nome, Email = $"{Guid.NewGuid():N}@exemplo.com",
            Perfil = PerfilUsuario.Administrador, AcessoTodasObras = true, AdministradorPlataforma = plataforma };
        await using var db = f.Fabrica.Criar();
        db.Usuarios.Add(u);
        ContaServico.DefinirSenha(db, u, senha, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
        return u;
    }

    private PlataformaServico Plat(Usuario u) => new(f.Sistema, new Logado(u.EmpresaId, u.Id, u.Nome), f.Relogio);
    private ContaServico Conta => new(f.Sistema, f.Fabrica, f.Usuario, f.Relogio);

    [FatoComBanco]
    public async Task Plataforma_cria_construtora_da_acesso_entre_construtoras_suspende_e_registra()
    {
        var adm = await ContaAsync("Admin Plataforma Ficticio", plataforma: true);
        var comum = await ContaAsync("Usuario Comum Ficticio", plataforma: false);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Plat(comum).ConstrutorasAsync());
        Assert.False(await Plat(comum).EhAdministradorAsync());

        // Nova construtora com estrutura de loteamento e primeira obra.
        var est = EstruturaLocais.Loteamento;
        var nova = new Empresa { RazaoSocial = "Incorporadora Ficticia Nova Ltda", Codigo = "NV" + Random.Shared.Next(999), Cnpj = Dados.Cnpj(), NivelSiac = "B" };
        var loginAdm = $"admnova.{Guid.NewGuid():N}"[..24];
        var r = await Plat(adm).CriarConstrutoraAsync(new(nova, est, "Admin Nova Ficticio", "adm.nova@exemplo.com", loginAdm, "OB01", "Residencial Ficticio", "Cascavel", "pr", "Rua Ficticia, 100"));
        Assert.False(r.Vinculado);
        Assert.NotNull(r.SenhaProvisoria);
        var login = await Conta.EntrarAsync(loginAdm, r.SenhaProvisoria);
        Assert.NotNull(login.Usuario);
        Assert.True(login.TrocarSenha);
        await using (var db = f.Sistema.Criar(r.EmpresaId, "Teste"))
        {
            var e = await db.Empresas.SingleAsync(x => x.Id == r.EmpresaId);
            Assert.Equal(("Loteamento", est.Serializar()), (e.ModeloEstrutura, e.NomesNiveis));
            Assert.Single(await db.Obras.IgnoreQueryFilters().Where(o => o.EmpresaId == r.EmpresaId).ToListAsync());
            Assert.True(await db.Listas.IgnoreQueryFilters().AnyAsync(l => l.EmpresaId == r.EmpresaId));          // carga padrão
        }
        var lista = await Plat(adm).ConstrutorasAsync();
        Assert.Contains(lista, c => c.Id == r.EmpresaId && c.Modelo.StartsWith("Loteamento") && c.Obras == 1 && c.Usuarios == 1);

        // Mesma conta (comum) passa a acessar a nova construtora: login único, dois acessos.
        await Plat(adm).SalvarAcessoAsync(comum.Id, r.EmpresaId, PerfilUsuario.Engenharia, true);
        var acessos = await Conta.AcessosAsync(comum.Id);
        Assert.Equal(2, acessos.Count);
        Assert.True(acessos[0].Principal);
        var vinc = acessos.Single(a => !a.Principal);
        Assert.Equal((r.EmpresaId, PerfilUsuario.Engenharia), (vinc.EmpresaId, vinc.Perfil));
        var ac = (await Conta.AcessoAsync(comum.Id, vinc.UsuarioId))!.Value;
        Assert.Equal((r.EmpresaId, "", comum.Id), (ac.Usuario.EmpresaId, ac.Usuario.Login, ac.Usuario.ContaId!.Value));
        Assert.Null(await Conta.AcessoAsync(adm.Id, vinc.UsuarioId));                                               // acesso de outra conta

        // Novo construtora com login que já existe vira vínculo (mesma senha).
        var outra = new Empresa { RazaoSocial = "Construtora Ficticia Dois Ltda", Codigo = "D" + Random.Shared.Next(999), NivelSiac = "A" };
        var r2 = await Plat(adm).CriarConstrutoraAsync(new(outra, EstruturaLocais.Edificio, comum.Nome, comum.Email, comum.Login, null, null, null, null, null));
        Assert.True(r2.Vinculado);
        Assert.Null(r2.SenhaProvisoria);
        Assert.Equal(3, (await Conta.AcessosAsync(comum.Id)).Count);

        // Suspensa: some dos acessos.
        var e2 = await Plat(adm).ConstrutoraAsync(r2.EmpresaId);
        e2.Ativa = false;
        await Plat(adm).SalvarConstrutoraAsync(e2, EstruturaLocais.Edificio);
        Assert.Equal(2, (await Conta.AcessosAsync(comum.Id)).Count);

        // Suporte: vínculo temporário de administrador, com motivo.
        await Assert.ThrowsAsync<ValidacaoException>(() => Plat(adm).SuporteAsync(r.EmpresaId, ""));
        var sup = await Plat(adm).SuporteAsync(r.EmpresaId, "conferir estrutura a pedido do cliente");
        var acSup = (await Conta.AcessosAsync(adm.Id)).Single(a => a.UsuarioId == sup);
        Assert.Equal(PerfilUsuario.Administrador, acSup.Perfil);
        Assert.NotNull(acSup.SuporteAte);

        // Retirar acesso e bloquear conta.
        await Plat(adm).SalvarAcessoAsync(comum.Id, r.EmpresaId, PerfilUsuario.Engenharia, false);
        Assert.Single(await Conta.AcessosAsync(comum.Id));
        await Plat(adm).BloquearContaAsync(comum.Id, true);
        Assert.Null((await Conta.EntrarAsync(comum.Login, "Senha#Ficticia1")).Usuario);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Plat(adm).BloquearContaAsync(adm.Id, true));

        var reg = await Plat(adm).RegistrosAsync();
        foreach (var acao in new[] { "Criou construtora", "Deu acesso", "Suspendeu construtora", "Entrou como suporte", "Retirou acesso", "Bloqueou conta" })
            Assert.Contains(reg, x => x.Acao == acao);
    }

    [FatoComBanco]
    public async Task Estrutura_da_construtora_da_nome_as_unidades_geradas()
    {
        await using (var db = f.Fabrica.Criar())
        {
            var e = await db.Empresas.SingleAsync(x => x.Id == f.EmpresaId);
            e.ModeloEstrutura = "Loteamento"; e.NomesNiveis = EstruturaLocais.Loteamento.Serializar();
            await db.SaveChangesAsync();
        }
        try
        {
            var emp = await Dados.MontarEmpreendimentoAsync(f);
            await using var db = f.Fabrica.CriarLeitura();
            var unidades = await db.Locais.Where(l => l.ObraId == emp.ObraId && l.Tipo == GeradorEmpreendimento.TipoApartamento).ToListAsync();
            Assert.NotEmpty(unidades);
            Assert.All(unidades, u => Assert.StartsWith("Casa ", u.Nome));
            Assert.Equal("Quadra", EstruturaLocais.De("Loteamento", null).Rotulo(GeradorEmpreendimento.TipoTorre));
        }
        finally
        {
            await using var db = f.Fabrica.Criar();
            var e = await db.Empresas.SingleAsync(x => x.Id == f.EmpresaId);
            e.ModeloEstrutura = null; e.NomesNiveis = null;
            await db.SaveChangesAsync();
        }
    }
}
