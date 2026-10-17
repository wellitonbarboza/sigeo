using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>Local incluído na rede (apartamento, pavimento ou área comum), na ordem de ataque.</summary>
public class RedeLocal : Entidade
{
    public Guid RedeId { get; set; }
    public Guid LocalId { get; set; }
    public int Ordem { get; set; }
}
