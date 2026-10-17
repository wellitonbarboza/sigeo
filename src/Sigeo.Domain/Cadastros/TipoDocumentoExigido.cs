using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Cadastros;

/// <summary>
/// Catálogo de tipos de documento (LAO, Certificado PBQP-H, laudo, FISPQ, ART, CND, contrato social, NR-35, ASO…).
/// Define validade padrão, antecedência do alerta e se o vencimento bloqueia compra/acesso à obra.
/// </summary>
public class TipoDocumentoExigido : EntidadeAuditavel
{
    public AplicaA AplicaA { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int? ValidadePadraoDias { get; set; }
    public int DiasAlertaAntesVencimento { get; set; } = 30;
    /// <summary>Exigido de todo fornecedor/funcionário (senão, só quando o tipo de fornecimento pedir).</summary>
    public bool Obrigatorio { get; set; } = true;
    /// <summary>Para quais tipos de fornecimento é exigido (Nenhum = todos).</summary>
    public TipoFornecimento ExigidoPara { get; set; }
    /// <summary>Documento que acompanha cada entrega (ex.: certificado do lote do aço) — não tem validade própria.</summary>
    public bool PorEntrega { get; set; }
    public MomentoDocumento Momento { get; set; } = MomentoDocumento.Cadastro;
    public Guid? MaterialControladoId { get; set; }
    public Guid? ServicoControladoId { get; set; }
    public int DiasLembrete { get; set; } = 30;
    public string? Fonte { get; set; }
    /// <summary>Se vencido: bloqueia compra (fornecedor) ou acesso à obra (funcionário).</summary>
    public bool BloqueiaQuandoVencido { get; set; } = true;
    /// <summary>LGPD: documento com dado sensível (ex.: ASO). Arquivo vai para container restrito.</summary>
    public bool ContemDadoSensivel { get; set; }
    public bool Ativo { get; set; } = true;
    /// <summary>Frequência: no cadastro, por validade, mensal (competência + dia limite), por contrato, por medição, por entrega, anual.</summary>
    public FrequenciaDocumento Frequencia { get; set; } = FrequenciaDocumento.Validade;
    /// <summary>Mensal: dia limite para entregar a competência anterior (padrão 20).</summary>
    public int? DiaLimite { get; set; }
    /// <summary>Categoria (lista): Empresa, Fiscal, Trabalhista, SST, RH, Técnico…</summary>
    public string? Categoria { get; set; }
    /// <summary>Documento dos funcionários do fornecedor (ASO, ficha de registro, treinamentos…): entra quando o pré-cadastro inclui funcionários.</summary>
    public bool DeFuncionario { get; set; }
    /// <summary>Documento mensal que o fornecedor pode pedir para adiar (com justificativa aprovada pelo engenheiro) sem travar a medição.</summary>
    public bool PodeAdiar { get; set; }
    public int Ordem { get; set; }
}
