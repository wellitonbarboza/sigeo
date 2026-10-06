using System.Globalization;
using ClosedXML.Excel;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;

namespace QualyHub.Infrastructure.Importacao;

/// <summary>
/// Lê a planilha do kit de tipologia (abas Tipologias, Ambientes, Mix, Serviços e Aplicação; cabeçalho na linha 1).
/// Cabeçalhos e listas (tipo, escopo, regra, nível) são comparados sem acento e sem caixa.
/// </summary>
public static class LeitorKitTipologia
{
    private static readonly CultureInfo Br = new("pt-BR");

    public static KitTipologia Ler(Stream arquivo)
    {
        using var wb = Planilha.Abrir(arquivo);
        var erros = new List<string>();
        var tips = Linhas(wb, erros, "Tipologias", true).Select(l => new KitTip(l.N,
            l.Txt("codigo"), l.Txt("nome"), l.Enum("tipo", TiposTipologia), l.Int("dormitorios"), l.Int("suites"), l.Dec("areaprivativam2") ?? l.Dec("areaprivativa") ?? 0,
            l.Dec("areaequivalentem2") ?? l.Dec("areaequivalente"), l.Int("pavimentos"), l.Bool("primeiroterreo", true), l.Bool("numeracaoporandar", false),
            l.Dec("pedireitopadraom") ?? l.Dec("pedireitopadrao"), l.TxtOpc("observacoes"))).ToList();
        var ambs = Linhas(wb, erros, "Ambientes", true).Select(l => new KitAmbiente(l.N,
            l.Txt("tipologia"), l.Int("ordem"), l.Enum("escopo", Escopos), l.Txt("nome"), l.Txt("sigla"), l.TxtOpc("categoria"),
            l.Dec("areapisom2") ?? l.Dec("areapiso") ?? 0, l.Dec("perimetrom") ?? l.Dec("perimetro") ?? 0, l.Dec("pedireitom") ?? l.Dec("pedireito"),
            l.Dec("areavaosm2") ?? l.Dec("areavaos") ?? 0, l.IntOpc("pavde"), l.IntOpc("pavate"))).ToList();
        var mix = Linhas(wb, erros, "Mix", false).Select(l => new KitMix(l.N,
            l.Txt("torre"), l.Int("pavde"), l.Int("pavate"), l.Txt("apartamento"), l.Int("aptosporpavimento") is var q and > 0 ? q : l.Int("quantidade"))).ToList();
        var servs = Linhas(wb, erros, "Servicos", true).Select(l => new KitServico(l.N,
            l.Txt("codigo"), l.Txt("descricao"), l.Txt("unidade"), l.TxtOpc("grupo") ?? l.TxtOpc("grupoeap"), l.Enum("regra", Regras),
            l.Dec("fator") ?? 1, l.TxtOpc("its"), l.TxtOpc("observacao"))).ToList();
        var aps = Linhas(wb, erros, "Aplicacao", true).Select(l => new KitAplicacao(l.N,
            l.Txt("tipologia"), l.Txt("servico"), l.Enum("nivel", Alvos), l.TxtOpc("ambiente"), l.Dec("quantidadefixa"))).ToList();
        ValidacaoException.Se(erros);
        return new KitTipologia(tips, ambs, mix, servs, aps);
    }

    private static readonly Dictionary<string, TipoTipologia> TiposTipologia = new()
    { ["apartamento"] = TipoTipologia.Apartamento, ["torre"] = TipoTipologia.Torre, ["implantacao"] = TipoTipologia.Implantacao };
    private static readonly Dictionary<string, EscopoAmbiente> Escopos = new()
    {
        ["comodo"] = EscopoAmbiente.Comodo, ["pavimento"] = EscopoAmbiente.AreaComumPavimento, ["areacomumdopavimento"] = EscopoAmbiente.AreaComumPavimento,
        ["torre"] = EscopoAmbiente.AreaComumTorre, ["areacomumdatorre"] = EscopoAmbiente.AreaComumTorre,
        ["condominio"] = EscopoAmbiente.AreaComumCondominio, ["areacomumdocondominio"] = EscopoAmbiente.AreaComumCondominio
    };
    private static readonly Dictionary<string, RegraQuantidade> Regras = new()
    {
        ["areadepiso"] = RegraQuantidade.AreaPiso, ["areapiso"] = RegraQuantidade.AreaPiso, ["areadeparede"] = RegraQuantidade.AreaParede, ["areaparede"] = RegraQuantidade.AreaParede,
        ["perimetro"] = RegraQuantidade.Perimetro, ["areadeteto"] = RegraQuantidade.AreaTeto, ["areateto"] = RegraQuantidade.AreaTeto,
        ["unidade"] = RegraQuantidade.Unidade, ["fixa"] = RegraQuantidade.Fixa, ["quantidadefixa"] = RegraQuantidade.Fixa
    };
    private static readonly Dictionary<string, AlvoServico> Alvos = new()
    {
        ["ambiente"] = AlvoServico.Ambiente, ["apartamento"] = AlvoServico.Apartamento, ["pavimento"] = AlvoServico.Pavimento,
        ["torre"] = AlvoServico.Torre, ["condominio"] = AlvoServico.Condominio
    };

