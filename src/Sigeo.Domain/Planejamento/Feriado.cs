using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>Feriado municipal/estadual ou parada da obra (os nacionais são calculados). ObraId nulo = vale para todas as obras.</summary>
public class Feriado : EntidadeAuditavel
{
    public DateOnly Data { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid? ObraId { get; set; }
}
