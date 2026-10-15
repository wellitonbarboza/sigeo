using Microsoft.EntityFrameworkCore;
namespace Groundkey.Application.Abstracoes;

/// <summary>
/// Cria um contexto curto por operação (padrão do Blazor Server: o circuito vive muito, o contexto não — e não é thread-safe).
/// </summary>
public interface IFabricaDb
{
    /// <summary>Contexto para gravar (rastreia o que for carregado).</summary>
    IGroundkeyDb Criar();

    /// <summary>Contexto só de leitura: nada é rastreado (sem snapshot por linha). Use em listagens e consultas de tela.</summary>
    IGroundkeyDb CriarLeitura();

    /// <summary>Usuário da operação (perfil, empresa, obras permitidas).</summary>
    IUsuarioAtual Usuario { get; }

    /// <summary>
    /// Várias gravações numa transação, compatível com a nova tentativa do Azure SQL: cada tentativa usa um contexto novo
    /// (nada fica meio salvo) e só confirma se tudo der certo.
    /// </summary>
    Task EmTransacaoAsync(Func<IGroundkeyDb, Task> acao, CancellationToken ct = default);
}

/// <summary>Contextos com identidade de sistema (portal externo, QR, carga padrão, rotinas) — sempre auditados.</summary>
public interface IFabricaSistema
{
    IGroundkeyDb Criar(Guid empresaId, string origem);
}

/// <summary>
/// <see cref="IFabricaDb"/> com identidade de sistema numa empresa: permite usar os serviços de aplicação nas rotinas
/// em segundo plano (busca de notas no SIEG às 6h30), com auditoria "sistema" e acesso a todas as obras da empresa.
/// </summary>
public sealed class FabricaDbSistema(IFabricaSistema sistema, Guid empresaId, string origem) : IFabricaDb
{
    public IGroundkeyDb Criar() => sistema.Criar(empresaId, origem);

    public IGroundkeyDb CriarLeitura()
    {
        var db = sistema.Criar(empresaId, origem);
        db.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        return db;
    }

    public IUsuarioAtual Usuario { get; } = new UsuarioSistema(empresaId, origem);

    public async Task EmTransacaoAsync(Func<IGroundkeyDb, Task> acao, CancellationToken ct = default)
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
