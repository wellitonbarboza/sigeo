using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Colaboracao;

/// <summary>Mensagem enviada dentro da conversa. Imutável: correção = nova mensagem.</summary>
public class MensagemConversa : Entidade
{
    public Guid ConversaId { get; set; }
    public Guid AutorId { get; set; }
    public string AutorNome { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public DateTimeOffset EnviadaEm { get; set; }
    public ICollection<LeituraMensagem> Leituras { get; set; } = [];
}
