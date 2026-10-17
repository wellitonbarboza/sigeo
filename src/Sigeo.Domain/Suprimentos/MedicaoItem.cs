using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>T15 · Item da medição (quantidade por item do contrato, com FVS de evidência).</summary>
public class MedicaoItem : Entidade
{
    public Guid MedicaoId { get; set; }
    public Medicao Medicao { get; set; } = null!;
    public Guid ContratoItemId { get; set; }
    public ContratoItem ContratoItem { get; set; } = null!;
    /// <summary>Local executado (torre/pavimento/unidade) — casa com a FVS do mesmo local.</summary>
    public Guid? LocalId { get; set; }
    public decimal QtdAnterior { get; set; }
    public decimal QtdPeriodo { get; set; }
    public decimal QtdAcumulada { get; set; }
    public decimal PercExecutado { get; set; }
    public decimal ValorPeriodo { get; set; }
    /// <summary>Número da FVS (texto) ou ficha vinculada.</summary>
    public string? FvsRef { get; set; }
    public Guid? FvsId { get; set; }
}
