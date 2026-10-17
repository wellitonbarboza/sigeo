using Sigeo.Domain.Cadastros;

namespace Sigeo.Application.Cadastros;

/// <summary>O que a tela de categorias carrega de uma vez.</summary>
public sealed record CategoriasDados(List<CategoriaProduto> Categorias, List<MaterialControlado> Materiais, Dictionary<(string? Grupo, string? Sub), int> ProdutosPorGrupo);
