using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Economia real (contra o último preço pago, sem índice), histórico de preço do item e aumentos acima do limite.</summary>
public sealed class EconomiaServico(IFabricaDb fabrica, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    /// <summary>Compras (itens de pedido não cancelados com produto) com o frete rateado no preço unitário.</summary>
    public static async Task<List<CompraPreco>> ComprasAsync(ISigeoDb db, IReadOnlyCollection<Guid>? produtos, DateOnly? ate, CancellationToken ct = default)
    {
        var fim = ate is { } a ? new DateTimeOffset(a.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : DateTimeOffset.MaxValue;
        var q = from pi in db.PedidoItens.AsNoTracking()
                join p in db.Pedidos.AsNoTracking() on pi.PedidoId equals p.Id
                where pi.ProdutoId != null && pi.ValorUnitario > 0 && pi.Quantidade > 0 && p.Status != StatusPedido.Cancelado && p.CriadoEm < fim
                select new { pi, p.Id, p.Numero, p.CriadoEm, p.FornecedorId, p.ObraId, p.ValorFrete, p.RequisicaoId };
        if (produtos is not null) q = q.Where(x => produtos.Contains(x.pi.ProdutoId!.Value));
        var linhas = await q.ToListAsync(ct);
        if (linhas.Count == 0) return [];
        var pedIds = linhas.Select(l => l.Id).Distinct().ToList();
        var totais = await db.PedidoItens.AsNoTracking().Where(i => pedIds.Contains(i.PedidoId)).GroupBy(i => i.PedidoId).Select(g => new { g.Key, T = g.Sum(i => i.ValorTotal) }).ToDictionaryAsync(x => x.Key, x => x.T, ct);
        var fIds = linhas.Select(l => l.FornecedorId).Distinct().ToList();
        var forn = await db.Fornecedores.AsNoTracking().Where(f => fIds.Contains(f.Id)).Select(f => new { f.Id, Nome = f.NomeFantasia ?? f.RazaoSocial }).ToDictionaryAsync(f => f.Id, f => f.Nome, ct);
        var obras = await db.Obras.AsNoTracking().Select(o => new { o.Id, o.Nome }).ToDictionaryAsync(o => o.Id, o => o.Nome, ct);
        var reqIds = linhas.Where(l => l.RequisicaoId != null).Select(l => l.RequisicaoId!.Value).Distinct().ToList();
        var emerg = (await db.Requisicoes.AsNoTracking().Where(r => reqIds.Contains(r.Id) && r.Emergencial).Select(r => r.Id).ToListAsync(ct)).ToHashSet();
        var riIds = linhas.Where(l => l.pi.RequisicaoItemId != null).Select(l => l.pi.RequisicaoItemId!.Value).Distinct().ToList();
        var lotes = await (from ri in db.RequisicaoItens.AsNoTracking()
                           join l in db.LotesCompra.AsNoTracking() on ri.LoteCompraId equals l.Id
                           where riIds.Contains(ri.Id)
                           select new { ri.Id, l.CompradorUsuarioId, l.CompradorNome }).ToDictionaryAsync(x => x.Id, ct);
        var prodIds = linhas.Select(l => l.pi.ProdutoId!.Value).Distinct().ToList();
        var prods = await db.Produtos.AsNoTracking().Where(p => prodIds.Contains(p.Id)).Select(p => new { p.Id, p.Grupo, p.SubGrupo }).ToDictionaryAsync(p => p.Id, ct);
        return linhas.Select(l =>
        {
            var tot = totais.GetValueOrDefault(l.Id);
            var frete = l.ValorFrete is > 0 && tot > 0 ? l.ValorFrete.Value * l.pi.ValorTotal / tot : 0;
            var unit = Math.Round((l.pi.ValorTotal + frete) / l.pi.Quantidade, 4);
            var lote = l.pi.RequisicaoItemId is { } ri ? lotes.GetValueOrDefault(ri) : null;
            var prod = prods.GetValueOrDefault(l.pi.ProdutoId!.Value);
            return new CompraPreco(l.pi.Id, l.Id, l.Numero, l.pi.ProdutoId!.Value, l.pi.Descricao, l.pi.Unidade ?? "UN", DateOnly.FromDateTime(l.CriadoEm.ToLocalTime().Date),
                l.FornecedorId, forn.GetValueOrDefault(l.FornecedorId, ""), l.ObraId, obras.GetValueOrDefault(l.ObraId, ""), l.pi.Quantidade, unit,
                l.RequisicaoId is { } rq && emerg.Contains(rq), lote?.CompradorUsuarioId, lote?.CompradorNome, prod?.Grupo, prod?.SubGrupo);
        }).ToList();
    }

    /// <summary>Avaliação das compras do período (a base olha todo o histórico anterior dos mesmos produtos).</summary>
    public static async Task<List<AvaliacaoCompra>> AvaliarPeriodoAsync(ISigeoDb db, DateOnly inicio, DateOnly fim, Guid? obra, CancellationToken ct = default)
    {
        var ini = new DateTimeOffset(inicio.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var fimDt = new DateTimeOffset(fim.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var prods = await (from pi in db.PedidoItens.AsNoTracking()
                           join p in db.Pedidos.AsNoTracking() on pi.PedidoId equals p.Id
                           where pi.ProdutoId != null && p.CriadoEm >= ini && p.CriadoEm < fimDt && p.Status != StatusPedido.Cancelado && (obra == null || p.ObraId == obra)
                           select pi.ProdutoId!.Value).Distinct().ToListAsync(ct);
        if (prods.Count == 0) return [];
        var p0 = await ParametrosServico.CarregarAsync(db, ct: ct);
        var compras = await ComprasAsync(db, prods, fim, ct);
        return EconomiaReal.Avaliar(compras, p0[Parametros.DesvioEconomiaPerc] / 100m)
            .Where(a => a.Compra.Data >= inicio && a.Compra.Data <= fim && (obra == null || a.Compra.ObraId == obra)).ToList();
    }

    public async Task<EconomiaDto> PainelAsync(DateOnly inicio, DateOnly fim, Guid? obra, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var p = await ParametrosServico.CarregarAsync(db, ct: ct);
        var limiteAumento = p[Parametros.AlertaAumentoPrecoPerc];
        // Evolução: 6 meses até o fim do período.
        var ini6 = new DateOnly(fim.Year, fim.Month, 1).AddMonths(-5);
        var todas = await AvaliarPeriodoAsync(db, ini6 < inicio ? ini6 : inicio, fim, obra, ct);
        var periodo = todas.Where(a => a.Compra.Data >= inicio).ToList();
        var metas = await db.Metas.AsNoTracking().Where(m => m.Ativa && m.Indicador == IndicadorMeta.EconomiaReal && m.Inicio <= fim && m.Fim >= inicio).ToListAsync(ct);
        decimal? metaSetor = metas.Where(m => m.Dono == DonoMeta.Setor).Select(m => (decimal?)m.Alvo).FirstOrDefault();
        LinhaEconomia L(string chave, string nome, IEnumerable<AvaliacaoCompra> g, decimal? meta)
        {
            var r = EconomiaReal.Resumir(g);
            return new LinhaEconomia(chave, nome, r.Comprado, r.BaseTotal, r.Economia, r.Percentual, meta);
        }
        var porComprador = periodo.GroupBy(a => a.Compra.CompradorId).Select(g => L(g.Key?.ToString() ?? "", g.First().Compra.Comprador ?? "Sem comprador (antes da carteira)", g,
            metas.Where(m => m.Dono == DonoMeta.Pessoa && m.UsuarioId == g.Key).Select(m => (decimal?)m.Alvo).FirstOrDefault() ?? metaSetor)).OrderByDescending(x => x.Comprado).ToList();
        var porCategoria = periodo.GroupBy(a => a.Compra.Grupo ?? "Sem grupo").Select(g => L(g.Key, g.Key, g, null)).OrderByDescending(x => x.Comprado).ToList();
        var mensal = todas.GroupBy(a => new DateOnly(a.Compra.Data.Year, a.Compra.Data.Month, 1)).OrderBy(g => g.Key)
            .Select(g => { var r = EconomiaReal.Resumir(g); return new MesEconomia(g.Key, r.Economia, r.Percentual, r.BaseTotal); }).ToList();
        var com = periodo.Where(a => a.Base == BaseEconomia.ComBase).ToList();
        var sem = periodo.Where(a => a.Base != BaseEconomia.ComBase).GroupBy(a => a.Motivo ?? "").Select(g => (g.Key, g.Count(), g.Sum(a => a.Compra.Total))).OrderByDescending(x => x.Item3).ToList();
        var aumentos = periodo.Where(a => a.PrecoBase is > 0 && a.VariacaoPerc > limiteAumento && a.Base != BaseEconomia.Emergencial).OrderByDescending(a => a.VariacaoPerc).ToList();
        return new EconomiaDto(EconomiaReal.Resumir(periodo), metaSetor, porComprador, porCategoria, mensal,
            com.Where(a => a.Economia > 0).OrderByDescending(a => a.Economia).Take(8).ToList(), com.Where(a => a.Economia < 0).OrderBy(a => a.Economia).Take(8).ToList(),
            sem, aumentos, limiteAumento);
    }

    public async Task<HistoricoDto?> HistoricoAsync(Guid produtoId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var prod = await db.Produtos.AsNoTracking().Where(p => p.Id == produtoId).Select(p => new { p.Id, p.Nome, p.Unidade, p.Grupo }).FirstOrDefaultAsync(ct);
        if (prod is null) return null;
        var compras = await ComprasAsync(db, [produtoId], null, ct);
        return new HistoricoDto(prod.Id, prod.Nome, prod.Unidade, prod.Grupo, HistoricoPreco.Calcular(compras.Where(c => c.Data >= Hoje.AddMonths(-24)), Hoje));
    }

    /// <summary>Último preço pago e aumentos seguidos por produto (aviso no mapa de cotação).</summary>
    public static async Task<Dictionary<Guid, (decimal Ultimo, int AumentosSeguidos, DateOnly Data)>> ResumoPrecosAsync(ISigeoDb db, IReadOnlyCollection<Guid> produtos, DateOnly hoje, CancellationToken ct = default)
    {
        if (produtos.Count == 0) return [];
        var compras = await ComprasAsync(db, produtos, null, ct);
        return compras.GroupBy(c => c.ProdutoId).Select(g => (g.Key, H: HistoricoPreco.Calcular(g, hoje))).Where(x => x.H.Ultimo is not null)
            .ToDictionary(x => x.Key, x => (x.H.Ultimo!.PrecoUnitario, x.H.AumentosSeguidos, x.H.Ultimo.Data));
    }
}
