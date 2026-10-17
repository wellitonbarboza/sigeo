using Sigeo.Domain.Common;
using Sigeo.Domain.Aprovacoes;

namespace Sigeo.Domain.Colaboracao;

public class TarefaProrrogacao : Entidade
{
    public Guid TarefaId { get; set; }
    public Tarefa Tarefa { get; set; } = null!;
    public DateOnly PrazoAnterior { get; set; }
    public DateOnly NovoPrazo { get; set; }
    public int Dias { get; set; }
    public string Justificativa { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public DateTimeOffset Em { get; set; } = DateTimeOffset.UtcNow;
}
