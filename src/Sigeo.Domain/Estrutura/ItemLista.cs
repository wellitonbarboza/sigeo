using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>Item de lista configurável (Atividade, Categoria de serviço, Critério de qualificação, Processo/Área…).</summary>
public class ItemLista : EntidadeAuditavel
{
    public TipoLista Tipo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public bool Ativo { get; set; } = true;
}
