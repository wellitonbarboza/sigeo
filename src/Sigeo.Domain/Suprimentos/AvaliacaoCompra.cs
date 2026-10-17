namespace Sigeo.Domain.Suprimentos;

/// <summary>Resultado da compra contra o preço de base (último preço pago válido do mesmo produto e unidade).</summary>
public sealed record AvaliacaoCompra(CompraPreco Compra, BaseEconomia Base, decimal? PrecoBase, CompraPreco? CompraBase, string? Motivo)
{
    /// <summary>Positivo = pagou menos que a base; negativo = pagou mais (entra no total, para o número ser honesto).</summary>
    public decimal Economia => Base == BaseEconomia.ComBase && PrecoBase is { } b ? Math.Round((b - Compra.PrecoUnitario) * Compra.Quantidade, 2) : 0;
    public decimal ValorBase => Base == BaseEconomia.ComBase && PrecoBase is { } b ? Math.Round(b * Compra.Quantidade, 2) : 0;
    public decimal? VariacaoPerc => PrecoBase is { } b && b > 0 ? Math.Round((Compra.PrecoUnitario - b) / b * 100, 1) : null;
}
