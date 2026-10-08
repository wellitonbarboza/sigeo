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

/// <summary>Liberação de pagamento (T18).</summary>
public sealed class PagamentosServico(IFabricaDb fabrica, AprovacaoServico aprovacoes, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── T18 Pagamento ─────────

    public async Task<Guid> EncaminharPagamentoAsync(LiberacaoPagamento l, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        Pedido? ped = l.PedidoId is { } pid ? await db.Pedidos.FirstAsync(p => p.Id == pid, ct) : null;
        Medicao? med = l.MedicaoId is { } mid ? await db.Medicoes.Include(m => m.Contrato).FirstAsync(m => m.Id == mid, ct) : null;
        var recs = l.PedidoId is null ? [] : await db.Recebimentos.AsNoTracking().Where(r => r.PedidoId == l.PedidoId).ToListAsync(ct);
        l.FornecedorId = ped?.FornecedorId ?? med?.Contrato.FornecedorId ?? l.FornecedorId;
        l.ObraId = ped?.ObraId ?? med?.ObraId;
        l.ContratoId ??= med?.ContratoId;
        var trava = TravasQualidade.VerificarPagamento(l, ped, recs, med);
        l.Bloqueios = trava.Resumo;
        ValidacaoException.Se(trava.Bloqueios);
        l.Numero = await Numerador.ProximoAsync(db, "Pagamento", ct);
        l.EncaminhadoPor = usuario.Nome; l.EncaminhadoEm = relogio.GetUtcNow();
        db.Pagamentos.Add(l);
        if (med is not null) { med.ChaveNFe = l.ChaveNFe; med.Status = StatusMedicao.NfRecebida; }
        await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Pagamento, l.Id, l.ObraId, $"Pagamento NF {l.ChaveNFe[^9..]}", l.ValorLiberado, ct);
        await db.SaveChangesAsync(ct);
        return l.Id;
    }

    public async Task MarcarPagoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var l = await db.Pagamentos.FirstAsync(x => x.Id == id, ct);
        if (l.Status != StatusPagamento.Liberado) throw new RegraDeNegocioException("Pagamento ainda não liberado pelo Diretor Técnico.");
        l.Status = StatusPagamento.Pago;
        if (l.MedicaoId is { } mid) (await db.Medicoes.FirstAsync(m => m.Id == mid, ct)).Status = StatusMedicao.Paga;
        await db.SaveChangesAsync(ct);
    }
}
