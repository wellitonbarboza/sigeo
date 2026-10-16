using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>
/// T14 · Medição mensal + avaliação ponderada do prestador. Nasce no SIGEO (D8) e só depois vira lançamento no ERP.
/// Nota &lt; 9 abre plano de ação automaticamente (pontual; &lt; 7 completo + reunião).
/// </summary>
public class Medicao : EntidadeDaObra, IHistoricoTemporal
{
    public Guid ContratoId { get; set; }
    public Contrato Contrato { get; set; } = null!;
    public int NumeroMedicao { get; set; }
    public DateOnly PeriodoInicio { get; set; }
    public DateOnly PeriodoFim { get; set; }
    public decimal ValorPeriodo { get; set; }
    public decimal ValorAcumulado { get; set; }
    public decimal Saldo { get; set; }
    public decimal ValorRetencao { get; set; }
    public decimal? NotaEscopo { get; set; }
    /// <summary>Nulo = não se aplica (o peso é redistribuído entre os demais critérios).</summary>
    public decimal? NotaMaterial { get; set; }
    public decimal? NotaServico { get; set; }
    public decimal? NotaLimpeza { get; set; }
    public decimal? NotaDocSeg { get; set; }
    public decimal? NotaFinal { get; set; }
    public FaixaNota Faixa { get; set; }
    public DateOnly? DataReuniao { get; set; }
    public Guid? AvaliadorUsuarioId { get; set; }
    public string? AvaliadorNome { get; set; }
    public DateTimeOffset? AvaliadoEm { get; set; }
    public Guid? SolicitacaoAprovacaoId { get; set; }
    public string? AprovadoDiretorPor { get; set; }
    public DateTimeOffset? AprovadoDiretorEm { get; set; }
    public string? MotivoReprovacao { get; set; }
    public string? ChaveNFe { get; set; }
    public StatusMedicao Status { get; set; } = StatusMedicao.EmElaboracao;
    public string? Bloqueios { get; set; }
    /// <summary>Plano de ação aberto automaticamente pela nota (quando houver).</summary>
    public Guid? PlanoAcaoId { get; set; }

    public ICollection<MedicaoItem> Itens { get; set; } = [];

    /// <summary>Recalcula quantidades/valores a partir do contrato e das medições anteriores já aprovadas/pagas.</summary>
    public void Recalcular(IEnumerable<Medicao> anteriores)
    {
        var prev = anteriores.Where(m => m.Id != Id && m.NumeroMedicao < NumeroMedicao && m.Status is not StatusMedicao.Reprovada && !m.Excluido)
                             .SelectMany(m => m.Itens).ToList();
        foreach (var i in Itens)
        {
            var ci = Contrato.Itens.FirstOrDefault(c => c.Id == i.ContratoItemId)
                     ?? throw new Aprovacoes.RegraDeNegocioException("Item da medição não pertence ao contrato.");
            i.ContratoItem = ci;
            // Item contratado por local: acumulado e % são do local (cada linha da medição é um serviço × local).
            var cl = i.LocalId is { } lid ? ci.Locais.FirstOrDefault(x => x.LocalId == lid) : null;
            // Anterior = medições anteriores + linhas irmãs desta medição (mesmo item e local) que vêm antes: evita medir duas vezes na mesma medição.
            var irmas = Itens.TakeWhile(x => !ReferenceEquals(x, i)).Where(x => x.ContratoItemId == i.ContratoItemId && (cl is null || x.LocalId == i.LocalId)).Sum(x => x.QtdPeriodo);
            i.QtdAnterior = prev.Where(p => p.ContratoItemId == i.ContratoItemId && (cl is null || p.LocalId == i.LocalId)).Sum(p => p.QtdPeriodo) + irmas;
            i.QtdAcumulada = i.QtdAnterior + i.QtdPeriodo;
            var baseQtd = cl?.Quantidade ?? ci.Quantidade;
            i.PercExecutado = baseQtd == 0 ? 0 : Math.Round(100m * i.QtdAcumulada / baseQtd, 1);
            i.ValorPeriodo = Math.Round(i.QtdPeriodo * ci.PrecoUnitario, 2);
        }
        ValorPeriodo = Itens.Sum(i => i.ValorPeriodo);
        var valorAnterior = prev.Sum(p => p.ValorPeriodo);
        ValorAcumulado = valorAnterior + ValorPeriodo;
        Saldo = Contrato.ValorTotal - ValorAcumulado;
        ValorRetencao = Math.Round(ValorPeriodo * Contrato.RetencaoPerc / 100m, 2);
    }

