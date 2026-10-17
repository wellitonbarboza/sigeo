using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

public class DocumentoRelacionado : Entidade
{
    public Guid DocumentoId { get; set; }
    public Guid RelacionadoId { get; set; }
}
