using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela de estoque (saldos, kardex e inventários de um almoxarifado).</summary>
public sealed class EstoqueConsultas(IFabricaDb fabrica)
{
    /// <summary>Saldos do almoxarifado e o disponível no central (só quando <paramref name="centralId"/> é outro almoxarifado).</summary>
    public async Task<EstoqueSaldos> SaldosAsync(Guid almoxId, Guid? centralId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var lista = await db.SaldosEstoque.AsNoTracking().Where(s => s.AlmoxarifadoId == almoxId).ToListAsync(ct);
        Dictionary<Guid, decimal> central = centralId is { } c
            ? await db.SaldosEstoque.AsNoTracking().Where(s => s.AlmoxarifadoId == c).ToDictionaryAsync(s => s.ProdutoId, s => s.Disponivel, ct)
            : [];
        return new(lista, central);
    }

    /// <summary>
    /// Movimentos do almoxarifado (kardex), mais recentes primeiro, até o limite. Com <paramref name="lerNomesLocais"/>,
    /// traz também o nome dos locais citados (a tela usa a própria lista de locais quando já a tem).
    /// </summary>
    public async Task<EstoqueMovimentos> MovimentosAsync(Guid almoxId, int limite, bool lerNomesLocais, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var lista = await db.MovimentosEstoque.AsNoTracking().Where(m => m.AlmoxarifadoId == almoxId).OrderByDescending(m => m.Data).ThenByDescending(m => m.CriadoEm).Take(limite).ToListAsync(ct);
        var locIds = lista.Where(m => m.LocalId != null).Select(m => m.LocalId!.Value).Distinct().ToList();
        var locs = lerNomesLocais ? await db.Locais.AsNoTracking().Where(l => locIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.Nome, ct) : null;
        return new(lista, locs);
    }

    /// <summary>Inventários do almoxarifado com itens, do mais recente ao mais antigo.</summary>
    public async Task<List<InventarioEstoque>> InventariosAsync(Guid almoxId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Inventarios.AsNoTracking().Include(i => i.Itens).Where(i => i.AlmoxarifadoId == almoxId).OrderByDescending(i => i.Numero).ToListAsync(ct);
    }

    /// <summary>Inventário aberto com os itens.</summary>
    public async Task<InventarioEstoque> InventarioAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Inventarios.AsNoTracking().Include(i => i.Itens).FirstAsync(i => i.Id == id, ct);
    }
}
