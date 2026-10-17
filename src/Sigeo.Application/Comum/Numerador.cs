using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Comum;

/// <summary>Numeração sequencial por empresa/chave, com concorrência otimista (RowVersion) e nova tentativa.</summary>
public static class Numerador
{
    public static async Task<int> ProximoAsync(ISigeoDb db, string chave, CancellationToken ct = default)
    {
        var seq = db.Sequencias.Local.FirstOrDefault(s => s.Chave == chave && s.EmpresaId == db.EmpresaAtualId)
                  ?? await db.Sequencias.FirstOrDefaultAsync(s => s.EmpresaId == db.EmpresaAtualId && s.Chave == chave, ct);
        if (seq is null)
        {
            seq = new Sequencia { EmpresaId = db.EmpresaAtualId, Chave = chave };
            db.Sequencias.Add(seq);
        }
        return seq.Proximo();
    }

    /// <summary>Como <see cref="ProximoAsync"/>, mas, se a sequência ainda não existe, começa do maior número já emitido (ex.: último código da categoria).</summary>
    public static async Task<int> ProximoAPartirDeAsync(ISigeoDb db, string chave, Func<Task<int>> ultimoExistente, CancellationToken ct = default)
    {
        var seq = db.Sequencias.Local.FirstOrDefault(s => s.Chave == chave && s.EmpresaId == db.EmpresaAtualId)
                  ?? await db.Sequencias.FirstOrDefaultAsync(s => s.EmpresaId == db.EmpresaAtualId && s.Chave == chave, ct);
        if (seq is null)
        {
            seq = new Sequencia { EmpresaId = db.EmpresaAtualId, Chave = chave, Ultimo = await ultimoExistente() };
            db.Sequencias.Add(seq);
        }
        return seq.Proximo();
    }
}
