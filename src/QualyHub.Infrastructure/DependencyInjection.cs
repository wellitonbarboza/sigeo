using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        services.AddScoped<AcessoInterceptor>();
        services.TryAddScoped<Application.Servicos.IContextoTela, Application.Servicos.SemContextoTela>();
        services.AddScoped<Application.Servicos.AcessoServico>();
        services.AddDbContext<QualyHubDbContext>((sp, o) => o
            .UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(5);          // Azure SQL serverless "acorda" do auto-pause em alguns segundos
                sql.MigrationsHistoryTable("__Migracoes", "qh");
                // Várias coleções no mesmo Include viram consultas separadas (sem produto cartesiano: a linha-pai não volta N×M vezes).
                sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            })
            .AddInterceptors(sp.GetRequiredService<AcessoInterceptor>(), sp.GetRequiredService<AuditoriaInterceptor>()));
        services.AddScoped<ImportadorFornecedores>();
        services.AddSingleton<IFabricaSistema>(new Servicos.FabricaSistema(connectionString));
        services.AddScoped<IFabricaDb, Servicos.FabricaDb>();
        services.AddScoped<IExclusao, Servicos.Exclusao>();
        services.AddScoped<AprovacaoServico>();
        services.AddScoped<Application.Consultas.PaineisConsultas>();
        services.AddScoped<Application.Consultas.EngenhariaConsultas>();
        services.AddScoped<Application.Fiscal.NotasFiscaisServico>();
        // Suprimentos: um serviço por processo (antes um só com 7 processos).
        services.AddScoped<RequisicoesServico>();
        services.AddScoped<CotacoesServico>();
        services.AddScoped<PedidosServico>();
        services.AddScoped<ContratosServico>();
        services.AddScoped<RecebimentosServico>();
        services.AddScoped<TransferenciasServico>();
        services.AddScoped<EstoqueServico>();
        services.AddScoped<MarcasServico>();
        services.AddScoped<EquipamentosServico>();
        services.AddScoped<ContratosRecorrentesServico>();
        services.AddScoped<MedicoesServico>();
        services.AddScoped<PagamentosServico>();
        services.AddScoped<AvaliacaoFornecedoresServico>();
        services.AddScoped<PlanosAcaoServico>();
        services.AddScoped<CadastrosServico>();
        services.AddScoped<SistemaServico>();
        services.AddScoped<AnexosServico>();
        services.AddScoped<AcessoFornecedorServico>();
        services.AddScoped<KitTipologiaServico>();
        services.AddScoped<KitModelosServico>();
        services.AddScoped<ConvitesFornecedorServico>();
        services.AddScoped<DocumentosServico>();
        services.AddScoped<PortalServico>();
        services.AddScoped<QualidadeObraServico>();
        services.AddScoped<SstServico>();
        services.AddScoped<ColaboracaoServico>();
        services.AddScoped<CodigosProdutoServico>();
        services.AddScoped<EngenhariaServico>();
        services.AddScoped<OrcamentoServico>();
        services.AddScoped<PlanejamentoServico>();
        services.AddScoped<CurtoPrazoServico>();
        services.AddScoped<AcompanhamentoServico>();
        services.AddScoped<DiarioObraServico>();
        services.AddScoped<MensagensServico>();
        services.AddSingleton<QualyHub.Application.Comum.CanalAvisos>();
        services.AddScoped<MaoObraServico>();
        services.AddScoped<CustoRealizadoServico>();
        services.AddScoped<Importacao.ImportadorHistoricoErp>();

        services.AddScoped<EstabelecimentosServico>();
        services.AddScoped<FornecedoresServico>();
        services.AddScoped<Importacao.ImportadorEspecificacao>();
        services.AddScoped<ContaServico>();
        services.AddScoped<PlataformaServico>();
        services.AddScoped<Importacao.ImportadorFornecedoresExpertData>();
        services.AddScoped<Importacao.ImportadorProdutosExpertData>();
        // LGPD: chave HMAC do CPF vem do Key Vault em produção; sem chave, gera uma efêmera (apenas desenvolvimento).
        services.AddSingleton(new ProtecaoCpf(chaveCpf ?? System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
        return services;
    }
}

public static class DependencyInjectionComunicacao
{
    /// <summary>
    /// E-mail/WhatsApp pelo Azure Communication Services (seção QualyHub:Comunicacao). Sem configuração:
    /// em desenvolvimento registra no log; em produção os canais ficam indisponíveis (a tela avisa).
    /// </summary>
    public static IServiceCollection AddQualyHubComunicacao(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration cfg, bool desenvolvimento)
    {
        var sec = cfg.GetSection("QualyHub:Comunicacao");
        var o = new Comunicacao.OpcoesComunicacao
        {
            Endpoint = sec["Endpoint"], EmailRemetente = sec["EmailRemetente"], WhatsAppCanalId = sec["WhatsAppCanalId"],
            WhatsAppModeloCodigo = sec["WhatsAppModeloCodigo"], WhatsAppModeloConvite = sec["WhatsAppModeloConvite"],
            WhatsAppIdioma = sec["WhatsAppIdioma"] is { Length: > 0 } idioma ? idioma : "pt_BR"
        };
        if (!string.IsNullOrWhiteSpace(o.Endpoint))
            services.AddSingleton<IEnvioMensagens>(sp => new Comunicacao.EnvioAcs(o, new Azure.Identity.DefaultAzureCredential(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Comunicacao.EnvioAcs>>()));
        else if (desenvolvimento) services.AddSingleton<IEnvioMensagens, Comunicacao.EnvioLocalLog>();
        else services.AddSingleton<IEnvioMensagens, Comunicacao.EnvioIndisponivel>();
        return services;
    }
    /// <summary>
    /// Integrações externas: SIEG (notas fiscais de entrada). Sem credencial da empresa o conector fica registrado,
    /// mas responde "não configurada" — a tela mostra só a importação por arquivo.
    /// </summary>
    public static IServiceCollection AddQualyHubIntegracoes(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration cfg)
    {
        services.AddHttpClient("sieg", c => c.Timeout = TimeSpan.FromMinutes(3));
        services.AddSingleton<Application.Fiscal.IFonteNotas, Servicos.FonteSieg>();
        // IA (Azure OpenAI + Document Intelligence): sem endpoint configurado os botões não aparecem.
        services.AddHttpClient("ia", c => c.Timeout = TimeSpan.FromMinutes(2));
        services.AddSingleton<Application.IA.IAssistenteIa, Servicos.AssistenteAzureOpenAi>();
        services.AddSingleton<Application.IA.ILeitorDocumentos, Servicos.LeitorDocumentIntelligence>();
        services.AddSingleton(new Application.IA.LimitesIa(int.TryParse(cfg["IA:LimiteMensalPorEmpresa"], out var l) ? l : 3000));
        services.AddScoped<Application.IA.AnalisesIaServico>();
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
