using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Planejamento;

/// <summary>Leituras do editor de redes (linha de balanço): redes, vínculos, locais com custo e composições.</summary>
public sealed class EditorRedesConsultas(IFabricaDb fabrica)
{
    public async Task<EditorRedesDados> CarregarAsync(Guid obra, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var redes = await db.RedesPlanejamento.AsNoTracking().Include(r => r.Locais).Include(r => r.Pacotes).ThenInclude(p => p.Ajustes).AsSplitQuery()
            .Where(r => r.ObraId == obra).OrderBy(r => r.Ordem).ToListAsync(ct);
        var vinculos = await db.VinculosRede.AsNoTracking().Where(v => v.ObraId == obra).ToListAsync(ct);
        var defs = await PlanejamentoServico.DefinicoesAsync(db, obra, ct);
        var usados = await db.ServicosLocais.AsNoTracking().Where(s => s.ObraId == obra).Select(s => s.ComposicaoId).Distinct().ToListAsync(ct);
        var comps = await db.Composicoes.AsNoTracking().Where(c => c.Ativa).OrderBy(c => c.Codigo).Select(c => new EditorRedesComposicao(c.Id, c.Codigo, c.Descricao)).ToListAsync(ct);
        return new(redes, vinculos, defs.Custo, defs.Locais, usados, comps);
    }
}
