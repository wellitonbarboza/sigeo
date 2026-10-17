using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da fila de compras (a fila em si vem de AtendimentoServico).</summary>
public sealed class FilaComprasConsultas(IFabricaDb fabrica)
{
    /// <summary>Comprador atual de cada lote (visão de todos, para reatribuir).</summary>
    public async Task<Dictionary<Guid, Guid?>> CompradoresDosLotesAsync(IReadOnlyCollection<Guid> lotes, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.LotesCompra.AsNoTracking().Where(x => lotes.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.CompradorUsuarioId, ct);
    }
}
