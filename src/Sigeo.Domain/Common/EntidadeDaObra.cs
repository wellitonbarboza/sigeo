namespace Sigeo.Domain.Common;

/// <summary>Entidade vinculada a uma obra (base do filtro de acesso por obra).</summary>
public abstract class EntidadeDaObra : EntidadeAuditavel, IPertenceObra
{
    public Guid ObraId { get; set; }
}
