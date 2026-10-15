using Groundkey.Domain.Common;

namespace Groundkey.Domain.Importacao;

/// <summary>
/// Layout de colunas do export do ERP (mapeamento salvo para não remapear a cada arquivo).
/// MapeamentoJson: { "CpfCnpj": "CNPJ/CPF", "RazaoSocial": "Razão Social", ... } (campo Groundkey → cabeçalho do arquivo)
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

/// <summary>
/// De-para de importação, reaproveitado nas próximas cargas: texto do sistema de origem (insumo, fornecedor, fase, serviço/local do ERP)
/// → cadastro do Groundkey. Ignorar = a origem não entra (ex.: insumo de teste). Local vale por obra; os demais, pela construtora.
/// </summary>
public class VinculoOrigem : EntidadeAuditavel
{
    public TipoVinculoOrigem Tipo { get; set; }
    /// <summary>Sistema de origem (ex.: "ERP").</summary>
    public string Sistema { get; set; } = "ERP";
    public Guid? ObraId { get; set; }
    /// <summary>Texto da origem normalizado (sem acento/caixa/espaços extras) — é a chave.</summary>
    public string Chave { get; set; } = string.Empty;
    public string TextoOrigem { get; set; } = string.Empty;
    /// <summary>Produto, fornecedor ou local escolhido.</summary>
    public Guid? DestinoId { get; set; }
    /// <summary>Fase (grupo de serviço) escolhida.</summary>
    public string? DestinoTexto { get; set; }
    public bool Ignorar { get; set; }
}
