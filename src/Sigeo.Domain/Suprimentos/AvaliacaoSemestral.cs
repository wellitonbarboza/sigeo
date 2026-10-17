using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>T17 · Avaliação semestral consolidada (FVM + medições + planos). Decide manter ou desqualificar.</summary>
public class AvaliacaoSemestral : EntidadeAuditavel, IHistoricoTemporal
{
    /// <summary>Ex.: 2026-S2</summary>
    public string Periodo { get; set; } = string.Empty;
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public int QtdFvm { get; set; }
    public decimal? PercFvmAprovadas { get; set; }
    public int QtdMedicoes { get; set; }
    public decimal? NotaMedia { get; set; }
    public int PlanosAbertos { get; set; }
    public int QtdRnc { get; set; }
    public DecisaoSemestral? Decisao { get; set; }
    public string? Participantes { get; set; }
    public DateOnly? DataReuniao { get; set; }
    public DateOnly? DataComunicado { get; set; }

    /// <summary>IDF médio das entregas avaliadas por nota no período (0,4 prazo + 0,4 qualidade + 0,2 documentação, pesos em parâmetros).</summary>
    public decimal? IdfMedio { get; set; }
    public int QtdEntregasIdf { get; set; }

    /// <summary>Período semestral ("2026-S2") ou trimestral ("2026-T4"), conforme a periodicidade da avaliação (meses).</summary>
    public static string PeriodoDe(DateOnly d, int meses = 6) => meses <= 3
        ? $"{d.Year}-T{(d.Month - 1) / 3 + 1}"
        : $"{d.Year}-S{(d.Month <= 6 ? 1 : 2)}";

    public static (DateOnly Inicio, DateOnly Fim) Intervalo(string periodo)
    {
        var ano = int.Parse(periodo[..4]);
        if (periodo.Length >= 7 && periodo[5] == 'T' && int.TryParse(periodo[6..], out var t) && t is >= 1 and <= 4)
        {
            var ini = new DateOnly(ano, (t - 1) * 3 + 1, 1);
            return (ini, ini.AddMonths(3).AddDays(-1));
        }
        return periodo.EndsWith("S1") ? (new DateOnly(ano, 1, 1), new DateOnly(ano, 6, 30)) : (new DateOnly(ano, 7, 1), new DateOnly(ano, 12, 31));
    }

    /// <summary>Classificação pelo IDF: ≥ preferencial → Preferencial; ≥ mínimo → Sob acompanhamento; abaixo → Desqualificado.</summary>
    public static string? ClassificarIdf(decimal? idf, decimal preferencial, decimal minimo) => idf switch
    {
        null => null,
        var v when v >= preferencial => "Preferencial",
        var v when v >= minimo => "Sob acompanhamento",
        _ => "Desqualificado"
    };

    /// <summary>Consolida os indicadores do fornecedor no semestre.</summary>
    public void Consolidar(IEnumerable<Recebimento> recebimentos, IEnumerable<Medicao> medicoes, IEnumerable<PlanoAcaoFornecedor> planos)
    {
        var (ini, fim) = Intervalo(Periodo);
        var fvm = recebimentos.Where(r => r.FornecedorId == FornecedorId && !r.Excluido && r.FvmAprovada is not null
                                          && DateOnly.FromDateTime(r.DataRecebimento.Date) >= ini && DateOnly.FromDateTime(r.DataRecebimento.Date) <= fim).ToList();
        QtdFvm = fvm.Count;
        PercFvmAprovadas = QtdFvm == 0 ? null : Math.Round(100m * fvm.Count(r => r.FvmAprovada == true) / QtdFvm, 1);
        var idf = recebimentos.Where(r => r.FornecedorId == FornecedorId && !r.Excluido && r.Idf is not null
                                          && DateOnly.FromDateTime(r.DataRecebimento.Date) >= ini && DateOnly.FromDateTime(r.DataRecebimento.Date) <= fim)
            .Select(r => r.Idf!.Value).ToList();
        QtdEntregasIdf = idf.Count;
        IdfMedio = idf.Count == 0 ? null : Math.Round(idf.Average(), 2);
        var med = medicoes.Where(m => m.Contrato.FornecedorId == FornecedorId && !m.Excluido && m.NotaFinal is not null
                                      && m.PeriodoFim >= ini && m.PeriodoFim <= fim).ToList();
        QtdMedicoes = med.Count;
        NotaMedia = QtdMedicoes == 0 ? null : Math.Round(med.Average(m => m.NotaFinal!.Value), 2);
        PlanosAbertos = planos.Count(p => p.FornecedorId == FornecedorId && !p.Excluido && p.Status != StatusPlanoAcao.Concluida);
    }

    public IReadOnlyList<string> ValidarDecisao()
    {
        var e = new List<string>();
        if (Decisao is null) e.Add("Registre a decisão.");
        if (string.IsNullOrWhiteSpace(Participantes)) e.Add("Informe os participantes da reunião.");
        if (DataReuniao is null) e.Add("Informe a data da reunião.");
        if (Decisao == DecisaoSemestral.Desqualificar && DataComunicado is null) e.Add("Desqualificação: informe a data do comunicado enviado.");
        return e;
    }
}
