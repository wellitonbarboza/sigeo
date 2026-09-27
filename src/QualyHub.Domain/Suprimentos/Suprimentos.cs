using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;

namespace QualyHub.Domain.Suprimentos;

/// <summary>Pedido de compra importado do Obra Prima. O QualyHub aprova; o Obra Prima continua sendo o ERP.</summary>
public class Pedido : EntidadeDaObra, IOrigemObraPrima, IHistoricoTemporal
{
    public string Numero { get; set; } = string.Empty;
    public DateOnly DataPedido { get; set; }
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public decimal ValorTotal { get; set; }
    public DateOnly? PrevisaoEntrega { get; set; }
    public string? Solicitante { get; set; }
    public StatusAprovacao Status { get; set; } = StatusAprovacao.Rascunho;
    /// <summary>Motivos de bloqueio calculados pelas regras (fornecedor não qualificado, documento vencido...).</summary>
    public string? Bloqueios { get; set; }
    public string? CodigoObraPrima { get; set; }
    public Guid? UltimaImportacaoId { get; set; }

    public ICollection<PedidoItem> Itens { get; set; } = [];
}

public class PedidoItem : Entidade
{
    public Guid PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;
    public string CodigoInsumo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Unidade { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    /// <summary>Preenchido na importação quando o insumo casa com um material controlado.</summary>
    public Guid? MaterialControladoId { get; set; }
    public MaterialControlado? MaterialControlado { get; set; }
    public decimal QuantidadeRecebidaAceita { get; set; }
}
