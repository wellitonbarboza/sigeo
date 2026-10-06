using System.Globalization;
using ClosedXML.Excel;

namespace QualyHub.Infrastructure.Importacao;

/// <summary>Leitura de planilhas por aba com cabeçalho na linha 1 (nomes comparados sem acento, caixa e pontuação).</summary>
internal static class AbasPlanilha
{
    private static readonly CultureInfo Br = new("pt-BR");

    internal sealed class Linha(int n, Dictionary<string, IXLCell> cells, string aba, List<string> erros)
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
    internal static string Chave(string? s) => new(LeitorPlanilha.Normalizar((s ?? "").Replace("²", "2")).Where(char.IsLetterOrDigit).ToArray());

    internal static List<Linha> Linhas(XLWorkbook wb, List<string> erros, string aba, bool obrigatoria)
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
