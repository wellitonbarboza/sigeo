using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Planejamento;

namespace Sigeo.Application.Planejamento;

/// <summary>Dados do editor de redes; <see cref="Custo"/> é o custo orçado (composição, local) já somado na subárvore.</summary>
public sealed record EditorRedesDados(List<RedePlanejamento> Redes, List<VinculoRede> Vinculos, Func<Guid, Guid, decimal> Custo,
    List<Local> Locais, List<Guid> ComposicoesUsadas, List<EditorRedesComposicao> Composicoes);

public sealed record EditorRedesComposicao(Guid Id, string Codigo, string Descricao);
