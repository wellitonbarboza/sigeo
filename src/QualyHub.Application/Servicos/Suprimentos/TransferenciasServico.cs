using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Regras;
using QualyHub.Domain.Suprimentos;
using QualyHub.Domain.Qualidade;

namespace QualyHub.Application.Servicos;

/// <summary>Transferência de material do almoxarifado central para a obra/SPE.</summary>
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
            Data = Hoje, Numero = await Numerador.ProximoAsync(db, "Transferencia", ct)
        };
        foreach (var i in r.Itens.Where(i => i.QtdRecebida > 0))
        {
            var pi = r.Pedido?.Itens.FirstOrDefault(x => x.Id == i.PedidoItemId);
            t.Itens.Add(new TransferenciaItem
            {
                TransferenciaId = t.Id, RecebimentoItemId = i.Id, ProdutoId = pi?.ProdutoId, Descricao = i.Descricao, Unidade = i.Unidade, Quantidade = i.QtdRecebida,
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
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelarTransferenciaAsync(Guid id, string motivo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new RegraDeNegocioException("Informe o motivo do cancelamento.");
        await using var db = fabrica.Criar();
        var t = await db.Transferencias.FirstAsync(x => x.Id == id, ct);
        if (t.Status is StatusTransferencia.Recebida or StatusTransferencia.Cancelada) throw new RegraDeNegocioException("Transferência já recebida ou cancelada.");
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
        await db.SaveChangesAsync(ct);
    }
}
