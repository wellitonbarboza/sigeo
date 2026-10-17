using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Planejamento;

/// <summary>Leituras da tela do diário de obra (RDO).</summary>
public sealed class DiarioObraConsultas(IFabricaDb fabrica)
{
    /// <summary>Almoxarifado da obra e os equipamentos não baixados nem devolvidos, por patrimônio.</summary>
    public async Task<DiarioEquipamentosDados> EquipamentosAsync(Guid obra, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var almox = await db.Almoxarifados.AsNoTracking().Where(a => a.ObraId == obra).Select(a => (Guid?)a.Id).FirstOrDefaultAsync(ct);
        var eqs = await db.Ativos.AsNoTracking().Where(e => e.Situacao != SituacaoEquipamento.Baixado && e.Situacao != SituacaoEquipamento.Devolvido).OrderBy(e => e.Patrimonio)
            .Select(e => new DiarioEquipamentoItem(e.Id, e.Patrimonio, e.Nome, e.AlmoxarifadoId)).ToListAsync(ct);
        return new(almox, eqs);
    }
}
