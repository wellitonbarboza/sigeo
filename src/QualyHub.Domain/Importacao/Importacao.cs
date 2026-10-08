using QualyHub.Domain.Common;

namespace QualyHub.Domain.Importacao;

/// <summary>
/// Layout de colunas do export do ERP (mapeamento salvo para não remapear a cada arquivo).
/// MapeamentoJson: { "CpfCnpj": "CNPJ/CPF", "RazaoSocial": "Razão Social", ... } (campo QualyHub → cabeçalho do arquivo)
/// </summary>
public class LayoutImportacao : EntidadeAuditavel
{
    public TipoImportacao Tipo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int LinhaCabecalho { get; set; } = 1;
    public string? NomePlanilha { get; set; }
    public string Delimitador { get; set; } = ";";
    public string MapeamentoJson { get; set; } = "{}";
    public bool Padrao { get; set; }
}

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

public class LinhaImportacao : Entidade
{
    public Guid LoteImportacaoId { get; set; }
    public int NumeroLinha { get; set; }
    public string ChaveNatural { get; set; } = string.Empty;   // CNPJ, nº pedido, nº medição...
    public string DadosJson { get; set; } = "{}";
    public AcaoLinhaImportacao Acao { get; set; }
    public string? Erros { get; set; }
    public Guid? EntidadeAfetadaId { get; set; }
}
