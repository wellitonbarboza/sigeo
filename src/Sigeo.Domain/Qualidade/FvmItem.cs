using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

// ═══════════════════════ Q13/Q14/Q15 — FVM detalhada, ensaios e rastreabilidade ═══════════════════════

/// <summary>Q13 · Resultado de cada item do modelo de FVM no recebimento.</summary>
public class FvmItem : Entidade
{
    public Guid RecebimentoId { get; set; }
    public Guid? RecebimentoItemId { get; set; }
    public Guid ItemVerificacaoId { get; set; }
    public int Ordem { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Criterio { get; set; } = string.Empty;
    public decimal? ValorMin { get; set; }
    public decimal? ValorMax { get; set; }
    public bool Bloqueia { get; set; }
    public string? ValorMedido { get; set; }
    public ResultadoItem Resultado { get; set; }
    public string? Observacao { get; set; }
}
