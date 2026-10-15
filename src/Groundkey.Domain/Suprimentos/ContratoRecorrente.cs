using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Common;

namespace Groundkey.Domain.Suprimentos;

// ═══════════════════════ Contrato recorrente: parcelas (locação, serviço contínuo, manutenção) ═══════════════════════

/// <summary>Medição = medição com avaliação (como sempre foi). As demais geram parcelas automáticas.</summary>
public enum FormaCobranca { Medicao = 0, ValorFixo = 1, Diaria = 2, Uso = 3 }
public enum Periodicidade { Mensal = 1, Quinzenal = 2, Semanal = 3, DatasCombinadas = 4 }
public enum StatusParcela { Prevista = 1, Conferida = 2, Aprovada = 3, Liberada = 4, Paga = 5, Cancelada = 6 }

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

/// <summary>Uso lançado no período (contratos "por uso": caçamba, bombeamento…).</summary>
public class UsoContrato : EntidadeDaObra
{
    public Guid ContratoId { get; set; }
    public Guid ContratoItemId { get; set; }
    public DateOnly Data { get; set; }
    public decimal Quantidade { get; set; }
    public string? Documento { get; set; }
    public string? Observacao { get; set; }
}

/// <summary>Equipamento locado em campo (chegada → devolução) para o cálculo por diária.</summary>
public sealed record PeriodoLocacao(Guid ContratoItemId, string Descricao, decimal Quantidade, DateOnly Chegada, DateOnly? Devolucao);

public sealed record LinhaPrevisto(string Descricao, decimal Quantidade, string Base, decimal Unitario, decimal Total);

/// <summary>Regras da cobrança recorrente (funções puras, testáveis).</summary>
public static class RegrasRecorrencia
{
    /// <summary>Períodos de cobrança dentro da vigência.</summary>
    public static List<(DateOnly Inicio, DateOnly Fim)> Periodos(Contrato c)
    {
        var r = new List<(DateOnly, DateOnly)>();
        var ini = c.VigenciaInicio; var fimVig = c.VigenciaFim;
        if (fimVig < ini) return r;
        if (c.Periodicidade == Periodicidade.DatasCombinadas)
        {
            var datas = (c.DatasCombinadas ?? "").Split([';', ',', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => DateOnly.TryParseExact(s, "dd/MM/yyyy", out var d) || DateOnly.TryParse(s, out d) ? d : (DateOnly?)null)
                .Where(d => d is not null && d >= ini && d <= fimVig).Select(d => d!.Value).Distinct().Order().ToList();
            var a = ini;
            foreach (var d in datas) { r.Add((a, d)); a = d.AddDays(1); }
            return r;
        }
        var atual = ini;
        while (atual <= fimVig && r.Count < 240)
        {
            DateOnly fim;
            switch (c.Periodicidade)
            {
                case Periodicidade.Quinzenal: fim = atual.AddDays(14); break;
                case Periodicidade.Semanal: fim = atual.AddDays(6); break;
                default:
                    var dia = Math.Clamp(c.DiaCorte, 1, 31);
                    DateOnly Corte(int ano, int mes) => new(ano, mes, Math.Min(dia, DateTime.DaysInMonth(ano, mes)));
                    fim = Corte(atual.Year, atual.Month);
                    if (fim < atual) { var prox = atual.AddMonths(1); fim = Corte(prox.Year, prox.Month); }
                    break;
            }
            if (fim > fimVig) fim = fimVig;
            r.Add((atual, fim));
            atual = fim.AddDays(1);
        }
        return r;
    }

    /// <summary>Vencimento: dia combinado no mês do fim do período (ou no seguinte, se o dia já passou).</summary>
    public static DateOnly Vencimento(DateOnly fimPeriodo, int dia)
    {
        dia = Math.Clamp(dia, 1, 31);
        DateOnly Em(DateOnly m) => new(m.Year, m.Month, Math.Min(dia, DateTime.DaysInMonth(m.Year, m.Month)));
        var v = Em(fimPeriodo);
        return v > fimPeriodo ? v : Em(fimPeriodo.AddMonths(1));
    }

    /// <summary>Valor previsto do período e como foi calculado.</summary>
    public static List<LinhaPrevisto> Previsto(Contrato c, DateOnly inicio, DateOnly fim, IEnumerable<PeriodoLocacao> locacoes, IEnumerable<UsoContrato> usos)
    {
        var linhas = new List<LinhaPrevisto>();
        switch (c.FormaCobranca)
        {
            case FormaCobranca.ValorFixo:
                foreach (var i in c.Itens) linhas.Add(new(i.Descricao, i.Quantidade, $"{i.Unidade} / período", i.PrecoUnitario, i.Total));
                break;
            case FormaCobranca.Uso:
                foreach (var i in c.Itens)
                {
                    var q = usos.Where(u => u.ContratoItemId == i.Id && u.Data >= inicio && u.Data <= fim).Sum(u => u.Quantidade);
                    if (q > 0) linhas.Add(new(i.Descricao, q, i.Unidade, i.PrecoUnitario, Math.Round(q * i.PrecoUnitario, 2)));
                }
                break;
            case FormaCobranca.Diaria:
                foreach (var l in locacoes)
                {
                    var item = c.Itens.FirstOrDefault(i => i.Id == l.ContratoItemId);
                    if (item is null) continue;
                    var a = l.Chegada > inicio ? l.Chegada : inicio;
                    var b = l.Devolucao is { } d && d < fim ? d : fim;
                    if (b < a) continue;
                    var dias = b.DayNumber - a.DayNumber + 1;
                    var porDia = dias * item.PrecoUnitario;
                    // Diárias até o valor mensal: a partir daí cobra o mensal (teto por mês do calendário).
                    var porMes = item.PrecoMensal is { } pm ? pm * Math.Ceiling(dias / 31m) : decimal.MaxValue;
                    var usaMes = porMes < porDia;
                    var unit = usaMes ? porMes : porDia;
                    linhas.Add(new($"{l.Descricao} ({a:dd/MM} a {b:dd/MM}{(l.Devolucao is { } dv && dv <= fim ? " · devolvido" : "")})", l.Quantidade,
                        usaMes ? $"mensal ({dias} dias)" : $"{dias} diária(s)", unit, Math.Round(l.Quantidade * unit, 2)));
                }
                break;
        }
        return linhas;
    }

    public static string Texto(IEnumerable<LinhaPrevisto> linhas) =>
        string.Join('\n', linhas.Select(l => $"{l.Descricao} | {l.Quantidade:0.###} | {l.Base} | {l.Unitario:0.00} | {l.Total:0.00}"));
}
