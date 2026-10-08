using System.Globalization;
using System.Text.RegularExpressions;
using QualyHub.Domain.Aprovacoes;

namespace QualyHub.Domain.Planejamento;

/// <summary>
/// Motor de rede (método do caminho crítico) em dias úteis: ida (datas mais cedo), volta (datas mais tarde), folga total,
/// caminho crítico das atividades não concluídas e soma dos agrupadores. Função pura: o mesmo cálculo roda no servidor e nos testes.
/// Convenção: es/ef = índices de dia útil; ef = es + duração − 1 (marco: duração 0, ef = es − 1).
/// </summary>
public static partial class MotorCronograma
{
    public sealed record Resultado(DateOnly Termino, int Criticas, int Atividades);

    public static Resultado Calcular(IReadOnlyCollection<AtividadeCronograma> atividades, IReadOnlyCollection<VinculoAtividade> vinculos,
        DateOnly inicioProjeto, Calendario cal)
    {
        var folhas = atividades.Where(a => !a.Resumo).ToList();
        var porId = folhas.ToDictionary(a => a.Id);
        var validos = vinculos.Where(v => v.PredecessoraId != v.SucessoraId && porId.ContainsKey(v.PredecessoraId) && porId.ContainsKey(v.SucessoraId)).ToList();
        var preds = validos.ToLookup(v => v.SucessoraId);
        var sucs = validos.ToLookup(v => v.PredecessoraId);
        var ordem = OrdemTopologica(folhas, validos);
        var b0 = cal.Indice(inicioProjeto);

        var es = new Dictionary<Guid, int>(); var ef = new Dictionary<Guid, int>();
        foreach (var a in ordem)
        {
            var d = Math.Max(0, a.Duracao);
            var inicio = b0;
            if (!preds[a.Id].Any() && a.InicioMinimo is { } min) inicio = Math.Max(inicio, cal.Indice(min));
            foreach (var v in preds[a.Id]) inicio = Math.Max(inicio, Restricao(v, es[v.PredecessoraId], ef[v.PredecessoraId], d));
            es[a.Id] = inicio; ef[a.Id] = inicio + d - 1;
        }
        var fimProjeto = folhas.Count == 0 ? b0 : folhas.Max(a => Math.Max(ef[a.Id], es[a.Id] - 1));

        var lf = folhas.ToDictionary(a => a.Id, _ => fimProjeto);
        for (var i = ordem.Count - 1; i >= 0; i--)
        {
            var a = ordem[i]; var d = Math.Max(0, a.Duracao);
            var limite = fimProjeto;
            foreach (var v in sucs[a.Id])
            {
                var s = porId[v.SucessoraId]; var ds = Math.Max(0, s.Duracao);
                var lfs = lf[s.Id]; var lss = lfs - ds + 1;
                var l = v.Tipo switch
                {
                    TipoVinculo.TerminoInicio => lss - 1 - v.Lag,
                    TipoVinculo.InicioInicio => lss - v.Lag + d - 1,
                    TipoVinculo.TerminoTermino => lfs - v.Lag,
                    _ => lfs - v.Lag + d - 1
                };
                limite = Math.Min(limite, l);
            }
            lf[a.Id] = limite;
        }

        var criticas = 0;
        foreach (var a in folhas)
        {
            var d = Math.Max(0, a.Duracao);
            a.Inicio = cal.Data(d == 0 && es[a.Id] > b0 ? es[a.Id] - 1 : es[a.Id]);
            a.Termino = d == 0 ? a.Inicio : cal.Data(ef[a.Id]);
            a.Folga = lf[a.Id] - ef[a.Id];
            a.Critica = a.Folga <= 0 && !a.Concluida;
            if (a.Critica) criticas++;
        }
        Somar(atividades, inicioProjeto, cal);
        return new Resultado(cal.Data(fimProjeto), criticas, folhas.Count);
    }

    /// <summary>Início mais cedo exigido pelo vínculo (es/ef da predecessora; d = duração da sucessora).</summary>
    private static int Restricao(VinculoAtividade v, int esP, int efP, int d) => v.Tipo switch
    {
        TipoVinculo.TerminoInicio => efP + 1 + v.Lag,
        TipoVinculo.InicioInicio => esP + v.Lag,
        TipoVinculo.TerminoTermino => efP + v.Lag - d + 1,
        _ => esP + v.Lag - d + 1
    };

