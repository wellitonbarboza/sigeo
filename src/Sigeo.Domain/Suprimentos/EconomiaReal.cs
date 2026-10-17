namespace Sigeo.Domain.Suprimentos;

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
