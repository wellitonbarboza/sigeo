using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>Equipamento no dia: horas trabalhadas e se ficou parado.</summary>
public class DiarioEquipamento : Entidade
{
    public Guid DiarioId { get; set; }
    public Guid EquipamentoId { get; set; }
    public decimal Horas { get; set; }
    public bool Parado { get; set; }
    public string? Observacao { get; set; }
}
