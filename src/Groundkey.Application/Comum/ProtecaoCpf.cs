using System.Security.Cryptography;
using System.Text;

namespace Groundkey.Application.Comum;

/// <summary>
/// LGPD — CPF de funcionário nunca em claro no banco: HMAC-SHA256 com chave do Key Vault (permite casar na importação)
/// + versão mascarada para exibição.
/// </summary>
public sealed class ProtecaoCpf(byte[] chaveHmac)
{
    public string Hash(string cpf) =>
        Convert.ToHexString(HMACSHA256.HashData(chaveHmac, Encoding.UTF8.GetBytes(Conversor.SomenteDigitos(cpf))));

    public static string Mascarar(string cpf)
    {
        var d = Conversor.SomenteDigitos(cpf);
        return d.Length == 11 ? $"***.{d[3..6]}.{d[6..9]}-**" : "***";
    }
}
