using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Cadastros;

/// <summary>Link seguro do portal do fornecedor (tela 24): envio de documentos sem login, com prazo e registro de IP.</summary>
/// <summary>Grupo do catálogo atendido pelo fornecedor. Subgrupos vazios = todos.</summary>
public class FornecedorGrupo : Entidade
{
    public Guid FornecedorId { get; set; }
    public Guid CategoriaProdutoId { get; set; }
    /// <summary>Ids dos subgrupos atendidos, separados por vírgula (vazio = todos).</summary>
    public string? Subgrupos { get; set; }
    public bool Todos => string.IsNullOrWhiteSpace(Subgrupos);
}
