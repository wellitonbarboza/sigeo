using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Planejamento;

/// <summary>Leitura da impressão do cronograma (Gantt em SVG).</summary>
public sealed class PlanejamentoImprimirConsultas(IFabricaDb fabrica)
{
    /// <summary>Versão com atividades e vínculos, nome da obra e calendário; null se a versão não existe.</summary>
    public async Task<PlanejamentoImpressaoDados?> CarregarAsync(Guid versao, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var v = await db.VersoesCronograma.AsNoTracking().Include(v => v.Atividades).Include(v => v.Vinculos).AsSplitQuery().FirstOrDefaultAsync(v => v.Id == versao, ct);
        if (v is null) return null;
        var obra = await db.Obras.AsNoTracking().Where(o => o.Id == v.ObraId).Select(o => o.Codigo + " – " + o.Nome).FirstAsync(ct);
        var cal = await PlanejamentoServico.CalendarioAsync(db, v.ObraId, ct);
        return new(v, obra, cal);
    }
}
