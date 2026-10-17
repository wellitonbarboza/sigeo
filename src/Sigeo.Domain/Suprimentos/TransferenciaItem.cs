using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

public class TransferenciaItem : Entidade
{
    public Guid TransferenciaId { get; set; }
    public Guid? ProdutoId { get; set; }
    public Guid? RecebimentoItemId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Unidade { get; set; }
    public decimal Quantidade { get; set; }
    public decimal? QuantidadeRecebida { get; set; }
    public decimal CustoUnitario { get; set; }
    public string? Lote { get; set; }
    /// <summary>Local de aplicação previsto (opcional).</summary>
    public Guid? LocalId { get; set; }
    public decimal Valor => Math.Round(Quantidade * CustoUnitario, 2);
}
