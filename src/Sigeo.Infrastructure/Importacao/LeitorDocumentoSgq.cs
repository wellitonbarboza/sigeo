using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Sigeo.Domain.Common;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Sigeo.Infrastructure.Importacao;

/// <summary>
/// Lê os arquivos do SGQ da empresa para importação em lote:
/// <list type="bullet">
/// <item>Procedimento/ITS em .docx: código, título e revisão vêm das propriedades do arquivo (Título "CÓDIGO — Nome"),
/// do cabeçalho (IDENTIFICAÇÃO / VERSÃO) ou, na falta, do nome do arquivo. A descrição da revisão vem da tabela de histórico.</item>
/// <item>Ficha de verificação (FVS) em .xlsx com aba "BANCO DE DADOS": cabeçalho (Identificação, Título, Revisão, Documento de
/// referência, Momento, Unidade de inspeção, Amostragem, Regime), pré-requisitos e a tabela de itens (Nº | Item | Método | Critério).</item>
/// </list>
/// O código do arquivo pode diferir do nome do arquivo (ex.: "FVS-01_…xlsx" com identificação FVS-03): vale o conteúdo.
/// </summary>
public static partial class LeitorDocumentoSgq
{
    public const string AbaFicha = "BANCO DE DADOS";

    [GeneratedRegex(@"\b(PEIS|ITS|PES|IT|PO|PR|DT|FORM|FVS|FVM|MQ|PQO|DS|P|F)\s?[.\-]?\s?\d{1,3}(?:[.\-]\d{1,3})*(?:\s?[A-Z]\b)?", RegexOptions.IgnoreCase)]
    private static partial Regex RxCodigo();
    [GeneratedRegex(@"(?:VERS[AÃ]O|REVIS[AÃ]O|REV)\.?\s*:?\s*(\d{1,2})\b", RegexOptions.IgnoreCase)]
    private static partial Regex RxRevisao();
    [GeneratedRegex(@"\s+[—–-]\s+")]
    private static partial Regex RxTravessao();

