using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Plano de manutenção preventiva: por tempo (dias), por uso (horas/km) ou por evento (antes de mobilizar).</summary>
public class PlanoManutencao : EntidadeAuditavel
{
    public Guid EquipamentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public GatilhoManutencao Gatilho { get; set; } = GatilhoManutencao.Tempo;
    public int? IntervaloDias { get; set; }
    public decimal? IntervaloUso { get; set; }
    public DateOnly? UltimaData { get; set; }
    public decimal? UltimaLeitura { get; set; }
    /// <summary>Quem executa: mecânico próprio, técnico de segurança, contrato…</summary>
    public string Executante { get; set; } = string.Empty;
    public Guid? ContratoId { get; set; }
    /// <summary>Itens do checklist, um por linha.</summary>
    public string? Checklist { get; set; }
    public int DiasAntecedencia { get; set; } = 7;
    public bool Ativo { get; set; } = true;

    public DateOnly? ProximaData => Gatilho == GatilhoManutencao.Tempo && IntervaloDias is > 0 ? (UltimaData?.AddDays(IntervaloDias.Value)) : null;
    public decimal? ProximaLeitura => Gatilho == GatilhoManutencao.Uso && IntervaloUso is > 0 ? (UltimaLeitura ?? 0) + IntervaloUso : null;
    public IReadOnlyList<string> ItensChecklist => (Checklist ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public bool Vencido(DateOnly hoje, decimal leituraAtual) =>
        Ativo && (ProximaData is { } d && d < hoje || ProximaLeitura is { } l && leituraAtual >= l);
    public bool PertoDeVencer(DateOnly hoje, decimal leituraAtual) =>
        Ativo && !Vencido(hoje, leituraAtual) && (ProximaData is { } d && d <= hoje.AddDays(DiasAntecedencia) || ProximaLeitura is { } l && IntervaloUso is > 0 && leituraAtual >= l - IntervaloUso.Value * 0.1m);
}
