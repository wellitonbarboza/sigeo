using System.Security.Cryptography;
using System.Text;
using QualyHub.Domain.Common;

namespace QualyHub.Domain.Cadastros;

public enum CanalCodigo { Email = 1, WhatsApp = 2 }

/// <summary>
/// Código de acesso de uso único à Área do Fornecedor (CNPJ/CPF + código enviado a um contato do cadastro).
/// Só o hash do código é guardado. Vale poucos minutos e aceita poucas tentativas.
/// </summary>
public class CodigoAcessoFornecedor : Entidade
{
    public const int ValidadeMinutos = 10;
    public const int MaxTentativas = 5;

    /// <summary>CNPJ/CPF só dígitos (a busca é por documento, em todas as construtoras).</summary>
    public string Documento { get; set; } = string.Empty;
    public Guid EmpresaId { get; set; }
    public Guid FornecedorId { get; set; }
    public Guid ContatoId { get; set; }
    public CanalCodigo Canal { get; set; }
    /// <summary>Destino normalizado (e-mail em minúsculas ou telefone E.164) — é o que fica "verificado" na sessão.</summary>
    public string Destino { get; set; } = string.Empty;
    public string CodigoHash { get; set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset ExpiraEm { get; set; }
    public int Tentativas { get; set; }
    public DateTimeOffset? UsadoEm { get; set; }
    public string? Ip { get; set; }

    public bool Vigente(DateTimeOffset agora) => UsadoEm is null && agora < ExpiraEm && Tentativas < MaxTentativas;

    /// <summary>Hash do código amarrado ao registro (o mesmo código em outro registro não confere).</summary>
    public static string Hash(Guid id, string codigo) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{id:N}:{codigo.Trim()}"))).ToLowerInvariant();

    public static string NovoCodigo() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("000000");

    /// <summary>Confere o código: conta a tentativa; certo → marca usado (uso único).</summary>
    public bool Conferir(string codigo, DateTimeOffset agora)
    {
        if (!Vigente(agora)) return false;
        Tentativas++;
        var ok = CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Hash(Id, codigo)), Encoding.ASCII.GetBytes(CodigoHash));
        if (ok) UsadoEm = agora;
        return ok;
    }
}

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
