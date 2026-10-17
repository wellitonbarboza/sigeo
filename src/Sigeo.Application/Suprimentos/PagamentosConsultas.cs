using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela de liberação de pagamento (lista, opções do novo e abertura).</summary>
public sealed class PagamentosConsultas(IFabricaDb fabrica)
{
    /// <summary>Lista das liberações, mais recentes primeiro, até o limite da grade.</summary>
    public async Task<List<PagamentoLinha>> ListaAsync(int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var forn = await db.Fornecedores.AsNoTracking().ToDictionaryAsync(f => f.Id, f => f.NomeExibicao, ct);
        return (await db.Pagamentos.AsNoTracking().OrderByDescending(p => p.Numero).Take(limite).ToListAsync(ct))
            .Select(p => new PagamentoLinha(p.Id, p.Numero, p.Origem, forn.GetValueOrDefault(p.FornecedorId, ""), p.ChaveNFe.Length >= 9 ? "…" + p.ChaveNFe[^9..] : p.ChaveNFe,
                p.ValorNF, p.ValorLiberado, p.Vencimento, p.Status, p.CriadoPor)).ToList();
    }

    /// <summary>Pedidos e medições que podem gerar uma nova liberação.</summary>
    public async Task<PagamentoOpcoes> OpcoesNovoAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var pedidos = await db.Pedidos.AsNoTracking().Where(p => p.Status == StatusPedido.Entregue || p.Status == StatusPedido.EntregueParcial || p.Status == StatusPedido.EnviadoFornecedor)
            .OrderByDescending(p => p.Numero).Select(p => new Opcao(p.Id, "Pedido " + p.Numero + " – " + p.Fornecedor.RazaoSocial)).ToListAsync(ct);
        var medicoes = await db.Medicoes.AsNoTracking().Where(m => m.Status == StatusMedicao.Aprovada)
            .Select(m => new Opcao(m.Id, m.Contrato.Numero + " · med. " + m.NumeroMedicao + " – " + m.Contrato.Fornecedor.RazaoSocial)).ToListAsync(ct);
        return new(pedidos, medicoes);
    }

    /// <summary>Pedido escolhido com seus itens e recebimentos.</summary>
    public async Task<PagamentoPedido> PedidoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var pedido = await db.Pedidos.AsNoTracking().Include(p => p.Itens).FirstAsync(p => p.Id == id, ct);
        var recs = await db.Recebimentos.AsNoTracking().Include(r => r.Itens).Where(r => r.PedidoId == id).ToListAsync(ct);
        return new(pedido, recs);
    }

    /// <summary>Medição escolhida com contrato e fornecedor.</summary>
    public async Task<Medicao> MedicaoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Medicoes.AsNoTracking().Include(m => m.Contrato).ThenInclude(k => k.Fornecedor).FirstAsync(m => m.Id == id, ct);
    }

    /// <summary>Liberação aberta com pedido, recebimentos e medição de origem.</summary>
    public async Task<PagamentoAberto> AbrirAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var l = await db.Pagamentos.AsNoTracking().FirstAsync(p => p.Id == id, ct);
        var pedido = l.PedidoId is { } pid ? await db.Pedidos.AsNoTracking().Include(p => p.Itens).FirstAsync(p => p.Id == pid, ct) : null;
        List<Recebimento> recs = l.PedidoId is { } p2 ? await db.Recebimentos.AsNoTracking().Include(r => r.Itens).Where(r => r.PedidoId == p2).ToListAsync(ct) : [];
        var medicao = l.MedicaoId is { } mid ? await db.Medicoes.AsNoTracking().Include(m => m.Contrato).ThenInclude(k => k.Fornecedor).FirstAsync(m => m.Id == mid, ct) : null;
        return new(l, pedido, recs, medicao);
    }
}
