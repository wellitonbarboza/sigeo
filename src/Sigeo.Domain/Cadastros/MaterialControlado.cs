using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Cadastros;

/// <summary>Material controlado (lista de materiais controlados). Dispara especificação técnica, FVM e fornecedor qualificado.</summary>
public class MaterialControlado : EntidadeAuditavel, IOrigemErp
{
    /// <summary>Ex.: 01, 05.01</summary>
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Unidade { get; set; }
    /// <summary>Grupo > Sub-grupo do catálogo de produtos que herda a marcação de controlado.</summary>
    public string? GrupoCatalogo { get; set; }
    /// <summary>Grupo do catálogo ligado (1 grupo = 1 material): produtos do grupo viram controlados automaticamente.</summary>
    public Guid? CategoriaProdutoId { get; set; }
    /// <summary>Modelo de especificação técnica (tabela de especificação e ensaio de materiais).</summary>
    public string? EspecificacaoModelo { get; set; }
    /// <summary>Documentos que o fornecedor deve apresentar (LAO, certificado do lote, laudo, FISPQ…) — sai impresso no pedido.</summary>
    public string? DocumentosExigidos { get; set; }
    public bool ExigeEspecificacao { get; set; } = true;
    public bool ExigeFvmParaAceite { get; set; } = true;
    public bool ExigeFornecedorQualificado { get; set; } = true;
    public bool ConsultarPsq { get; set; } = true;
    public string? SubGrupos { get; set; }
    public string? Ensaios { get; set; }
    public string ConstaPsq { get; set; } = "Verificar";
    /// <summary>Norma / especificação de referência do material (ex.: ABNT NBR 7480:2024).</summary>
    public string? Norma { get; set; }
    /// <summary>Formas de qualificação aceitas para o fornecedor (ex.: "A ou B": PSQ, certificação, histórico, ensaio/visita).</summary>
    public string? FormasQualificacao { get; set; }
    /// <summary>Critério / observação da qualificação do fornecedor deste material.</summary>
    public string? CriterioQualificacao { get; set; }
    /// <summary>Armazenamento e preservação na obra (procedimento de inspeção e armazenamento).</summary>
    public string? Armazenamento { get; set; }
    /// <summary>Serviços (ITS/procedimentos) que aplicam o material, separados por " · ".</summary>
    public string? ServicosQueUtilizam { get; set; }
    public bool Ativo { get; set; } = true;
    public string? CodigoErp { get; set; }
    public Guid? UltimaImportacaoId { get; set; }
}
