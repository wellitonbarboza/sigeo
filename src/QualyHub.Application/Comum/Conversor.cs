using System.Globalization;

namespace QualyHub.Application.Comum;

/// <summary>Conversões tolerantes ao formato brasileiro (1.234,56 · 31/12/2026 · 12.345.678/0001-90).</summary>
public static class Conversor
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public static string SomenteDigitos(string? v) => new((v ?? "").Where(char.IsDigit).ToArray());

    public static decimal? Decimal(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        v = v.Replace("R$", "").Trim();
        // "1.234" / "12.345.678" sem vírgula = separador de milhar brasileiro, não decimal.
        if (System.Text.RegularExpressions.Regex.IsMatch(v, @"^-?\d{1,3}(\.\d{3})+$")) v = v.Replace(".", "");
        if (decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var inv) && !v.Contains(',')) return inv;
        return decimal.TryParse(v, NumberStyles.Number, PtBr, out var br) ? br : null;
    }

    public static DateOnly? Data(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        string[] formatos = ["yyyy-MM-dd", "dd/MM/yyyy", "dd/MM/yy", "dd-MM-yyyy"];
        return DateOnly.TryParseExact(v.Trim()[..Math.Min(10, v.Trim().Length)], formatos, PtBr, DateTimeStyles.None, out var d) ? d : null;
    }

    /// <summary>00.000.000/0000-00 a partir dos 14 dígitos.</summary>
    public static string FormatarCnpj(string digitos) => Convert.ToUInt64(digitos).ToString(@"00\.000\.000\/0000\-00");

    public static bool CnpjValido(string digitos) => digitos.Length == 14 && digitos.Distinct().Count() > 1 && Dv(digitos, 12) && Dv(digitos, 13);
    public static bool CpfValido(string d) => d.Length == 11 && d.Distinct().Count() > 1 && DvCpf(d, 9) && DvCpf(d, 10);

    private static bool Dv(string c, int pos)
    {
        int[] pesos = pos == 12 ? [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2] : [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        var soma = pesos.Select((p, i) => p * (c[i] - '0')).Sum();
        var resto = soma % 11;
        return c[pos] - '0' == (resto < 2 ? 0 : 11 - resto);
    }

    private static bool DvCpf(string c, int pos)
    {
        var soma = Enumerable.Range(0, pos).Sum(i => (c[i] - '0') * (pos + 1 - i));
        var resto = soma * 10 % 11 % 10;
        return c[pos] - '0' == resto;
    }
}

