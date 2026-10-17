using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>Regras de login e senha (sem dependência de infraestrutura).</summary>
public static class PoliticaSenha
{
    public const int TamanhoMinimo = 6;
    public const int TamanhoMaximo = 128;
    public const int MaxTentativas = 5;
    public static readonly TimeSpan TempoBloqueio = TimeSpan.FromMinutes(15);

    public static string NormalizarLogin(string? login) => (login ?? "").Trim().ToLowerInvariant();

    public static IReadOnlyList<string> ValidarLogin(string? login)
    {
        var l = NormalizarLogin(login);
        var e = new List<string>();
        if (l.Length is < 3 or > 60) e.Add("Login deve ter de 3 a 60 caracteres.");
        else if (!l.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' or '@')) e.Add("Login aceita letras sem acento, números, ponto, hífen, sublinhado e @");
        return e;
    }

    /// <summary>Regra definida pela empresa (28/09): só o tamanho mínimo; composição livre. O bloqueio por tentativas continua.</summary>
    public static IReadOnlyList<string> Validar(string? senha, string? login = null, string? nome = null)
    {
        var e = new List<string>();
        senha ??= "";
        if (senha.Length < TamanhoMinimo) e.Add($"A senha precisa ter pelo menos {TamanhoMinimo} caracteres.");
        if (senha.Length > TamanhoMaximo) e.Add($"A senha pode ter no máximo {TamanhoMaximo} caracteres.");
        return e;
    }
}
