using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

/// <summary>D04 · Cópia controlada numerada (física, eletrônica ou externa via GRD), com QR de vigência.</summary>
public class CopiaControlada : EntidadeAuditavel, IHistoricoTemporal
{
    /// <summary>&lt;CÓDIGO&gt;-R&lt;nn&gt;-C&lt;nnn&gt;</summary>
    public string NumeroCopia { get; set; } = string.Empty;
    public Guid DocumentoRevisaoId { get; set; }
    public DocumentoRevisao Revisao { get; set; } = null!;
    public TipoCopia TipoCopia { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public Guid? ObraId { get; set; }
    public Guid? SolicitacaoCopiaId { get; set; }
    public Guid? GrdId { get; set; }
    public DateTimeOffset? EntregueEm { get; set; }
    public string? RecebidoPor { get; set; }
    public string QrToken { get; set; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
    public StatusCopia Status { get; set; } = StatusCopia.EmUso;
    public DateOnly? PrazoRecolhimento { get; set; }
    public DateTimeOffset? RecolhidaEm { get; set; }
    public string? RecolhidaPor { get; set; }
    public DestinoObsoleta? DestinoObsoleta { get; set; }

    public static string Numerar(string codigoDocumento, int revisao, int sequencial) => $"{codigoDocumento}-R{revisao:00}-C{sequencial:000}";

    public void Recolher(string por, DestinoObsoleta destino, DateTimeOffset agora)
    {
        if (Status != StatusCopia.RecolhimentoPendente && Status != StatusCopia.EmUso)
            throw new RegraDeNegocioException("Cópia já recolhida ou extraviada.");
        Status = StatusCopia.Recolhida;
        RecolhidaPor = por;
        RecolhidaEm = agora;
        DestinoObsoleta = destino;
    }
}
