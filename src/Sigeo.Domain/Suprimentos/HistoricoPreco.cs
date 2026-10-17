namespace Sigeo.Domain.Suprimentos;

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
