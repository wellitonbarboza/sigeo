using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>Quais obras cada usuário enxerga (base do filtro de acesso por obra).</summary>
public class UsuarioObra : Entidade
{
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public Guid ObraId { get; set; }
    public Obra Obra { get; set; } = null!;
}
