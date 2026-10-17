using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Qualidade;

/// <summary>Leituras da tela de RNC (relatórios de não conformidade), só leitura.</summary>
public sealed class RncsConsultas(IFabricaDb fabrica)
{
    /// <summary>RNCs mais recentes (até o limite) com os números das ações corretivas e os nomes dos locais.</summary>
    public async Task<RncsLista> ListarAsync(int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var itens = await db.Rncs.AsNoTracking().OrderByDescending(r => r.Numero).Take(limite).ToListAsync(ct);
        var acs = await db.AcoesCorretivas.AsNoTracking().ToDictionaryAsync(a => a.Id, a => a.Numero, ct);
        var locNomes = await db.Locais.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Nome, ct);
        return new(itens, acs, locNomes);
    }

    public async Task<Rnc> AbrirAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Rncs.AsNoTracking().FirstAsync(x => x.Id == id, ct);
    }
}
