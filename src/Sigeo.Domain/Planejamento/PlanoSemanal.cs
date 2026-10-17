using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>
/// Plano semanal (Last Planner / curto prazo): o que a obra se compromete a fazer na semana (segunda a domingo).
/// Fechado, apura o PPC = compromissos concluídos ÷ compromissos planejados. O que foi feito sem estar no plano
/// entra como "fora da meta" e não conta no PPC.
/// </summary>
public class PlanoSemanal : EntidadeDaObra, IHistoricoTemporal
{
    public DateOnly Semana { get; set; }
    public StatusPlanoSemanal Status { get; set; } = StatusPlanoSemanal.Aberto;
    public string? Observacao { get; set; }
    public DateTimeOffset? FechadoEm { get; set; }
    public string? FechadoPor { get; set; }
    /// <summary>PPC apurado no fechamento (0–100), guardado para a série histórica não mudar depois.</summary>
    public decimal? Ppc { get; set; }
    public ICollection<CompromissoSemanal> Compromissos { get; set; } = [];

    public DateOnly Fim => Semana.AddDays(6);
    public bool Editavel => Status == StatusPlanoSemanal.Aberto;

    /// <summary>Segunda-feira da semana da data.</summary>
    public static DateOnly SegundaDe(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    public static (int Planejados, int Concluidos, int ForaDaMeta, decimal? Ppc) Apurar(IEnumerable<CompromissoSemanal> itens)
    {
        var l = itens.ToList();
        var plan = l.Count(c => !c.ForaDaMeta);
        var ok = l.Count(c => !c.ForaDaMeta && c.Situacao == SituacaoCompromisso.Concluido);
        var fora = l.Count(c => c.ForaDaMeta && c.Situacao == SituacaoCompromisso.Concluido);
        return (plan, ok, fora, plan == 0 ? null : Math.Round(100m * ok / plan, 1));
    }

    /// <summary>Fecha a semana: todo compromisso planejado precisa estar apontado; não concluído precisa de causa.</summary>
    public void Fechar(string usuario, DateTimeOffset agora)
    {
        if (!Editavel) throw new RegraDeNegocioException("Semana já fechada.");
        var erros = new List<string>();
        foreach (var c in Compromissos)
        {
            if (c.Situacao == SituacaoCompromisso.Pendente) erros.Add($"Aponte \"{c.Descricao}\" (concluído ou não).");
            else if (c.Situacao == SituacaoCompromisso.NaoConcluido && c.Causa is null) erros.Add($"Informe a causa de \"{c.Descricao}\" não ter sido concluído.");
        }
        if (Compromissos.Count == 0) erros.Add("A semana não tem compromissos.");
        if (erros.Count > 0) throw new RegraDeNegocioException(string.Join(" ", erros.Take(5)) + (erros.Count > 5 ? $" (+{erros.Count - 5})" : ""));
        Ppc = Apurar(Compromissos).Ppc;
        Status = StatusPlanoSemanal.Fechado; FechadoEm = agora; FechadoPor = usuario;
    }
}
