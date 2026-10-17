using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Engenharia;

/// <summary>Leituras da tela do empreendimento (obras do cadastro e a estrutura da obra escolhida).</summary>
public sealed class EmpreendimentoConsultas(IFabricaDb fabrica)
{
    /// <summary>Todas as obras (inclusive inativas), por código.</summary>
    public async Task<List<Obra>> ObrasAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Obras.OrderBy(x => x.Codigo).ToListAsync(ct);
    }

    /// <summary>Obras e, com obra escolhida, os locais, agrupamentos, SPEs e tipologias dela.</summary>
    public async Task<EmpreendimentoDados> CarregarAsync(Guid? obra, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var obras = await db.Obras.OrderBy(x => x.Codigo).ToListAsync(ct);
        if (obra is not { } o) return new(obras, null);
        var locais = await db.Locais.Where(l => l.ObraId == o).ToListAsync(ct);
        var quadras = await db.Quadras.Where(q => q.ObraId == o).OrderBy(q => q.Ordem).ThenBy(q => q.Codigo).ToListAsync(ct);
        var spes = await db.Spes.Where(s => s.ObraId == o).OrderBy(s => s.Codigo).ToListAsync(ct);
        var tips = await db.Tipologias.Include(t => t.Mix).Where(t => t.ObraId == o).OrderBy(t => t.Tipo).ThenBy(t => t.Codigo).ToListAsync(ct);
        return new(obras, new(locais, quadras, spes, tips));
    }
}
