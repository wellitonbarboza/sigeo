using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Groundkey.Application.Comum;
using Groundkey.Domain.Fiscal;

namespace Groundkey.Application.Fiscal;

/// <summary>Resultado da leitura de um XML: nota (NF-e modelo 55), cancelamento (evento 110111) ou arquivo ignorado.</summary>
public abstract record XmlLido(string Arquivo);
public sealed record NotaLida(string Arquivo, NotaFiscalEntrada Nota, string Xml) : XmlLido(Arquivo);
public sealed record CancelamentoLido(string Arquivo, string Chave) : XmlLido(Arquivo);
public sealed record XmlIgnorado(string Arquivo, string Motivo) : XmlLido(Arquivo);

/// <summary>
/// Leitura do XML da NF-e (layout 4.00, com ou sem nfeProc) e do evento de cancelamento. Aceita um XML solto ou um ZIP com vários.
/// Não confia no arquivo: DTD desligado, tamanho limitado, campos numéricos lidos com cultura invariante.
/// </summary>
public static partial class LeitorNfe
{
    private static readonly XNamespace Ns = "http://www.portalfiscal.inf.br/nfe";
    public const long TamanhoMaximoXml = 5 * 1024 * 1024;
    public const int MaximoArquivosZip = 2000;

    [GeneratedRegex(@"pedido\s*(?:de\s+compra)?\s*(?:n[º°o.]*|numero|número|#)?\s*[:\-]?\s*(\d{1,9})", RegexOptions.IgnoreCase)]
    private static partial Regex PedidoNoTexto();

