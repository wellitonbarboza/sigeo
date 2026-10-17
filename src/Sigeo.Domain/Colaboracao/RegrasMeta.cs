using Sigeo.Domain.Common;

namespace Sigeo.Domain.Colaboracao;

public static class RegrasMeta
{
    /// <summary>% atingido (1 = 100%). Maior é melhor: realizado ÷ alvo. Menor é melhor: alvo ÷ realizado (realizado 0 = atingiu).</summary>
    public static decimal? Atingido(decimal alvo, decimal? valor, bool menorMelhor)
    {
        if (valor is not { } v) return null;
        if (menorMelhor) return v <= 0 ? 1 : alvo <= 0 ? 0 : Math.Round(alvo / v, 4);
        return alvo <= 0 ? null : Math.Round(v / alvo, 4);
    }

    /// <summary>Situação: atingida (≥ 100%), atenção (≥ 85%), em risco.</summary>
    public static (string Texto, string Tom) Situacao(decimal? atingido) => atingido switch
    {
        null => ("Sem dado", "neutro"),
        >= 1 => ("Atingida", "ok"),
        >= 0.85m => ("Atenção", "aviso"),
        _ => ("Em risco", "erro")
    };

    /// <summary>
    /// Nota do placar (0 a 10) para acompanhar e avaliar o desempenho: metas atingidas (60%, ponderadas pelo peso, cada uma limitada a 100%)
    /// + tarefas aceitas no prazo (40%). Só um dos lados com dado: ele vale 100%. A nota não altera nenhuma meta.
    /// </summary>
    public static decimal? Nota(IEnumerable<(decimal? Atingido, decimal Peso)> metas, int tarefasNoPrazo, int tarefasTotal)
    {
        var m = metas.Where(x => x.Atingido is not null && x.Peso > 0).ToList();
        decimal? pm = m.Count == 0 ? null : m.Sum(x => Math.Min(1, Math.Max(0, x.Atingido!.Value)) * x.Peso) / m.Sum(x => x.Peso);
        decimal? pt = tarefasTotal <= 0 ? null : (decimal)tarefasNoPrazo / tarefasTotal;
        if (pm is null && pt is null) return null;
        var n = pm is { } a && pt is { } b ? a * 0.6m + b * 0.4m : pm ?? pt!.Value;
        return Math.Round(n * 10, 1);
    }

    /// <summary>Tarefa conta como entregue no prazo quando concluída até o prazo original e aceita (ou sem aceite exigido).</summary>
    public static bool NoPrazo(Tarefa t) =>
        t.Status == StatusTarefa.Concluida && t.ConcluidaEm is { } c && DateOnly.FromDateTime(c.LocalDateTime) <= t.PrazoOriginal
        && t.Aceite is AceiteTarefa.NaoSeAplica or AceiteTarefa.Aceita;
}