    private static List<AtividadeCronograma> OrdemTopologica(List<AtividadeCronograma> folhas, List<VinculoAtividade> vinculos)
    {
        var grau = folhas.ToDictionary(a => a.Id, _ => 0);
        foreach (var v in vinculos) grau[v.SucessoraId]++;
        var sucs = vinculos.ToLookup(v => v.PredecessoraId, v => v.SucessoraId);
        var porId = folhas.ToDictionary(a => a.Id);
        var fila = new Queue<AtividadeCronograma>(folhas.Where(a => grau[a.Id] == 0).OrderBy(a => a.Ordem));
        var r = new List<AtividadeCronograma>(folhas.Count);
        while (fila.Count > 0)
        {
            var a = fila.Dequeue(); r.Add(a);
            foreach (var s in sucs[a.Id]) if (--grau[s] == 0) fila.Enqueue(porId[s]);
        }
        if (r.Count < folhas.Count)
        {
            var nomes = folhas.Where(a => grau[a.Id] > 0).Take(3).Select(a => $"{a.Eap} {a.Nome}".Trim());
            throw new RegraDeNegocioException($"Vínculos em ciclo (uma atividade acaba esperando por ela mesma): {string.Join("; ", nomes)}.");
        }
        return r;
    }

    /// <summary>Agrupadores: menor início, maior término, custo somado e avanço ponderado pelo custo (ou duração).</summary>
    public static void Somar(IReadOnlyCollection<AtividadeCronograma> atividades, DateOnly inicioProjeto, Calendario cal)
    {
        var filhos = atividades.ToLookup(a => a.PaiId);
        void Visitar(AtividadeCronograma p)
        {
            var fs = filhos[p.Id].ToList();
            foreach (var f in fs.Where(f => f.Resumo)) Visitar(f);
            if (fs.Count == 0) { p.Inicio = p.Termino = inicioProjeto; p.Duracao = 0; p.Custo = 0; p.Folga = null; p.Critica = false; return; }
            p.Inicio = fs.Min(f => f.Inicio); p.Termino = fs.Max(f => f.Termino);
            p.Duracao = cal.DiasUteisEntre(p.Inicio, p.Termino) + (cal.EhUtil(p.Termino) ? 1 : 0);
            p.Custo = fs.Sum(f => f.Custo);
            var pesoTotal = p.Custo > 0 ? p.Custo : fs.Sum(f => (decimal)Math.Max(1, f.Duracao));
            p.Avanco = pesoTotal == 0 ? 0 : Math.Round(fs.Sum(f => f.Avanco * (p.Custo > 0 ? f.Custo : Math.Max(1, f.Duracao))) / pesoTotal, 1);
            p.Folga = fs.Where(f => f.Folga != null).Select(f => f.Folga).DefaultIfEmpty(null).Min();
            p.Critica = fs.Any(f => f.Critica);
        }
        foreach (var r in atividades.Where(a => a.Resumo && a.PaiId == null)) Visitar(r);
    }

    /// <summary>Numeração da EAP pela hierarquia e ordem (1, 1.1, 1.1.1…).</summary>
    public static void Renumerar(IReadOnlyCollection<AtividadeCronograma> atividades)
    {
        var filhos = atividades.ToLookup(a => a.PaiId);
        void Numerar(Guid? pai, string prefixo)
        {
            var i = 0;
            foreach (var a in filhos[pai].OrderBy(a => a.Ordem))
            {
                a.Ordem = ++i;
                a.Eap = prefixo.Length == 0 ? i.ToString(CultureInfo.InvariantCulture) : $"{prefixo}.{i}";
                Numerar(a.Id, a.Eap);
            }
        }
        Numerar(null, "");
    }

