using Sigeo.Domain.Common;
using Sigeo.Domain.Financeiro;

namespace Sigeo.Domain.Incorporacao;

/// <summary>Apuração mensal do RET da incorporação (regime de caixa: receita recebida no mês).</summary>
public class ApuracaoRet : EntidadeAuditavel
{
    public Guid IncorporacaoId { get; set; }
    public Guid ObraId { get; set; }
    public DateOnly Competencia { get; set; }
    public RegimeTributario Regime { get; set; }
    public decimal ReceitaRecebida { get; set; }
    public decimal Irpj { get; set; }
    public decimal Csll { get; set; }
    public decimal Pis { get; set; }
    public decimal Cofins { get; set; }
    public decimal Cbs { get; set; }
    public decimal Ibs { get; set; }
    public decimal AliquotaTotal { get; set; }
    public Guid? TituloId { get; set; }
    public decimal Total => Irpj + Csll + Pis + Cofins + Cbs + Ibs;
}