    /// <summary>Σ nota × peso, com redistribuição do peso de "qualidade do material" quando não se aplica.</summary>
    public void CalcularNota(ParametrosVigentes p)
    {
        var criterios = new List<(decimal? Nota, decimal Peso)>
        {
            (NotaEscopo, p[Parametros.PesoEscopo]),
            (NotaMaterial, p[Parametros.PesoMaterial]),
            (NotaServico, p[Parametros.PesoServico]),
            (NotaLimpeza, p[Parametros.PesoLimpeza]),
            (NotaDocSeg, p[Parametros.PesoDocSeg]),
        };
        if (criterios.Any(c => c.Nota is < 0 or > 10)) throw new Aprovacoes.RegraDeNegocioException("As notas vão de 0 a 10.");
        var obrigatorios = new[] { NotaEscopo, NotaServico, NotaLimpeza, NotaDocSeg };
        if (obrigatorios.Any(n => n is null)) { NotaFinal = null; Faixa = FaixaNota.Nenhuma; return; }

        var validos = criterios.Where(c => c.Nota is not null).ToList();
        var somaPesos = validos.Sum(c => c.Peso);
        NotaFinal = Math.Round(validos.Sum(c => c.Nota!.Value * c.Peso) / somaPesos, 2);
        Faixa = NotaFinal < p[Parametros.NotaPlanoCompleto] ? FaixaNota.ReuniaoPlanoCompleto
              : NotaFinal < p[Parametros.NotaPlanoPontual] ? FaixaNota.PlanoPontual
              : FaixaNota.Nenhuma;
    }

    public static string DescricaoFaixa(FaixaNota f, ParametrosVigentes p) => f switch
    {
        FaixaNota.ReuniaoPlanoCompleto => $"0 – {p[Parametros.NotaPlanoCompleto] - 0.01m:0.00} · reunião + plano de ação completo",
        FaixaNota.PlanoPontual => $"{p[Parametros.NotaPlanoCompleto]:0.00} – {p[Parametros.NotaPlanoPontual] - 0.01m:0.00} · plano de ação pontual",
        _ => $"≥ {p[Parametros.NotaPlanoPontual]:0.00} · nenhuma ação"
    };
}

/// <summary>T15 · Item da medição (quantidade por item do contrato, com FVS de evidência).</summary>
public class MedicaoItem : Entidade
{
    public Guid MedicaoId { get; set; }
    public Medicao Medicao { get; set; } = null!;
    public Guid ContratoItemId { get; set; }
    public ContratoItem ContratoItem { get; set; } = null!;
    /// <summary>Local executado (torre/pavimento/unidade) — casa com a FVS do mesmo local.</summary>
    public Guid? LocalId { get; set; }
    public decimal QtdAnterior { get; set; }
    public decimal QtdPeriodo { get; set; }
    public decimal QtdAcumulada { get; set; }
    public decimal PercExecutado { get; set; }
    public decimal ValorPeriodo { get; set; }
    /// <summary>Número da FVS (texto) ou ficha vinculada.</summary>
    public string? FvsRef { get; set; }
    public Guid? FvsId { get; set; }
}

/// <summary>T16 · Plano de ação do fornecedor (aberto por nota de medição, FVM reprovada, avaliação semestral ou reclamação).</summary>
public class PlanoAcaoFornecedor : EntidadeAuditavel, IHistoricoTemporal
{
    /// <summary>Ex.: PA-0087</summary>
    public string Numero { get; set; } = string.Empty;
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public Guid? ObraId { get; set; }
    public OrigemPlanoAcao Origem { get; set; }
    public Guid? OrigemId { get; set; }
    public TipoPlanoAcao TipoPlano { get; set; }
    public string Problema { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public DateOnly Prazo { get; set; }
    public StatusPlanoAcao Status { get; set; } = StatusPlanoAcao.Aberta;
    public Eficacia Eficacia { get; set; }
    public string? EficaciaComentario { get; set; }

    public void Concluir(Eficacia eficacia, string? comentario)
    {
        if (eficacia == Eficacia.NaoVerificada) throw new Aprovacoes.RegraDeNegocioException("Registre a verificação de eficácia para concluir.");
        Eficacia = eficacia;
        EficaciaComentario = comentario;
        Status = StatusPlanoAcao.Concluida;
    }
}

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

/// <summary>T18 · Liberação de pagamento (3-way: pedido × recebimento/FVM × NF; serviço: medição aprovada).</summary>
public class LiberacaoPagamento : EntidadeAuditavel, IHistoricoTemporal
{
    public int Numero { get; set; }
    public Guid? ObraId { get; set; }
    public OrigemPagamento Origem { get; set; }
    public string ChaveNFe { get; set; } = string.Empty;
    public Guid FornecedorId { get; set; }
    public Guid? PedidoId { get; set; }
    public Guid? ContratoId { get; set; }
    public Guid? MedicaoId { get; set; }
    public decimal ValorNF { get; set; }
    public decimal ValorLiberado { get; set; }
    public string? JustificativaDiferenca { get; set; }
    public bool RecebimentoOk { get; set; }
    public string? EncaminhadoPor { get; set; }
    public DateTimeOffset? EncaminhadoEm { get; set; }
    public Guid? SolicitacaoAprovacaoId { get; set; }
    public string? LiberadoPor { get; set; }
    public DateTimeOffset? LiberadoEm { get; set; }
    public DateOnly Vencimento { get; set; }
    public StatusPagamento Status { get; set; } = StatusPagamento.Aguardando;
    public string? Bloqueios { get; set; }
}
