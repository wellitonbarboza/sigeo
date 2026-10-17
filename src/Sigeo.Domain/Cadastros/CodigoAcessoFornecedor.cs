using System.Security.Cryptography;
using System.Text;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Cadastros;

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
