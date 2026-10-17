using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela de transferências (só consulta; gravações ficam em TransferenciasServico).</summary>
public sealed class TransferenciasConsultas(IFabricaDb fabrica)
{
    public async Task<TransferenciasLista> ListaAsync(int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var lista = await db.Transferencias.AsNoTracking().Include(t => t.Itens).OrderByDescending(t => t.Numero).Take(limite).ToListAsync(ct);
        var idsRec = lista.Where(t => t.RecebimentoId != null).Select(t => t.RecebimentoId!.Value).Distinct().ToList();
        var recs = await db.Recebimentos.AsNoTracking().Where(r => idsRec.Contains(r.Id)).Select(r => new { r.Id, r.Numero }).ToDictionaryAsync(r => r.Id, r => r.Numero, ct);
        return new(lista, recs);
    }

    public async Task<TransferenciaMaterial> TransferenciaAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Transferencias.AsNoTracking().Include(t => t.Itens).FirstAsync(t => t.Id == id, ct);
    }
}
