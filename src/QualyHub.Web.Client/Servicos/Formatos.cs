using System.Globalization;

namespace QualyHub.Web.Servicos;

/// <summary>Conversões dos valores digitados nas telas (input type=month / type=number).</summary>
public static partial class Rotulos
{
    /// <summary>Lê o valor de um &lt;input type="month"&gt; ("yyyy-MM") como o 1º dia do mês.</summary>
    public static DateOnly? LerMes(object? valor) =>
        DateOnly.TryParseExact($"{valor}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    /// <summary>Lê um número digitado (input type=number usa ponto; aceita também vírgula decimal). Vazio = 0.</summary>
    public static bool LerDecimal(object? valor, out decimal resultado)
    {
        var t = valor?.ToString()?.Trim();
        if (string.IsNullOrEmpty(t)) { resultado = 0; return true; }
        if (decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out resultado)) return true;
        return decimal.TryParse(t, NumberStyles.Number, Br, out resultado);
    }
}
