using Sigeo.Domain.Common;
using Sigeo.Domain.Financeiro;

namespace Sigeo.Domain.Incorporacao;

/// <summary>Comissão da venda (corretor, imobiliária, gerente). Vira título a pagar: à vista, parcelada ou quando o cliente pagar X%.</summary>
public class ComissaoVenda : Entidade
{
    public Guid ContratoVendaId { get; set; }
    public Guid FornecedorId { get; set; }
    public string Papel { get; set; } = "Corretor";
    public decimal Percentual { get; set; }
    public decimal Valor { get; set; }
    public FormaComissao Forma { get; set; } = FormaComissao.AVista;
    public int Parcelas { get; set; } = 1;
    public decimal? PercentualPagoMinimo { get; set; }
    public Guid? TituloId { get; set; }
    public bool Estornada { get; set; }
}
