using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>Atividade executada no dia: descrição, local opcional; as fotos são anexos ("DiarioAtividade", Id).</summary>
public class DiarioAtividade : Entidade
{
    public Guid DiarioId { get; set; }
    public int Ordem { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public Guid? LocalId { get; set; }
    public string? AtividadeChave { get; set; }
}
