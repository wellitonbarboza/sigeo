using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Sst;

namespace Sigeo.Application.Sst;

/// <summary>Leituras da tela de permissões de trabalho (Sst/PermissoesTrabalho).</summary>
public sealed class PermissoesTrabalhoConsultas(IFabricaDb fabrica)
{
    /// <summary>PTs mais recentes (até <paramref name="limite"/>) e nome de cada local.</summary>
    public async Task<PermissoesTrabalhoDados> ListarAsync(int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var itens = await db.PermissoesTrabalho.AsNoTracking().Include(p => p.Executantes).OrderByDescending(p => p.Inicio).Take(limite).ToListAsync(ct);
        var locNomes = await db.Locais.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Nome, ct);
        return new(itens, locNomes);
    }

    public async Task<PermissaoTrabalho> ObterAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.PermissoesTrabalho.AsNoTracking().Include(p => p.Executantes).FirstAsync(p => p.Id == id, ct);
    }
}
