using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;

namespace Sigeo.Application.Cadastros;

/// <summary>Leituras da tela de centros de custo (Cadastros/CentrosCusto).</summary>
public sealed class CentrosCustoConsultas(IFabricaDb fabrica)
{
    /// <summary>Centros de custo e o valor comprometido em cada um (pedidos aprovados pela requisição + contratos ativos).</summary>
    public async Task<CentrosCustoDados> CarregarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var ccs = await db.CentrosCusto.AsNoTracking().OrderBy(o => o.Codigo).ToListAsync(ct);
        var peds = await db.Pedidos.AsNoTracking().Where(p => p.Status >= StatusPedido.Aprovado && p.Status != StatusPedido.Cancelado).Include(p => p.Itens).ToListAsync(ct);
        var reqs = await db.Requisicoes.AsNoTracking().Where(r => r.CentroCustoId != null).ToDictionaryAsync(r => r.Id, r => r.CentroCustoId!.Value, ct);
        var ctrs = await db.Contratos.AsNoTracking().Include(k => k.Itens).Where(k => k.CentroCustoId != null && k.Status == StatusContrato.Ativo).ToListAsync(ct);
        var comprometido = peds.Where(p => p.RequisicaoId is { } r && reqs.ContainsKey(r)).GroupBy(p => reqs[p.RequisicaoId!.Value]).ToDictionary(g => g.Key, g => g.Sum(p => p.Valor));
        foreach (var k in ctrs) comprometido[k.CentroCustoId!.Value] = comprometido.GetValueOrDefault(k.CentroCustoId!.Value) + k.ValorTotal;
        return new(ccs, comprometido);
    }
}
