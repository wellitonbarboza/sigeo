namespace Sigeo.Domain.Common;

/// <summary>Base de todas as entidades. Guid v7 (sequencial) evita fragmentação de índice no Azure SQL.</summary>
public abstract class Entidade
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
}
