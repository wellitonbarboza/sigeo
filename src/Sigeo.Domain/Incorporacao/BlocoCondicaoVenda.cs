using Sigeo.Domain.Common;
using Sigeo.Domain.Financeiro;

namespace Sigeo.Domain.Incorporacao;

/// <summary>Bloco da condição de venda: N parcelas de mesmo valor a cada M meses a partir do 1º vencimento.</summary>
public class BlocoCondicaoVenda : Entidade
{
    public Guid ContratoVendaId { get; set; }
    public TipoParcelaVenda Tipo { get; set; } = TipoParcelaVenda.Mensal;
    public int Quantidade { get; set; } = 1;
    public decimal ValorParcela { get; set; }
    public DateOnly PrimeiroVencimento { get; set; }
    /// <summary>Meses entre parcelas (1 mensal, 6 semestral, 12 anual).</summary>
    public int IntervaloMeses { get; set; } = 1;
    public Guid? FornecedorPermutaId { get; set; }
    public decimal Total => Quantidade * ValorParcela;

    public IEnumerable<(DateOnly Vencimento, decimal Valor)> Parcelas() =>
        Enumerable.Range(0, Math.Max(Quantidade, 0)).Select(i => (PrimeiroVencimento.AddMonths(i * Math.Max(IntervaloMeses, 1)), ValorParcela));
}
