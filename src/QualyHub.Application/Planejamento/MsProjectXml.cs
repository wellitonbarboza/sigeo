using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Planejamento;

namespace QualyHub.Application.Planejamento;

/// <summary>
/// Formato XML do MS Project (MSPDI): exporta EAP, durações, datas, avanço, custo e vínculos (com folga);
/// importa o mesmo formato para uma versão em elaboração. Dia = 8 h; folga em décimos de minuto (padrão do MSPDI).
/// </summary>
public static partial class MsProjectXml
{
    private static readonly XNamespace Ns = "http://schemas.microsoft.com/project";
    private const int MinutosDia = 480;

    public sealed record Lido(List<AtividadeCronograma> Atividades, List<VinculoAtividade> Vinculos, DateOnly? Inicio);

    private static int TipoMsp(TipoVinculo t) => t switch
    {
        TipoVinculo.TerminoTermino => 0, TipoVinculo.InicioTermino => 2, TipoVinculo.InicioInicio => 3, _ => 1
    };

    private static TipoVinculo TipoQh(int t) => t switch
    {
        0 => TipoVinculo.TerminoTermino, 2 => TipoVinculo.InicioTermino, 3 => TipoVinculo.InicioInicio, _ => TipoVinculo.TerminoInicio
    };

    public static string Escrever(string nomeProjeto, DateOnly inicio, IReadOnlyCollection<AtividadeCronograma> atividades, IReadOnlyCollection<VinculoAtividade> vinculos)
    {
        static string Dt(DateOnly d, bool fim) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + (fim ? "T17:00:00" : "T08:00:00");
        var filhos = atividades.ToLookup(a => a.PaiId);
        var ordem = new List<(AtividadeCronograma A, int Nivel)>();
        void Visitar(Guid? pai, int nivel) { foreach (var a in filhos[pai].OrderBy(a => a.Ordem)) { ordem.Add((a, nivel)); Visitar(a.Id, nivel + 1); } }
        Visitar(null, 1);
        var uid = ordem.Select((x, i) => (x.A.Id, i + 1)).ToDictionary(x => x.Id, x => x.Item2);
        var preds = vinculos.ToLookup(v => v.SucessoraId);

        var tasks = new XElement(Ns + "Tasks");
        foreach (var (a, nivel) in ordem)
        {
            var t = new XElement(Ns + "Task",
                new XElement(Ns + "UID", uid[a.Id]), new XElement(Ns + "ID", uid[a.Id]), new XElement(Ns + "Name", a.Nome),
                new XElement(Ns + "WBS", a.Eap), new XElement(Ns + "OutlineNumber", a.Eap), new XElement(Ns + "OutlineLevel", nivel),
                new XElement(Ns + "Summary", a.Resumo ? 1 : 0), new XElement(Ns + "Milestone", a.Marco ? 1 : 0),
                new XElement(Ns + "Start", Dt(a.Inicio, false)), new XElement(Ns + "Finish", Dt(a.Termino, !a.Marco)),
                new XElement(Ns + "Duration", $"PT{Math.Max(0, a.Duracao) * 8}H0M0S"), new XElement(Ns + "DurationFormat", 7),
                new XElement(Ns + "PercentComplete", Math.Round(a.Avanco)), new XElement(Ns + "Cost", Math.Round(a.Custo * 100)));
            if (a.InicioBase is { } bi && a.TerminoBase is { } bf)
                t.Add(new XElement(Ns + "Baseline", new XElement(Ns + "Number", 0), new XElement(Ns + "Start", Dt(bi, false)), new XElement(Ns + "Finish", Dt(bf, true))));
            foreach (var v in preds[a.Id].Where(v => uid.ContainsKey(v.PredecessoraId)))
                t.Add(new XElement(Ns + "PredecessorLink", new XElement(Ns + "PredecessorUID", uid[v.PredecessoraId]), new XElement(Ns + "Type", TipoMsp(v.Tipo)),
                    new XElement(Ns + "LinkLag", v.Lag * MinutosDia * 10), new XElement(Ns + "LagFormat", 7)));
            tasks.Add(t);
        }
        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(Ns + "Project",
                new XElement(Ns + "Name", nomeProjeto), new XElement(Ns + "Title", nomeProjeto),
                new XElement(Ns + "StartDate", Dt(inicio, false)), new XElement(Ns + "MinutesPerDay", MinutosDia), new XElement(Ns + "MinutesPerWeek", MinutosDia * 5),
                new XElement(Ns + "DaysPerMonth", 20), new XElement(Ns + "CurrencySymbol", "R$"), tasks));
        using var sw = new Utf8Writer();
        doc.Save(sw);
        return sw.ToString();
    }

    private sealed class Utf8Writer : StringWriter { public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8; }

    [GeneratedRegex(@"^PT(?:(?<h>\d+(?:\.\d+)?)H)?(?:(?<m>\d+(?:\.\d+)?)M)?(?:(?<s>\d+(?:\.\d+)?)S)?$")]
    private static partial Regex Duracao();

    public static Lido Ler(Stream xml)
    {
        XDocument doc;
        try
        {
            using var r = XmlReader.Create(xml, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            doc = XDocument.Load(r);
        }
        catch (XmlException) { throw new RegraDeNegocioException("Arquivo não é um XML do MS Project válido (salve como \"Formato XML\" no Project)."); }
        var proj = doc.Root;
        if (proj is null || proj.Name.LocalName != "Project") throw new RegraDeNegocioException("O XML não é do MS Project (falta o elemento Project).");
        var ns = proj.Name.Namespace;
        string? V(XElement e, string n) => e.Element(ns + n)?.Value;
        static DateOnly? Data(string? s) => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? DateOnly.FromDateTime(d) : null;

        var atividades = new List<AtividadeCronograma>();
        var porUid = new Dictionary<int, AtividadeCronograma>();
        var pilha = new List<AtividadeCronograma>();
        var linksBrutos = new List<(int Pred, int Suc, int Tipo, long Lag, int Formato)>();
        foreach (var t in proj.Element(ns + "Tasks")?.Elements(ns + "Task") ?? [])
        {
            if (!int.TryParse(V(t, "UID"), out var uid) || uid == 0) continue;           // UID 0 = resumo do projeto
            if (V(t, "IsNull") == "1" || string.IsNullOrWhiteSpace(V(t, "Name"))) continue;
            var nivel = int.TryParse(V(t, "OutlineLevel"), out var n) ? Math.Max(1, n) : 1;
            var dur = 0;
            var m = Duracao().Match(V(t, "Duration") ?? "");
            if (m.Success)
            {
                var horas = (m.Groups["h"].Success ? double.Parse(m.Groups["h"].Value, CultureInfo.InvariantCulture) : 0)
                          + (m.Groups["m"].Success ? double.Parse(m.Groups["m"].Value, CultureInfo.InvariantCulture) / 60 : 0);
                dur = (int)Math.Round(horas / 8, MidpointRounding.AwayFromZero);
            }
            var a = new AtividadeCronograma
            {
                Nome = V(t, "Name")!.Trim()[..Math.Min(250, V(t, "Name")!.Trim().Length)],
                Resumo = V(t, "Summary") == "1",
                Duracao = V(t, "Milestone") == "1" ? 0 : Math.Max(V(t, "Summary") == "1" ? 0 : 1, dur),
                Avanco = decimal.TryParse(V(t, "PercentComplete"), NumberStyles.Number, CultureInfo.InvariantCulture, out var pc) ? Math.Clamp(pc, 0, 100) : 0,
                Custo = decimal.TryParse(V(t, "Cost"), NumberStyles.Number, CultureInfo.InvariantCulture, out var c) ? Math.Round(c / 100, 2) : 0,
                Inicio = Data(V(t, "Start")) ?? default, Termino = Data(V(t, "Finish")) ?? default
            };
            var b = t.Elements(ns + "Baseline").FirstOrDefault(x => V(x, "Number") is null or "0");
            if (b is not null) { a.InicioBase = Data(V(b, "Start")); a.TerminoBase = Data(V(b, "Finish")); }
            while (pilha.Count >= nivel) pilha.RemoveAt(pilha.Count - 1);
            a.PaiId = pilha.Count > 0 ? pilha[^1].Id : null;
            if (a.PaiId is { } pai) atividades.First(x => x.Id == pai).Resumo = true;
            a.Ordem = atividades.Count(x => x.PaiId == a.PaiId) + 1;
            pilha.Add(a);
            atividades.Add(a); porUid[uid] = a;
            foreach (var l in t.Elements(ns + "PredecessorLink"))
                if (int.TryParse(V(l, "PredecessorUID"), out var p))
                    linksBrutos.Add((p, uid, int.TryParse(V(l, "Type"), out var tp) ? tp : 1, long.TryParse(V(l, "LinkLag"), out var lag) ? lag : 0, int.TryParse(V(l, "LagFormat"), out var lf) ? lf : 7));
        }
        if (atividades.Count == 0) throw new RegraDeNegocioException("O arquivo não tem tarefas.");
        // tarefas resumo do Project não levam custo/duração próprios: o motor soma dos filhos
        foreach (var a in atividades.Where(a => a.Resumo)) { a.Custo = 0; a.Duracao = 0; }
        var vinculos = new List<VinculoAtividade>();
        foreach (var (p, s, tipo, lag, _) in linksBrutos)
        {
            if (!porUid.TryGetValue(p, out var pa) || !porUid.TryGetValue(s, out var sa) || pa.Resumo || sa.Resumo || pa.Id == sa.Id) continue;
            if (vinculos.Any(v => v.PredecessoraId == pa.Id && v.SucessoraId == sa.Id)) continue;
            vinculos.Add(new VinculoAtividade { PredecessoraId = pa.Id, SucessoraId = sa.Id, Tipo = TipoQh(tipo), Lag = (int)Math.Round(lag / (MinutosDia * 10.0)) });
        }
        // folhas sem predecessora mantêm a data do Project como início mínimo
        var comPred = vinculos.Select(v => v.SucessoraId).ToHashSet();
        foreach (var a in atividades.Where(a => !a.Resumo && !comPred.Contains(a.Id) && a.Inicio != default)) a.InicioMinimo = a.Inicio;
        var inicio = Data(V(proj, "StartDate")) ?? atividades.Where(a => a.Inicio != default).Select(a => (DateOnly?)a.Inicio).Min();
        return new Lido(atividades, vinculos, inicio);
    }
}
