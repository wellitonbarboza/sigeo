using QualyHub.Domain.Common;

namespace QualyHub.Domain.Sgq;

/// <summary>Documento do SGQ na lista mestra (MQ, PO, PEIS, DS, FORM, PQO).</summary>
public class DocumentoSgq : EntidadeAuditavel, IHistoricoTemporal
{
    public TipoDocumentoSgq Tipo { get; set; }
    public string Codigo { get; set; } = string.Empty;        // ex.: PEIS-07
    public string Titulo { get; set; } = string.Empty;
    public string? ProcessoResponsavel { get; set; }
    /// <summary>Nulo = documento corporativo; preenchido = específico da obra (ex.: PQO).</summary>
    public Guid? ObraId { get; set; }
    public int RevisaoVigente { get; set; }
    public StatusDocumentoSgq Status { get; set; } = StatusDocumentoSgq.EmElaboracao;
    public int? PrazoRetencaoAnos { get; set; }
    public ICollection<VersaoDocumentoSgq> Versoes { get; set; } = [];
}

/// <summary>Cada revisão é imutável após aprovada. Revisões anteriores viram "Obsoleto" automaticamente.</summary>
public class VersaoDocumentoSgq : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid DocumentoSgqId { get; set; }
    public DocumentoSgq Documento { get; set; } = null!;
    public int Revisao { get; set; }
    public string DescricaoAlteracao { get; set; } = string.Empty;
    public string ArquivoBlob { get; set; } = string.Empty;
    public string ArquivoHashSha256 { get; set; } = string.Empty;
    public string? ElaboradoPor { get; set; }
    public string? AnalisadoPor { get; set; }
    public string? AprovadoPor { get; set; }
    public DateTimeOffset? AprovadoEm { get; set; }
    public StatusDocumentoSgq Status { get; set; } = StatusDocumentoSgq.EmElaboracao;
    public ICollection<DistribuicaoDocumento> Distribuicoes { get; set; } = [];
}

/// <summary>Controle de cópias distribuídas (inclusive impressas no canteiro) para recolher obsoletos.</summary>
public class DistribuicaoDocumento : Entidade
{
    public Guid VersaoDocumentoSgqId { get; set; }
    public Guid? ObraId { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public bool CopiaImpressa { get; set; }
    public DateTimeOffset DistribuidoEm { get; set; }
    public DateTimeOffset? RecolhidoEm { get; set; }
}
