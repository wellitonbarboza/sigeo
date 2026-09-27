using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using QualyHub.Infrastructure.Contexto;
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
            })
            .AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>()));
        services.AddScoped<ImportadorFornecedores>();
        services.AddSingleton(new Servicos.FabricaSistema(connectionString));
        services.AddScoped<Servicos.FabricaDb>();
        services.AddScoped<Servicos.AprovacaoServico>();
        services.AddScoped<Servicos.SuprimentosServico>();
        services.AddScoped<Servicos.DocumentosServico>();
        services.AddScoped<Servicos.PortalServico>();
        services.AddScoped<Servicos.QualidadeObraServico>();
        services.AddScoped<Servicos.SstServico>();
        services.AddScoped<Servicos.ColaboracaoServico>();
        services.AddScoped<Servicos.CodigosProdutoServico>();
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
        var cs = Environment.GetEnvironmentVariable("QH_SQL")
                 ?? "Server=localhost,1433;Database=QualyHub;User Id=sa;Password=Dev@12345;TrustServerCertificate=True";
        var opts = new DbContextOptionsBuilder<QualyHubDbContext>().UseSqlServer(cs, s => s.MigrationsHistoryTable("__Migracoes", "qh")).Options;
        return new QualyHubDbContext(opts, new UsuarioSistema(Guid.Empty, "Migracao"));
    }
}
