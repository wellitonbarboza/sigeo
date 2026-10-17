using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

public class GrdItem : Entidade
{
    public Guid GrdId { get; set; }
    public Guid DocumentoRevisaoId { get; set; }
    public DocumentoRevisao Revisao { get; set; } = null!;
}
