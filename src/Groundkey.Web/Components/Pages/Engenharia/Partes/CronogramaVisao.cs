using System.Globalization;
using System.Net;
using System.Text;
using Groundkey.Domain.Planejamento;

namespace Groundkey.Web.Components.Pages.Engenharia.Partes;

/// <summary>Linha do Gantt da Syncfusion (Id inteiro; a atividade real é <see cref="AtividadeId"/>).</summary>
public sealed class LinhaGantt
{
    public int Id { get; set; }
    public int? PaiId { get; set; }
    public Guid AtividadeId { get; set; }
    public string Eap { get; set; } = "";
    public string Nome { get; set; } = "";
    public DateTime Inicio { get; set; }
    public DateTime Termino { get; set; }
    public int Dias { get; set; }
    public int Avanco { get; set; }
    public string? Dependencia { get; set; }
    public DateTime? InicioBase { get; set; }
    public DateTime? TerminoBase { get; set; }
    public string Predecessoras { get; set; } = "";
    public int? Folga { get; set; }
    public decimal Custo { get; set; }
    public int? Desvio { get; set; }
    public string Classe { get; set; } = "";
    public bool Resumo { get; set; }
}

/// <summary>Monta as linhas do Gantt, os indicadores e os desenhos (SVG) do cronograma — impressão A0–A4, linha de balanço e desembolso.</summary>
public static class CronogramaVisao
{
    private static readonly CultureInfo Br = new("pt-BR");
    private static readonly string[] Meses = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];

    public static DateTime Ini(DateOnly d) => d.ToDateTime(new TimeOnly(8, 0));
    public static DateTime Fim(DateOnly d) => d.ToDateTime(new TimeOnly(17, 0));

    public static (List<LinhaGantt> Linhas, Dictionary<Guid, int> Ids) Linhas(VersaoCronograma v, Calendario cal, IReadOnlyDictionary<Guid, int>? coresRede = null)
    {
        var filhos = v.Atividades.ToLookup(a => a.PaiId);
        var ordem = new List<AtividadeCronograma>();
        void Visitar(Guid? pai) { foreach (var a in filhos[pai].OrderBy(a => a.Ordem)) { ordem.Add(a); Visitar(a.Id); } }
        Visitar(null);
        var ids = ordem.Select((a, i) => (a.Id, i + 1)).ToDictionary(x => x.Id, x => x.Item2);
        var porId = v.Atividades.ToDictionary(a => a.Id);
        var preds = v.Vinculos.ToLookup(l => l.SucessoraId);
        var linhas = ordem.Select(a => new LinhaGantt
        {
            Id = ids[a.Id], PaiId = a.PaiId is { } p ? ids[p] : null, AtividadeId = a.Id, Eap = a.Eap, Nome = a.Nome,
            Inicio = Ini(a.Inicio), Termino = a.Marco ? Ini(a.Termino) : Fim(a.Termino), Dias = a.Duracao, Avanco = (int)Math.Round(a.Avanco),
            Dependencia = a.Resumo ? null : string.Join(",", preds[a.Id].Where(l => ids.ContainsKey(l.PredecessoraId)).Select(l =>
                $"{ids[l.PredecessoraId]}{(l.Tipo switch { TipoVinculo.InicioInicio => "SS", TipoVinculo.TerminoTermino => "FF", TipoVinculo.InicioTermino => "SF", _ => "FS" })}{(l.Lag == 0 ? "" : l.Lag > 0 ? $"+{l.Lag}" : l.Lag.ToString(CultureInfo.InvariantCulture))}")),
            InicioBase = a.InicioBase is { } bi ? Ini(bi) : null, TerminoBase = a.TerminoBase is { } bf ? Fim(bf) : null,
            Predecessoras = a.Resumo ? "" : MotorCronograma.FormatarPredecessoras(a.Id, v.Vinculos.ToList(), porId),
            Folga = a.Folga, Custo = a.Custo, Resumo = a.Resumo,
            Desvio = a.TerminoBase is { } tb ? cal.DiasUteisEntre(tb, a.Termino) : null,
            Classe = (a.Resumo ? "qh-resumo" : a.Critica ? "qh-critica" : $"qh-rede{(a.RedeId is { } r && coresRede != null && coresRede.TryGetValue(r, out var c) ? c : 0)}")
        }).ToList();
        return (linhas, ids);
    }

    private static string X(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);
    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

    /// <summary>
    /// Gantt em SVG para impressão (A0–A4): tabela (EAP, atividade, datas, %) + barras, linha de base, vínculos simplificados,
    /// caminho crítico e data de status. Escala por semana/mês conforme a largura disponível.
    /// </summary>
    public static string GanttSvg(VersaoCronograma v, Calendario cal, double largura, int nivelMaximo, bool comBase, string obra)
    {
        var filhos = v.Atividades.ToLookup(a => a.PaiId);
        var linhas = new List<(AtividadeCronograma A, int Nivel)>();
        void Visitar(Guid? pai, int n) { foreach (var a in filhos[pai].OrderBy(a => a.Ordem)) { linhas.Add((a, n)); if (n < nivelMaximo) Visitar(a.Id, n + 1); } }
        Visitar(null, 1);
        if (linhas.Count == 0) return "<svg xmlns='http://www.w3.org/2000/svg' width='400' height='40'><text x='10' y='24'>Sem atividades</text></svg>";
        var ini = linhas.Min(l => l.A.InicioBase is { } b && comBase && b < l.A.Inicio ? b : l.A.Inicio);
        var fim = linhas.Max(l => l.A.TerminoBase is { } b && comBase && b > l.A.Termino ? b : l.A.Termino);
        ini = new DateOnly(ini.Year, ini.Month, 1); fim = new DateOnly(fim.Year, fim.Month, 1).AddMonths(1);
        const double alt = 16, cab = 46, tabela = 520;
        var dias = fim.DayNumber - ini.DayNumber;
        var areaW = Math.Max(300, largura - tabela);
        var pxDia = areaW / dias;
        double Xd(DateOnly d) => tabela + (d.DayNumber - ini.DayNumber) * pxDia;
        var h = cab + linhas.Count * alt + 8;
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' width='{X(largura)}' height='{X(h)}' viewBox='0 0 {X(largura)} {X(h)}' font-family='Inter,Arial,sans-serif' font-size='9'>");
        sb.Append($"<rect x='0' y='0' width='{X(largura)}' height='{X(h)}' fill='#fff'/>");
        // cabeçalho
        sb.Append("<g fill='#475569' font-weight='600'>");
        sb.Append($"<text x='4' y='{X(cab - 8)}'>EAP</text><text x='48' y='{X(cab - 8)}'>Atividade</text><text x='{X(tabela - 210)}' y='{X(cab - 8)}'>Dur.</text><text x='{X(tabela - 170)}' y='{X(cab - 8)}'>Início</text><text x='{X(tabela - 115)}' y='{X(cab - 8)}'>Término</text><text x='{X(tabela - 60)}' y='{X(cab - 8)}'>Folga</text><text x='{X(tabela - 26)}' y='{X(cab - 8)}'>%</text>");
        sb.Append("</g>");
        for (var m = ini; m < fim; m = m.AddMonths(1))
        {
            var x = Xd(m); var w = (m.AddMonths(1).DayNumber - m.DayNumber) * pxDia;
            sb.Append($"<line x1='{X(x)}' x2='{X(x)}' y1='{X(cab - 30)}' y2='{X(h)}' stroke='#e2e8f0'/>");
            if (w > 26) sb.Append($"<text x='{X(x + 3)}' y='{X(cab - 18)}' fill='#334155' font-weight='600'>{Meses[m.Month - 1]}/{m.Year % 100:00}</text>");
        }
        sb.Append($"<line x1='0' x2='{X(largura)}' y1='{X(cab)}' y2='{X(cab)}' stroke='#94a3b8'/>");
        var pos = new Dictionary<Guid, double>();
        for (var i = 0; i < linhas.Count; i++)
        {
            var (a, n) = linhas[i]; var y = cab + i * alt; pos[a.Id] = y;
            if (i % 2 == 1) sb.Append($"<rect x='0' y='{X(y)}' width='{X(largura)}' height='{X(alt)}' fill='#f8fafc'/>");
            var peso = a.Resumo ? " font-weight='600'" : "";
            var nome = a.Nome.Length > 58 - n * 2 ? a.Nome[..(56 - n * 2)] + "…" : a.Nome;
            sb.Append($"<text x='4' y='{X(y + 11)}' fill='#475569'>{E(a.Eap)}</text><text x='{X(40 + n * 8)}' y='{X(y + 11)}'{peso}>{E(nome)}</text>");
            sb.Append($"<text x='{X(tabela - 210)}' y='{X(y + 11)}'>{a.Duracao}d</text><text x='{X(tabela - 170)}' y='{X(y + 11)}'>{a.Inicio:dd/MM/yy}</text><text x='{X(tabela - 115)}' y='{X(y + 11)}'>{a.Termino:dd/MM/yy}</text>");
            sb.Append($"<text x='{X(tabela - 60)}' y='{X(y + 11)}'{(a.Critica ? " fill='#b42318'" : "")}>{(a.Folga?.ToString(Br) ?? "")}</text><text x='{X(tabela - 26)}' y='{X(y + 11)}'>{a.Avanco:0}</text>");
            if (comBase && a.InicioBase is { } bi && a.TerminoBase is { } bf)
                sb.Append($"<rect x='{X(Xd(bi))}' y='{X(y + 11)}' width='{X(Math.Max(1.5, (bf.DayNumber - bi.DayNumber + 1) * pxDia))}' height='3' fill='#a5a8ee'/>");
            var x0 = Xd(a.Inicio); var wBar = Math.Max(a.Marco ? 0 : 1.5, (a.Termino.DayNumber - a.Inicio.DayNumber + 1) * pxDia);
            if (a.Marco) sb.Append($"<path d='M{X(x0)} {X(y + 2)} l5 6 l-5 6 l-5 -6 z' fill='{(a.Critica ? "#b42318" : "#334155")}'/>");
            else if (a.Resumo) sb.Append($"<rect x='{X(x0)}' y='{X(y + 4)}' width='{X(wBar)}' height='5' fill='#64748b'/>");
            else
            {
                var cor = a.Critica ? "#d64545" : "#4f52d6";
                sb.Append($"<rect x='{X(x0)}' y='{X(y + 3)}' width='{X(wBar)}' height='8' rx='1.5' fill='{cor}' fill-opacity='0.35'/>");
                if (a.Avanco > 0) sb.Append($"<rect x='{X(x0)}' y='{X(y + 3)}' width='{X(wBar * (double)(a.Avanco / 100m))}' height='8' rx='1.5' fill='{cor}'/>");
            }
        }
        if (nivelMaximo >= 9)
            foreach (var l in v.Vinculos)
            {
                if (!pos.TryGetValue(l.PredecessoraId, out var yp) || !pos.TryGetValue(l.SucessoraId, out var ys)) continue;
                var p = v.Atividades.First(a => a.Id == l.PredecessoraId); var s = v.Atividades.First(a => a.Id == l.SucessoraId);
                var xp = Xd(p.Termino) + pxDia; var xs = Xd(s.Inicio);
                sb.Append($"<path d='M{X(xp)} {X(yp + 7)} H{X(Math.Max(xp + 3, xs - 3))} V{X(ys + 7)} H{X(xs)}' fill='none' stroke='{(p.Critica && s.Critica ? "#d64545" : "#94a3b8")}' stroke-width='0.6'/>");
            }
        if (v.DataStatus is { } st && st >= ini && st <= fim)
            sb.Append($"<line x1='{X(Xd(st))}' x2='{X(Xd(st))}' y1='{X(cab - 30)}' y2='{X(h)}' stroke='#d4691e' stroke-dasharray='3 2'/><text x='{X(Xd(st) + 3)}' y='{X(cab - 32)}' fill='#d4691e'>status {st:dd/MM/yy}</text>");
        sb.Append($"<line x1='{X(tabela)}' x2='{X(tabela)}' y1='0' y2='{X(h)}' stroke='#94a3b8'/>");
        sb.Append("</svg>");
        return sb.ToString();
    }

    public sealed record SerieLob(string Pacote, string Cor, List<(int Linha, DateOnly Inicio, DateOnly Termino, DateOnly? InicioBase, DateOnly? TerminoBase, bool Critica)> Pontos);

    /// <summary>Linha de balanço: locais da rede no eixo vertical (de baixo para cima), tempo no horizontal, uma linha por pacote.</summary>
    public static string LinhaBalancoSvg(IReadOnlyList<string> locais, IReadOnlyList<SerieLob> series, DateOnly? status, double largura, bool comBase)
    {
        var todos = series.SelectMany(s => s.Pontos).ToList();
        if (locais.Count == 0 || todos.Count == 0) return "";
        var ini = todos.Min(p => comBase && p.InicioBase is { } b && b < p.Inicio ? b : p.Inicio);
        var fim = todos.Max(p => comBase && p.TerminoBase is { } b && b > p.Termino ? b : p.Termino);
        ini = new DateOnly(ini.Year, ini.Month, 1); fim = new DateOnly(fim.Year, fim.Month, 1).AddMonths(1);
        const double esq = 130, topo = 26, base_ = 24;
        var altLinha = Math.Clamp(420.0 / locais.Count, 9, 26);
        var h = topo + locais.Count * altLinha + base_;
        var pxDia = (largura - esq - 10) / (fim.DayNumber - ini.DayNumber);
        double Xd(DateOnly d) => esq + (d.DayNumber - ini.DayNumber) * pxDia;
        double Yl(int linha) => topo + (locais.Count - 1 - linha) * altLinha; // primeiro local embaixo
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' width='{X(largura)}' height='{X(h)}' viewBox='0 0 {X(largura)} {X(h)}' font-family='Inter,Arial,sans-serif' font-size='10' role='img' aria-label='Linha de balanço'>");
        for (var i = 0; i < locais.Count; i++)
        {
            var y = Yl(i);
            if (i % 2 == 0) sb.Append($"<rect x='{X(esq)}' y='{X(y)}' width='{X(largura - esq - 10)}' height='{X(altLinha)}' fill='#f8fafc'/>");
            if (altLinha >= 10 || i % 2 == 0) sb.Append($"<text x='{X(esq - 6)}' y='{X(y + altLinha / 2 + 3)}' text-anchor='end' fill='#475569'>{E(locais[i])}</text>");
        }
        for (var m = ini; m < fim; m = m.AddMonths(1))
        {
            sb.Append($"<line x1='{X(Xd(m))}' x2='{X(Xd(m))}' y1='{X(topo)}' y2='{X(h - base_)}' stroke='#e2e8f0'/>");
            if ((m.AddMonths(1).DayNumber - m.DayNumber) * pxDia > 28) sb.Append($"<text x='{X(Xd(m) + 3)}' y='{X(h - 8)}' fill='#334155'>{Meses[m.Month - 1]}/{m.Year % 100:00}</text>");
        }
        foreach (var s in series)
        {
            var pts = s.Pontos.OrderBy(p => p.Linha).ToList();
            if (comBase && pts.All(p => p.InicioBase is not null && p.TerminoBase is not null))
                sb.Append($"<polyline points='{string.Join(' ', pts.Select(p => $"{X(Xd(p.InicioBase!.Value))},{X(Yl(p.Linha) + altLinha / 2)}"))}' fill='none' stroke='{s.Cor}' stroke-width='1' stroke-dasharray='4 3' opacity='0.6'/>");
            foreach (var p in pts)
            {
                var y = Yl(p.Linha);
                sb.Append($"<rect x='{X(Xd(p.Inicio))}' y='{X(y + altLinha * 0.2)}' width='{X(Math.Max(2, (p.Termino.DayNumber - p.Inicio.DayNumber + 1) * pxDia))}' height='{X(altLinha * 0.6)}' rx='2' fill='{s.Cor}' fill-opacity='{(p.Critica ? "0.95" : "0.7")}'{(p.Critica ? " stroke='#b42318' stroke-width='1'" : "")}><title>{E(s.Pacote)} · {E(locais[p.Linha])}: {p.Inicio:dd/MM/yy} a {p.Termino:dd/MM/yy}</title></rect>");
            }
            sb.Append($"<polyline points='{string.Join(' ', pts.Select(p => $"{X(Xd(p.Inicio))},{X(Yl(p.Linha) + altLinha / 2)}"))}' fill='none' stroke='{s.Cor}' stroke-width='1.6'/>");
        }
        if (status is { } st && st >= ini && st <= fim)
            sb.Append($"<line x1='{X(Xd(st))}' x2='{X(Xd(st))}' y1='{X(topo - 6)}' y2='{X(h - base_)}' stroke='#d4691e' stroke-dasharray='3 2'/><text x='{X(Xd(st) + 3)}' y='{X(topo - 10)}' fill='#d4691e'>status</text>");
        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>Desembolso mensal (barras base × atual) com curvas S acumuladas (% do total da base).</summary>
    public static string DesembolsoSvg(SortedDictionary<DateOnly, decimal> atual, SortedDictionary<DateOnly, decimal> baseM, DateOnly? status, double largura)
    {
        var meses = atual.Keys.Union(baseM.Keys).Distinct().Order().ToList();
        if (meses.Count == 0) return "";
        const double esq = 70, dir = 46, topo = 14, rodape = 28, h = 300;
        var max = (double)Math.Max(1, meses.Max(m => Math.Max(atual.GetValueOrDefault(m), baseM.GetValueOrDefault(m))));
        var passo = Passo(max / 4); var teto = Math.Ceiling(max / passo) * passo;
        var bw = (largura - esq - dir) / meses.Count;
        double Y(double v) => topo + (h - topo - rodape) * (1 - v / teto);
        var totalBase = (double)baseM.Values.Sum(); var totalAtual = (double)atual.Values.Sum();
        var total = Math.Max(1, Math.Max(totalBase, totalAtual));
        double Yp(double frac) => topo + (h - topo - rodape) * (1 - frac);
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' width='{X(largura)}' height='{X(h)}' viewBox='0 0 {X(largura)} {X(h)}' font-family='Inter,Arial,sans-serif' font-size='10' role='img' aria-label='Desembolso mensal e curva S'>");
        for (var v = 0.0; v <= teto + 1; v += passo)
            sb.Append($"<line x1='{X(esq)}' x2='{X(largura - dir)}' y1='{X(Y(v))}' y2='{X(Y(v))}' stroke='#e2e8f0'/><text x='{X(esq - 6)}' y='{X(Y(v) + 3)}' text-anchor='end' fill='#64748b'>{Curto(v)}</text>");
        for (var p = 0; p <= 100; p += 25)
            sb.Append($"<text x='{X(largura - dir + 6)}' y='{X(Yp(p / 100.0) + 3)}' fill='#64748b'>{p}%</text>");
        double acB = 0, acA = 0; var linhaB = new List<string>(); var linhaA = new List<string>();
        for (var i = 0; i < meses.Count; i++)
        {
            var m = meses[i]; var x0 = esq + i * bw; var w = Math.Max(2, (bw - 6) / 2);
            double vb = (double)baseM.GetValueOrDefault(m), va = (double)atual.GetValueOrDefault(m);
            sb.Append($"<rect x='{X(x0 + 2)}' y='{X(Y(vb))}' width='{X(w)}' height='{X(Math.Max(0, h - rodape - Y(vb)))}' rx='2' fill='#4c5fd5'><title>{Meses[m.Month - 1]}/{m.Year}: base {vb.ToString("C0", Br)}</title></rect>");
            sb.Append($"<rect x='{X(x0 + 4 + w)}' y='{X(Y(va))}' width='{X(w)}' height='{X(Math.Max(0, h - rodape - Y(va)))}' rx='2' fill='#d4691e'><title>{Meses[m.Month - 1]}/{m.Year}: versão {va.ToString("C0", Br)}</title></rect>");
            acB += vb; acA += va;
            linhaB.Add($"{X(x0 + bw / 2)},{X(Yp(acB / total))}"); linhaA.Add($"{X(x0 + bw / 2)},{X(Yp(acA / total))}");
            if (meses.Count <= 18 || i % 2 == 0) sb.Append($"<text x='{X(x0 + bw / 2)}' y='{X(h - 10)}' text-anchor='middle' fill='#334155'>{Meses[m.Month - 1]}{(m.Month == 1 || i == 0 ? "/" + (m.Year % 100).ToString("00") : "")}</text>");
        }
        if (totalBase > 0) sb.Append($"<polyline points='{string.Join(' ', linhaB)}' fill='none' stroke='#4c5fd5' stroke-width='2'/>");
        sb.Append($"<polyline points='{string.Join(' ', linhaA)}' fill='none' stroke='#d4691e' stroke-width='2'/>");
        if (status is { } st)
        {
            var idx = meses.FindIndex(m => m.Year == st.Year && m.Month == st.Month);
            if (idx >= 0) { var xs = esq + idx * bw + bw * (st.Day / 31.0); sb.Append($"<line x1='{X(xs)}' x2='{X(xs)}' y1='{X(topo)}' y2='{X(h - rodape)}' stroke='#178a6b' stroke-dasharray='3 2'/><text x='{X(xs + 3)}' y='{X(topo + 8)}' fill='#178a6b'>status</text>"); }
        }
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static double Passo(double x) { var p = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(1, x)))); var n = x / p; return (n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10) * p; }
    private static string Curto(double v) => v >= 1e6 ? (v / 1e6).ToString("0.#", Br) + " mi" : v >= 1e3 ? (v / 1e3).ToString("0", Br) + " mil" : v.ToString("0", Br);
}
