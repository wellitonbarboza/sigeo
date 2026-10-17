using Sigeo.Domain.Common;

namespace Sigeo.Domain.Engenharia;

public class EquipeMembro : Entidade
{
    public Guid EquipeId { get; set; }
    public Guid ColaboradorId { get; set; }
    public DateOnly Desde { get; set; }
    public DateOnly? Ate { get; set; }
}
