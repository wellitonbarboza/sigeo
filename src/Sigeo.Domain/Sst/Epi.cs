using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

// ═══════════════════════ S08/S08A — EPI ═══════════════════════

public class Epi : EntidadeAuditavel
{
    public string Descricao { get; set; } = string.Empty;
    public string Ca { get; set; } = string.Empty;
    public DateOnly ValidadeCa { get; set; }
    public int? VidaUtilDias { get; set; }
    public Guid? ProdutoId { get; set; }
    public bool Ativo { get; set; } = true;
}
