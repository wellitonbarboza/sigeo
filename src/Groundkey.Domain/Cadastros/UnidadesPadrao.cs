using System.Globalization;
using System.Text;

namespace Groundkey.Domain.Cadastros;

/// <summary>
/// Unidades de medida padronizadas do catálogo. Grafias equivalentes vindas de ERPs e planilhas
/// (UND/un/und → UN, m²/M² → M2, pç → PC, MÊS → MES…) viram uma só, para não duplicar produto nem quebrar comparação de preço.
/// </summary>
public static class UnidadesPadrao
{
    /// <summary>Sugestões da tela (datalist).</summary>
    public static readonly string[] Lista =
        ["UN", "PC", "KG", "M", "M2", "M3", "L", "CX", "RL", "SC", "PAR", "BR", "GL", "CJ", "JG", "TON", "VB", "H", "MES", "DIA", "FD", "PCT", "KIT", "LATA", "CT", "BD", "CE", "SER"];

    private static readonly Dictionary<string, string> Equivalentes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["UND"] = "UN", ["UNID"] = "UN", ["UNI"] = "UN", ["UNIDADE"] = "UN", ["U"] = "UN", ["UNIDADES"] = "UN",
        ["PEC"] = "PC", ["PECA"] = "PC", ["PCS"] = "PC", ["PECAS"] = "PC",
        ["MT"] = "M", ["MTS"] = "M", ["METRO"] = "M", ["MTL"] = "M", ["ML"] = "M", ["METROS"] = "M",
        ["PARES"] = "PAR", ["PR"] = "PAR", ["PARE"] = "PAR",
        ["MT2"] = "M2", ["METRO2"] = "M2", ["MT3"] = "M3", ["METRO3"] = "M3",
        ["LT"] = "L", ["LTS"] = "L", ["LITRO"] = "L", ["LITROS"] = "L", ["KGS"] = "KG", ["QUILO"] = "KG", ["T"] = "TON", ["TN"] = "TON",
        ["CXA"] = "CX", ["CAIXA"] = "CX", ["ROLO"] = "RL", ["SACO"] = "SC", ["SACOS"] = "SC", ["GALAO"] = "GL", ["BARRA"] = "BR", ["BARRAS"] = "BR",
        ["HR"] = "H", ["HORA"] = "H", ["HORAS"] = "H", ["MENSAL"] = "MES", ["MESES"] = "MES", ["DIARIA"] = "DIA", ["DIAS"] = "DIA",
        ["VERBA"] = "VB", ["FARDO"] = "FD", ["PACOTE"] = "PCT", ["PC."] = "PC", ["CONJ"] = "CJ", ["CONJUNTO"] = "CJ", ["JOGO"] = "JG",
    };

    /// <summary>Unidade padronizada (vazio = UN). Ex.: "und" → UN, "m²" → M2, "pç" → PC, "MÊS" → MES.</summary>
    public static string Normalizar(string? unidade)
    {
        var u = SemAcento((unidade ?? "").Trim()).ToUpperInvariant()
            .Replace("²", "2", StringComparison.Ordinal).Replace("³", "3", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
        if (u.Length == 0) return "UN";
        if (Equivalentes.TryGetValue(u, out var p)) return p;
        var semPonto = u.TrimEnd('.');
        return Equivalentes.TryGetValue(semPonto, out p) ? p : semPonto.Length > 10 ? semPonto[..10] : semPonto;
    }

    private static string SemAcento(string s) => new(s.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
}
