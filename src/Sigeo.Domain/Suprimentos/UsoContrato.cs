using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Uso lançado no período (contratos "por uso": caçamba, bombeamento…).</summary>
public class UsoContrato : EntidadeDaObra
{
    public Guid ContratoId { get; set; }
    public Guid ContratoItemId { get; set; }
    public DateOnly Data { get; set; }
    public decimal Quantidade { get; set; }
    public string? Documento { get; set; }
    public string? Observacao { get; set; }
}
