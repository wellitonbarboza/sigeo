using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Regras da cobrança recorrente (funções puras, testáveis).</summary>
public static class RegrasRecorrencia
{
    /// <summary>Períodos de cobrança dentro da vigência.</summary>
    public static List<(DateOnly Inicio, DateOnly Fim)> Periodos(Contrato c)
    {
        var r = new List<(DateOnly, DateOnly)>();
        var ini = c.VigenciaInicio; var fimVig = c.VigenciaFim;
        if (fimVig < ini) return r;
        if (c.Periodicidade == Periodicidade.DatasCombinadas)
        {
            var datas = (c.DatasCombinadas ?? "").Split([';', ',', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => DateOnly.TryParseExact(s, "dd/MM/yyyy", out var d) || DateOnly.TryParse(s, out d) ? d : (DateOnly?)null)
                .Where(d => d is not null && d >= ini && d <= fimVig).Select(d => d!.Value).Distinct().Order().ToList();
            var a = ini;
            foreach (var d in datas) { r.Add((a, d)); a = d.AddDays(1); }
            return r;
        }
        var atual = ini;
        while (atual <= fimVig && r.Count < 240)
        {
            DateOnly fim;
            switch (c.Periodicidade)
            {
                case Periodicidade.Quinzenal: fim = atual.AddDays(14); break;
                case Periodicidade.Semanal: fim = atual.AddDays(6); break;
                default:
                    var dia = Math.Clamp(c.DiaCorte, 1, 31);
                    DateOnly Corte(int ano, int mes) => new(ano, mes, Math.Min(dia, DateTime.DaysInMonth(ano, mes)));
                    fim = Corte(atual.Year, atual.Month);
                    if (fim < atual) { var prox = atual.AddMonths(1); fim = Corte(prox.Year, prox.Month); }
                    break;
            }
            if (fim > fimVig) fim = fimVig;
            r.Add((atual, fim));
            atual = fim.AddDays(1);
        }
        return r;
    }

    /// <summary>Vencimento: dia combinado no mês do fim do período (ou no seguinte, se o dia já passou).</summary>
    public static DateOnly Vencimento(DateOnly fimPeriodo, int dia)
    {
        dia = Math.Clamp(dia, 1, 31);
        DateOnly Em(DateOnly m) => new(m.Year, m.Month, Math.Min(dia, DateTime.DaysInMonth(m.Year, m.Month)));
        var v = Em(fimPeriodo);
        return v > fimPeriodo ? v : Em(fimPeriodo.AddMonths(1));
    }

    /// <summary>Valor previsto do período e como foi calculado.</summary>
    public static List<LinhaPrevisto> Previsto(Contrato c, DateOnly inicio, DateOnly fim, IEnumerable<PeriodoLocacao> locacoes, IEnumerable<UsoContrato> usos)
    {
        var linhas = new List<LinhaPrevisto>();
        switch (c.FormaCobranca)
        {
            case FormaCobranca.ValorFixo:
                foreach (var i in c.Itens) linhas.Add(new(i.Descricao, i.Quantidade, $"{i.Unidade} / período", i.PrecoUnitario, i.Total));
                break;
            case FormaCobranca.Uso:
                foreach (var i in c.Itens)
                {
                    var q = usos.Where(u => u.ContratoItemId == i.Id && u.Data >= inicio && u.Data <= fim).Sum(u => u.Quantidade);
                    if (q > 0) linhas.Add(new(i.Descricao, q, i.Unidade, i.PrecoUnitario, Math.Round(q * i.PrecoUnitario, 2)));
                }
                break;
            case FormaCobranca.Diaria:
                foreach (var l in locacoes)
                {
                    var item = c.Itens.FirstOrDefault(i => i.Id == l.ContratoItemId);
                    if (item is null) continue;
                    var a = l.Chegada > inicio ? l.Chegada : inicio;
                    var b = l.Devolucao is { } d && d < fim ? d : fim;
                    if (b < a) continue;
                    var dias = b.DayNumber - a.DayNumber + 1;
                    var porDia = dias * item.PrecoUnitario;
                    // Diárias até o valor mensal: a partir daí cobra o mensal (teto por mês do calendário).
                    var porMes = item.PrecoMensal is { } pm ? pm * Math.Ceiling(dias / 31m) : decimal.MaxValue;
                    var usaMes = porMes < porDia;
                    var unit = usaMes ? porMes : porDia;
                    linhas.Add(new($"{l.Descricao} ({a:dd/MM} a {b:dd/MM}{(l.Devolucao is { } dv && dv <= fim ? " · devolvido" : "")})", l.Quantidade,
                        usaMes ? $"mensal ({dias} dias)" : $"{dias} diária(s)", unit, Math.Round(l.Quantidade * unit, 2)));
                }
                break;
        }
        return linhas;
    }

    public static string Texto(IEnumerable<LinhaPrevisto> linhas) =>
        string.Join('\n', linhas.Select(l => $"{l.Descricao} | {l.Quantidade:0.###} | {l.Base} | {l.Unitario:0.00} | {l.Total:0.00}"));
}