    /// <summary>Lê um arquivo (.xml ou .zip). Itens inválidos voltam como <see cref="XmlIgnorado"/> com o motivo.</summary>
    public static List<XmlLido> Ler(Stream conteudo, string nomeArquivo)
    {
        if (nomeArquivo.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            var r = new List<XmlLido>();
            using var zip = new ZipArchive(conteudo, ZipArchiveMode.Read);
            foreach (var e in zip.Entries.Where(e => e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)).Take(MaximoArquivosZip))
            {
                if (e.Length > TamanhoMaximoXml) { r.Add(new XmlIgnorado(e.FullName, "Arquivo muito grande para uma NF-e.")); continue; }
                using var s = e.Open();
                r.Add(LerXml(s, e.FullName));
            }
            return r;
        }
        return [LerXml(conteudo, nomeArquivo)];
    }

    public static XmlLido LerXml(Stream s, string arquivo)
    {
        string texto;
        using (var leitor = new StreamReader(s)) texto = leitor.ReadToEnd();
        return LerTexto(texto, arquivo);
    }

    public static XmlLido LerTexto(string texto, string arquivo)
    {
        XDocument doc;
        try
        {
            var cfg = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = TamanhoMaximoXml };
            using var r = XmlReader.Create(new StringReader(texto), cfg);
            doc = XDocument.Load(r);
        }
        catch (XmlException) { return new XmlIgnorado(arquivo, "XML inválido."); }

        // Cancelamento: procEventoNFe/evento/infEvento com tpEvento 110111
        if (doc.Descendants(Ns + "infEvento").FirstOrDefault() is { } ev)
        {
            var tipo = ev.Element(Ns + "tpEvento")?.Value;
            var chave = ev.Element(Ns + "chNFe")?.Value;
            return tipo == "110111" && chave is { Length: 44 } ? new CancelamentoLido(arquivo, chave) : new XmlIgnorado(arquivo, $"Evento {tipo} não tratado.");
        }

        var inf = doc.Descendants(Ns + "infNFe").FirstOrDefault();
        if (inf is null) return new XmlIgnorado(arquivo, doc.Descendants(Ns + "resNFe").Any() ? "Resumo da nota (sem itens): baixe o XML completo." : "Não é NF-e.");
        var ide = inf.Element(Ns + "ide");
        var modelo = ide?.Element(Ns + "mod")?.Value ?? "";
        if (modelo != "55") return new XmlIgnorado(arquivo, $"Modelo {modelo} não é NF-e de compra (55).");
        var chaveNota = (inf.Attribute("Id")?.Value ?? "").Replace("NFe", "", StringComparison.Ordinal);
        if (chaveNota.Length != 44) chaveNota = doc.Descendants(Ns + "chNFe").FirstOrDefault()?.Value ?? chaveNota;
        if (chaveNota.Length != 44 || !chaveNota.All(char.IsDigit)) return new XmlIgnorado(arquivo, "Chave de acesso ausente ou inválida.");

        var emit = inf.Element(Ns + "emit");
        var dest = inf.Element(Ns + "dest");
        var tot = inf.Element(Ns + "total")?.Element(Ns + "ICMSTot");
        var infCpl = inf.Element(Ns + "infAdic")?.Element(Ns + "infCpl")?.Value?.Trim();
        var nota = new NotaFiscalEntrada
        {
            Chave = chaveNota,
            Numero = int.TryParse(ide?.Element(Ns + "nNF")?.Value, out var n) ? n : 0,
            Serie = ide?.Element(Ns + "serie")?.Value ?? "",
            Modelo = modelo,
            Emissao = DateTimeOffset.TryParse(ide?.Element(Ns + "dhEmi")?.Value ?? ide?.Element(Ns + "dEmi")?.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d) ? d : DateTimeOffset.MinValue,
            EmitenteCnpj = Conversor.SomenteDigitos(emit?.Element(Ns + "CNPJ")?.Value ?? emit?.Element(Ns + "CPF")?.Value),
            EmitenteNome = Cortar(emit?.Element(Ns + "xNome")?.Value, 200) ?? "",
            DestinatarioCnpj = Conversor.SomenteDigitos(dest?.Element(Ns + "CNPJ")?.Value ?? dest?.Element(Ns + "CPF")?.Value),
            DestinatarioNome = Cortar(dest?.Element(Ns + "xNome")?.Value, 200),
            ValorTotal = Dec(tot?.Element(Ns + "vNF")?.Value),
            ValorFrete = tot?.Element(Ns + "vFrete") is { } vf && Dec(vf.Value) is > 0 and var f ? f : null,
            InformacoesComplementares = Cortar(infCpl, 4000)
        };
        foreach (var det in inf.Elements(Ns + "det"))
        {
            var p = det.Element(Ns + "prod");
            if (p is null) continue;
            nota.Itens.Add(new NotaFiscalItem
            {
                NotaFiscalId = nota.Id,
                Item = int.TryParse(det.Attribute("nItem")?.Value, out var ni) ? ni : nota.Itens.Count + 1,
                Codigo = Cortar(p.Element(Ns + "cProd")?.Value, 60),
                Descricao = Cortar(p.Element(Ns + "xProd")?.Value, 500) ?? "",
                Ncm = p.Element(Ns + "NCM")?.Value,
                Cfop = p.Element(Ns + "CFOP")?.Value,
                Unidade = Cortar(p.Element(Ns + "uCom")?.Value, 10),
                Quantidade = Dec(p.Element(Ns + "qCom")?.Value),
                ValorUnitario = Dec(p.Element(Ns + "vUnCom")?.Value),
                Valor = Dec(p.Element(Ns + "vProd")?.Value),
                PedidoCitado = Cortar(p.Element(Ns + "xPed")?.Value?.Trim(), 30),
                ItemPedidoCitado = Cortar(p.Element(Ns + "nItemPed")?.Value?.Trim(), 10)
            });
        }
        nota.PedidosCitados = Cortar(string.Join(",", PedidosCitados(nota)), 200);
        return new NotaLida(arquivo, nota, texto);
    }

    /// <summary>Números de pedido citados: xPed dos itens (só dígitos) e "Pedido nº 123" nas informações complementares.</summary>
    public static IReadOnlyList<int> PedidosCitados(NotaFiscalEntrada n)
    {
        var r = new SortedSet<int>();
        foreach (var i in n.Itens)
            if (Conversor.SomenteDigitos(i.PedidoCitado) is { Length: > 0 and <= 9 } x && int.TryParse(x, out var v) && v > 0) r.Add(v);
        foreach (Match m in PedidoNoTexto().Matches(n.InformacoesComplementares ?? ""))
            if (int.TryParse(m.Groups[1].Value, out var v) && v > 0) r.Add(v);
        return [.. r];
    }

    private static decimal Dec(string? v) => decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0;
    private static string? Cortar(string? v, int max) => v is null ? null : v.Length <= max ? v : v[..max];
}
