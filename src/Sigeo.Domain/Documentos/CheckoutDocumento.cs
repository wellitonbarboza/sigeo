using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

/// <summary>D08 · Check-out / bloqueio (impede edição simultânea).</summary>
public class CheckoutDocumento : EntidadeAuditavel
{
    public Guid DocumentoRevisaoId { get; set; }
    public Guid UsuarioId { get; set; }
    public string UsuarioNome { get; set; } = string.Empty;
    public DateTimeOffset CheckoutEm { get; set; }
    public DateOnly Prazo { get; set; }
    public DateTimeOffset? CheckinEm { get; set; }
    public string? LiberadoForcadoPor { get; set; }

    public bool Ativo => CheckinEm is null && LiberadoForcadoPor is null;
}
