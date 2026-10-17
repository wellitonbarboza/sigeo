using System.Text.RegularExpressions;

namespace Sigeo.Domain.Fiscal;

/// <summary>
/// Chave de acesso da NF-e/NFC-e: cUF(2) AAMM(4) CNPJ/CPF do emitente(14) modelo(2) série(3) nNF(9) tpEmis(1) cNF(8) DV(1).
/// O DV é módulo 11 com pesos 2..9 da direita para a esquerda (resto 0 ou 1 → DV 0).
/// </summary>
public static partial class ChaveNfe
{
    [GeneratedRegex(@"\d[\d\s.\-]{42,70}\d")]
    private static partial Regex Sequencia();

    public static string SoDigitos(string? s) => new((s ?? "").Where(char.IsDigit).ToArray());

    public static int DigitoVerificador(string chave43)
    {
        var soma = 0; var peso = 2;
        for (var i = chave43.Length - 1; i >= 0; i--)
        {
            soma += (chave43[i] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    /// <summary>44 dígitos, mês válido e DV correto.</summary>
    public static bool Valida(string? chave)
    {
        var c = SoDigitos(chave);
        if (c.Length != 44) return false;
        var mes = int.Parse(c.AsSpan(4, 2));
        if (mes is < 1 or > 12) return false;
        return DigitoVerificador(c[..43]) == c[43] - '0';
    }

    public static PartesChaveNfe? Partes(string? chave)
    {
        if (!Valida(chave)) return null;
        var c = SoDigitos(chave);
        return new PartesChaveNfe(c[..2], 2000 + int.Parse(c.AsSpan(2, 2)), int.Parse(c.AsSpan(4, 2)), c.Substring(6, 14), c.Substring(20, 2),
            int.Parse(c.AsSpan(22, 3)).ToString(), int.Parse(c.AsSpan(25, 9)));
    }

    /// <summary>
    /// Primeira chave válida dentro de um texto (código de barras lido, OCR do DANFE ou URL do QR Code da NFC-e com "p=chave|...").
    /// Aceita a chave impressa em grupos de 4 dígitos separados por espaço.
    /// </summary>
    public static string? Encontrar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var direta = SoDigitos(texto);
        if (direta.Length == 44 && Valida(direta)) return direta;
        foreach (Match m in Sequencia().Matches(texto))
        {
            var d = SoDigitos(m.Value);
            if (d.Length == 44 && Valida(d)) return d;
            // Sequência maior (chave grudada em outros números): procura uma janela de 44 com DV correto.
            for (var i = 0; i + 44 <= d.Length; i++)
                if (Valida(d.Substring(i, 44))) return d.Substring(i, 44);
        }
        return null;
    }
}
