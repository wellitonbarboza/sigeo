using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Família de equipamento (elevação, concreto, energia, veículos…). Configurável: campos extras e se usa horímetro/hodômetro.</summary>
public class FamiliaEquipamento : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    public bool UsaHorimetro { get; set; }
    public bool UsaHodometro { get; set; }
    /// <summary>Campos extras separados por ";" (ex.: "Placa;RENAVAM;Chassi").</summary>
    public string? CamposExtras { get; set; }
    public Criticidade CriticidadePadrao { get; set; } = Criticidade.B;
    public bool Ativa { get; set; } = true;

    public IReadOnlyList<string> Campos => (CamposExtras ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
