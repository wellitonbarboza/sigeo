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
