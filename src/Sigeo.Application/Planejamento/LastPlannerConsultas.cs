using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Planejamento;

namespace Sigeo.Application.Planejamento;

/// <summary>Leituras da tela Last Planner (acompanhamento semanal).</summary>
public sealed class LastPlannerConsultas(IFabricaDb fabrica)
{
    /// <summary>Versões do cronograma da obra, mais nova primeiro.</summary>
    public async Task<List<VersaoCronograma>> VersoesAsync(Guid obra, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.VersoesCronograma.AsNoTracking().Where(v => v.ObraId == obra).OrderByDescending(v => v.Numero).ToListAsync(ct);
    }
}
