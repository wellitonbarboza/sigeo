using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Comum;

/// <summary>Erros de validação para mostrar na tela (lista legível).</summary>
public sealed class ValidacaoException(IReadOnlyList<string> erros) : Exception(string.Join(" | ", erros))
{
    public IReadOnlyList<string> Erros { get; } = erros;

    public static void Se(IReadOnlyList<string> erros)
    {
        if (erros.Count > 0) throw new ValidacaoException(erros);
    }
}
