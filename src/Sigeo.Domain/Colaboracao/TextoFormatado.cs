using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Colaboracao;

/// <summary>
/// Texto formatado das mensagens sem HTML: **negrito**, _itálico_, linhas iniciadas por "- " viram lista.
/// O texto é sempre codificado antes de aplicar a formatação — nada que o usuário digite vira HTML ativo.
/// </summary>
public static class TextoFormatado
{
    public static string ParaHtml(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return "";
        var sb = new System.Text.StringBuilder();
        var emLista = false;
        foreach (var bruta in texto.Replace("\r\n", "\n").Split('\n'))
        {
            var linha = bruta.TrimEnd();
            var item = linha.StartsWith("- ") || linha.StartsWith("• ");
            if (item && !emLista) { sb.Append("<ul>"); emLista = true; }
            if (!item && emLista) { sb.Append("</ul>"); emLista = false; }
            var conteudo = Inline(Codificar(item ? linha[2..] : linha));
            if (item) sb.Append("<li>").Append(conteudo).Append("</li>");
            else if (linha.Length == 0) sb.Append("<br>");
            else sb.Append("<p>").Append(conteudo).Append("</p>");
        }
        if (emLista) sb.Append("</ul>");
        return sb.ToString();
    }

    /// <summary>Codifica só o que importa para o HTML (&amp; &lt; &gt; aspas); acentos ficam legíveis.</summary>
    private static string Codificar(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");

    private static string Inline(string s)
    {
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\*\*(.+?)\*\*", "<b>$1</b>");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"(?<![\w])_(.+?)_(?![\w])", "<i>$1</i>");
        return s;
    }

    /// <summary>Resumo de uma linha (lista, e-mail, prévia): sem marcações.</summary>
    public static string Resumo(string? texto, int max = 120)
    {
        var t = System.Text.RegularExpressions.Regex.Replace((texto ?? "").Replace("**", "").Replace("\n- ", " · ").Replace('\n', ' '), @"\s+", " ").Trim();
        return t.Length <= max ? t : t[..(max - 1)].TrimEnd() + "…";
    }
}