    private sealed class Linha(int n, Dictionary<string, IXLCell> cells, string aba, List<string> erros)
    {
        public int N => n;
        private IXLCell? C(string k) => cells.GetValueOrDefault(k);
        public string? TxtOpc(string k) => C(k)?.GetString().Trim() is { Length: > 0 } s ? s : null;
        public string Txt(string k) => TxtOpc(k) ?? "";

        public decimal? Dec(string k)
        {
            var c = C(k);
            if (c is null || c.IsEmpty()) return null;
            if (c.DataType == XLDataType.Number) return (decimal)c.GetDouble();
            var s = c.GetString().Trim();
            if (s.Length == 0) return null;
            if (decimal.TryParse(s, NumberStyles.Number, Br, out var v)) return v;
            erros.Add($"{aba} linha {n}: valor \"{s}\" não é número.");
            return null;
        }

        public int? IntOpc(string k) => Dec(k) is { } d ? (int)Math.Round(d) : null;
        public int Int(string k) => IntOpc(k) ?? 0;

        public bool Bool(string k, bool padrao) => LeitorPlanilha.Normalizar(TxtOpc(k)) switch
        {
            "sim" or "s" or "true" or "1" or "x" => true,
            "nao" or "n" or "false" or "0" => false,
            _ => padrao
        };

        public T Enum<T>(string k, Dictionary<string, T> mapa) where T : struct
        {
            var s = Chave(TxtOpc(k));
            if (mapa.TryGetValue(s, out var v)) return v;
            erros.Add($"{aba} linha {n}: \"{TxtOpc(k)}\" inválido em {k} (use {string.Join(", ", mapa.Keys.Distinct().Take(6))}).");
            return default;
        }
    }

    /// <summary>Cabeçalho sem acento, caixa, espaço e pontuação: "Área piso (m²)" → "areapisom2"; "Pav. de" → "pavde".</summary>
    private static string Chave(string? s) => new(LeitorPlanilha.Normalizar((s ?? "").Replace("²", "2")).Where(char.IsLetterOrDigit).ToArray());

    private static List<Linha> Linhas(XLWorkbook wb, List<string> erros, string aba, bool obrigatoria)
    {
        var ws = wb.Worksheets.FirstOrDefault(w => LeitorPlanilha.Normalizar(w.Name) == LeitorPlanilha.Normalizar(aba));
        if (ws is null)
        {
            if (obrigatoria) erros.Add($"Aba \"{aba}\" não encontrada.");
            return [];
        }
        var cab = ws.Row(1);
        var ultimaCol = cab.LastCellUsed()?.Address.ColumnNumber ?? 0;
        var cols = Enumerable.Range(1, ultimaCol).Select(c => (c, Nome: Chave(cab.Cell(c).GetString()))).ToList();
        var lista = new List<Linha>();
        var ultima = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= ultima; r++)
        {
            var row = ws.Row(r);
            if (cols.All(c => row.Cell(c.c).IsEmpty())) continue;
            if (LeitorPlanilha.Normalizar(row.Cell(1).GetString()).StartsWith('#')) continue; // linha de comentário
            lista.Add(new Linha(r, cols.Where(c => c.Nome.Length > 0).GroupBy(c => c.Nome).ToDictionary(g => g.Key, g => row.Cell(g.First().c)), ws.Name, erros));
        }
        return lista;
    }
}
