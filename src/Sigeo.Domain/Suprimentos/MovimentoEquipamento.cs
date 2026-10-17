using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Movimentação do equipamento (chegada, transferência entre obras, devolução, manutenção) com checklist.</summary>
public class MovimentoEquipamento : EntidadeAuditavel
{
    public Guid EquipamentoId { get; set; }
    public DateOnly Data { get; set; }
    public TipoMovimentoEquipamento Tipo { get; set; }
    public Guid? DeAlmoxarifadoId { get; set; }
    public Guid? ParaAlmoxarifadoId { get; set; }
    public string? Checklist { get; set; }
    public string? Observacao { get; set; }
}
