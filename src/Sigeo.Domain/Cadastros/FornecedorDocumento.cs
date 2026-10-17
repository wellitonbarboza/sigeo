using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Cadastros;

/// <summary>T10 · Documento do fornecedor (vencimento controlado; vencido bloqueia compra conforme o tipo).</summary>
public class FornecedorDocumento : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public Guid TipoDocumentoExigidoId { get; set; }
    public TipoDocumentoExigido TipoDocumento { get; set; } = null!;
    /// <summary>Documento mensal: 1º dia do mês de competência.</summary>
    public DateOnly? Competencia { get; set; }
    public string? Numero { get; set; }
    public DateOnly? DataEmissao { get; set; }
    public DateOnly? DataValidade { get; set; }
    public bool Bloqueia { get; set; } = true;
    /// <summary>Caminho no Blob Storage (container "documentos"). Nunca URL pública.</summary>
    public string? ArquivoBlob { get; set; }
    public string? ArquivoNomeOriginal { get; set; }
    public string? ArquivoHashSha256 { get; set; }
    public StatusDocumento Status { get; set; } = StatusDocumento.PendenteEnvio;
    /// <summary>Enviado pelo portal do fornecedor: aguarda validação de Suprimentos.</summary>
    public bool EnviadoPeloPortal { get; set; }
    public string? ResponsavelEnvio { get; set; }
    public string? MotivoRecusa { get; set; }
    /// <summary>Itens conferidos na validação (CNPJ confere, datas conferem, legível, autenticidade consultada).</summary>
    public string? Conferencia { get; set; }
    public Guid? ValidadoPorUsuarioId { get; set; }
    public DateTimeOffset? ValidadoEm { get; set; }
    public string? Observacao { get; set; }
    /// <summary>Versão anterior deste documento (nova versão nunca apaga a anterior).</summary>
    public Guid? SubstituiDocumentoId { get; set; }
}
