namespace Sigeo.Application.Comum;

/// <summary>
/// Regras puras das ferramentas privadas (ex.: Planning Lab): caminho escondido, quem pode abrir e quais
/// arquivos podem ser servidos. Não é módulo do SIGEO: não aparece no menu, não usa banco nem perfis de acesso.
/// </summary>
public static class AcessoPrivado
{
    /// <summary>Normaliza o caminho configurado ("x/abc/" → "/x/abc"). Nulo = ferramenta desligada.</summary>
    public static string? NormalizarCaminho(string? caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho)) return null;
        var c = "/" + caminho.Trim().Trim('/');
        if (c.Length < 6) return null;                                   // caminho curto demais = fácil de adivinhar
        if (c.Contains("..") || c.Contains('\\') || c.Contains("//")) return null;
        return c;
    }

    /// <summary>Lista de e-mails liberados (separados por vírgula, ponto e vírgula ou espaço).</summary>
    public static HashSet<string> Emails(string? lista) =>
        (lista ?? "").Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Só entra quem está logado com e-mail da lista. Lista vazia = ninguém.</summary>
    public static bool Liberado(string? email, IReadOnlySet<string> liberados) =>
        !string.IsNullOrWhiteSpace(email) && liberados.Count > 0 && liberados.Contains(email.Trim());

    /// <summary>
    /// Converte o resto do endereço no nome do arquivo ("" → index.html). Nulo = endereço inválido
    /// (tentativa de sair da pasta, arquivo oculto, caracteres estranhos).
    /// </summary>
    public static string? Arquivo(string? resto)
    {
        var r = (resto ?? "").TrimStart('/');
        if (r.Length == 0) return "index.html";
        if (r.Length > 300 || r.Contains('\\') || r.Contains("//") || r.Contains(':')) return null;
        foreach (var parte in r.Split('/'))
            if (parte.Length == 0 || parte == "." || parte == ".." || parte.StartsWith('.')) return null;
        foreach (var ch in r)
            if (!(char.IsAsciiLetterOrDigit(ch) || ch is '/' or '.' or '-' or '_')) return null;
        return r;
    }

    /// <summary>Arquivos que o navegador precisa sempre revalidar (o restante é versionado pelo build).</summary>
    public static bool SempreRevalidar(string arquivo) =>
        arquivo is "index.html" or "service-worker.js" or "service-worker-assets.js" or "manifest.webmanifest"
        || arquivo.EndsWith("blazor.boot.json", StringComparison.Ordinal);

    /// <summary>Nome válido para o histórico (mesmas regras de arquivo, sem cair no index.html).</summary>
    public static string? NomeDado(string? resto)
    {
        var r = (resto ?? "").TrimStart('/');
        return r.Length == 0 ? null : Arquivo(r);
    }

    /// <summary>Prefixo de listagem: vazio ou um caminho válido terminado em "/".</summary>
    public static string? PrefixoDado(string? prefixo)
    {
        var p = (prefixo ?? "").Trim().TrimStart('/');
        if (p.Length == 0) return "";
        return Arquivo(p.TrimEnd('/')) is { } ok ? ok + "/" : null;
    }

    /// <summary>Versões enviadas são imutáveis: não podem ser regravadas nem apagadas.</summary>
    public static bool Imutavel(string nome) => nome.Split('/').Contains("versoes");
}
