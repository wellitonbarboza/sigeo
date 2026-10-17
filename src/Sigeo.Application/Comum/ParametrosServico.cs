using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Comum;

public static class ParametrosServico
{
    public static async Task<ParametrosVigentes> CarregarAsync(ISigeoDb db, CancellationToken ct = default) =>
        new(await db.Parametros.AsNoTracking().ToDictionaryAsync(p => p.Chave, p => p.Valor, ct));
}
