using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

public class Participante : Entidade
{
    public Guid TurmaId { get; set; }
    public Guid ColaboradorId { get; set; }
    public bool Presenca { get; set; }
    public string? Avaliacao { get; set; }
    public ResultadoParticipante? Resultado { get; set; }
    public DateOnly? ValidoAte { get; set; }
}
