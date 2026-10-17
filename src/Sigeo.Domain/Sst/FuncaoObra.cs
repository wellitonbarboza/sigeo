using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

// ═══════════════════════ S02/S03/S03A — Funções e exigências ═══════════════════════

/// <summary>S02 · Função da obra (base da matriz de exigências).</summary>
public class FuncaoObra : EntidadeAuditavel
{
    public string Descricao { get; set; } = string.Empty;
    public string? Cbo { get; set; }
    public string? Riscos { get; set; }
    public bool Ativa { get; set; } = true;
    public ICollection<FuncaoExigencia> Exigencias { get; set; } = [];
}
