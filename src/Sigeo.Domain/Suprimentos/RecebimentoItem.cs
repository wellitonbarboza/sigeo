using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

public class RecebimentoItem : Entidade
{
    public Guid RecebimentoId { get; set; }
    public Recebimento Recebimento { get; set; } = null!;
    public Guid? PedidoItemId { get; set; }
    /// <summary>Produto (vem do item do pedido): é ele que entra no estoque.</summary>
    public Guid? ProdutoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Unidade { get; set; }
    public decimal QtdPedida { get; set; }
    public decimal QtdRecebida { get; set; }
    public bool MaterialControlado { get; set; }
    public Guid? MaterialControladoId { get; set; }
    public string? Lote { get; set; }
    /// <summary>Marca recebida (confere com a do pedido; diferente é divergência).</summary>
    public string? Marca { get; set; }
    public bool DocumentosRecebidos { get; set; }
}
