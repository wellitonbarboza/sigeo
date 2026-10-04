using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using QualyHub.Infrastructure.Dados;
using QualyHub.Infrastructure.Importacao;

namespace QualyHub.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra o QualyHub. Em produção a connection string usa Managed Identity
    /// (Authentication=Active Directory Default) — sem senha de banco em configuração.
    /// </summary>
    public static IServiceCollection AddQualyHubInfra(this IServiceCollection services, string connectionString, byte[]? chaveCpf = null)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AuditoriaInterceptor>();
        services.AddDbContext<QualyHubDbContext>((sp, o) => o
            .UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(5);          // Azure SQL serverless "acorda" do auto-pause em alguns segundos
                sql.MigrationsHistoryTable("__Migracoes", "qh");
                // Várias coleções no mesmo Include viram consultas separadas (sem produto cartesiano: a linha-pai não volta N×M vezes).
                sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            })
            .AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>()));
        services.AddScoped<ImportadorFornecedores>();
        services.AddSingleton<IFabricaSistema>(new Servicos.FabricaSistema(connectionString));
        services.AddScoped<IFabricaDb, Servicos.FabricaDb>();
        services.AddScoped<AprovacaoServico>();
        // Suprimentos: um serviço por processo (antes um só com 7 processos).
        services.AddScoped<RequisicoesServico>();
        services.AddScoped<CotacoesServico>();
        services.AddScoped<PedidosServico>();
        services.AddScoped<ContratosServico>();
        services.AddScoped<RecebimentosServico>();
        services.AddScoped<TransferenciasServico>();
        services.AddScoped<MedicoesServico>();
        services.AddScoped<PagamentosServico>();
        services.AddScoped<AvaliacaoFornecedoresServico>();
        services.AddScoped<PlanosAcaoServico>();
        services.AddScoped<CadastrosServico>();
        services.AddScoped<SistemaServico>();
        services.AddScoped<AnexosServico>();
        services.AddScoped<DocumentosServico>();
        services.AddScoped<PortalServico>();
        services.AddScoped<QualidadeObraServico>();
        services.AddScoped<SstServico>();
        services.AddScoped<ColaboracaoServico>();
        services.AddScoped<CodigosProdutoServico>();
        services.AddScoped<EngenhariaServico>();
        services.AddScoped<OrcamentoServico>();
        services.AddScoped<MaoObraServico>();
        services.AddScoped<EstabelecimentosServico>();
        services.AddScoped<FornecedoresServico>();
        services.AddScoped<Importacao.ImportadorEspecificacao>();
        services.AddScoped<ContaServico>();
        services.AddScoped<Importacao.ImportadorFornecedoresExpertData>();
        services.AddScoped<Importacao.ImportadorProdutosExpertData>();
        // LGPD: chave HMAC do CPF vem do Key Vault em produção; sem chave, gera uma efêmera (apenas desenvolvimento).
        services.AddSingleton(new ProtecaoCpf(chaveCpf ?? System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
        return services;
    }
}

/// <summary>Usado apenas pelo "dotnet ef migrations". Configure QH_SQL no ambiente local.</summary>
public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<QualyHubDbContext>
{
    public QualyHubDbContext CreateDbContext(string[] args)
    {
        // Sem senha no código: a connection string vem só do ambiente de quem gera a migração.
        var cs = Environment.GetEnvironmentVariable("QH_SQL")
                 ?? throw new InvalidOperationException("Defina a variável QH_SQL com a connection string do SQL Server local para gerar migrações.");
        var opts = new DbContextOptionsBuilder<QualyHubDbContext>().UseSqlServer(cs, s => s.MigrationsHistoryTable("__Migracoes", "qh")).Options;
        return new QualyHubDbContext(opts, new UsuarioSistema(Guid.Empty, "Migracao"));
    }
}
