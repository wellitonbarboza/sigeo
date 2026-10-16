using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Cadastros;

/// <summary>
/// Grupo (Pai nulo) ou subgrupo de produto, com a sigla de 3 letras usada no código (HID-CNX-0001).
/// A sigla é sugerida pelo sistema e pode ser ajustada; produtos já usados mantêm o código emitido.
/// </summary>
public class CategoriaProduto : EntidadeAuditavel
{
    public Guid? PaiId { get; set; }
    public CategoriaProduto? Pai { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    /// <summary>Tipo de custo herdado pelo produto do grupo que ainda não tem tipo (curva ABC, custo por tipo, financeiro).</summary>
    public Common.TipoCusto? TipoCustoPadrao { get; set; }
    /// <summary>Fase padrão do orçamento (grupo de serviço da EAP) para compra sem composição: o custo realizado cai nela.</summary>
    public string? FasePadrao { get; set; }
    public ICollection<CategoriaProduto> Subgrupos { get; set; } = [];
}

/// <summary>Regras do código padronizado de produto: GGG-SSS-0000; sem classificação → TMP-000000.</summary>
public static partial class CodigoProduto
{
    public const string PrefixoProvisorio = "TMP";

    private static readonly HashSet<string> Genericas = ["DE", "DA", "DO", "DAS", "DOS", "E", "PARA", "A", "O", "EM", "COM", "MATERIAL", "MATERIAIS"];

    [GeneratedRegex("^[A-Z0-9]{3}$")] private static partial Regex SiglaValida();
    [GeneratedRegex("^[A-Z0-9]{3}-[A-Z0-9]{3}-\\d{4,}$")] private static partial Regex Padrao();

    public static bool EhSiglaValida(string? s) => s is not null && SiglaValida().IsMatch(s);
    public static bool EhProvisorio(string? codigo) => codigo?.StartsWith(PrefixoProvisorio + "-", StringComparison.Ordinal) == true;
    public static bool EhPadrao(string? codigo) => codigo is not null && Padrao().IsMatch(codigo);
    public static string Prefixo(string siglaGrupo, string siglaSub) => $"{siglaGrupo}-{siglaSub}-";
    public static string Montar(string siglaGrupo, string siglaSub, int seq) => $"{Prefixo(siglaGrupo, siglaSub)}{seq:0000}";
    public static string Provisorio(int seq) => $"{PrefixoProvisorio}-{seq:000000}";

    /// <summary>Número final de um código no padrão (para continuar a sequência do último cadastro).</summary>
    public static int Sequencia(string codigo) => int.TryParse(codigo[(codigo.LastIndexOf('-') + 1)..], out var n) ? n : 0;

    public static string Normalizar(string s) =>
        new string(s.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray())
            .ToUpperInvariant();

    /// <summary>
    /// Sugere sigla de 3 caracteres que não conflite com as já usadas no mesmo nível
    /// (grupos entre si; subgrupos dentro do grupo). Ex.: Hidráulica → HID; Ferragens (com FER ocupado) → FRG.
    /// </summary>
    public static string SugerirSigla(string nome, IEnumerable<string> ocupadas)
    {
        var usadas = new HashSet<string>(ocupadas, StringComparer.OrdinalIgnoreCase);
        var todas = Regex.Matches(Normalizar(nome), "[A-Z0-9]+").Select(m => m.Value).ToList();
        var w = todas.Where(x => !Genericas.Contains(x)).ToList();
        if (w.Count == 0) w = todas.Count > 0 ? todas : ["XXX"];
        var p = w[0];
        var cons = p[0] + new string(p.Skip(1).Where(c => "AEIOU".IndexOf(c) < 0).ToArray());
        var candidatos = new List<string> { p.PadRight(3, 'X')[..3] };
        if (w.Count > 1) { candidatos.Add((p[..1] + w[1]).PadRight(3, 'X')[..3]); candidatos.Add((p[..Math.Min(2, p.Length)] + w[1][..1]).PadRight(3, 'X')[..3]); }
        if (w.Count > 2) candidatos.Add($"{w[0][0]}{w[1][0]}{w[2][0]}");
        candidatos.Add(cons.PadRight(3, 'X')[..3]);
        for (var i = 1; i < p.Length; i++) for (var j = i + 1; j < p.Length; j++) candidatos.Add($"{p[0]}{p[i]}{p[j]}");
        foreach (var c in candidatos) if (EhSiglaValida(c) && !usadas.Contains(c)) return c;
        for (var n = 1; n < 100; n++) { var c = $"{p[0]}{n:00}"; if (!usadas.Contains(c)) return c; }
        throw new InvalidOperationException("Não foi possível sugerir sigla.");
    }
}
