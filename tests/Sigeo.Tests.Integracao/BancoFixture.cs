using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Sigeo.Application.Carga;
using Sigeo.Infrastructure.Dados;
using Sigeo.Infrastructure.Servicos;

namespace Sigeo.Tests.Integracao;

[CollectionDefinition(Nome)]
public sealed class ColecaoBanco : ICollectionFixture<BancoFixture>
{
    public const string Nome = "Banco SQL Server";
}

/// <summary>
/// Um banco novo por execução (nome com sufixo aleatório): aplica as migrations, cria a empresa fictícia e a carga padrão,
/// e apaga o banco no fim. Cada teste cria a própria obra, então os cenários não interferem entre si.
/// </summary>
public sealed class BancoFixture : IAsyncLifetime
{
    public bool Disponivel { get; private set; }
    public string NomeBanco { get; } = $"QH_Teste_{Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant()}";
    public Guid EmpresaId { get; } = Guid.CreateVersion7();
    public UsuarioSistema Usuario { get; private set; } = null!;
    public TimeProvider Relogio { get; } = TimeProvider.System;
    public IFabricaDb Fabrica { get; private set; } = null!;
    private DbContextOptions<SigeoDbContext>? opcoes;
    private string _cs = "";

    public async Task InitializeAsync()
    {
        if (FatoComBancoAttribute.ConnectionString is not { } cs) return; // sem banco: os testes são pulados
        var csb = new SqlConnectionStringBuilder(cs) { InitialCatalog = NomeBanco };
        _cs = csb.ConnectionString;
        Usuario = new UsuarioSistema(EmpresaId, "TesteIntegracao");
        // Mesmas opções do DI (DependencyInjection.AddSigeoInfra): o teste exercita o mesmo caminho da produção.
        opcoes = new DbContextOptionsBuilder<SigeoDbContext>()
            .UseSqlServer(csb.ConnectionString, sql =>
            {
                sql.EnableRetryOnFailure(5);
                sql.MigrationsHistoryTable("__Migracoes", "qh");
                sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            })
            .AddInterceptors(new AuditoriaInterceptor(Usuario, Relogio))
            .Options;
        Fabrica = new FabricaDb(opcoes, Usuario);

        await using (var db = Fabrica.Criar())
            await db.Database.MigrateAsync();
        await using (var db = Fabrica.Criar())
        {
            db.Empresas.Add(new Empresa { Id = EmpresaId, RazaoSocial = "Construtora Ficticia de Testes Ltda", NomeFantasia = "Ficticia", Cnpj = Dados.Cnpj(), Codigo = "TST" });
            await db.SaveChangesAsync();
        }
        await using (var db = Fabrica.Criar())
            await CargaPadrao.AplicarAsync(db, EmpresaId);
        Disponivel = true;
    }

    public async Task DisposeAsync()
    {
        if (opcoes is null) return;
        try
        {
            SqlConnection.ClearAllPools(); // conexões paradas no pool prendem o banco e impedem o DROP
            await using var db = new SigeoDbContext(opcoes, Usuario);
            await db.Database.EnsureDeletedAsync();
        }
        catch
        {
            // Limpeza é melhor-esforço: o nome é único, então um banco que sobrar não atrapalha a próxima execução.
        }
    }

    // ───────── Serviços reais, montados como no DI ─────────
    public AprovacaoServico Aprovacoes => new(Usuario, Relogio, Fabrica);
    public EngenhariaServico Engenharia => new(Fabrica);
    public OrcamentoServico Orcamento => new(Fabrica);
    public MaoObraServico MaoObra => new(Fabrica);
    public RequisicoesServico Requisicoes => new(Fabrica, Aprovacoes, Usuario, Relogio);
    public ContratosServico Contratos => new(Fabrica, Aprovacoes, Relogio);
    public MedicoesServico Medicoes => new(Fabrica, Aprovacoes, Usuario, Relogio);
    public IFabricaSistema Sistema => new FabricaSistema(_cs);
    public string ConnectionString => _cs;
    public AcessoFornecedorServico AcessoFornecedor(IEnvioMensagens envio) => new(Sistema, envio, Relogio);
}
