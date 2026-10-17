using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

public class Constatacao : Entidade
{
    public Guid AuditoriaInternaId { get; set; }
    public int Ordem { get; set; }
    public string Requisito { get; set; } = string.Empty;
    public string Pergunta { get; set; } = string.Empty;
    public string? Evidencia { get; set; }
    public TipoConstatacao Tipo { get; set; }
    public Guid? RncId { get; set; }
}
