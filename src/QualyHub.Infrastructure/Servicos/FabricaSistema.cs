using Microsoft.EntityFrameworkCore;
using QualyHub.Application.Abstracoes;
using QualyHub.Infrastructure.Dados;

namespace QualyHub.Infrastructure.Servicos;

/// <summary>Contextos com identidade de sistema (portal externo, GRD, carga inicial, rotinas) — sempre auditados.</summary>
public sealed class FabricaSistema(string connectionString) : IFabricaSistema
{
    public IQualyHubDb Criar(Guid empresaId, string origem)
    {
        var usuario = new UsuarioSistema(empresaId, origem);
        var opts = new DbContextOptionsBuilder<QualyHubDbContext>()
            .UseSqlServer(connectionString, s =>
            {
                // Portal, QR e carga padrão também precisam de nova tentativa: o Azure SQL serverless acorda da pausa em alguns segundos.
                s.EnableRetryOnFailure(5);
                s.MigrationsHistoryTable("__Migracoes", "qh");
                s.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            })
            .AddInterceptors(new AuditoriaInterceptor(usuario, TimeProvider.System))
            .Options;
        return new QualyHubDbContext(opts, usuario);
    }
}

