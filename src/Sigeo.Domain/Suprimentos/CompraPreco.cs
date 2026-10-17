namespace Sigeo.Domain.Suprimentos;

/// <summary>Uma compra (item de pedido) para o histórico de preço e a economia real. Preço unitário já com o frete rateado.</summary>
public sealed record CompraPreco(
    Guid PedidoItemId, Guid PedidoId, int PedidoNumero, Guid ProdutoId, string Descricao, string Unidade, DateOnly Data,
    Guid FornecedorId, string Fornecedor, Guid ObraId, string Obra, decimal Quantidade, decimal PrecoUnitario, bool Emergencial,
    Guid? CompradorId = null, string? Comprador = null, string? Grupo = null, string? SubGrupo = null)
{
    public decimal Total => Math.Round(Quantidade * PrecoUnitario, 2);
}
