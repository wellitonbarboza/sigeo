using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

public class InventarioItem : Entidade
{
    public Guid InventarioId { get; set; }
    public Guid ProdutoId { get; set; }
    public decimal SaldoSistema { get; set; }
    public decimal? Contado { get; set; }
    /// <summary>Custo informado na implantação (no cíclico vale o custo médio).</summary>
    public decimal? CustoUnitario { get; set; }
    public string? Observacao { get; set; }
    public decimal Diferenca => (Contado ?? SaldoSistema) - SaldoSistema;
}
