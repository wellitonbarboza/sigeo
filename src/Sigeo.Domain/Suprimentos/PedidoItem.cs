using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

public class PedidoItem : Entidade
{
    public Guid PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;
    public Guid? RequisicaoItemId { get; set; }
    public Guid? ProdutoId { get; set; }
    public string CodigoInsumo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Unidade { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    public Guid? MaterialControladoId { get; set; }
    public MaterialControlado? MaterialControlado { get; set; }
    public string? EspecificacaoTecnica { get; set; }
    public string? DocumentosExigidos { get; set; }
    /// <summary>Marca comprada (vem da cotação vencedora).</summary>
    public string? Marca { get; set; }
    public decimal QuantidadeRecebidaAceita { get; set; }
}
