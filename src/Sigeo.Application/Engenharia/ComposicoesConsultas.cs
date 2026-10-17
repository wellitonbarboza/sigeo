using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Engenharia;

namespace Sigeo.Application.Engenharia;

/// <summary>Leituras da tela de serviços e composições.</summary>
public sealed class ComposicoesConsultas(IFabricaDb fabrica)
{
    /// <summary>Composições com itens, por grupo e código.</summary>
    public async Task<List<Composicao>> ListaAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Composicoes.AsNoTracking().Include(c => c.Itens).OrderBy(c => c.Grupo).ThenBy(c => c.Codigo).ToListAsync(ct);
    }

    /// <summary>Composição com itens, para edição.</summary>
    public async Task<Composicao> ComposicaoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Composicoes.AsNoTracking().Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
    }
}
