using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Planejamento;

/// <summary>Leituras da programação semanal (filtro de SPE).</summary>
public sealed class ProgramacaoSemanalConsultas(IFabricaDb fabrica)
{
    /// <summary>SPEs da obra por código, com o nome fantasia (ou a razão social).</summary>
    public async Task<List<ProgramacaoSpe>> SpesAsync(Guid obra, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Spes.AsNoTracking().Where(s => s.ObraId == obra).OrderBy(s => s.Codigo).Select(s => new ProgramacaoSpe(s.Id, s.NomeFantasia ?? s.RazaoSocial)).ToListAsync(ct);
    }
}
