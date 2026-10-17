using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

/// <summary>Cautela: entrega e devolução da ferramenta de medição (para colaborador próprio, empreiteira ou setor).</summary>
public class CautelaFerramenta : EntidadeAuditavel
{
    public Guid EquipamentoMedicaoId { get; set; }
    public DateOnly Data { get; set; }
    public string Para { get; set; } = string.Empty;
    public Guid? ColaboradorId { get; set; }
    public Guid? FornecedorId { get; set; }
    public DateOnly? DevolvidaEm { get; set; }
    public string? EntreguePor { get; set; }
    public string? Observacao { get; set; }
}