    public static bool EhDocx(string nome) => Path.GetExtension(nome).Equals(".docx", StringComparison.OrdinalIgnoreCase);
    public static bool EhXlsx(string nome) => Path.GetExtension(nome).Equals(".xlsx", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(nome).Equals(".xlsm", StringComparison.OrdinalIgnoreCase);

    /// <summary>Normaliza o código: caixa alta, separador original mantido (PEIS.03, FVS-03, ITS 03.02).</summary>
    public static string NormalizarCodigo(string codigo) => Regex.Replace(codigo.Trim().ToUpperInvariant(), @"\s+", " ");

    /// <summary>Código no início de um texto ("PEIS.03 — Montagem de forma REV01" → PEIS.03).</summary>
    public static string? CodigoNoInicio(string? texto)
    {
        var m = RxCodigo().Match(texto ?? "");
        return m.Success && m.Index <= 2 ? NormalizarCodigo(m.Value) : null;
    }

    /// <summary>Nome depois do código e antes da revisão ("PEIS.03 — Montagem de forma REV01" → Montagem de forma).</summary>
    public static string? NomeDepoisDoCodigo(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var m = RxCodigo().Match(texto);
        var resto = m.Success && m.Index <= 2 ? texto[(m.Index + m.Length)..] : texto;
        resto = Regex.Replace(resto, @"\s*\bREV(?:IS[AÃ]O)?\.?\s*\d{1,2}\s*$", "", RegexOptions.IgnoreCase);
        resto = resto.Trim().TrimStart('—', '–', '-', ':', '·').Trim();
        return resto.Length == 0 ? null : resto;
    }

    // ───────── Identificação de um arquivo (docx/xlsx/pdf) ─────────

    public static IdentificacaoArquivoSgq Identificar(Stream arquivo, string nomeArquivo)
    {
        var (codNome, revNome, tituloNome) = PeloNome(nomeArquivo);
        try
        {
            if (EhDocx(nomeArquivo)) return Docx(arquivo, nomeArquivo, codNome, revNome, tituloNome);
            if (EhXlsx(nomeArquivo)) return Xlsx(arquivo, nomeArquivo, codNome, revNome, tituloNome);
            if (Path.GetExtension(nomeArquivo).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                && TituloPdf(arquivo) is { } tp && CodigoNoInicio(tp) is { } cpdf)
                return new IdentificacaoArquivoSgq(nomeArquivo, cpdf, NomeDepoisDoCodigo(tp) ?? tituloNome, revNome, null, "propriedades do PDF");
        }
        catch (Exception e) when (e is not (OperationCanceledException or OutOfMemoryException))
        {
            return new IdentificacaoArquivoSgq(nomeArquivo, codNome, tituloNome, revNome, null, "nome do arquivo (não foi possível abrir o conteúdo)");
        }
        return new IdentificacaoArquivoSgq(nomeArquivo, codNome, tituloNome, revNome, null, "nome do arquivo");
    }

    /// <summary>Título do PDF (dicionário Info, texto literal ou hex UTF-16). Null quando comprimido ou ausente.</summary>
    public static string? TituloPdf(Stream s)
    {
        using var ms = Copiar(s);
        var bytes = ms.ToArray();
        var texto = Encoding.Latin1.GetString(bytes);
        var m = Regex.Match(texto, @"/Title\s*(?:\((?<lit>(?:\\.|[^\\)])*)\)|<(?<hex>[0-9A-Fa-f\s]*)>)");
        if (!m.Success) return null;
        if (m.Groups["hex"].Success)
        {
            var hex = Regex.Replace(m.Groups["hex"].Value, @"\s", "");
            if (hex.Length % 2 == 1) return null;
            var b = Convert.FromHexString(hex);
            var t = b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF ? Encoding.BigEndianUnicode.GetString(b, 2, b.Length - 2) : Encoding.Latin1.GetString(b);
            return string.IsNullOrWhiteSpace(t) ? null : t.Trim();
        }
        var lit = Regex.Replace(m.Groups["lit"].Value, @"\\(.)", "$1");
        var raw = Encoding.Latin1.GetBytes(lit);
        var r = raw.Length >= 2 && raw[0] == 0xFE && raw[1] == 0xFF ? Encoding.BigEndianUnicode.GetString(raw, 2, raw.Length - 2) : lit;
        return string.IsNullOrWhiteSpace(r) ? null : r.Trim();
    }

    /// <summary>"PEIS.01-Montagem_de_forma.Empresa.REV01.docx" → (PEIS.01, 1, "Montagem de forma").</summary>
    public static (string? Codigo, int? Revisao, string? Titulo) PeloNome(string nomeArquivo)
    {
        var nome = Path.GetFileNameWithoutExtension(nomeArquivo);
        var cod = CodigoNoInicio(nome);
        int? rev = null;
        var mr = Regex.Match(nome, @"[._\s-]REV(?:IS[AÃ]O)?\.?\s*(\d{1,2})\b", RegexOptions.IgnoreCase);
        if (mr.Success) { rev = int.Parse(mr.Groups[1].Value, CultureInfo.InvariantCulture); nome = nome[..mr.Index]; }
        if (cod is not null) nome = nome[(RxCodigo().Match(nome).Length)..];
        var partes = nome.Trim('.', '_', '-', ' ').Split('.', StringSplitOptions.RemoveEmptyEntries);
        var titulo = partes.Length == 0 ? null : partes[0].Replace('_', ' ').Trim('-', ' ');
        return (cod, rev, string.IsNullOrWhiteSpace(titulo) ? null : titulo);
    }

    private static IdentificacaoArquivoSgq Docx(Stream s, string nome, string? codNome, int? revNome, string? tituloNome)
    {
        using var ms = Copiar(s);
        using var doc = WordprocessingDocument.Open(ms, false);
        string? codigo = null, titulo = null; int? revisao = null; var fonte = new List<string>();

        // 1) Propriedade "Título" do arquivo: "PEIS.03 — Montagem de forma".
        var tituloProp = doc.PackageProperties.Title;
        if (CodigoNoInicio(tituloProp) is { } cp) { codigo = cp; titulo = NomeDepoisDoCodigo(tituloProp); fonte.Add("propriedades"); }

        // 2) Cabeçalho: IDENTIFICAÇÃO / VERSÃO.
        var cabecalho = string.Join(" | ", doc.MainDocumentPart?.HeaderParts.Select(h => Texto(h.Header)) ?? []);
        if (codigo is null)
        {
            var mi = Regex.Match(cabecalho, @"IDENTIFICA[ÇC][AÃ]O\s*:?\s*(.{2,20})", RegexOptions.IgnoreCase);
            if (mi.Success && CodigoNoInicio(mi.Groups[1].Value.Trim()) is { } ch) { codigo = ch; fonte.Add("cabeçalho"); }
        }
        var mv = RxRevisao().Match(cabecalho);
        if (mv.Success) revisao = int.Parse(mv.Groups[1].Value, CultureInfo.InvariantCulture);

        // 3) Nome do arquivo para o que faltar.
        codigo ??= codNome; revisao ??= revNome; titulo ??= tituloNome;
        if (codNome is not null && fonte.Count == 0) fonte.Add("nome do arquivo");

        // Descrição da revisão: tabela de histórico (Rev. | Data | Descrição).
        string? descricao = null;
        var corpo = doc.MainDocumentPart?.Document?.Body;
        if (corpo is not null && revisao is { } r)
            foreach (var t in corpo.Elements<W.Table>())
            {
                var linhas = t.Elements<W.TableRow>().Select(l => l.Elements<W.TableCell>().Select(c => Texto(c).Trim()).ToList()).ToList();
                if (linhas.Count < 2 || linhas[0].Count < 3 || !Sem(linhas[0][0]).StartsWith("rev") || !Sem(linhas[0][^1]).StartsWith("descri")) continue;
                var alvo = linhas.Skip(1).LastOrDefault(l => int.TryParse(l[0], out var n) && n == r);
                if (alvo is not null) descricao = Limitar(alvo[^1], 1000);
            }
        return new IdentificacaoArquivoSgq(nome, codigo, titulo, revisao, descricao, string.Join(" + ", fonte.Distinct()));
    }

    private static IdentificacaoArquivoSgq Xlsx(Stream s, string nome, string? codNome, int? revNome, string? tituloNome)
    {
        using var ms = Copiar(s);
        using var wb = Planilha.Abrir(ms);
        if (!wb.TryGetWorksheet(AbaFicha, out var ws)) return new IdentificacaoArquivoSgq(nome, codNome, tituloNome, revNome, null, "nome do arquivo");
        var c = Campos(ws);
        var codigo = c.GetValueOrDefault("identificacao") is { Length: > 0 } id ? NormalizarCodigo(id) : codNome;
        var nomeRef = NomeDepoisDoCodigo(c.GetValueOrDefault("documentodereferencia"));
        var titulo = nomeRef is not null && Sem(nomeRef) == Sem(c.GetValueOrDefault("titulodoservico")) ? nomeRef
            : Frase(c.GetValueOrDefault("titulodoservico")) ?? Frase(c.GetValueOrDefault("material")) ?? tituloNome;
        int? rev = int.TryParse(c.GetValueOrDefault("revisao"), out var n) ? n : revNome;
        return new IdentificacaoArquivoSgq(nome, codigo, titulo, rev, null, c.ContainsKey("identificacao") ? "aba " + AbaFicha : "nome do arquivo");
    }

    // ───────── Ficha FVS (aba BANCO DE DADOS) → modelo de FVS ─────────

    private static readonly string[] Equipamentos =
        ["trena", "nivel", "prumo", "esquadro", "regua", "paquimetro", "laser", "termometro", "slump", "balanca", "manometro", "multimetro",
         "megometro", "escantilhao", "teodolito", "estacao total", "medidor", "gabarito", "probe", "torquimetro", "dinamometro"];

    /// <summary>Lê uma ficha FVS ou FVM (pasta de trabalho com a aba BANCO DE DADOS). Retorna null se não for ficha.</summary>
    public static (KitModelo Modelo, List<KitItem> Itens)? LerFichaFvs(Stream arquivo, string nomeArquivo, int linhaModelo) =>
        LerFicha(arquivo, nomeArquivo, linhaModelo) is { } f ? (f.Modelo, f.Itens) : null;

    /// <summary>
    /// Ficha da empresa (aba BANCO DE DADOS). FVS: cabeçalho com "Título do serviço" e "Documento de referência" (a ITS).
    /// FVM: cabeçalho com "Material", "Norma / especificação", "Documentos exigidos", "Ensaio", "Armazenamento" e "Serviços que utilizam" —
    /// também devolve o material controlado (código = número da FVM: FVM-02 → 02).
    /// </summary>
    public static (KitModelo Modelo, List<KitItem> Itens, KitMaterialControlado? Material)? LerFicha(Stream arquivo, string nomeArquivo, int linhaModelo)
    {
        using var ms = Copiar(arquivo);
        using var wb = Planilha.Abrir(ms);
        if (!wb.TryGetWorksheet(AbaFicha, out var ws)) return null;
        var c = Campos(ws);
        var codigo = c.GetValueOrDefault("identificacao") is { Length: > 0 } id ? NormalizarCodigo(id) : PeloNome(nomeArquivo).Codigo;
        if (string.IsNullOrWhiteSpace(codigo)) throw new Sigeo.Domain.Aprovacoes.RegraDeNegocioException($"{nomeArquivo}: ficha sem \"Identificação\" na aba {AbaFicha}.");
        if (!c.ContainsKey("titulodoservico") && c.GetValueOrDefault("material") is { Length: > 0 }) return Fvm(ws, c, codigo, linhaModelo);

        var referencia = c.GetValueOrDefault("documentodereferencia");
        var its = CodigoNoInicio(referencia);
        var nomeIts = NomeDepoisDoCodigo(referencia);
        var tituloServico = c.GetValueOrDefault("titulodoservico");
        // Título com acentos e caixa do documento de referência quando o da ficha está todo em maiúsculas.
        var titulo = nomeIts is not null && Sem(nomeIts) == Sem(tituloServico) ? nomeIts : Frase(tituloServico) ?? nomeIts ?? codigo;
        var revisao = int.TryParse(c.GetValueOrDefault("revisao"), out var r) ? r : 0;

        var orient = new StringBuilder();
        if (PreRequisitos(ws) is { Length: > 0 } pre) orient.AppendLine("Pré-requisitos para a execução do serviço:").AppendLine(pre);
        if (c.FirstOrDefault(k => k.Key.StartsWith("regimedeexecucao")).Value is { Length: > 0 } regime) orient.AppendLine($"Regime de execução: {regime}.");
        var unidade = c.FirstOrDefault(k => k.Key.StartsWith("unidadedeinspecao")).Value;
        if (unidade is { Length: > 0 }) orient.AppendLine($"Unidade de inspeção: {unidade}.");
        var modelo = new KitModelo(linhaModelo, TipoModelo.FVS, codigo, titulo, revisao, its, nomeIts, null, null,
            null, c.GetValueOrDefault("amostragem"), c.GetValueOrDefault("momentodainspecao"),
            NivelDoLocal(unidade),
            LiberaMedicao: true, Quarentena: false, Rastrear: false, Limitar(orient.ToString().Trim(), 4000), Publicar: true);
        return (modelo, Itens(ws, codigo), null);
    }

    private static (KitModelo, List<KitItem>, KitMaterialControlado?) Fvm(IXLWorksheet ws, Dictionary<string, string> c, string codigo, int linhaModelo)
    {
        string? Campo(string prefixo) => c.FirstOrDefault(k => k.Key.StartsWith(prefixo)).Value is { Length: > 0 } v ? v : null;
        var nome = Frase(c["material"])!;
        var numero = Regex.Match(codigo, @"\d+(?:\.\d+)?$").Value;
        var norma = Campo("norma");
        var docs = Campo("documentosexigidos");
        var ensaio = Campo("ensaio");
        var armaz = Campo("armazenamento");
        var servicos = Campo("servicosqueutilizam") ?? Campo("servicos");
        var itens = Itens(ws, codigo, out var amostras);
        var amostragem = amostras.Count == 0 ? "Conforme cada item" : amostras.Count <= 3 ? string.Join("; ", amostras) : "Conforme cada item (ver critério)";
        var orient = new StringBuilder();
        if (docs is not null) orient.AppendLine($"Documentos exigidos do fornecedor: {docs}");
        if (ensaio is not null && ensaio.Trim() is not ("—" or "-")) orient.AppendLine($"Ensaio: {ensaio}");
        if (armaz is not null) orient.AppendLine($"Armazenamento: {armaz}");
        if (servicos is not null) orient.AppendLine($"Serviços que utilizam: {servicos}");
        var revisao = int.TryParse(c.GetValueOrDefault("revisao"), out var r) ? r : 0;
        var modelo = new KitModelo(linhaModelo, TipoModelo.FVM, codigo, nome, revisao, null, null, numero.Length > 0 ? numero : codigo, nome,
            norma, amostragem, "No recebimento", null, LiberaMedicao: false, Quarentena: false, Rastrear: false, Limitar(orient.ToString().Trim(), 4000), Publicar: true);
        var material = new KitMaterialControlado(numero.Length > 0 ? numero : codigo, nome, norma, docs, ensaio is null or "—" or "-" ? null : ensaio, armaz, null, null, servicos);
        return (modelo, itens, material);
    }

    /// <summary>
    /// Aba de requisitos por material (cabeçalho com Grupo/Nº, Material, Documentos exigidos, Forma de qualificação, Critério…),
    /// como a da lista de fornecedores qualificados. Nada encontrado → lista vazia.
    /// </summary>
    public static List<KitMaterialControlado> LerRequisitosMateriais(XLWorkbook wb)
    {
        var lista = new List<KitMaterialControlado>();
        foreach (var ws in wb.Worksheets)
        {
            var ultima = Math.Min(ws.LastRowUsed()?.RowNumber() ?? 0, 30);
            for (var l = 1; l <= ultima; l++)
            {
                var cab = ws.Row(l).CellsUsed().ToDictionary(x => Sem(x.GetString()), x => x.Address.ColumnNumber);
                int Col(params string[] p) => cab.FirstOrDefault(k => p.Any(k.Key.StartsWith)).Value;
                int cCod = Col("grupo", "nº", "no"), cNome = Col("material"), cForma = Col("formadequalificacao", "forma");
                if (cCod == 0 || cNome == 0 || cForma == 0) continue;
                int cDocs = Col("documentosexigidos", "documentos"), cCrit = Col("criteriodequalificacao", "criterio"), cReg = Col("registro"), cNorma = Col("norma");
                var fim = ws.LastRowUsed()?.RowNumber() ?? l;
                for (var k = l + 1; k <= fim; k++)
                {
                    var row = ws.Row(k);
                    var cod = row.Cell(cCod).GetString().Trim();
                    var nome = row.Cell(cNome).GetString().Trim();
                    if (cod.Length == 0 || nome.Length == 0 || cod.Length > 8) continue;
                    string? T(int col) => col == 0 ? null : row.Cell(col).GetString().Trim() is { Length: > 0 } v ? v : null;
                    var reg = T(cReg);
                    var servicos = reg is null ? null : string.Join(" · ", reg.Split('·', StringSplitOptions.TrimEntries).Where(x => !Regex.IsMatch(x, @"^(FVM|FORM|F)\b", RegexOptions.IgnoreCase)));
                    lista.Add(new KitMaterialControlado(cod, nome, T(cNorma), T(cDocs), null, null, T(cForma), T(cCrit), string.IsNullOrWhiteSpace(servicos) ? null : servicos));
                }
                break;
            }
        }
        return lista;
    }

    /// <summary>
    /// Nível de local em que a FVS é aberta. Só trava quando a ficha cita um único nível conhecido (ex.: "Pavimento");
    /// texto com alternativas ("Pavimento / peça", "Unidade / ambiente") fica livre — a descrição vai para as orientações.
    /// </summary>
    public static string? NivelDoLocal(string? unidade)
    {
        var t = Sem(unidade);
        if (t.Length == 0 || (unidade ?? "").IndexOfAny(['/', ',', ';', '(']) >= 0 || unidade!.Contains(" e ") || unidade.Contains(" ou ")) return null;
        return t switch
        {
            "pavimento" => "Pavimento", "torre" => "Torre", "apartamento" or "unidade" or "apto" => "Apartamento",
            "comodo" or "ambiente" => "Cômodo", "areacomum" => "Área comum", "condominio" => "Condomínio", _ => null
        };
    }

    private static List<KitItem> Itens(IXLWorksheet ws, string codigo) => Itens(ws, codigo, out _);

    private static List<KitItem> Itens(IXLWorksheet ws, string codigo, out List<string> amostragens)
    {
        var itens = new List<KitItem>();
        amostragens = [];
        var colAmostra = 0;
        var ultima = ws.LastRowUsed()?.RowNumber() ?? 0;
        int? cab = null; int colN = 0, colItem = 0, colMet = 0, colCrit = 0;
        for (var l = 1; l <= ultima && cab is null; l++)
            foreach (var cel in ws.Row(l).CellsUsed())
            {
                var t = Sem(cel.GetString());
                if (t is "n" or "no" or "nº" or "n°") { colN = cel.Address.ColumnNumber; cab = l; }
                if (t.StartsWith("itemdeinspecao") || t.StartsWith("itemdeverificacao")) colItem = cel.Address.ColumnNumber;
                if (t.StartsWith("metodo")) colMet = cel.Address.ColumnNumber;
                if (t.StartsWith("criterio")) colCrit = cel.Address.ColumnNumber;
                if (t.StartsWith("amostragem")) colAmostra = cel.Address.ColumnNumber;
            }
        if (cab is null || colItem == 0 || colCrit == 0) return itens;
        for (var l = cab.Value + 1; l <= ultima; l++)
        {
            var row = ws.Row(l);
            var desc = row.Cell(colItem).GetString().Trim();
            var crit = row.Cell(colCrit).GetString().Trim();
            if (desc.Length == 0 && crit.Length == 0) continue;
            if (Sem(desc).StartsWith("linhasreservas") || Sem(row.Cell(colN).GetString()).StartsWith("referencias")) break;
            if (desc.Length == 0 || crit.Length == 0) continue;
            var met = colMet == 0 ? "Visual" : row.Cell(colMet).GetString().Trim();
            var metSem = Sem(met);
            var equip = Equipamentos.Any(e => metSem.Contains(e.Replace(" ", "")));
            var amostra = colAmostra == 0 ? "" : row.Cell(colAmostra).GetString().Trim();
            if (amostra.Length > 0)
            {
                if (!amostragens.Contains(amostra)) amostragens.Add(amostra);
                crit = $"{crit} · Amostragem: {amostra}";
            }
            itens.Add(new KitItem(l, codigo, itens.Count + 1, desc, met.Length == 0 ? "Visual" : met, crit, equip, FotoItem.SoEmNaoConformidade, true, null, null));
        }
        return itens;
    }

    private static string? PreRequisitos(IXLWorksheet ws)
    {
        var ultima = ws.LastRowUsed()?.RowNumber() ?? 0;
        for (var l = 1; l <= ultima; l++)
            if (ws.Row(l).CellsUsed().Any(c => Sem(c.GetString()).StartsWith("prerequisitos")))
                for (var k = l + 1; k <= Math.Min(ultima, l + 3); k++)
                    if (ws.Row(k).CellsUsed().Select(c => c.GetString().Trim()).FirstOrDefault(t => t.Length > 0) is { } texto)
                        return Sem(texto) is "n" or "no" or "nº" ? null : texto;
        return null;
    }

    /// <summary>Pares rótulo → valor (célula seguinte preenchida na mesma linha). Chave sem acento/espaço/caixa.</summary>
    private static Dictionary<string, string> Campos(IXLWorksheet ws)
    {
        var d = new Dictionary<string, string>();
        var ultima = Math.Min(ws.LastRowUsed()?.RowNumber() ?? 0, 60);
        for (var l = 1; l <= ultima; l++)
        {
            var cels = ws.Row(l).CellsUsed().ToList();
            if (cels.Count < 2) continue;
            var chave = Sem(cels[0].GetString());
            if (chave.Length == 0 || d.ContainsKey(chave)) continue;
            var valor = cels[1].DataType == XLDataType.DateTime ? cels[1].GetDateTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : cels[1].GetString().Trim();
            if (valor.Length > 0) d[chave] = valor;
        }
        return d;
    }

    // ───────── utilitários ─────────

    private static string Texto(DocumentFormat.OpenXml.OpenXmlElement e)
    {
        var sb = new StringBuilder();
        foreach (var p in e.Descendants<W.Paragraph>())
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(string.Concat(p.Descendants<W.Text>().Select(t => t.Text)));
        }
        return sb.ToString();
    }

