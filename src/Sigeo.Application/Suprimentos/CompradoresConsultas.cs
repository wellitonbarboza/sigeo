using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela de carteiras de compradores (as carteiras vêm de AtendimentoServico).</summary>
public sealed class CompradoresConsultas(IFabricaDb fabrica)
{
    /// <summary>Subgrupos ativos do catálogo por nome do grupo, em ordem alfabética.</summary>
    public async Task<Dictionary<string, List<string>>> SubgruposAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var cats = await db.CategoriasProduto.AsNoTracking().Where(c => c.Ativo).Select(c => new { c.Id, c.PaiId, c.Nome }).ToListAsync(ct);
        var porId = cats.ToDictionary(c => c.Id);
        return cats.Where(c => c.PaiId != null && porId.ContainsKey(c.PaiId.Value)).GroupBy(c => porId[c.PaiId!.Value].Nome)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Nome).OrderBy(x => x).ToList());
    }
}