    /// <summary>
    /// "Arrastar só move": atividade com predecessora muda a folga (lag) do vínculo que a segura; sem predecessora, muda o início mínimo.
    /// Não grava restrição de data. Retorna os vínculos alterados.
    /// </summary>
    public static List<VinculoAtividade> Mover(AtividadeCronograma a, DateOnly novoInicio, IReadOnlyCollection<AtividadeCronograma> atividades,
        IReadOnlyCollection<VinculoAtividade> vinculos, Calendario cal)
    {
        if (a.Resumo) throw new RegraDeNegocioException("Mova as atividades, não o agrupador.");
        var alvo = cal.Indice(cal.ProximoUtil(novoInicio));
        var porId = atividades.ToDictionary(x => x.Id);
        var preds = vinculos.Where(v => v.SucessoraId == a.Id && porId.ContainsKey(v.PredecessoraId)).ToList();
        var mudados = new List<VinculoAtividade>();
        if (preds.Count == 0) { a.InicioMinimo = cal.Data(alvo); return mudados; }
        var d = Math.Max(0, a.Duracao);
        var restr = preds.Select(v =>
        {
            var p = porId[v.PredecessoraId];
            int esP, efP;
            if (p.Marco) { efP = cal.Indice(p.Termino); esP = efP + 1; }
            else { esP = cal.Indice(p.Inicio); efP = cal.Indice(p.Termino); }
            return (v, c: Restricao(v, esP, efP, d));
        }).ToList();
        var maior = restr.Max(r => r.c);
        if (alvo > maior)
        {
            var (v, c) = restr.First(r => r.c == maior);
            v.Lag += alvo - c; mudados.Add(v);
        }
        else
            foreach (var (v, c) in restr.Where(r => r.c > alvo)) { v.Lag -= c - alvo; mudados.Add(v); }
        return mudados;
    }

    // ───────── Predecessoras em texto (padrão "1.2.3TI+2; 4II-1"), por EAP ─────────

    [GeneratedRegex(@"^\s*(?<eap>\d+(?:\.\d+)*)\s*(?<tipo>TI|II|TT|IT|FS|SS|FF|SF)?\s*(?<lag>[+-]\s*\d+)?\s*(?:d|du|dias?)?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex RegexPredecessora();

    public sealed record Predecessora(string Eap, TipoVinculo Tipo, int Lag);

    public static List<Predecessora> LerPredecessoras(string? texto)
    {
        var r = new List<Predecessora>();
        if (string.IsNullOrWhiteSpace(texto)) return r;
        foreach (var parte in texto.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var m = RegexPredecessora().Match(parte);
            if (!m.Success) throw new RegraDeNegocioException($"Predecessora \"{parte}\" inválida. Use a EAP com tipo e folga, ex.: 1.2.3TI+2, 4II, 2.1TT-1.");
            var tipo = (m.Groups["tipo"].Success ? m.Groups["tipo"].Value.ToUpperInvariant() : "TI") switch
            {
                "II" or "SS" => TipoVinculo.InicioInicio,
                "TT" or "FF" => TipoVinculo.TerminoTermino,
                "IT" or "SF" => TipoVinculo.InicioTermino,
                _ => TipoVinculo.TerminoInicio
            };
            var lag = m.Groups["lag"].Success ? int.Parse(m.Groups["lag"].Value.Replace(" ", ""), CultureInfo.InvariantCulture) : 0;
            r.Add(new Predecessora(m.Groups["eap"].Value, tipo, lag));
        }
        return r;
    }

    public static string Sigla(TipoVinculo t) => t switch
    {
        TipoVinculo.InicioInicio => "II",
        TipoVinculo.TerminoTermino => "TT",
        TipoVinculo.InicioTermino => "IT",
        _ => "TI"
    };

    public static string FormatarPredecessoras(Guid sucessoraId, IReadOnlyCollection<VinculoAtividade> vinculos, IReadOnlyDictionary<Guid, AtividadeCronograma> porId) =>
        string.Join("; ", vinculos.Where(v => v.SucessoraId == sucessoraId && porId.ContainsKey(v.PredecessoraId))
            .Select(v => (eap: porId[v.PredecessoraId].Eap, v))
            .OrderBy(x => x.eap, StringComparer.Ordinal)
            .Select(x => x.eap + Sigla(x.v.Tipo) + (x.v.Lag == 0 ? "" : x.v.Lag > 0 ? $"+{x.v.Lag}" : x.v.Lag.ToString(CultureInfo.InvariantCulture))));
}
