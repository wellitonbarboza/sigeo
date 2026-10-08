using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using QualyHub.Domain.Importacao;

namespace QualyHub.Infrastructure.Importacao;

/// <summary>Linha lida do arquivo, já traduzida para os nomes de campo do QualyHub via layout.</summary>
public sealed record LinhaLida(int NumeroLinha, IReadOnlyDictionary<string, string?> Campos)
{
    public string? this[string campo] => Campos.TryGetValue(campo, out var v) ? v : null;
}

/// <summary>
/// Lê .xlsx ou .csv exportado do ERP e aplica o mapeamento de colunas do layout.
/// Cabeçalhos são comparados sem acento/caixa/espaços — tolera pequenas variações entre versões do export.
/// </summary>
public static class LeitorPlanilha
{
    public static IReadOnlyList<LinhaLida> Ler(Stream arquivo, string nomeArquivo, LayoutImportacao layout)
    {
        var mapa = JsonSerializer.Deserialize<Dictionary<string, string>>(layout.MapeamentoJson)
                   ?? throw new InvalidOperationException("Layout sem mapeamento de colunas.");

        var bruto = Path.GetExtension(nomeArquivo).ToLowerInvariant() switch
        {
            ".xlsx" or ".xlsm" => LerExcel(arquivo, layout),
            ".csv" or ".txt" => LerCsv(arquivo, layout),
            var ext => throw new NotSupportedException($"Formato {ext} não suportado. Exporte do ERP em .xlsx ou .csv.")
        };

        var faltando = mapa.Values.Where(col => !bruto.Cabecalhos.Contains(Normalizar(col))).ToList();
        if (faltando.Count > 0)
            throw new InvalidOperationException($"Colunas não encontradas no arquivo: {string.Join(", ", faltando)}. Verifique o layout de export.");

        return bruto.Linhas
            .Select(l => new LinhaLida(l.Numero, mapa.ToDictionary(m => m.Key, m => l.Valores.GetValueOrDefault(Normalizar(m.Value)))))
            .Where(l => l.Campos.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
            .ToList();
    }

    private sealed record Bruto(HashSet<string> Cabecalhos, List<(int Numero, Dictionary<string, string?> Valores)> Linhas);

    private static Bruto LerExcel(Stream s, LayoutImportacao layout)
    {
        using var wb = Planilha.Abrir(s);
        var ws = layout.NomePlanilha is { Length: > 0 } nome ? wb.Worksheet(nome) : wb.Worksheets.First();
        var linhaCab = ws.Row(layout.LinhaCabecalho);
        var ultimaCol = linhaCab.LastCellUsed()?.Address.ColumnNumber ?? 0;
        var cab = Enumerable.Range(1, ultimaCol).ToDictionary(c => c, c => Normalizar(linhaCab.Cell(c).GetString()));

        var linhas = new List<(int, Dictionary<string, string?>)>();
        var ultima = ws.LastRowUsed()?.RowNumber() ?? layout.LinhaCabecalho;
        for (var r = layout.LinhaCabecalho + 1; r <= ultima; r++)
        {
            var row = ws.Row(r);
            var valores = new Dictionary<string, string?>();
            foreach (var (col, nomeCab) in cab)
            {
                if (string.IsNullOrEmpty(nomeCab)) continue;
                var cell = row.Cell(col);
                valores[nomeCab] = cell.DataType switch
                {
                    XLDataType.DateTime => cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    XLDataType.Number => cell.GetDouble().ToString(new CultureInfo("pt-BR")), // "1,234": mesma regra do CSV, sem ambiguidade de milhar
                    _ => cell.GetString().Trim()
                };
            }
            linhas.Add((r, valores));
        }
        return new Bruto([.. cab.Values.Where(v => v.Length > 0)], linhas);
    }

    private static Bruto LerCsv(Stream s, LayoutImportacao layout)
    {
        // Exports brasileiros costumam vir em Windows-1252; detecta BOM UTF-8, senão usa Latin1.
        using var reader = new StreamReader(s, Encoding.Latin1, detectEncodingFromByteOrderMarks: true);
        for (var i = 1; i < layout.LinhaCabecalho; i++) reader.ReadLine();

        var cfg = new CsvConfiguration(new CultureInfo("pt-BR")) { Delimiter = layout.Delimitador, BadDataFound = null, MissingFieldFound = null };
        using var csv = new CsvReader(reader, cfg);
        csv.Read(); csv.ReadHeader();
        var cab = csv.HeaderRecord!.Select(Normalizar).ToArray();

        var linhas = new List<(int, Dictionary<string, string?>)>();
        var n = layout.LinhaCabecalho;
        while (csv.Read())
        {
            n++;
            var valores = new Dictionary<string, string?>();
            for (var i = 0; i < cab.Length; i++) valores[cab[i]] = csv.GetField(i)?.Trim();
            linhas.Add((n, valores));
        }
        return new Bruto([.. cab.Where(c => c.Length > 0)], linhas);
    }

    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var sb = new StringBuilder();
        foreach (var ch in texto.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark && !char.IsWhiteSpace(ch))
                sb.Append(char.ToLowerInvariant(ch));
        return sb.ToString();
    }
}

/// <summary>Abre .xlsx enviado pelo usuário: arquivo corrompido/formato errado vira mensagem legível (não derruba a tela).</summary>
public static class Planilha
{
    public static ClosedXML.Excel.XLWorkbook Abrir(Stream arquivo)
    {
        try { return new ClosedXML.Excel.XLWorkbook(arquivo); }
        catch (Exception e) when (e is InvalidDataException or ArgumentException or IOException
                                       or InvalidOperationException or System.Xml.XmlException or NotSupportedException)
        { throw new QualyHub.Domain.Aprovacoes.RegraDeNegocioException("Não foi possível ler a planilha: envie um arquivo .xlsx válido (Excel 2007 ou superior)."); }
    }
}