    private static MemoryStream Copiar(Stream s)
    {
        var ms = new MemoryStream();
        if (s.CanSeek) s.Position = 0;
        s.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }

    private static string Limitar(string s, int max) => s.Length <= max ? s : s[..max];

    /// <summary>"MONTAGEM DE FORMA" → "Montagem de forma" (texto já com minúsculas fica como está).</summary>
    public static string? Frase(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        if (s.Any(char.IsLower)) return s;
        var br = new CultureInfo("pt-BR");
        // Siglas curtas (PU, PVC, NBR) ficam em maiúsculas; palavras de ligação e as demais vão para minúsculas.
        var palavras = s.Split(' ');
        for (var i = 0; i < palavras.Length; i++)
        {
            var nucleo = new string(palavras[i].Where(char.IsLetter).ToArray());
            var sigla = i > 0 && nucleo.Length is >= 2 and <= 3 && !Ligacao.Contains(nucleo.ToLower(br));
            if (!sigla) palavras[i] = palavras[i].ToLower(br);
        }
        var baixo = string.Join(' ', palavras);
        return char.ToUpper(baixo[0], br) + baixo[1..];
    }

    private static readonly HashSet<string> Ligacao =
        ["de", "do", "da", "dos", "das", "em", "no", "na", "nos", "nas", "com", "por", "para", "ou", "ao", "aos", "os", "as", "um", "uma", "sem", "sob", "e", "o", "a",
         "aço", "uso", "via", "pó", "gás", "cal", "fio", "kit", "mão", "obra", "tubo", "piso", "teto", "laje"];

    /// <summary>Sem acento, sem espaço/pontuação, minúsculas.</summary>
    public static string Sem(string? s)
    {
        var d = (s ?? "").Normalize(NormalizationForm.FormD);
        return new string(d.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark && (char.IsLetterOrDigit(ch) || ch is 'º' or '°'))
            .Select(char.ToLowerInvariant).ToArray());
    }
}
