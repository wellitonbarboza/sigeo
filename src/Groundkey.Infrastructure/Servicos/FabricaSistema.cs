using Microsoft.EntityFrameworkCore;
using Groundkey.Application.Abstracoes;
using Groundkey.Infrastructure.Dados;

namespace Groundkey.Infrastructure.Servicos;

/// <summary>Contextos com identidade de sistema (portal externo, GRD, carga inicial, rotinas) — sempre auditados.</summary>
public sealed class FabricaSistema(string connectionString) : IFabricaSistema
{
    public IGroundkeyDb Criar(Guid empresaId, string origem)
    {
        var usuario = new UsuarioSistema(empresaId, origem);
        var opts = new DbContextOptionsBuilder<GroundkeyDbContext>()
            .UseSqlServer(connectionString, s =>
            {
                // Portal, QR e carga padrão também precisam de nova tentativa: o Azure SQL serverless acorda da pausa em alguns segundos.
                s.EnableRetryOnFailure(5);
                s.MigrationsHistoryTable("__Migracoes", "qh");
                s.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            })
            .AddInterceptors(new AuditoriaInterceptor(usuario, TimeProvider.System))
            .Options;
        return new GroundkeyDbContext(opts, usuario);
    }
}

