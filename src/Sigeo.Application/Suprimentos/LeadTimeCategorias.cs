using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Lead time padrão por categoria do catálogo: produto sem lead time herda o do subgrupo, senão o do grupo.</summary>
public static class LeadTimeCategorias
{
    public static async Task<Dictionary<(string Grupo, string Sub), int>> MapaAsync(ISigeoDb db, CancellationToken ct = default)
    {
        var cats = await db.CategoriasProduto.AsNoTracking().Where(c => c.Ativo).Select(c => new { c.Id, c.PaiId, c.Nome, c.LeadTimeDias }).ToListAsync(ct);
        var porId = cats.ToDictionary(c => c.Id);
        var r = new Dictionary<(string, string), int>();
        foreach (var c in cats.Where(c => c.LeadTimeDias is > 0))
        {
            var chave = c.PaiId is { } pid && porId.TryGetValue(pid, out var pai) ? (K(pai.Nome), K(c.Nome)) : (K(c.Nome), "");
            r[chave] = c.LeadTimeDias!.Value;
        }
        return r;
    }

    public static int De(Dictionary<(string Grupo, string Sub), int> mapa, string? grupo, string? subGrupo) =>
        mapa.TryGetValue((K(grupo), K(subGrupo)), out var s) ? s : mapa.TryGetValue((K(grupo), ""), out var g) ? g : 0;

    private static string K(string? s) => (s ?? "").Trim().ToUpperInvariant();

    /// <summary>Completa o lead time dos itens que vieram sem (produto sem lead time próprio).</summary>
    public static async Task CompletarAsync(ISigeoDb db, IEnumerable<RequisicaoItem> itens, CancellationToken ct = default)
    {
        var sem = itens.Where(i => i.LeadTimeDias <= 0 && i.ProdutoId != null).ToList();
        if (sem.Count == 0) return;
        var mapa = await MapaAsync(db, ct);
        if (mapa.Count == 0) return;
        var ids = sem.Select(i => i.ProdutoId!.Value).Distinct().ToList();
        var prods = await db.Produtos.AsNoTracking().Where(p => ids.Contains(p.Id)).Select(p => new { p.Id, p.Grupo, p.SubGrupo }).ToDictionaryAsync(p => p.Id, ct);
        foreach (var i in sem)
            if (prods.TryGetValue(i.ProdutoId!.Value, out var p)) i.LeadTimeDias = De(mapa, p.Grupo, p.SubGrupo);
    }
}
