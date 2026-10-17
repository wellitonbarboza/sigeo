using Sigeo.Domain.Common;

namespace Sigeo.Domain.Colaboracao;

public enum DonoMeta { Setor = 1, Pessoa = 2, Equipe = 3 }

/// <summary>De onde vem o valor realizado da meta (automático) ou lançado à mão com evidência.</summary>
public enum IndicadorMeta
{
    Manual = 0, EconomiaReal = 1, TempoCompra = 2, EntregaNoPrazo = 3, CotacaoCompetitiva = 4,
    PpcLastPlanner = 5, TarefasNoPrazo = 6, AtendimentoNecessidade = 7
}

public enum AceiteTarefa { NaoSeAplica = 0, Aguardando = 1, Aceita = 2, Devolvida = 3 }

/// <summary>Meta de um setor, pessoa ou equipe num período, com alvo, sentido (maior/menor é melhor) e peso na nota.</summary>
public class Meta : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    public DonoMeta Dono { get; set; } = DonoMeta.Setor;
    /// <summary>Setor dono (Suprimentos, Engenharia, Qualidade…) quando a meta é do setor.</summary>
    public string? Setor { get; set; }
    public Guid? UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }
    public Guid? EquipeId { get; set; }
    public string? EquipeNome { get; set; }
    public Guid? ObraId { get; set; }
    /// <summary>Serviço ligado (ex.: Alvenaria) para o placar por serviço.</summary>
    public string? Servico { get; set; }
    public DateOnly Inicio { get; set; }
    public DateOnly Fim { get; set; }
    public IndicadorMeta Indicador { get; set; } = IndicadorMeta.Manual;
    public decimal Alvo { get; set; }
    public bool MenorMelhor { get; set; }
    public string Unidade { get; set; } = "%";
    /// <summary>Peso na nota de quem é dono (0 a 100).</summary>
    public decimal Peso { get; set; } = 100;
    public decimal? ValorManual { get; set; }
    public string? Evidencia { get; set; }
    public bool Ativa { get; set; } = true;
    public ICollection<MetaApuracao> Apuracoes { get; set; } = [];

    public string DonoNome => Dono switch { DonoMeta.Pessoa => UsuarioNome ?? "Pessoa", DonoMeta.Equipe => EquipeNome ?? "Equipe", _ => Setor ?? "Setor" };

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Nome)) e.Add("Informe o nome da meta.");
        if (Fim < Inicio) e.Add("Fim do período antes do início.");
        if (Dono == DonoMeta.Pessoa && UsuarioId is null) e.Add("Meta individual: escolha a pessoa.");
        if (Dono == DonoMeta.Equipe && EquipeId is null) e.Add("Meta de equipe: escolha a equipe.");
        if (Dono == DonoMeta.Setor && string.IsNullOrWhiteSpace(Setor)) e.Add("Meta do setor: informe o setor.");
        if (Alvo <= 0 && !MenorMelhor) e.Add("Informe o alvo.");
        if (Peso is < 0 or > 100) e.Add("Peso entre 0 e 100.");
        return e;
    }
}

/// <summary>Retrato da apuração (fechamento mensal congela o valor para comparar e avaliar).</summary>
public class MetaApuracao : Entidade
{
    public Guid MetaId { get; set; }
    public DateOnly Mes { get; set; }
    public decimal? Valor { get; set; }
    public decimal? PercAtingido { get; set; }
    public DateTimeOffset ApuradoEm { get; set; }
    public string? ApuradoPor { get; set; }
}

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
