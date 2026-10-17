using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Colaboracao;

/// <summary>"Lido por quem e quando": primeira leitura de cada pessoa em cada mensagem (evidência).</summary>
public class LeituraMensagem : Entidade
{
    public Guid MensagemId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTimeOffset LidaEm { get; set; }
}
