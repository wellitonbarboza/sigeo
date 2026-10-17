using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

/// <summary>D12 · GRD – guia de remessa de documentos a terceiros (cria cópias externas; confirmação por link).</summary>
public class Grd : EntidadeAuditavel
{
    public string Numero { get; set; } = string.Empty;
    public Guid FornecedorId { get; set; }
    public string? Contato { get; set; }
    public Guid? ContratoId { get; set; }
    public Guid? PedidoId { get; set; }
    public DateTimeOffset? EnviadoEm { get; set; }
    public string? EnviadoPor { get; set; }
    public Guid TokenConfirmacao { get; set; } = Guid.NewGuid();
    public DateTimeOffset? ConfirmadoEm { get; set; }
    public ICollection<GrdItem> Itens { get; set; } = [];
}
