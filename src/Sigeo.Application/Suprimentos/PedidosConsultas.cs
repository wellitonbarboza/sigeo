using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela de pedidos (só consulta; gravações ficam em PedidosServico).</summary>
public sealed class PedidosConsultas(IFabricaDb fabrica)
{
    public async Task<PedidosLista> ListaAsync(int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var lista = await db.Pedidos.AsNoTracking().Include(p => p.Itens).Include(p => p.Fornecedor).OrderByDescending(p => p.Numero).Take(limite).ToListAsync(ct);
        var obras = await db.Obras.AsNoTracking().ToDictionaryAsync(o => o.Id, o => o.Nome, ct);
        return new(lista, obras);
    }

    public async Task<PedidoCarregado> PedidoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var p = await db.Pedidos.AsNoTracking().Include(p => p.Itens).FirstAsync(p => p.Id == id, ct);
        var locaisItem = await QuadroLocais.CarregarAsync(db, p.Itens.Where(i => i.RequisicaoItemId != null).Select(i => i.RequisicaoItemId!.Value).Distinct().ToList(), ct);
        var fornecedor = await db.Fornecedores.AsNoTracking().Include(f => f.Qualificacoes).Include(f => f.Documentos).ThenInclude(d => d.TipoDocumento).FirstAsync(f => f.Id == p.FornecedorId, ct);
        return new(p, locaisItem, fornecedor);
    }
}
