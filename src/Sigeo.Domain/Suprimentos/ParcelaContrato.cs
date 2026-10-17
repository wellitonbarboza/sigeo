using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Parcela do período: valor previsto pelo contrato, nota lançada (almoxarife ou SIEG) e o fluxo até o pagamento.</summary>
public class ParcelaContrato : EntidadeDaObra, IHistoricoTemporal
{
    public Guid ContratoId { get; set; }
    public int Numero { get; set; }
    public DateOnly Inicio { get; set; }
    public DateOnly Fim { get; set; }
    public DateOnly Vencimento { get; set; }
    public decimal ValorPrevisto { get; set; }
    /// <summary>Como o previsto foi calculado (linhas "descrição | qtd | base | total").</summary>
    public string? Composicao { get; set; }
    public string? NotaNumero { get; set; }
    public decimal? NotaValor { get; set; }
    public Guid? NotaFiscalId { get; set; }
    public string? NotaOrigem { get; set; }
    public StatusParcela Status { get; set; } = StatusParcela.Prevista;
    public string? Justificativa { get; set; }
    public Guid? OrdemServicoId { get; set; }
    public string? ConferidoPor { get; set; }
    public DateTimeOffset? ConferidoEm { get; set; }
    public string? AprovadoPor { get; set; }
    public DateTimeOffset? AprovadoEm { get; set; }
    public string? LiberadoPor { get; set; }
    public DateTimeOffset? LiberadoEm { get; set; }
    public DateOnly? PagoEm { get; set; }
    public string? Observacao { get; set; }

    public decimal? Diferenca => NotaValor is { } n ? n - ValorPrevisto : null;
    public bool ForaDaTolerancia(decimal tolPerc) => Diferenca is { } d && Math.Abs(d) > Math.Round(ValorPrevisto * tolPerc / 100m, 2) + 0.01m;
    public bool Vencida(DateOnly hoje) => Status is StatusParcela.Prevista or StatusParcela.Conferida && Vencimento < hoje;

    public void LancarNota(string numero, decimal valor, string origem, Guid? notaFiscalId, string usuario, DateTimeOffset agora)
    {
        if (Status is not (StatusParcela.Prevista or StatusParcela.Conferida)) throw new RegraDeNegocioException("A parcela já foi aprovada: a nota não pode ser trocada.");
        if (valor <= 0) throw new RegraDeNegocioException("Informe o valor da nota.");
        if (string.IsNullOrWhiteSpace(numero)) throw new RegraDeNegocioException("Informe o número da nota.");
        NotaNumero = numero.Trim(); NotaValor = valor; NotaOrigem = origem; NotaFiscalId = notaFiscalId;
        Status = StatusParcela.Conferida; ConferidoPor = usuario; ConferidoEm = agora;
    }

    public void Aprovar(decimal tolPerc, bool exigeOs, string usuario, DateTimeOffset agora)
    {
        if (Status != StatusParcela.Conferida) throw new RegraDeNegocioException("Só parcela conferida (com nota) pode ser aprovada.");
        if (ForaDaTolerancia(tolPerc) && string.IsNullOrWhiteSpace(Justificativa))
            throw new RegraDeNegocioException($"Nota fora da tolerância do contrato ({tolPerc:0.##}%): informe a justificativa.");
        if (exigeOs && OrdemServicoId is null) throw new RegraDeNegocioException("Contrato de manutenção: registre a OS da visita do período antes de aprovar.");
        Status = StatusParcela.Aprovada; AprovadoPor = usuario; AprovadoEm = agora;
    }

    public void Liberar(string usuario, DateTimeOffset agora)
    {
        if (Status != StatusParcela.Aprovada) throw new RegraDeNegocioException("Só parcela aprovada pode ser liberada para pagamento.");
        Status = StatusParcela.Liberada; LiberadoPor = usuario; LiberadoEm = agora;
    }

    public void MarcarPaga(DateOnly data)
    {
        if (Status != StatusParcela.Liberada) throw new RegraDeNegocioException("Só parcela liberada pode ser marcada como paga.");
        Status = StatusParcela.Paga; PagoEm = data;
    }

    public void Devolver(string motivo)
    {
        if (Status is not (StatusParcela.Conferida or StatusParcela.Aprovada)) throw new RegraDeNegocioException("Só parcela conferida ou aprovada volta para conferência.");
        if (string.IsNullOrWhiteSpace(motivo)) throw new RegraDeNegocioException("Informe o motivo da devolução.");
        Status = StatusParcela.Prevista; Observacao = motivo.Trim(); AprovadoPor = null; AprovadoEm = null;
    }
}
