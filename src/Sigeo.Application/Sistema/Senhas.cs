using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Sistema;

/// <summary>Hash de senha PBKDF2-HMAC-SHA512 (210.000 iterações, sal de 16 bytes). Formato: pbkdf2-sha512$iter$sal$hash.</summary>
public static class Senhas
{
    private const int Iteracoes = 210_000;
    private const string Prefixo = "pbkdf2-sha512";
    // Usado para gastar o mesmo tempo quando o login não existe (não revela quais logins são válidos).
    private static readonly string HashFicticio = Gerar("senha-ficticia-para-tempo-constante-1");

    public static string Gerar(string senha)
    {
        var sal = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, sal, Iteracoes, HashAlgorithmName.SHA512, 32);
        return $"{Prefixo}${Iteracoes}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string senha, string? armazenado)
    {
        var p = (armazenado ?? HashFicticio).Split('$');
        if (p.Length != 4 || p[0] != Prefixo || !int.TryParse(p[1], out var it)) { Verificar(senha, HashFicticio); return false; }
        var esperado = Convert.FromBase64String(p[3]);
        var calculado = Rfc2898DeriveBytes.Pbkdf2(senha, Convert.FromBase64String(p[2]), it, HashAlgorithmName.SHA512, esperado.Length);
        return armazenado is not null && CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }

    /// <summary>Senha provisória legível (sem 0/O, 1/l/I), 12 caracteres com letras e números.</summary>
    public static string GerarProvisoria()
    {
        const string letras = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ", numeros = "23456789";
        const string todos = letras + numeros;
        while (true)
        {
            var s = new string(Enumerable.Range(0, 12).Select(_ => todos[RandomNumberGenerator.GetInt32(todos.Length)]).ToArray());
            if (s.Any(char.IsLetter) && s.Count(char.IsDigit) >= 2) return s;
        }
    }
}
