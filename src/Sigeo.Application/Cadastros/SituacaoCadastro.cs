using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Abstracoes;
using Sigeo.Application.Comum;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Fiscal;

namespace Sigeo.Application.Cadastros;

/// <summary>
/// Situação do insumo frente ao catálogo padronizado: grupo e subgrupo ativos em Categorias de Produto = padronizado;
/// grupo/subgrupo "A Cadastrar" ou vazio = a classificar; o resto (grupo/subgrupo que saiu do catálogo) = fora do catálogo.
/// </summary>
public static class SituacaoCadastro
{
    public const string ACadastrar = "A Cadastrar";

    /// <summary>Grupos ativos do catálogo e seus subgrupos ativos (nomes).</summary>
    public static async Task<Dictionary<string, HashSet<string>>> CatalogoAsync(ISigeoDb db, CancellationToken ct)
    {
        var cats = await db.CategoriasProduto.AsNoTracking().Where(c => c.Ativo).Select(c => new { c.Id, c.PaiId, c.Nome }).ToListAsync(ct);
        return cats.Where(c => c.PaiId == null)
            .GroupBy(g => g.Nome.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => cats.Where(s => g.Any(x => x.Id == s.PaiId)).Select(s => s.Nome.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
    }

    public static SituacaoCadastroProduto De(string? grupo, string? subgrupo, IReadOnlyDictionary<string, HashSet<string>> catalogo)
    {
        if (string.IsNullOrWhiteSpace(grupo) || grupo.Trim().Equals(ACadastrar, StringComparison.OrdinalIgnoreCase)
            || (subgrupo?.Trim().Equals(ACadastrar, StringComparison.OrdinalIgnoreCase) ?? false))
            return SituacaoCadastroProduto.AClassificar;
        if (!catalogo.TryGetValue(grupo.Trim(), out var subs)) return SituacaoCadastroProduto.ForaDoCatalogo;
        return string.IsNullOrWhiteSpace(subgrupo) || subs.Contains(subgrupo.Trim()) ? SituacaoCadastroProduto.Padronizado : SituacaoCadastroProduto.ForaDoCatalogo;
    }

    /// <summary>Mesma regra de <see cref="De"/>, traduzida para SQL.</summary>
    public static IQueryable<Produto> Filtrar(IQueryable<Produto> q, IQueryable<CategoriaProduto> cats, FiltroCadastroProduto filtro)
    {
        var ativas = cats.Where(c => c.Ativo);
        IQueryable<Produto> aClassificar(IQueryable<Produto> x) =>
            x.Where(p => p.Grupo == null || p.Grupo.Trim() == "" || p.Grupo == ACadastrar || p.SubGrupo == ACadastrar);
        IQueryable<Produto> padronizados(IQueryable<Produto> x) =>
            x.Where(p => p.Grupo != null && p.Grupo.Trim() != "" && p.Grupo != ACadastrar && (p.SubGrupo == null || p.SubGrupo != ACadastrar)
                && ativas.Any(g => g.PaiId == null && g.Nome == p.Grupo
                    && (p.SubGrupo == null || p.SubGrupo.Trim() == "" || ativas.Any(s => s.PaiId == g.Id && s.Nome == p.SubGrupo))));
        return filtro switch
        {
            FiltroCadastroProduto.Padronizados => padronizados(q),
            FiltroCadastroProduto.AClassificar => aClassificar(q),
            FiltroCadastroProduto.ForaDoCatalogo => q.Where(p => p.Grupo != null && p.Grupo.Trim() != "" && p.Grupo != ACadastrar && (p.SubGrupo == null || p.SubGrupo != ACadastrar)
                && !ativas.Any(g => g.PaiId == null && g.Nome == p.Grupo
                    && (p.SubGrupo == null || p.SubGrupo.Trim() == "" || ativas.Any(s => s.PaiId == g.Id && s.Nome == p.SubGrupo)))),
            FiltroCadastroProduto.Antigos => q.Where(p => !(p.Grupo != null && p.Grupo.Trim() != "" && p.Grupo != ACadastrar && (p.SubGrupo == null || p.SubGrupo != ACadastrar)
                && ativas.Any(g => g.PaiId == null && g.Nome == p.Grupo
                    && (p.SubGrupo == null || p.SubGrupo.Trim() == "" || ativas.Any(s => s.PaiId == g.Id && s.Nome == p.SubGrupo))))),
            _ => q,
        };
    }
}
