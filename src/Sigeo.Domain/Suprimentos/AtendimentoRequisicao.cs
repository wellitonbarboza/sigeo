using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Regras do atendimento parcial: saldo por item e situação da requisição.</summary>
public static class AtendimentoRequisicao
{
    public sealed record LinhaCotacao(Guid CotacaoId, Guid RequisicaoItemId, decimal Quantidade);
    public sealed record LinhaPedido(Guid RequisicaoItemId, decimal Quantidade, decimal Recebida);

    /// <summary>
    /// Saldo de cada item. Em cotação = quantidade nas cotações ainda abertas (uma vez por cotação, não por fornecedor);
    /// pedido e recebido = soma dos itens de pedido não cancelados ligados ao item.
    /// </summary>
    public static List<SaldoItem> Calcular(IEnumerable<RequisicaoItem> itens, IEnumerable<LinhaCotacao> cotacoesAbertas, IEnumerable<LinhaPedido> pedidos)
    {
        var cot = cotacoesAbertas.GroupBy(c => (c.CotacaoId, c.RequisicaoItemId)).Select(g => (g.Key.RequisicaoItemId, Qtd: g.Max(x => x.Quantidade)))
            .GroupBy(x => x.RequisicaoItemId).ToDictionary(g => g.Key, g => g.Sum(x => x.Qtd));
        var ped = pedidos.GroupBy(p => p.RequisicaoItemId).ToDictionary(g => g.Key, g => (Q: g.Sum(x => x.Quantidade), R: g.Sum(x => x.Recebida)));
        return itens.Select(i =>
        {
            var requisitado = i.Status == StatusItemRequisicao.Cancelado && i.QtdCancelada == 0 ? 0 : i.Quantidade;
            var p = ped.GetValueOrDefault(i.Id);
            return new SaldoItem(i.Id, requisitado, cot.GetValueOrDefault(i.Id), p.Q, p.R, Math.Min(i.QtdCancelada, requisitado));
        }).ToList();
    }

    /// <summary>
    /// Situação da requisição a partir dos saldos: sem nada comprado fica como está (autorizada); comprando = em compra;
    /// algo recebido = parcialmente atendida; tudo recebido = atendida; recebido + cancelado = encerrada com saldo.
    /// </summary>
    public static StatusRequisicao Situacao(StatusRequisicao atual, IReadOnlyCollection<SaldoItem> saldos)
    {
        if (atual is StatusRequisicao.Rascunho or StatusRequisicao.AguardandoEngenheiro or StatusRequisicao.AguardandoDiretorTecnico
            or StatusRequisicao.NaoAutorizada or StatusRequisicao.DevolvidaAjuste or StatusRequisicao.Cancelada) return atual;
        var ativos = saldos.Where(s => s.Requisitado > 0).ToList();
        if (ativos.Count == 0) return atual;
        if (ativos.All(s => s.Atendido)) return ativos.Any(s => s.Cancelado > 0) ? StatusRequisicao.EncerradaComSaldo : StatusRequisicao.Atendida;
        if (ativos.Any(s => s.Recebido > 0)) return StatusRequisicao.ParcialmenteAtendida;
        if (ativos.Any(s => s.Pedido > 0 || s.EmCotacao > 0)) return StatusRequisicao.EmCotacao;
        return ativos.Any(s => s.Cancelado > 0) ? StatusRequisicao.ParcialmenteAtendida : StatusRequisicao.Autorizada;
    }

    public static SituacaoLote SituacaoLote(IReadOnlyCollection<SaldoItem> saldosDoLote)
    {
        var ativos = saldosDoLote.Where(s => s.Requisitado > 0).ToList();
        if (ativos.Count == 0 || ativos.All(s => s.Cancelado >= s.Requisitado)) return Suprimentos.SituacaoLote.Encerrado;
        if (ativos.All(s => s.Atendido)) return ativos.Any(s => s.Cancelado > 0) ? Suprimentos.SituacaoLote.Encerrado : Suprimentos.SituacaoLote.Atendido;
        if (ativos.All(s => s.APedir == 0)) return Suprimentos.SituacaoLote.EmRecebimento;
        return Suprimentos.SituacaoLote.EmCompra;
    }

    /// <summary>Encerrar saldo: cancela o que falta pedir do item, com justificativa (fica no histórico).</summary>
    public static decimal EncerrarSaldo(RequisicaoItem item, SaldoItem saldo, string? motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new Aprovacoes.RegraDeNegocioException("Encerrar saldo exige justificativa.");
        if (saldo.EmCotacao > 0) throw new Aprovacoes.RegraDeNegocioException($"{item.Descricao}: há quantidade em cotação aberta. Conclua ou cancele a cotação antes.");
        var falta = saldo.APedir;
        if (falta <= 0) throw new Aprovacoes.RegraDeNegocioException($"{item.Descricao}: não há saldo a pedir.");
        item.QtdCancelada += falta;
        item.MotivoCancelamento = string.IsNullOrWhiteSpace(item.MotivoCancelamento) ? motivo.Trim() : $"{item.MotivoCancelamento} | {motivo.Trim()}";
        return falta;
    }
}
