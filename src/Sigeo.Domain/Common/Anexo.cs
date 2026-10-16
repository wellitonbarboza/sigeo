namespace Sigeo.Domain.Common;

/// <summary>
/// Anexo genérico (proposta, contrato assinado, evidência de qualificação, foto de FVM, ata…).
/// Arquivo no Blob Storage (nunca URL pública); hash SHA-256 prova que o arquivo não foi trocado.
/// </summary>
public class Anexo : EntidadeAuditavel
{
    /// <summary>Nome da entidade dona (ex.: "Requisicao", "Contrato", "FornecedorQualificacao").</summary>
    public string Entidade { get; set; } = string.Empty;
    public Guid EntidadeId { get; set; }
    /// <summary>Ex.: Escopo, Proposta, ContratoAssinado, Evidencia, Foto, Ata, NotaFiscal.</summary>
    public string Categoria { get; set; } = "Geral";
    public string NomeOriginal { get; set; } = string.Empty;
    public string ArquivoBlob { get; set; } = string.Empty;
    public string ArquivoHashSha256 { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long TamanhoBytes { get; set; }
}

/// <summary>Numeração sequencial por empresa e chave (ex.: "Requisicao", "Contrato-2026", "Copia-P.SUP.01-R01").</summary>
public class Sequencia : Entidade, IPertenceEmpresa
{
    public Guid EmpresaId { get; set; }
    public string Chave { get; set; } = string.Empty;
    public int Ultimo { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public int Proximo() => ++Ultimo;
}
