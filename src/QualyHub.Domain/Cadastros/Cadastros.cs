using QualyHub.Domain.Common;

namespace QualyHub.Domain.Cadastros;

/// <summary>
/// Catálogo de documentos exigidos (CND Federal, FGTS, Trabalhista, Contrato Social, NR-35, ASO, Ficha de EPI...).
/// Define validade padrão, antecedência do alerta e se o vencimento bloqueia pedido/acesso à obra.
/// </summary>
public class TipoDocumentoExigido : EntidadeAuditavel
{
    public AplicaA AplicaA { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int? ValidadePadraoDias { get; set; }
    public int DiasAlertaAntesVencimento { get; set; } = 15;
    public bool Obrigatorio { get; set; } = true;
    /// <summary>Se vencido: bloqueia pedido (fornecedor) ou liberação de acesso à obra (funcionário).</summary>
    public bool BloqueiaQuandoVencido { get; set; } = true;
    /// <summary>LGPD: documento com dado sensível (ex.: ASO). Arquivo vai para container restrito.</summary>
    public bool ContemDadoSensivel { get; set; }
    public bool Ativo { get; set; } = true;
}

public class Fornecedor : EntidadeAuditavel, IOrigemObraPrima, IHistoricoTemporal
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string CpfCnpj { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    /// <summary>Fornece material controlado, serviço controlado, ou ambos (define se exige avaliação SiAC).</summary>
    public bool FornecedorMaterialControlado { get; set; }
    public bool PrestadorServicoControlado { get; set; }
    public SituacaoFornecedor Situacao { get; set; } = SituacaoFornecedor.EmQualificacao;
    public DateOnly? DataQualificacao { get; set; }
    public DateOnly? ProximaReavaliacao { get; set; }
    public string? CodigoObraPrima { get; set; }
    public Guid? UltimaImportacaoId { get; set; }

    public ICollection<FornecedorDocumento> Documentos { get; set; } = [];
    public ICollection<FornecedorAvaliacao> Avaliacoes { get; set; } = [];

    public bool ExigeQualificacaoSgq => FornecedorMaterialControlado || PrestadorServicoControlado;
}

public class FornecedorDocumento : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public Guid TipoDocumentoExigidoId { get; set; }
    public TipoDocumentoExigido TipoDocumento { get; set; } = null!;
    public DateOnly? DataEmissao { get; set; }
    public DateOnly? DataValidade { get; set; }
    /// <summary>Caminho no Blob Storage (container "documentos"). Nunca URL pública.</summary>
    public string? ArquivoBlob { get; set; }
    public string? ArquivoNomeOriginal { get; set; }
    public string? ArquivoHashSha256 { get; set; }
    public StatusDocumento Status { get; set; } = StatusDocumento.PendenteEnvio;
    public string? Observacao { get; set; }
}

/// <summary>Avaliação/requalificação de fornecedor (requisito SiAC de aquisição).</summary>
public class FornecedorAvaliacao : EntidadeAuditavel
{
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public Guid? ObraId { get; set; }
    public DateOnly DataAvaliacao { get; set; }
    public string Tipo { get; set; } = "Periódica"; // Qualificação inicial, Periódica, Por ocorrência
    /// <summary>Notas de 0 a 10 por critério.</summary>
    public decimal NotaPrazo { get; set; }
    public decimal NotaQualidade { get; set; }
    public decimal NotaAtendimento { get; set; }
    public decimal NotaDocumentacao { get; set; }
    public decimal NotaFinal { get; set; }
    public SituacaoFornecedor Resultado { get; set; }
    public StatusAprovacao StatusAprovacao { get; set; } = StatusAprovacao.Rascunho;
    public string? Observacao { get; set; }

    public void CalcularNota(decimal notaMinimaQualificado = 7m, decimal notaMinimaRestricao = 5m)
    {
        NotaFinal = Math.Round((NotaPrazo + NotaQualidade + NotaAtendimento + NotaDocumentacao) / 4m, 2);
        Resultado = NotaFinal >= notaMinimaQualificado ? SituacaoFornecedor.Qualificado
                  : NotaFinal >= notaMinimaRestricao ? SituacaoFornecedor.QualificadoComRestricao
                  : SituacaoFornecedor.Bloqueado;
    }
}

