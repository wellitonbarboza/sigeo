using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Planejamento;

namespace Sigeo.Application.Planejamento;

/// <summary>Leituras da tela de indicadores do planejamento (só leitura).</summary>
public sealed class PlanejamentoTelaConsultas(IFabricaDb fabrica)
{
    /// <summary>Versões da obra (mais nova primeiro) e o calendário dela.</summary>
    public async Task<PlanejamentoTelaObraDados> ObraAsync(Guid obra, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var versoes = await db.VersoesCronograma.AsNoTracking().Where(v => v.ObraId == obra)
            .OrderByDescending(v => v.Numero).ToListAsync(ct);
        var cal = await PlanejamentoServico.CalendarioAsync(db, obra, ct);
        return new(versoes, cal);
    }

    /// <summary>Versão com as atividades.</summary>
    public async Task<VersaoCronograma> VersaoAsync(Guid versao, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.VersoesCronograma.AsNoTracking().Include(v => v.Atividades).FirstAsync(v => v.Id == versao, ct);
    }
}
