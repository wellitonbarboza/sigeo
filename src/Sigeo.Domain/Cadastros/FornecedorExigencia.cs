using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Cadastros;

/// <summary>Documento exigido deste fornecedor (da matriz de exigências, ajustável no pré-cadastro).</summary>
public class FornecedorExigencia : Entidade
{
    public Guid FornecedorId { get; set; }
    public Guid TipoDocumentoExigidoId { get; set; }
    public bool Obrigatorio { get; set; } = true;
}
