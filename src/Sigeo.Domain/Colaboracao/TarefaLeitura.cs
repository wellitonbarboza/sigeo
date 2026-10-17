using Sigeo.Domain.Common;
using Sigeo.Domain.Aprovacoes;

namespace Sigeo.Domain.Colaboracao;

public class TarefaLeitura : Entidade
{
    public Guid TarefaId { get; set; }
    public Tarefa Tarefa { get; set; } = null!;
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTimeOffset LidaEm { get; set; }
}
