using Sigeo.Domain.Common;
using Sigeo.Domain.Financeiro;

namespace Sigeo.Domain.Incorporacao;

/// <summary>Alíquotas por regime e vigência (a reforma tributária muda a divisão sem mexer no código: cadastra-se nova vigência).</summary>
public class AliquotaRegime : EntidadeAuditavel
{
    public RegimeTributario Regime { get; set; }
    public DateOnly VigenciaInicio { get; set; }
    public decimal Irpj { get; set; }
    public decimal Csll { get; set; }
    public decimal Pis { get; set; }
    public decimal Cofins { get; set; }
    public decimal Cbs { get; set; }
    public decimal Ibs { get; set; }
    public string? CodigoReceita { get; set; }
    public string? Observacao { get; set; }
    public decimal Total => Irpj + Csll + Pis + Cofins + Cbs + Ibs;
}
