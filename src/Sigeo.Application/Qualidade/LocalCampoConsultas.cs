using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Qualidade;

/// <summary>Leituras do local aberto pelo QR no celular (tela de campo), só leitura.</summary>
public sealed class LocalCampoConsultas(IFabricaDb fabrica)
{
    /// <summary>Local da etiqueta (null quando o QR não existe).</summary>
    public async Task<Local?> LocalAsync(string qr, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Locais.AsNoTracking().FirstOrDefaultAsync(l => l.QrToken == qr, ct);
    }

    /// <summary>FVS não canceladas do local, da mais recente.</summary>
    public async Task<List<Fvs>> FvssAsync(Guid local, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Fvss.AsNoTracking().Where(f => f.LocalId == local && f.Status != StatusFvs.Cancelada).OrderByDescending(f => f.DataInspecao).ToListAsync(ct);
    }
}
