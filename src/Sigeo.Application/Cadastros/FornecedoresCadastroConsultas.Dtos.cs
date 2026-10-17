using Sigeo.Domain.Cadastros;

namespace Sigeo.Application.Cadastros;

/// <summary>Listas de apoio do formulário de fornecedor.</summary>
public sealed record FornecedorCadastroApoio(List<CategoriaProduto> Categorias, List<TipoDocumentoExigido> TiposDocumento);
