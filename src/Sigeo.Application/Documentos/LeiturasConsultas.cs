using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Documentos;

namespace Sigeo.Application.Documentos;

/// <summary>Leituras da tela de ciência de leitura dos documentos, só leitura.</summary>
public sealed class LeiturasConsultas(IFabricaDb fabrica)
{
    /// <summary>Ciências com a revisão e o documento, por prazo.</summary>
    public async Task<List<Ciencia>> ListarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Ciencias.AsNoTracking().Include(c => c.Revisao).ThenInclude(r => r.Documento).OrderBy(c => c.Prazo).ToListAsync(ct);
    }
}
