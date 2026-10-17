using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Qualidade;

/// <summary>Leituras do mapa de inspeções (locais × modelos de FVS), só leitura.</summary>
public sealed class MapaConsultas(IFabricaDb fabrica)
{
    /// <summary>Meta de aprovação na 1ª inspeção (parâmetro).</summary>
    public async Task<decimal> MetaAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return (await ParametrosServico.CarregarAsync(db, ct))[Parametros.MetaFvs1aInspecaoPerc];
    }

    /// <summary>Torres da obra, linhas do mapa (filhos da torre escolhida ou de todas) e FVS não canceladas.</summary>
    public async Task<MapaInspecoes> CarregarAsync(Guid obra, Guid? torre, Guid? prestador, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var locais = await db.Locais.AsNoTracking().Where(l => l.ObraId == obra).ToListAsync(ct);
        var torres = locais.Where(l => l.LocalPaiId == null).OrderBy(l => l.Ordem).ToList();
        var raizes = torre is { } t ? [t] : torres.Select(x => x.Id).ToList();
        var linhas = locais.Where(l => l.LocalPaiId is { } p && raizes.Contains(p)).OrderBy(l => l.LocalPaiId).ThenByDescending(l => l.Ordem).ToList();
        if (linhas.Count == 0) linhas = locais.Where(l => raizes.Contains(l.Id)).ToList();
        var q = db.Fvss.AsNoTracking().Include(f => f.Itens).Where(f => f.ObraId == obra && f.Status != StatusFvs.Cancelada);
        if (prestador is { } pr) q = q.Where(f => f.FornecedorId == pr);
        var fvs = await q.OrderByDescending(f => f.DataInspecao).ToListAsync(ct);
        return new(torres, linhas, fvs);
    }
}
