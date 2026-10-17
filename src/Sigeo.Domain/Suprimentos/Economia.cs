namespace Sigeo.Domain.Suprimentos;

/// <summary>Uma compra (item de pedido) para o histórico de preço e a economia real. Preço unitário já com o frete rateado.</summary>
public sealed record CompraPreco(
    Guid PedidoItemId, Guid PedidoId, int PedidoNumero, Guid ProdutoId, string Descricao, string Unidade, DateOnly Data,
    Guid FornecedorId, string Fornecedor, Guid ObraId, string Obra, decimal Quantidade, decimal PrecoUnitario, bool Emergencial,
    Guid? CompradorId = null, string? Comprador = null, string? Grupo = null, string? SubGrupo = null)
{
    public decimal Total => Math.Round(Quantidade * PrecoUnitario, 2);
}

public enum BaseEconomia { ComBase = 1, SemBase = 2, Emergencial = 3, Desvio = 4 }

/// <summary>Resultado da compra contra o preço de base (último preço pago válido do mesmo produto e unidade).</summary>
public sealed record AvaliacaoCompra(CompraPreco Compra, BaseEconomia Base, decimal? PrecoBase, CompraPreco? CompraBase, string? Motivo)
{
    /// <summary>Positivo = pagou menos que a base; negativo = pagou mais (entra no total, para o número ser honesto).</summary>
    public decimal Economia => Base == BaseEconomia.ComBase && PrecoBase is { } b ? Math.Round((b - Compra.PrecoUnitario) * Compra.Quantidade, 2) : 0;
    public decimal ValorBase => Base == BaseEconomia.ComBase && PrecoBase is { } b ? Math.Round(b * Compra.Quantidade, 2) : 0;
    public decimal? VariacaoPerc => PrecoBase is { } b && b > 0 ? Math.Round((Compra.PrecoUnitario - b) / b * 100, 1) : null;
}

/// <summary>
/// Economia real: quanto se pagou a menos que o último preço pago do próprio item (mesma unidade), sem correção por índice.
/// Fora da base: compra emergencial e desvio acima do limite (padrão 40%) — e nenhuma das duas vira base da próxima.
/// </summary>
public static class EconomiaReal
{
    public const decimal LimiteDesvioPadrao = 0.40m;

    private static string U(string? u) => (u ?? "").Trim().ToUpperInvariant();

    public static List<AvaliacaoCompra> Avaliar(IEnumerable<CompraPreco> compras, decimal limiteDesvio = LimiteDesvioPadrao)
    {
        var r = new List<AvaliacaoCompra>();
        foreach (var g in compras.GroupBy(c => c.ProdutoId))
        {
            var validas = new List<CompraPreco>();
            var todas = new List<CompraPreco>();
            foreach (var c in g.OrderBy(c => c.Data).ThenBy(c => c.PedidoNumero).ThenBy(c => c.PedidoItemId))
            {
                var anterior = validas.LastOrDefault(v => U(v.Unidade) == U(c.Unidade) && v.PedidoId != c.PedidoId);
                if (c.Emergencial) r.Add(new(c, BaseEconomia.Emergencial, anterior?.PrecoUnitario, anterior, "Compra emergencial"));
                else if (anterior is null)
                    r.Add(new(c, BaseEconomia.SemBase, null, null, todas.Any(t => U(t.Unidade) != U(c.Unidade)) ? "Unidade diferente das compras anteriores" : "Primeira compra do item"));
                else if (anterior.PrecoUnitario > 0 && Math.Abs(c.PrecoUnitario - anterior.PrecoUnitario) / anterior.PrecoUnitario > limiteDesvio)
                    r.Add(new(c, BaseEconomia.Desvio, anterior.PrecoUnitario, anterior, $"Diferença acima de {limiteDesvio:P0} do último preço"));
                else { r.Add(new(c, BaseEconomia.ComBase, anterior.PrecoUnitario, anterior, null)); }
                todas.Add(c);
                if (!c.Emergencial && r[^1].Base != BaseEconomia.Desvio) validas.Add(c);
            }
        }
        return r;
    }

    public sealed record Resumo(decimal Comprado, decimal CompradoComBase, decimal BaseTotal, decimal Economia, decimal Ganhos, decimal Perdas, int SemBase, decimal ValorSemBase)
    {
        public decimal Percentual => BaseTotal == 0 ? 0 : Math.Round(Economia / BaseTotal * 100, 2);
        public decimal Cobertura => Comprado == 0 ? 0 : Math.Round(CompradoComBase / Comprado * 100, 1);
    }

