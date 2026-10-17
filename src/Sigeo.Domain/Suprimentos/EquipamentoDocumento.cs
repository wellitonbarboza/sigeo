using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

public class EquipamentoDocumento : Entidade
{
    public Guid EquipamentoId { get; set; }
    /// <summary>Ex.: Laudo NR-12, ART de montagem, seguro.</summary>
    public string Nome { get; set; } = string.Empty;
    public DateOnly? Vencimento { get; set; }
    public bool Bloqueia { get; set; } = true;
}
