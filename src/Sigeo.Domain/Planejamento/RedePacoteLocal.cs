using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>Ajuste manual: pacote incluído/excluído num local (sobrepõe o padrão vindo do orçamento).</summary>
public class RedePacoteLocal : Entidade
{
    public Guid PacoteId { get; set; }
    public Guid LocalId { get; set; }
    public bool Incluido { get; set; }
}
