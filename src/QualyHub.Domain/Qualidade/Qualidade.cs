using QualyHub.Domain.Common;

namespace QualyHub.Domain.Qualidade;

/// <summary>
/// Modelo de formulário (construtor próprio de FVS/FVM). Cada revisão do PEIS gera nova versão do modelo;
/// fichas já preenchidas continuam apontando para a versão usada (rastreabilidade).
/// </summary>
public class FormModelo : EntidadeAuditavel, IHistoricoTemporal
{
    public TipoFicha Tipo { get; set; }
    public string Codigo { get; set; } = string.Empty;       // ex.: FVS-07 Alvenaria de vedação
    public string Titulo { get; set; } = string.Empty;
    public int Versao { get; set; } = 1;
    public Guid? DocumentoSgqOrigemId { get; set; }          // PEIS de origem
    public bool Vigente { get; set; } = true;
    public bool ExigeFotoGeral { get; set; } = true;
    public ICollection<FormItem> Itens { get; set; } = [];
}

public class FormItem : Entidade
{
    public Guid FormModeloId { get; set; }
    public FormModelo FormModelo { get; set; } = null!;
    public int Ordem { get; set; }
    public string Grupo { get; set; } = "Execução";          // Pré-requisitos, Execução, Pós-execução / Documentos, Visual, Dimensional
    public string ItemVerificacao { get; set; } = string.Empty;
    public string CriterioAceitacao { get; set; } = string.Empty;
    public string? MetodoVerificacao { get; set; }
    public TipoResposta TipoResposta { get; set; } = TipoResposta.ConformeNaoConforme;
    public decimal? ToleranciaMin { get; set; }
    public decimal? ToleranciaMax { get; set; }
    public bool Obrigatorio { get; set; } = true;
    public bool ExigeFotoSeNaoConforme { get; set; } = true;
}

/// <summary>
/// Ficha preenchida (FVS ou FVM). Projetada para o app de campo offline:
/// - Id gerado no dispositivo (Guid v7) → idempotência na sincronização;
/// - CapturadoEm = hora real do campo; SincronizadoEm = chegada no servidor (auditor vê as duas);
/// - Após "Enviada", a ficha é imutável: correção = reinspeção (nova ficha vinculada).
/// </summary>
public class Ficha : EntidadeDaObra, IHistoricoTemporal
{
    public TipoFicha Tipo { get; set; }
    public string Numero { get; set; } = string.Empty;
    public Guid FormModeloId { get; set; }
    public FormModelo FormModelo { get; set; } = null!;
    public int VersaoModelo { get; set; }

    // FVS
    public Guid? ServicoControladoId { get; set; }
    public Guid? LocalId { get; set; }
    public Guid? PrestadorFornecedorId { get; set; }

    // FVM
    public Guid? MaterialControladoId { get; set; }
    public Guid? PedidoId { get; set; }
    public Guid? PedidoItemId { get; set; }
    public string? NotaFiscal { get; set; }
    public string? Lote { get; set; }
    public decimal? QuantidadeRecebida { get; set; }
    public decimal? QuantidadeAceita { get; set; }

    public StatusFicha Status { get; set; } = StatusFicha.Rascunho;
    public Guid? FichaOrigemReinspecaoId { get; set; }
    public string InspetorNome { get; set; } = string.Empty;
    public Guid InspetorUsuarioId { get; set; }
    public DateTimeOffset CapturadoEm { get; set; }
    public DateTimeOffset? SincronizadoEm { get; set; }
    public string? DispositivoId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Observacao { get; set; }
    public Guid? AprovadoPorUsuarioId { get; set; }
    public DateTimeOffset? AprovadoEm { get; set; }

    public ICollection<FichaResposta> Respostas { get; set; } = [];
    public ICollection<FichaEvidencia> Evidencias { get; set; } = [];

    public bool Editavel => Status == StatusFicha.Rascunho;
    public bool Liberada => Status is StatusFicha.Aprovada or StatusFicha.AprovadaAposReinspecao;
}

public class FichaResposta : Entidade
{
    public Guid FichaId { get; set; }
    public Ficha Ficha { get; set; } = null!;
    public Guid FormItemId { get; set; }
    public ResultadoItem Resultado { get; set; }
    public decimal? ValorNumerico { get; set; }
    public string? ValorTexto { get; set; }
    public string? Observacao { get; set; }
}

/// <summary>Foto/arquivo de evidência. Upload em fila no app de campo; hash garante integridade.</summary>
public class FichaEvidencia : Entidade
{
    public Guid FichaId { get; set; }
    public Ficha Ficha { get; set; } = null!;
    public Guid? FormItemId { get; set; }
    public string ArquivoBlob { get; set; } = string.Empty;
    public string ArquivoHashSha256 { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }
    public DateTimeOffset CapturadoEm { get; set; }
    public bool UploadConcluido { get; set; }
}

public class Rnc : EntidadeDaObra, IHistoricoTemporal
{
    public string Numero { get; set; } = string.Empty;
    public OrigemRnc Origem { get; set; }
    public Guid? FichaId { get; set; }
    public Guid? FornecedorId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? AcaoImediata { get; set; }
    public string? AnaliseCausa { get; set; }
    public string? AcaoCorretiva { get; set; }
    public string? Responsavel { get; set; }
    public DateOnly? PrazoTratativa { get; set; }
    public string? VerificacaoEficacia { get; set; }
    public bool? Eficaz { get; set; }
    public StatusRnc Status { get; set; } = StatusRnc.Aberta;
    public DateTimeOffset? EncerradaEm { get; set; }
}
