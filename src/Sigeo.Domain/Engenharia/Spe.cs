using System.Globalization;
using System.Text;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Engenharia;

/// <summary>
/// SPE (Sociedade de Propósito Específico) — 1 condomínio (quadra) = 1 SPE = 1 CNPJ. Todo custo, pedido e NF carrega a SPE
/// (segregação de custo por patrimônio; base do rateio e do extrato mensal). As torres do condomínio pertencem à SPE.
/// </summary>
public class Spe : EntidadeAuditavel, IHistoricoTemporal, IPertenceObra
{
    public Guid ObraId { get; set; }
    /// <summary>Quadra/condomínio da SPE (1 para 1).</summary>
    public Guid? QuadraId { get; set; }
    /// <summary>Sigla curta (ex.: IRE).</summary>
    public string Codigo { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string? InscricaoEstadual { get; set; }
    public string? EnderecoFaturamento { get; set; }
    public string? EmailFiscal { get; set; }
    /// <summary>Patrimônio de afetação / RET: Sim, Não ou Indefinido (a confirmar com a contabilidade).</summary>
    public string PatrimonioAfetacao { get; set; } = "Indefinido";
    /// <summary>Engenheiro responsável pela SPE: aprovador de 1º nível (alçada "engenheiro responsável") dos documentos da SPE.</summary>
    public Guid? EngenheiroId { get; set; }
    public bool Ativa { get; set; } = true;

    public string NomeExibicao => string.IsNullOrWhiteSpace(NomeFantasia) ? RazaoSocial : NomeFantasia!;
}
