using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Engenharia;

namespace Sigeo.Application.Engenharia;

/// <summary>Leituras da tela de tipologias (tipologias, serviços marcados, usos nos locais e composições ativas).</summary>
public sealed class TipologiasConsultas(IFabricaDb fabrica)
{
    public async Task<TipologiasDados> CarregarAsync(Guid obra, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var lista = await db.Tipologias.AsNoTracking().Include(t => t.Ambientes).Include(t => t.Mix).Where(t => t.ObraId == obra).OrderBy(t => t.Codigo).ToListAsync(ct);
        var aplic = await db.ServicoAplicacoes.AsNoTracking().Where(a => a.ObraId == obra).ToListAsync(ct);
        var usos = await db.Locais.AsNoTracking().Where(l => l.ObraId == obra && l.TipologiaId != null).GroupBy(l => l.TipologiaId!.Value).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);
        var comps = await db.Composicoes.AsNoTracking().Where(c => c.Ativa).OrderBy(c => c.Grupo).ThenBy(c => c.Codigo).ToListAsync(ct);
        return new(lista, aplic, usos, comps);
    }

    /// <summary>Tipologia com ambientes e mix, para edição.</summary>
    public async Task<Tipologia> TipologiaAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Tipologias.AsNoTracking().Include(x => x.Ambientes).Include(x => x.Mix).FirstAsync(x => x.Id == id, ct);
    }

    /// <summary>Serviços marcados nas tipologias da obra.</summary>
    public async Task<List<ServicoAplicacao>> AplicacoesAsync(Guid? obra, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.ServicoAplicacoes.AsNoTracking().Where(a => a.ObraId == obra).ToListAsync(ct);
    }
}
