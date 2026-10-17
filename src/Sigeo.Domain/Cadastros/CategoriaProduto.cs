using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Cadastros;

/// <summary>
/// Grupo (Pai nulo) ou subgrupo de produto, com a sigla de 3 letras usada no código (HID-CNX-0001).
/// A sigla é sugerida pelo sistema e pode ser ajustada; produtos já usados mantêm o código emitido.
/// </summary>
public class CategoriaProduto : EntidadeAuditavel
{
    public Guid? PaiId { get; set; }
    public CategoriaProduto? Pai { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    /// <summary>Tipo de custo herdado pelo produto do grupo que ainda não tem tipo (curva ABC, custo por tipo, financeiro).</summary>
    public Common.TipoCusto? TipoCustoPadrao { get; set; }
    /// <summary>Fase padrão do orçamento (grupo de serviço da EAP) para compra sem composição: o custo realizado cai nela.</summary>
    public string? FasePadrao { get; set; }
    /// <summary>Lead time padrão do grupo/subgrupo (dias): produto sem lead time herda (subgrupo, senão grupo).</summary>
    public int? LeadTimeDias { get; set; }
    public ICollection<CategoriaProduto> Subgrupos { get; set; } = [];
}
