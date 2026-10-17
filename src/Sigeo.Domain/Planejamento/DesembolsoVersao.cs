using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>Desembolso mensal da versão (custo orçado distribuído pelos dias úteis de cada atividade), gravado na aprovação.</summary>
public class DesembolsoVersao : Entidade
{
    public Guid VersaoId { get; set; }
    public DateOnly Mes { get; set; }
    public decimal Valor { get; set; }
    public decimal ValorBase { get; set; }
}
