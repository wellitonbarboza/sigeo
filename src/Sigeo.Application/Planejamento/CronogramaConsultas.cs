using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Planejamento;

namespace Sigeo.Application.Planejamento;

/// <summary>Leituras da tela de cronograma e reprogramação (versões, calendário, feriados e redes da obra).</summary>
public sealed class CronogramaConsultas(IFabricaDb fabrica)
{
    /// <summary>Obra da versão pedida no link (?versao=).</summary>
    public async Task<Guid?> ObraDaVersaoAsync(Guid versao, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.VersoesCronograma.AsNoTracking().Where(v => v.Id == versao).Select(v => (Guid?)v.ObraId).FirstOrDefaultAsync(ct);
    }

    /// <summary>Versões (mais nova primeiro), calendário e feriados da obra.</summary>
    public async Task<CronogramaObraDados> ObraAsync(Guid obra, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var versoes = await db.VersoesCronograma.AsNoTracking().Where(v => v.ObraId == obra).OrderByDescending(v => v.Numero).ToListAsync(ct);
        var cal = await PlanejamentoServico.CalendarioAsync(db, obra, ct);
        var feriados = await db.Feriados.AsNoTracking().Where(f => f.ObraId == null || f.ObraId == obra).OrderBy(f => f.Data).ToListAsync(ct);
        return new(versoes, cal, feriados);
    }

    /// <summary>Versão com atividades e vínculos, e as redes da obra dela.</summary>
    public async Task<CronogramaVersaoDados> VersaoAsync(Guid versao, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var v = await db.VersoesCronograma.AsNoTracking().Include(v => v.Atividades).Include(v => v.Vinculos).AsSplitQuery().FirstAsync(v => v.Id == versao, ct);
        var redes = await db.RedesPlanejamento.AsNoTracking().Include(r => r.Locais).Include(r => r.Pacotes).AsSplitQuery().Where(r => r.ObraId == v.ObraId).OrderBy(r => r.Ordem).ToListAsync(ct);
        return new(v, redes);
    }
}
