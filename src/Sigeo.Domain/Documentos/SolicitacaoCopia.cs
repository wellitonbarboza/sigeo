using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

/// <summary>D05 · Solicitação de cópia (liberação pela Qualidade gera as cópias numeradas).</summary>
public class SolicitacaoCopia : EntidadeAuditavel
{
    public int Numero { get; set; }
    public Guid SolicitadoPorUsuarioId { get; set; }
    public string SolicitadoPor { get; set; } = string.Empty;
    public DateTimeOffset SolicitadoEm { get; set; }
    public Guid? ObraId { get; set; }
    public string Copiadora { get; set; } = string.Empty;
    public DateOnly Programacao { get; set; }
    public string? LiberadoPor { get; set; }
    public DateTimeOffset? LiberadoEm { get; set; }
    public StatusSolicitacaoCopia Status { get; set; } = StatusSolicitacaoCopia.Solicitada;
    public string? MotivoRecusa { get; set; }
    public ICollection<SolicitacaoCopiaItem> Itens { get; set; } = [];

    public int QtdTotal => Itens.Sum(i => i.Quantidade);
}
