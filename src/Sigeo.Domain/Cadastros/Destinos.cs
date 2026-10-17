using System.Security.Cryptography;
using System.Text;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Cadastros;

/// <summary>Mascaramento e normalização dos destinos mostrados na tela pública.</summary>
public static class Destinos
{
    public static string? Email(string? e)
    {
        e = e?.Trim().ToLowerInvariant();
        return e is { Length: > 3 } && e.Contains('@') && e.IndexOf('@') > 0 && e.LastIndexOf('.') > e.IndexOf('@') ? e : null;
    }

    /// <summary>Celular brasileiro em E.164 (+55DDNNNNNNNNN); aceita com ou sem 55 e com máscara.</summary>
    public static string? Celular(string? t)
    {
        var d = new string((t ?? "").Where(char.IsDigit).ToArray());
        if (d.StartsWith("55") && d.Length is 12 or 13) d = d[2..];
        if (d.StartsWith('0')) d = d.TrimStart('0');
        return d.Length == 11 && d[2] == '9' ? "+55" + d : null; // só celular (WhatsApp)
    }

    public static string MascararEmail(string e)
    {
        var i = e.IndexOf('@');
        var u = e[..i]; var dom = e[(i + 1)..];
        var ponto = dom.LastIndexOf('.');
        var nomeDom = ponto > 0 ? dom[..ponto] : dom;
        return $"{u[..Math.Min(2, u.Length)]}•••@{nomeDom[..Math.Min(1, nomeDom.Length)]}•••{(ponto > 0 ? dom[ponto..] : "")}";
    }

    public static string MascararCelular(string e164) => $"(••) •••••-{e164[^4..]}";

    public static string MascararNome(string nome)
    {
        var p = nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return p.Length == 0 ? "Contato" : p[0] + (p.Length > 1 ? $" {p[^1][0]}." : "");
    }
}
