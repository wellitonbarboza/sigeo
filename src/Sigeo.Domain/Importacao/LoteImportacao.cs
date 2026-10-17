using Sigeo.Domain.Common;

namespace Sigeo.Domain.Importacao;

/// <summary>Lote de importação: staging → validação → prévia → confirmação. Nada entra no cadastro sem confirmação.</summary>
public class LoteImportacao : EntidadeAuditavel
{
    public TipoImportacao Tipo { get; set; }
    public Guid? ObraId { get; set; }
    public Guid? LayoutImportacaoId { get; set; }
    public string ArquivoNomeOriginal { get; set; } = string.Empty;
    public string ArquivoBlob { get; set; } = string.Empty;
    public string ArquivoHashSha256 { get; set; } = string.Empty;
    public StatusImportacao Status { get; set; } = StatusImportacao.Enviada;
    public int TotalLinhas { get; set; }
    public int LinhasInserir { get; set; }
    public int LinhasAtualizar { get; set; }
    public int LinhasSemAlteracao { get; set; }
    public int LinhasComErro { get; set; }
    public DateTimeOffset? ConfirmadoEm { get; set; }
    public string? ConfirmadoPor { get; set; }
    public ICollection<LinhaImportacao> Linhas { get; set; } = [];
}
