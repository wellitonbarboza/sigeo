using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Cadastros;

public class LinkPortalFornecedor : EntidadeAuditavel
{
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public string Codigo { get; set; } = string.Empty;           // ex.: SOL-0231
    /// <summary>Token aleatório (256 bits, base64url). Só o hash é consultado na validação.</summary>
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ValidoAte { get; set; }
    public bool Revogado { get; set; }
    public DateTimeOffset? UltimoAcessoEm { get; set; }
    public string? UltimoAcessoIp { get; set; }
    public string? EmailDestino { get; set; }
    public string Motivo { get; set; } = "Cadastro inicial";
    public string? ItensSolicitados { get; set; }
    public string? EnviadoPor { get; set; }
    public StatusSolicitacaoDoc Status { get; set; } = StatusSolicitacaoDoc.Enviada;

    public bool Valido(DateTimeOffset agora) => !Revogado && !Excluido && agora <= ValidoAte;
}
