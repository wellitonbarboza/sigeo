using System.Globalization;
using ExcelDataReader;
using Sigeo.Domain.Aprovacoes;

namespace Sigeo.Infrastructure.Importacao;

/// <summary>Relatórios do ERP reconhecidos pelo importador de histórico da obra (pelo cabeçalho, não pelo nome do arquivo).</summary>
public enum TipoRelatorioErp { Desconhecido = 0, Apropriacoes = 1, MedicaoOrdensCompra = 2, Fornecedores = 3, Insumos = 4, DocumentosFinanceiros = 5 }

/// <summary>Linha de relatório: valores pelo cabeçalho normalizado (sem acento, caixa e espaços).</summary>
public sealed class LinhaRelatorio(int numero, Dictionary<string, object?> valores)
{
    private static readonly CultureInfo Br = new("pt-BR");
    public int Numero => numero;
    public object? Bruto(string chave) => valores.GetValueOrDefault(chave);

    public string? Texto(string chave) => Bruto(chave) switch
    {
        null => null,
        double d => d.ToString("0.############", CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        var o => o.ToString()?.Trim() is { Length: > 0 } s && s != "-" ? s : null
    };

    public decimal? Dec(string chave) => Bruto(chave) switch
    {
        double d => (decimal)d,
        int i => i,
        decimal m => m,
        string s when decimal.TryParse(s.Trim(), NumberStyles.Number, Br, out var v) => v,
        _ => null
    };

    public DateOnly? Data(string chave) => Bruto(chave) switch
    {
        DateTime dt => DateOnly.FromDateTime(dt),
        double d when d is > 20000 and < 80000 => DateOnly.FromDateTime(DateTime.FromOADate(d)),
        string s when DateTime.TryParse(s.Trim(), Br, DateTimeStyles.None, out var dt) => DateOnly.FromDateTime(dt),
        _ => null
    };
}

public sealed record RelatorioErp(TipoRelatorioErp Tipo, string Arquivo, IReadOnlyList<LinhaRelatorio> Linhas);

/// <summary>
/// Lê relatórios exportados do ERP (.xlsx ou .xls): acha a linha de cabeçalho pela assinatura de colunas de cada relatório
/// (os relatórios têm título, filtros e células mescladas antes da tabela) e junta o valor que cai em coluna vizinha à do cabeçalho.
/// </summary>
public static class LeitorRelatorioErp
{
    static LeitorRelatorioErp() => System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); // .xls (BIFF) usa code page

    private static readonly (TipoRelatorioErp Tipo, string[] Colunas)[] Assinaturas =
    [
        (TipoRelatorioErp.Apropriacoes, ["entrada", "insumo", "fase", "servico", "total(r$)"]),
        (TipoRelatorioErp.MedicaoOrdensCompra, ["fornecedor", "ordemcompra", "item", "totalreceb."]),
        (TipoRelatorioErp.DocumentosFinanceiros, ["fornecedor", "c.gerencial", "situacao", "venc."]),
        (TipoRelatorioErp.Fornecedores, ["razaosocial", "nomefantasia", "cpf/cnpj"]),
        (TipoRelatorioErp.Insumos, ["nome", "categoria", "tipodecusto", "unidade"]),
    ];

    public static string Rotulo(TipoRelatorioErp t) => t switch
    {
        TipoRelatorioErp.Apropriacoes => "Apropriações de insumos",
        TipoRelatorioErp.MedicaoOrdensCompra => "Medição de ordens de compra",
        TipoRelatorioErp.Fornecedores => "Fornecedores",
        TipoRelatorioErp.Insumos => "Insumos cadastrados",
        TipoRelatorioErp.DocumentosFinanceiros => "Documentos financeiros",
        _ => "Não reconhecido"
    };

    public static RelatorioErp Ler(Stream arquivo, string nomeArquivo)
    {
        var tabela = LerCelulas(arquivo, nomeArquivo);
        for (var r = 0; r < Math.Min(tabela.Count, 60); r++)
        {
            var cab = tabela[r].Select((v, c) => (c, n: LeitorPlanilha.Normalizar(v as string))).Where(x => x.n.Length > 0).ToList();
            var nomes = cab.Select(x => x.n).ToHashSet();
            var tipo = Assinaturas.FirstOrDefault(a => a.Colunas.All(nomes.Contains)).Tipo;
            if (tipo == TipoRelatorioErp.Desconhecido) continue;
            var linhas = new List<LinhaRelatorio>();
            for (var i = r + 1; i < tabela.Count; i++)
            {
                var row = tabela[i];
                if (row.All(v => v is null || v is string { Length: 0 })) continue;
                var valores = new Dictionary<string, object?>();
                for (var k = 0; k < cab.Count; k++)
                {
                    var fim = k + 1 < cab.Count ? cab[k + 1].c : row.Length;
                    object? v = null;
                    for (var c = cab[k].c; c < fim && c < row.Length; c++)
                        if (row[c] is not null && row[c] is not string { Length: 0 }) { v = row[c]; break; }
                    valores.TryAdd(cab[k].n, v);
                }
                linhas.Add(new LinhaRelatorio(i + 1, valores));
            }
            return new RelatorioErp(tipo, nomeArquivo, linhas);
        }
        return new RelatorioErp(TipoRelatorioErp.Desconhecido, nomeArquivo, []);
    }

    private static List<object?[]> LerCelulas(Stream arquivo, string nomeArquivo)
    {
        var ext = Path.GetExtension(nomeArquivo).ToLowerInvariant();
        if (ext is not (".xlsx" or ".xls" or ".xlsm")) throw new RegraDeNegocioException($"{nomeArquivo}: envie o relatório exportado do ERP em Excel (.xlsx ou .xls).");
        try
        {
            using var ms = new MemoryStream();
            arquivo.CopyTo(ms); ms.Position = 0;
            using var reader = ExcelReaderFactory.CreateReader(ms);
            var linhas = new List<object?[]>();
            while (reader.Read())
            {
                var row = new object?[reader.FieldCount];
                for (var c = 0; c < reader.FieldCount; c++) row[c] = reader.GetValue(c) is string s ? s.Trim() : reader.GetValue(c);
                linhas.Add(row);
            }
            return linhas;
        }
        catch (Exception e) when (e is not RegraDeNegocioException and not OperationCanceledException)
        {
            throw new RegraDeNegocioException($"{nomeArquivo}: não foi possível ler a planilha ({e.GetType().Name}).");
        }
    }
}
