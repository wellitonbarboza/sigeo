using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Documentos;

namespace Sigeo.Application.Documentos;

/// <summary>Leituras da lista mestra de documentos do SGQ, só leitura.</summary>
public sealed class ListaMestraConsultas(IFabricaDb fabrica)
{
    /// <summary>Nomes das pastas e documentos não cancelados (com revisões e cópias), por código.</summary>
    public async Task<ListaMestraDados> CarregarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var pastas = await db.Pastas.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Nome, ct);
        var docs = await db.Documentos.AsNoTracking().Include(d => d.Revisoes).ThenInclude(r => r.Copias).Where(d => d.Status != StatusDocumentoSgq.Cancelado).OrderBy(d => d.Codigo).ToListAsync(ct);
        return new(pastas, docs);
    }
}
