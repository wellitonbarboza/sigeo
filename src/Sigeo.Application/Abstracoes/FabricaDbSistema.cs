using Microsoft.EntityFrameworkCore;
namespace Sigeo.Application.Abstracoes;

/// <summary>
/// <see cref="IFabricaDb"/> com identidade de sistema numa empresa: permite usar os serviços de aplicação nas rotinas
/// em segundo plano (busca de notas no SIEG às 6h30), com auditoria "sistema" e acesso a todas as obras da empresa.
/// </summary>
public sealed class FabricaDbSistema(IFabricaSistema sistema, Guid empresaId, string origem) : IFabricaDb
{
    public ISigeoDb Criar() => sistema.Criar(empresaId, origem);

    public ISigeoDb CriarLeitura()
    {
        var db = sistema.Criar(empresaId, origem);
        db.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        return db;
    }

    public IUsuarioAtual Usuario { get; } = new UsuarioSistema(empresaId, origem);

    public async Task EmTransacaoAsync(Func<ISigeoDb, Task> acao, CancellationToken ct = default)
    {
        await using var estrategia = Criar();
        await estrategia.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var db = Criar();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await acao(db);
            await tx.CommitAsync(ct);
        });
    }
}
