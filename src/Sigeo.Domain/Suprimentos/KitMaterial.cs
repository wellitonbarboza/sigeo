using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>
/// Kit de material. Automático: definido por etapa (grupo do serviço) e/ou disciplina — as quantidades saem do Orçamento por Local
/// de cada apartamento na hora da entrega. Manual: lista fixa (EPI de admissão, kit de ferramentas…).
/// </summary>
public class KitMaterial : EntidadeAuditavel
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public OrigemKit Origem { get; set; } = OrigemKit.Automatico;
    /// <summary>Obra do kit automático (o orçamento é da obra). Kit manual pode valer para todas.</summary>
    public Guid? ObraId { get; set; }
    /// <summary>Etapa = grupo do serviço (EAP).</summary>
    public string? Etapa { get; set; }
    public string? Disciplina { get; set; }
    public bool Ativo { get; set; } = true;
    public ICollection<KitMaterialItem> Itens { get; set; } = [];

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Nome)) e.Add("Informe o nome do kit.");
        if (Origem == OrigemKit.Automatico && ObraId is null) e.Add("Kit automático: informe a obra (o kit sai do orçamento dela).");
        if (Origem == OrigemKit.Automatico && string.IsNullOrWhiteSpace(Etapa) && string.IsNullOrWhiteSpace(Disciplina)) e.Add("Kit automático: informe a etapa e/ou a disciplina.");
        if (Origem == OrigemKit.Manual && Itens.Count == 0) e.Add("Kit manual: inclua os itens.");
        if (Itens.Any(i => i.Quantidade <= 0)) e.Add("Quantidade dos itens deve ser maior que zero.");
        if (Itens.GroupBy(i => i.ProdutoId).Any(g => g.Count() > 1)) e.Add("Produto repetido no kit.");
        return e;
    }
}
