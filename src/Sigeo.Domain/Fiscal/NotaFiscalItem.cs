using System.Globalization;
using System.Text;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Fiscal;

public class NotaFiscalItem : Entidade
{
    public Guid NotaFiscalId { get; set; }
    public int Item { get; set; }
    public string? Codigo { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Ncm { get; set; }
    public string? Cfop { get; set; }
    public string? Unidade { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Valor { get; set; }
    /// <summary>xPed do item (nº do pedido do cliente) e nItemPed, quando o fornecedor informa.</summary>
    public string? PedidoCitado { get; set; }
    public string? ItemPedidoCitado { get; set; }
    public Guid? PedidoItemId { get; set; }
    public OrigemVinculo? VinculoOrigem { get; set; }
    /// <summary>0–1: semelhança da descrição com o item do pedido (regra) ou confiança informada pela IA.</summary>
    public decimal? Confianca { get; set; }
}
