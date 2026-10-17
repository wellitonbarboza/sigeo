using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

public class SolicitacaoCopiaItem : Entidade
{
    public Guid SolicitacaoCopiaId { get; set; }
    public Guid DocumentoRevisaoId { get; set; }
    public DocumentoRevisao Revisao { get; set; } = null!;
    public int Quantidade { get; set; } = 1;
    public string Destinatario { get; set; } = string.Empty;
}
