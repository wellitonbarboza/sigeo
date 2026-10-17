using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Inventário (implantação, cíclico ou geral). Diferenças viram ajuste depois da aprovação.</summary>
public class InventarioEstoque : EntidadeAuditavel, IHistoricoTemporal
{
    public int Numero { get; set; }
    public Guid AlmoxarifadoId { get; set; }
    public TipoInventario Tipo { get; set; } = TipoInventario.Ciclico;
    public DateOnly Data { get; set; }
    public StatusInventario Status { get; set; } = StatusInventario.EmContagem;
    public string? Observacao { get; set; }
    public string? AprovadoPor { get; set; }
    public DateTimeOffset? AprovadoEm { get; set; }
    public ICollection<InventarioItem> Itens { get; set; } = [];
}
