using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Colaboracao;

/// <summary>Quem participa da conversa e o estado dela para essa pessoa (arquivada, até quando leu).</summary>
public class ParticipanteConversa : EntidadeAuditavel
{
    public Guid ConversaId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public PapelParticipante Papel { get; set; }
    public bool Arquivada { get; set; }
    /// <summary>Até quando a pessoa leu (mensagens enviadas depois disso contam como não lidas).</summary>
    public DateTimeOffset? LidaAte { get; set; }
}
