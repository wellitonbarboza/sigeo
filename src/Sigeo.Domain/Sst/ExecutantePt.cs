using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

public class ExecutantePt : Entidade
{
    public Guid PermissaoTrabalhoId { get; set; }
    public Guid ColaboradorId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public PapelPt Papel { get; set; } = PapelPt.Executante;
    public bool Liberado { get; set; }
    public string? Motivo { get; set; }
}
