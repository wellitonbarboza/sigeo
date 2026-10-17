using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Planejamento;

namespace Sigeo.Application.Planejamento;

/// <summary>Leitura da impressão do diário de obra (RDO).</summary>
public sealed class DiarioImprimirConsultas(IFabricaDb fabrica)
{
    /// <summary>Diário com efetivo, atividades e equipamentos, mais os nomes e fotos usados na folha; null se não existe.</summary>
    public async Task<DiarioImpressaoDados?> CarregarAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var d = await db.DiariosObra.AsNoTracking().Include(x => x.Efetivo).Include(x => x.Atividades).Include(x => x.EquipamentosDia).AsSplitQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (d is null) return null;
        var obra = await db.Obras.AsNoTracking().Where(o => o.Id == d.ObraId).Select(o => o.Codigo + " – " + o.Nome).FirstOrDefaultAsync(ct) ?? "";
        var eqIds = d.EquipamentosDia.Select(x => x.EquipamentoId).ToList();
        var equip = await db.Ativos.AsNoTracking().Where(e => eqIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, e => e.Patrimonio + " – " + e.Nome, ct);
        var locaisIds = d.Atividades.Where(a => a.LocalId != null).Select(a => a.LocalId!.Value).ToList();
        var locais = await db.Locais.AsNoTracking().Where(l => locaisIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.Nome, ct);
        var ids = d.Atividades.Select(a => a.Id).ToList();
        var fotos = (await db.Anexos.AsNoTracking().Where(a => a.Entidade == DiarioObraServico.EntidadeAtividade && ids.Contains(a.EntidadeId) && a.Categoria == "Foto")
            .Select(a => new { a.EntidadeId, a.Id, a.CriadoEm }).ToListAsync(ct)).GroupBy(a => a.EntidadeId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.CriadoEm).Select(x => x.Id).ToList());
        return new(d, obra, locais, equip, fotos);
    }
}
