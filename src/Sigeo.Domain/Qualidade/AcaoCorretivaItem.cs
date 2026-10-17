using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

public class AcaoCorretivaItem : Entidade
{
    public Guid AcaoCorretivaId { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public DateOnly Prazo { get; set; }
    public StatusItemPlano Status { get; set; } = StatusItemPlano.Pendente;
    public Guid? DocumentoId { get; set; }
    public string? Resultado { get; set; }
}
