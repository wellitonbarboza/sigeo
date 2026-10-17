using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Sst;

namespace Sigeo.Application.Sst;

/// <summary>Leituras da tela de inspeções de segurança (Sst/Inspecoes).</summary>
public sealed class InspecoesConsultas(IFabricaDb fabrica)
{
    /// <summary>Inspeções mais recentes (até <paramref name="limite"/>) e nome de cada local.</summary>
    public async Task<InspecoesDados> ListarAsync(int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var itens = await db.InspecoesSeguranca.AsNoTracking().Include(i => i.Itens).OrderByDescending(i => i.Data).Take(limite).ToListAsync(ct);
        var locNomes = await db.Locais.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Nome, ct);
        return new(itens, locNomes);
    }

    public async Task<InspecaoSeguranca> ObterAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.InspecoesSeguranca.AsNoTracking().Include(i => i.Itens).FirstAsync(i => i.Id == id, ct);
    }
}
