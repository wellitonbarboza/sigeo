using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Cadastros;

/// <summary>Leituras da tela de categorias do catálogo de produtos (Cadastros/Categorias).</summary>
public sealed class CategoriasConsultas(IFabricaDb fabrica)
{
    /// <summary>Categorias ativas, controlados vinculados a categoria e quantidade de produtos por grupo/subgrupo.</summary>
    public async Task<CategoriasDados> CarregarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var cats = await db.CategoriasProduto.Where(c => c.Ativo).ToListAsync(ct);
        var mats = await db.MateriaisControlados.AsNoTracking().Where(m => m.Ativo && m.CategoriaProdutoId != null).ToListAsync(ct);
        var qtd = (await db.Produtos.GroupBy(p => new { p.Grupo, p.SubGrupo }).Select(g => new { g.Key.Grupo, g.Key.SubGrupo, N = g.Count() }).ToListAsync(ct))
               .ToDictionary(x => (x.Grupo, x.SubGrupo), x => x.N);
        return new(cats, mats, qtd);
    }
}
