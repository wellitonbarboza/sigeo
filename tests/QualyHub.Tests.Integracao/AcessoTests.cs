using Microsoft.Extensions.DependencyInjection;
using QualyHub.Contratos;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Infrastructure.Dados;
using QualyHub.Infrastructure.Servicos;

namespace QualyHub.Tests.Integracao;

/// <summary>
/// Acesso por tela no banco real: telas liberadas pela plataforma, perfil de acesso padrão e editado, exceção por usuário,
/// barreira de gravação (interceptor) pela tela em que o usuário está e "Aprovar" no motor de aprovação.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public sealed class AcessoTests(BancoFixture f)
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

    private sealed class TelaFixa(string? rota) : IContextoTela { public string? Rota { get; set; } = rota; }

    /// <summary>Monta os serviços como no DI da Web: contexto com a barreira de acesso + auditoria, usuário logado e tela atual.</summary>
    private sealed class Sessao : IAsyncDisposable
    {
        public required ServiceProvider Sp { get; init; }
        public required TelaFixa Tela { get; init; }
        public IFabricaDb Fabrica => Sp.GetRequiredService<IFabricaDb>();
        public AcessoServico Acesso => Sp.GetRequiredService<AcessoServico>();
        public ValueTask DisposeAsync() => Sp.DisposeAsync();
    }

    private Sessao Entrar(Usuario u, string? rota)
    {
        var tela = new TelaFixa(rota);
        var logado = new Logado(f.EmpresaId, u.Id, u.Nome);
        var sc = new ServiceCollection();
        sc.AddSingleton<IUsuarioAtual>(logado);
        sc.AddSingleton<IContextoTela>(tela);
        sc.AddSingleton(f.Relogio);
        sc.AddSingleton<AcessoInterceptor>();
        sc.AddSingleton(sp => new DbContextOptionsBuilder<QualyHubDbContext>()
            .UseSqlServer(f.ConnectionString, sql => sql.MigrationsHistoryTable("__Migracoes", "qh"))
            .AddInterceptors(sp.GetRequiredService<AcessoInterceptor>(), new AuditoriaInterceptor(logado, f.Relogio)).Options);
        sc.AddSingleton<IFabricaDb>(sp => new FabricaDb(sp.GetRequiredService<DbContextOptions<QualyHubDbContext>>(), logado));
        sc.AddSingleton<AcessoServico>();
        return new Sessao { Sp = sc.BuildServiceProvider(), Tela = tela };
    }

    private async Task<Usuario> UsuarioAsync(PerfilUsuario perfil)
    {
        var u = new Usuario { Login = $"ac.{Guid.NewGuid():N}"[..20], Nome = $"Usuario {perfil} Ficticio", Email = $"{Guid.NewGuid():N}@exemplo.com", Perfil = perfil, AcessoTodasObras = true };
        await using var db = f.Fabrica.Criar();
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        return u;
    }

    private async Task BloquearAsync(string? telas)
    {
        await using var db = f.Fabrica.Criar();
        (await db.Empresas.SingleAsync(e => e.Id == f.EmpresaId)).TelasBloqueadas = telas;
        await db.SaveChangesAsync();
        AcessoServico.Invalidar();
    }

    [FatoComBanco]
    public async Task Perfil_padrao_telas_bloqueadas_e_excecao_por_usuario()
    {
        var campo = await UsuarioAsync(PerfilUsuario.Campo);
        await using var s = Entrar(campo, null);
        var m = await s.Acesso.MapaAsync();
        Assert.False(m.Administrador);
        Assert.True(m.Pode("requisicoes", AcaoTela.Incluir));
        Assert.False(m.Pode("requisicoes", AcaoTela.Excluir));
        Assert.True(m.Ve("pedidos"));
        Assert.False(m.Pode("pedidos", AcaoTela.Incluir));
        Assert.False(m.Ve("sistema/perfis"));

        // Plataforma desliga o SST da construtora: some do mapa e vira "não contratado".
        await BloquearAsync("sst/painel;sst/pt;sistema/log");
        try
        {
            m = await s.Acesso.MapaAsync();
            Assert.Contains("sst/painel", m.NaoContratadas);
            Assert.DoesNotContain("sistema/log", m.NaoContratadas); // Sistema é fixo
            Assert.Equal(SituacaoTela.NaoContratada, CatalogoTelas.Situacao(m, "/sst/pt?x=1", out _));
            var adm = await UsuarioAsync(PerfilUsuario.Administrador);
            await using var sa = Entrar(adm, null);
            var ma = await sa.Acesso.MapaAsync();
            Assert.True(ma.Administrador);
            Assert.False(ma.Ve("sst/painel"));
            Assert.True(ma.Pode("pedidos", AcaoTela.Excluir));

            // Administrador da construtora: perfil padrão de Campo ganha "Excluir" em Requisição; o usuário perde "Incluir" por exceção.
            var perfis = await sa.Acesso.PerfisAsync();
            var pc = perfis.Single(p => p.Base == PerfilUsuario.Campo);
            Assert.Equal(PerfisPadrao.Regras(PerfilUsuario.Campo)["requisicoes"], pc.Regras["requisicoes"]);
            var regras = new Dictionary<string, string>(pc.Regras) { ["requisicoes"] = "VIEX" };
            await sa.Acesso.SalvarPerfilAsync(pc.Id, pc.Nome, regras);
            Assert.True((await s.Acesso.MapaAsync()).Pode("requisicoes", AcaoTela.Excluir)); // vale sem sair e entrar
            await sa.Acesso.SalvarAcessoUsuarioAsync(campo.Id, null, new Dictionary<string, Dictionary<char, bool>>
            {
                ["requisicoes"] = new() { [AcaoTela.Incluir] = false },
                ["pedidos"] = new() { [AcaoTela.Aprovar] = true },
            });
            m = await s.Acesso.MapaAsync();
            Assert.Equal("VEX", m.Telas["requisicoes"]);
            Assert.True(m.Pode("pedidos", AcaoTela.Aprovar));
            var ex = Assert.Throws<RegraDeNegocioException>(() => AcessoServico.Conferir(m, "requisicoes", AcaoTela.Incluir));
            Assert.Contains("Requisição", ex.Message);

            // Usuário comum não administra perfis.
            await Assert.ThrowsAsync<RegraDeNegocioException>(() => s.Acesso.SalvarPerfilAsync(pc.Id, pc.Nome, regras));
            // Perfil criado (cópia) para o usuário; perfil padrão não pode ser excluído; perfil em uso também não.
            var copia = await sa.Acesso.DuplicarPerfilAsync(pc.Id);
            await sa.Acesso.SalvarAcessoUsuarioAsync(campo.Id, copia, new Dictionary<string, Dictionary<char, bool>>());
            await Assert.ThrowsAsync<RegraDeNegocioException>(() => sa.Acesso.ExcluirPerfilAsync(pc.Id));
            await Assert.ThrowsAsync<RegraDeNegocioException>(() => sa.Acesso.ExcluirPerfilAsync(copia));
            await sa.Acesso.RestaurarPadraoAsync(pc.Id);
        }
        finally { await BloquearAsync(null); }
    }

    [FatoComBanco]
    public async Task Gravacao_exige_incluir_ou_excluir_na_tela_atual()
    {
        var almox = await UsuarioAsync(PerfilUsuario.Almoxarifado); // vê Cadastro, sem incluir em Fornecedores
        await using var s = Entrar(almox, "fornecedores");
        var forn = new Fornecedor { RazaoSocial = "Fornecedor Ficticio Acesso Ltda", NomeFantasia = "Ficticio Acesso", CpfCnpj = Dados.Cnpj() };
        await using (var db = s.Fabrica.Criar())
        {
            db.Fornecedores.Add(forn);
            var e = await Assert.ThrowsAsync<RegraDeNegocioException>(() => db.SaveChangesAsync());
            Assert.Contains("Fornecedores", e.Message);
        }
        // Na tela de Recebimento ele inclui; fora de tela (rotina, login) não há barreira.
        s.Tela.Rota = null;
        await using (var db = s.Fabrica.Criar()) { db.Fornecedores.Add(forn); await db.SaveChangesAsync(); }

        // Excluir em tela com "Incluir" mas sem "Excluir": recusado; alterar passa.
        var sup = await UsuarioAsync(PerfilUsuario.Suprimentos);
        await using var s2 = Entrar(sup, "fornecedores/qualificacao"); // Suprimentos: IE em Cadastro
        await using (var db = s2.Fabrica.Criar())
        {
            var x = await db.Fornecedores.SingleAsync(y => y.Id == forn.Id);
            x.NomeFantasia = "Ficticio Acesso 2";
            await db.SaveChangesAsync();
        }
        var campo = await UsuarioAsync(PerfilUsuario.Campo);
        await using var s3 = Entrar(campo, "requisicoes"); // Campo: VIX em Requisição
        await using (var db = s3.Fabrica.Criar())
        {
            var x = await db.Fornecedores.SingleAsync(y => y.Id == forn.Id);
            db.Fornecedores.Remove(x);
            var e = await Assert.ThrowsAsync<RegraDeNegocioException>(() => db.SaveChangesAsync());
            Assert.Contains("excluir", e.Message);
        }
    }

    [FatoComBanco]
    public async Task Decidir_aprovacao_exige_aprovar_na_tela_do_documento()
    {
        var campo = await UsuarioAsync(PerfilUsuario.Campo);
        await using var s = Entrar(campo, null);
        var aprov = new AprovacaoServico(s.Sp.GetRequiredService<IUsuarioAtual>(), f.Relogio, s.Fabrica);
        await using var db = s.Fabrica.Criar();
        var sol = SolicitacaoAprovacao.Criar(EntidadeAprovavel.Pedido, Guid.NewGuid(), f.EmpresaId, null, "Pedido ficticio", 10m, campo.Id, campo.Nome,
            [new Alcada { Entidade = EntidadeAprovavel.Pedido, Ordem = 1, PerfilAprovador = PerfilUsuario.Campo }], DateTimeOffset.UtcNow);
        await using (var g = f.Fabrica.Criar()) { g.SolicitacoesAprovacao.Add(sol); await g.SaveChangesAsync(); }
        Assert.False(await aprov.PodeDecidirAsync(db, sol));
        var e = await Assert.ThrowsAsync<RegraDeNegocioException>(() => aprov.DecidirAsync(db, sol.Id, DecisaoAprovacao.Aprovado, null));
        Assert.Contains("aprovar", e.Message);
    }
}
