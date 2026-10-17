using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Fiscal;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela de notas fiscais de entrada (só consulta; importação e vínculo ficam em NotasFiscaisServico).</summary>
public sealed class NotasFiscaisConsultas(IFabricaDb fabrica)
{
    /// <summary>Notas pela situação ("fila" = recebidas ou vinculadas; vazio = todas) e os indicadores da fila.</summary>
    public async Task<NotasFiscaisLista> ListaAsync(string? situacao, int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var q = db.NotasFiscais.AsNoTracking();
        if (situacao == "fila") q = q.Where(n => n.Situacao == SituacaoNota.Recebida || n.Situacao == SituacaoNota.Vinculada);
        else if (Enum.TryParse<SituacaoNota>(situacao, out var s)) q = q.Where(n => n.Situacao == s);
        var lista = await q.OrderByDescending(n => n.Emissao).Take(limite).ToListAsync(ct);
        var pids = lista.Where(n => n.PedidoId != null).Select(n => n.PedidoId!.Value).Distinct().ToList();
        var numPedido = await db.Pedidos.IgnoreQueryFilters([FiltrosNomes.Obra]).AsNoTracking().Where(p => pids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Numero, ct);
        var fila = await db.NotasFiscais.AsNoTracking().Where(n => n.Situacao == SituacaoNota.Recebida || n.Situacao == SituacaoNota.Vinculada)
            .GroupBy(n => n.Situacao).Select(g => new { g.Key, Qtd = g.Count(), Valor = g.Sum(x => x.ValorTotal) }).ToListAsync(ct);
        var ha30 = DateTimeOffset.UtcNow.AddDays(-30);
        var canceladas = await db.NotasFiscais.AsNoTracking().CountAsync(n => n.Situacao == SituacaoNota.Cancelada && n.Emissao >= ha30, ct);
        var importacoes = await db.ImportacoesNota.AsNoTracking().OrderByDescending(i => i.CriadoEm).Take(10).ToListAsync(ct);
        return new(lista, numPedido,
            fila.FirstOrDefault(x => x.Key == SituacaoNota.Recebida)?.Qtd ?? 0,
            fila.FirstOrDefault(x => x.Key == SituacaoNota.Vinculada)?.Qtd ?? 0,
            fila.Sum(x => x.Valor), canceladas, importacoes);
    }

    /// <summary>Nota com itens; null quando não existe.</summary>
    public async Task<NotaFiscalCarregada?> NotaAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var n = await db.NotasFiscais.AsNoTracking().Include(n => n.Itens).FirstOrDefaultAsync(n => n.Id == id, ct);
        if (n is null) return null;
        var pedidosForn = n.FornecedorId is { } f
            ? (await db.Pedidos.AsNoTracking().Where(p => p.FornecedorId == f && (NotasFiscaisServico.PedidosAbertos.Contains(p.Status) || p.Id == n.PedidoId))
                .OrderByDescending(p => p.Numero).Select(p => new { p.Id, p.Numero, Obra = db.Obras.Where(o => o.Id == p.ObraId).Select(o => o.Nome).FirstOrDefault(), p.Entrega })
                .ToListAsync(ct)).Select(p => new Opcao(p.Id, $"Pedido {p.Numero} · {p.Obra} · entrega {p.Entrega:dd/MM/yyyy}")).ToList()
            : [];
        var itensPedido = n.PedidoId is { } pid
            ? await db.PedidoItens.AsNoTracking().Where(i => i.PedidoId == pid).OrderBy(i => i.Descricao)
                .Select(i => new ItemPedidoParaConferencia(i.Id, i.Descricao, i.Unidade, i.Quantidade - i.QuantidadeRecebidaAceita, i.CodigoInsumo)).ToListAsync(ct)
            : [];
        return new(n, pedidosForn, itensPedido);
    }
}
