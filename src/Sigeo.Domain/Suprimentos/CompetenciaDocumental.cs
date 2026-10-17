using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>
/// Documentação mensal de um contrato de mão de obra (competência = mês trabalhado). O fornecedor envia na competência,
/// a construtora confere item a item e a medição daquele mês só é aprovada com a competência conferida.
/// </summary>
public class CompetenciaDocumental : EntidadeDaObra
{
    public Guid ContratoId { get; set; }
    public Guid FornecedorId { get; set; }
    /// <summary>1º dia do mês trabalhado.</summary>
    public DateOnly Mes { get; set; }
    public SituacaoCompetencia Situacao { get; set; } = SituacaoCompetencia.Aberta;
    public DateTimeOffset? EnviadaEm { get; set; }
    public string? EnviadaPor { get; set; }
    public DateTimeOffset? ConferidaEm { get; set; }
    public string? ConferidaPor { get; set; }
    public string? MotivoReabertura { get; set; }
    public ICollection<ItemCompetencia> Itens { get; set; } = [];

    /// <summary>Dia limite de entrega (padrão: dia 20 do mês seguinte ao trabalhado).</summary>
    public DateOnly Limite(int dia = 20) => Mes.AddMonths(1).AddDays(Math.Clamp(dia, 1, 28) - 1);
    public bool Atrasada(DateOnly hoje, int dia = 20) => Situacao != SituacaoCompetencia.Conferida && hoje > Limite(dia);
    public int Entregues => Itens.Count(i => i.Situacao is SituacaoItemCompetencia.Enviado or SituacaoItemCompetencia.Aprovado or SituacaoItemCompetencia.Adiado);
    /// <summary>Medição liberada: conferida (itens adiados com aprovação não travam).</summary>
    public bool LiberaMedicao => Situacao == SituacaoCompetencia.Conferida;

    /// <summary>Marca como completa (fornecedor): todo item enviado, ou adiado/adiamento pedido.</summary>
    public void Enviar(string quem, DateTimeOffset agora)
    {
        if (Situacao is SituacaoCompetencia.Conferida) throw new Aprovacoes.RegraDeNegocioException("Competência já conferida.");
        var faltam = Itens.Where(i => i.Situacao is SituacaoItemCompetencia.Pendente or SituacaoItemCompetencia.Recusado).ToList();
        if (faltam.Count > 0) throw new Aprovacoes.RegraDeNegocioException($"Faltam {faltam.Count} item(ns): {string.Join(", ", faltam.Take(5).Select(f => f.Descricao))}{(faltam.Count > 5 ? "…" : "")}.");
        Situacao = SituacaoCompetencia.Enviada; EnviadaEm = agora; EnviadaPor = quem;
    }

    /// <summary>Conclui a conferência: todos aprovados (ou adiados com aprovação do engenheiro).</summary>
    public void Concluir(string quem, DateTimeOffset agora)
    {
        var abertos = Itens.Where(i => i.Situacao is not (SituacaoItemCompetencia.Aprovado or SituacaoItemCompetencia.Adiado)).ToList();
        if (abertos.Count > 0) throw new Aprovacoes.RegraDeNegocioException($"Há {abertos.Count} item(ns) sem aprovação: {string.Join(", ", abertos.Take(5).Select(a => a.Descricao))}.");
        Situacao = SituacaoCompetencia.Conferida; ConferidaEm = agora; ConferidaPor = quem;
    }

    public void Devolver(string? motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo) && !Itens.Any(i => i.Situacao == SituacaoItemCompetencia.Recusado))
            throw new Aprovacoes.RegraDeNegocioException("Recuse os itens com o motivo ou informe o motivo da devolução.");
        Situacao = SituacaoCompetencia.ComPendencia;
        MotivoReabertura = motivo;
    }

    public void Reabrir(string? motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new Aprovacoes.RegraDeNegocioException("Reabrir competência conferida exige motivo.");
        Situacao = SituacaoCompetencia.ComPendencia; MotivoReabertura = motivo.Trim(); ConferidaEm = null; ConferidaPor = null;
    }
}