    public static Resumo Resumir(IEnumerable<AvaliacaoCompra> l)
    {
        var lista = l.ToList();
        var com = lista.Where(a => a.Base == BaseEconomia.ComBase).ToList();
        var sem = lista.Where(a => a.Base != BaseEconomia.ComBase).ToList();
        return new Resumo(lista.Sum(a => a.Compra.Total), com.Sum(a => a.Compra.Total), com.Sum(a => a.ValorBase), com.Sum(a => a.Economia),
            com.Where(a => a.Economia > 0).Sum(a => a.Economia), com.Where(a => a.Economia < 0).Sum(a => a.Economia), sem.Count, sem.Sum(a => a.Compra.Total));
    }
}

/// <summary>Histórico de preço de um produto: último preço, alterações (aumentos e reduções) e resumo do período.</summary>
public static class HistoricoPreco
{
    public sealed record Linha(CompraPreco Compra, decimal? Anterior, decimal? Variacao, decimal? VariacaoPerc)
    {
        public bool Aumento => Variacao > 0;
        public bool Reducao => Variacao < 0;
    }

    public sealed record Resultado(IReadOnlyList<Linha> Linhas, CompraPreco? Ultimo, decimal? Variacao6m, decimal? Variacao12m,
        Linha? MaiorAumento, int AumentosSeguidos, decimal? Media6m);

    /// <summary>
    /// Linhas em ordem de data (mais antiga primeiro). Variação = contra a compra anterior na mesma unidade, sem as emergenciais.
    /// Aumentos seguidos = quantos aumentos consecutivos terminam na última compra.
    /// </summary>
    public static Resultado Calcular(IEnumerable<CompraPreco> compras, DateOnly hoje)
    {
        var ordem = compras.OrderBy(c => c.Data).ThenBy(c => c.PedidoNumero).ToList();
        var linhas = new List<Linha>();
        var porUnidade = new Dictionary<string, CompraPreco>();
        foreach (var c in ordem)
        {
            var u = c.Unidade.Trim().ToUpperInvariant();
            if (c.Emergencial) { linhas.Add(new(c, porUnidade.GetValueOrDefault(u)?.PrecoUnitario, null, null)); continue; }
            var ant = porUnidade.GetValueOrDefault(u);
            decimal? v = ant is null ? null : Math.Round(c.PrecoUnitario - ant.PrecoUnitario, 4);
            decimal? vp = ant is { PrecoUnitario: > 0 } ? Math.Round((c.PrecoUnitario - ant.PrecoUnitario) / ant.PrecoUnitario * 100, 1) : null;
            linhas.Add(new(c, ant?.PrecoUnitario, v, vp));
            porUnidade[u] = c;
        }
        var validas = linhas.Where(l => !l.Compra.Emergencial).ToList();
        var ultimo = validas.LastOrDefault()?.Compra;
        decimal? Var(int meses)
        {
            if (ultimo is null) return null;
            var desde = hoje.AddMonths(-meses);
            var primeira = validas.FirstOrDefault(l => l.Compra.Data >= desde && l.Compra.Unidade.Trim().ToUpperInvariant() == ultimo.Unidade.Trim().ToUpperInvariant())?.Compra;
            return primeira is null || primeira.PedidoItemId == ultimo.PedidoItemId || primeira.PrecoUnitario <= 0 ? null
                : Math.Round((ultimo.PrecoUnitario - primeira.PrecoUnitario) / primeira.PrecoUnitario * 100, 1);
        }
        var seguidos = 0;
        for (var i = validas.Count - 1; i >= 0 && validas[i].Aumento; i--) seguidos++;
        var seis = validas.Where(l => l.Compra.Data >= hoje.AddMonths(-6) && ultimo is not null && l.Compra.Unidade.Trim().ToUpperInvariant() == ultimo.Unidade.Trim().ToUpperInvariant()).ToList();
        var qtd = seis.Sum(l => l.Compra.Quantidade);
        decimal? media = qtd <= 0 ? null : Math.Round(seis.Sum(l => l.Compra.PrecoUnitario * l.Compra.Quantidade) / qtd, 4);
        var maior = validas.Where(l => l.Aumento && l.Compra.Data >= hoje.AddMonths(-12)).OrderByDescending(l => l.VariacaoPerc).FirstOrDefault();
        return new Resultado(linhas, ultimo, Var(6), Var(12), maior, seguidos, media);
    }
}