/// <summary>
/// Funcionário próprio ou de terceiro. LGPD (minimização): guarda apenas o necessário para controle
/// de acesso à obra e treinamentos — sem dados de saúde, endereço residencial ou dados bancários.
/// </summary>
public class Funcionario : EntidadeAuditavel, IOrigemObraPrima
{
    public string Nome { get; set; } = string.Empty;
    /// <summary>LGPD: CPF não é guardado em claro. Hash HMAC-SHA256 (chave no Key Vault) para casar na importação.</summary>
    public string CpfHash { get; set; } = string.Empty;
    /// <summary>Somente para exibição: ***.456.789-**</summary>
    public string CpfMascarado { get; set; } = string.Empty;
    public string? Funcao { get; set; }
    public bool Terceiro { get; set; }
    public Guid? FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }
    public bool Ativo { get; set; } = true;
    public string? CodigoObraPrima { get; set; }
    public Guid? UltimaImportacaoId { get; set; }

    public ICollection<FuncionarioDocumento> Documentos { get; set; } = [];
    public ICollection<FuncionarioObra> Obras { get; set; } = [];
}

public class FuncionarioObra : Entidade
{
    public Guid FuncionarioId { get; set; }
    public Funcionario Funcionario { get; set; } = null!;
    public Guid ObraId { get; set; }
    public DateOnly? DataIntegracao { get; set; }
    public bool AcessoLiberado { get; set; }
}

/// <summary>
/// Documento/treinamento do funcionário (NR-18, NR-35, NR-10, integração, ficha de EPI, ASO).
/// LGPD: para ASO guarda somente "Apto/Inapto" e validade — NUNCA CID, exames ou diagnóstico.
/// </summary>
public class FuncionarioDocumento : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid FuncionarioId { get; set; }
    public Funcionario Funcionario { get; set; } = null!;
    public Guid TipoDocumentoExigidoId { get; set; }
    public TipoDocumentoExigido TipoDocumento { get; set; } = null!;
    public DateOnly? DataRealizacao { get; set; }
    public DateOnly? DataValidade { get; set; }
    /// <summary>Somente para ASO: apto para a função. Nenhum outro dado de saúde.</summary>
    public bool? Apto { get; set; }
    public string? CargaHoraria { get; set; }
    public string? ArquivoBlob { get; set; }
    public string? ArquivoHashSha256 { get; set; }
    public StatusDocumento Status { get; set; } = StatusDocumento.PendenteEnvio;
}

/// <summary>Serviço controlado do SiAC (recorte do Nível B), vinculado ao PEIS e ao modelo de FVS.</summary>
public class ServicoControlado : EntidadeAuditavel, IOrigemObraPrima
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    /// <summary>Código(s) do item de orçamento/EAP no Obra Prima para cruzar com as medições.</summary>
    public string? CodigosItemOrcamento { get; set; }
    public Guid? DocumentoPeisId { get; set; }
    public Guid? FormModeloFvsId { get; set; }
    public bool ExigeFvsParaMedicao { get; set; } = true;
    public bool Ativo { get; set; } = true;
    public string? CodigoObraPrima { get; set; }
    public Guid? UltimaImportacaoId { get; set; }
}

/// <summary>Material controlado do SiAC, vinculado ao modelo de FVM (critérios de recebimento).</summary>
public class MaterialControlado : EntidadeAuditavel, IOrigemObraPrima
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Unidade { get; set; }
    /// <summary>Códigos de insumo no Obra Prima (separados por ;) para identificar itens de pedido controlados.</summary>
    public string? CodigosInsumo { get; set; }
    public Guid? FormModeloFvmId { get; set; }
    public bool ExigeFvmParaAceite { get; set; } = true;
    public bool ExigeFornecedorQualificado { get; set; } = true;
    public bool Ativo { get; set; } = true;
    public string? CodigoObraPrima { get; set; }
    public Guid? UltimaImportacaoId { get; set; }
}
