using Microsoft.EntityFrameworkCore;
using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Cadastros;
using Groundkey.Domain.Common;
using Groundkey.Domain.Engenharia;
using Groundkey.Domain.Estrutura;
using Groundkey.Domain.Regras;
using Groundkey.Domain.Suprimentos;
using Groundkey.Domain.Qualidade;

namespace Groundkey.Application.Servicos;

/// <summary>Transferência de material entre almoxarifados (central ↔ obra, obra ↔ obra). Envio baixa da origem; conferência entra no destino.</summary>
public sealed class TransferenciasServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── Transferência: almoxarifado central → obra/SPE ─────────

    /// <summary>Monta a transferência a partir do recebimento central (itens aceitos, custo do pedido, SPE do pedido).</summary>
    public async Task<Guid> GerarTransferenciaAsync(Guid recebimentoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var r = await db.Recebimentos.Include(x => x.Itens).Include(x => x.Pedido!).ThenInclude(p => p.Itens).FirstAsync(x => x.Id == recebimentoId, ct);
        if (r.Destino == DestinoRecebimento.Devolvido) throw new RegraDeNegocioException("Material devolvido não é transferido.");
        if (r.FvmAprovada is null) throw new RegraDeNegocioException("Registre a FVM antes de transferir.");
        if (!r.AlmoxarifadoCentral) throw new RegraDeNegocioException("Só material recebido no almoxarifado central é transferido (entrega direta na obra já é custo da obra).");
        if (await db.Transferencias.AnyAsync(t => t.RecebimentoId == r.Id && t.Status != StatusTransferencia.Cancelada, ct)) throw new RegraDeNegocioException("Este recebimento já tem transferência.");
        var t = new TransferenciaMaterial
        {
            ObraId = r.ObraId, RecebimentoId = r.Id, PedidoId = r.PedidoId, SpeId = r.Pedido?.SpeId, OrigemEstabelecimentoId = r.EstabelecimentoId ?? r.Pedido?.EstabelecimentoFaturamentoId,
            Data = Hoje, Numero = await Numerador.ProximoAsync(db, "Transferencia", ct),
            DestinoAlmoxarifadoId = (await MotorEstoque.DaObraAsync(db, r.ObraId, ct)).Id
        };
        // Recebimento que entrou no estoque do central: a transferência baixa de lá. Recebimento antigo (antes do estoque) não tem saldo para baixar.
        if (r.AlmoxarifadoId is { } origem && await db.MovimentosEstoque.AnyAsync(m => m.OrigemTipo == "Recebimento" && m.OrigemId == r.Id, ct))
            t.OrigemAlmoxarifadoId = origem;
        foreach (var i in r.Itens.Where(i => i.QtdRecebida > 0))
        {
            var pi = r.Pedido?.Itens.FirstOrDefault(x => x.Id == i.PedidoItemId);
            t.Itens.Add(new TransferenciaItem
            {
                TransferenciaId = t.Id, RecebimentoItemId = i.Id, ProdutoId = i.ProdutoId ?? pi?.ProdutoId, Descricao = i.Descricao, Unidade = i.Unidade, Quantidade = i.QtdRecebida,
                CustoUnitario = pi?.ValorUnitario ?? 0, Lote = i.Lote
            });
        }
        db.Transferencias.Add(t);
        await db.SaveChangesAsync(ct);
        return t.Id;
    }

    public async Task SalvarTransferenciaAsync(TransferenciaMaterial t, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (t.Numero == 0) t.Numero = await Numerador.ProximoAsync(db, "Transferencia", ct);
        var statusAtual = await db.Transferencias.Where(x => x.Id == t.Id).Select(x => (StatusTransferencia?)x.Status).FirstOrDefaultAsync(ct) ?? StatusTransferencia.Rascunho;
        if (statusAtual != StatusTransferencia.Rascunho) throw new RegraDeNegocioException("Transferência já enviada.");
        t.Status = statusAtual;
        foreach (var i in t.Itens) i.TransferenciaId = t.Id;
        var erros = t.Validar().ToList();
        // Não transfere mais do que foi recebido no almoxarifado.
        var recIds = t.Itens.Where(i => i.RecebimentoItemId != null).Select(i => i.RecebimentoItemId!.Value).ToList();
        var recebido = await db.Set<RecebimentoItem>().AsNoTracking().Where(x => recIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.QtdRecebida, ct);
        foreach (var i in t.Itens.Where(i => i.RecebimentoItemId is { } rid && recebido.TryGetValue(rid, out var q) && i.Quantidade > q))
            erros.Add($"{i.Descricao}: quantidade maior que a recebida ({recebido[i.RecebimentoItemId!.Value]:0.###}).");
        ValidacaoException.Se(erros);
        await Grafo.SalvarAsync(db, t, nameof(TransferenciaMaterial.Itens));
        await db.SaveChangesAsync(ct);
    }

    public async Task EnviarTransferenciaAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var t = await db.Transferencias.Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
        t.Enviar();
        // Sai do saldo da origem (fica em trânsito até a conferência no destino); o custo vai pelo custo médio da origem.
        if (t.OrigemAlmoxarifadoId is { } origem)
        {
            var agora = relogio.GetUtcNow();
            foreach (var i in t.Itens)
            {
                if (i.ProdutoId is not { } prod) throw new RegraDeNegocioException($"{i.Descricao}: item sem produto cadastrado não pode sair do estoque.");
                var m = await MotorEstoque.MovimentarAsync(db, new MovimentoEstoque
                {
                    AlmoxarifadoId = origem, ProdutoId = prod, Data = agora, Tipo = TipoMovimentoEstoque.TransferenciaSaida, Quantidade = -i.Quantidade,
                    Documento = $"Transferência {t.Numero} · enviada", OrigemTipo = "Transferencia", OrigemId = t.Id, Lote = i.Lote
                }, ct);
                i.CustoUnitario = m.CustoUnitario;
            }
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelarTransferenciaAsync(Guid id, string motivo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new RegraDeNegocioException("Informe o motivo do cancelamento.");
        await using var db = fabrica.Criar();
        var t = await db.Transferencias.FirstAsync(x => x.Id == id, ct);
        if (t.Status is StatusTransferencia.Recebida or StatusTransferencia.Cancelada) throw new RegraDeNegocioException("Transferência já recebida ou cancelada.");
        // Já enviada: o material volta para o saldo da origem.
        if (t.Status == StatusTransferencia.Enviada && t.OrigemAlmoxarifadoId is { } origem)
        {
            var itens = await db.TransferenciaItens.AsNoTracking().Where(i => i.TransferenciaId == t.Id && i.ProdutoId != null).ToListAsync(ct);
            foreach (var i in itens)
                await MotorEstoque.MovimentarAsync(db, new MovimentoEstoque
                {
                    AlmoxarifadoId = origem, ProdutoId = i.ProdutoId!.Value, Data = relogio.GetUtcNow(), Tipo = TipoMovimentoEstoque.TransferenciaEntrada, Quantidade = i.Quantidade,
                    CustoUnitario = i.CustoUnitario, Documento = $"Transferência {t.Numero} · cancelada (retorno)", OrigemTipo = "Transferencia", OrigemId = t.Id
                }, ct);
        }
        t.Status = StatusTransferencia.Cancelada;
        t.Observacao = string.IsNullOrWhiteSpace(t.Observacao) ? $"Cancelada: {motivo.Trim()}" : $"{t.Observacao}\nCancelada: {motivo.Trim()}";
        await db.SaveChangesAsync(ct);
    }

    public async Task ConferirTransferenciaAsync(Guid id, IReadOnlyDictionary<Guid, decimal> recebidas, string? observacao, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var t = await db.Transferencias.Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
        if (!string.IsNullOrWhiteSpace(observacao)) t.Observacao = observacao;
        t.Conferir(usuario.Nome, relogio.GetUtcNow(), recebidas);
        // Entra no destino o que foi conferido (a diferença fica registrada na observação).
        if (t.DestinoAlmoxarifadoId is { } destino)
        {
            var agora = relogio.GetUtcNow();
            foreach (var i in t.Itens.Where(i => i.ProdutoId is not null && (i.QuantidadeRecebida ?? i.Quantidade) > 0))
                await MotorEstoque.MovimentarAsync(db, new MovimentoEstoque
                {
                    AlmoxarifadoId = destino, ProdutoId = i.ProdutoId!.Value, Data = agora, Tipo = TipoMovimentoEstoque.TransferenciaEntrada, Quantidade = i.QuantidadeRecebida ?? i.Quantidade,
                    CustoUnitario = i.CustoUnitario, Documento = $"Transferência {t.Numero} · conferida", OrigemTipo = "Transferencia", OrigemId = t.Id, Lote = i.Lote
                }, ct);
        }
        await MotorEstoque.RemoverRestricoesAsync(db, t.Id, $"Material conferido na transferência {t.Numero}.", usuario.Nome, relogio.GetUtcNow(), ct);
        await db.SaveChangesAsync(ct);
    }
}
