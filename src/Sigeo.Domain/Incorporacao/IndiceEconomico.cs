using Sigeo.Domain.Common;
using Sigeo.Domain.Financeiro;

namespace Sigeo.Domain.Incorporacao;

/// <summary>Variação mensal de índice (INCC, IPCA, IGP-M…) em %, informada pelo financeiro.</summary>
public class IndiceEconomico : EntidadeAuditavel
{
    public string Indice { get; set; } = "INCC";
    public DateOnly Competencia { get; set; }
    public decimal VariacaoPerc { get; set; }
}
