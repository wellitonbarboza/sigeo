using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Fiscal;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela de recebimentos (só consulta; o registro fica em RecebimentosServico).</summary>
public sealed class RecebimentosConsultas(IFabricaDb fabrica)
{
    public async Task<ParametrosVigentes> ParametrosAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await ParametrosServico.CarregarAsync(db, ct);
    }

    /// <summary>NF-e importada com os itens.</summary>
    public async Task<NotaFiscalEntrada?> NotaAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.NotasFiscais.AsNoTracking().Include(x => x.Itens).FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<RecebimentosLista> ListaAsync(int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var lista = await db.Recebimentos.AsNoTracking().Include(r => r.Pedido).Include(r => r.Itens).OrderByDescending(r => r.Numero).Take(limite).ToListAsync(ct);
        var forn = await db.Fornecedores.AsNoTracking().ToDictionaryAsync(f => f.Id, f => f.NomeExibicao, ct);
        return new(lista, forn);
    }

    /// <summary>Pedidos aguardando entrega (enviados ou entregues em parte).</summary>
    public async Task<List<Opcao>> PedidosAguardandoAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Pedidos.AsNoTracking().Where(p => p.Status == StatusPedido.EnviadoFornecedor || p.Status == StatusPedido.EntregueParcial)
            .OrderBy(p => p.Numero).Select(p => new Opcao(p.Id, "Pedido " + p.Numero + " – " + p.Fornecedor.RazaoSocial)).ToListAsync(ct);
    }

    public async Task<Pedido> PedidoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Pedidos.AsNoTracking().Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
    }

    public async Task<RecebimentoCarregado> RecebimentoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var r = await db.Recebimentos.AsNoTracking().Include(r => r.Itens).Include(r => r.FvmItens).Include(r => r.Ensaios).Include(r => r.Aplicacoes).AsSplitQuery().FirstAsync(r => r.Id == id, ct);
        var pis = r.Itens.Where(i => i.PedidoItemId != null).Select(i => i.PedidoItemId!.Value).ToList();
        var marcas = new Dictionary<Guid, string?>();
        foreach (var pm in await db.PedidoItens.AsNoTracking().Where(i => pis.Contains(i.Id)).Select(i => new { i.Id, i.Marca }).ToListAsync(ct)) marcas[pm.Id] = pm.Marca;
        var ped = r.PedidoId is { } pid ? await db.Pedidos.AsNoTracking().Where(p => p.Id == pid).Select(p => p.Numero).FirstOrDefaultAsync(ct) : 0;
        var transf = await db.Transferencias.AsNoTracking().Where(t => t.RecebimentoId == id && t.Status != StatusTransferencia.Cancelada).Select(t => (Guid?)t.Id).FirstOrDefaultAsync(ct);
        return new(r, marcas, ped, transf);
    }
}
