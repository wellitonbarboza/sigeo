using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>Centro de custo (administração, estoque, obra/etapa). Pode apontar para uma obra.</summary>
public class CentroCusto : EntidadeAuditavel
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    /// <summary>Centro de custo pai (plano único em árvore com níveis livres).</summary>
    public Guid? PaiId { get; set; }
    /// <summary>Estabelecimentos onde pode ser usado (ids separados por vírgula; vazio = todos).</summary>
    public string? EstabelecimentosPermitidos { get; set; }
    public string? CodigoErp { get; set; }
    public Guid? ObraId { get; set; }
    public decimal? Orcamento { get; set; }
    public bool Ativo { get; set; } = true;
}
