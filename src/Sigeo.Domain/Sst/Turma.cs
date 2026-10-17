using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

public class Turma : EntidadeAuditavel, IHistoricoTemporal
{
    public string Codigo { get; set; } = string.Empty;
    public Guid TreinamentoId { get; set; }
    public Guid? ObraId { get; set; }
    public string? Origem { get; set; }
    public DateTimeOffset Data { get; set; }
    public string Instrutor { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;
    public StatusTurma Status { get; set; } = StatusTurma.Programada;
    public ICollection<Participante> Participantes { get; set; } = [];

    /// <summary>Conclui: presença + avaliação viram registro de competência com validade.</summary>
    public void Concluir(Treinamento t)
    {
        if (Status != StatusTurma.Programada) throw new RegraDeNegocioException("Turma já concluída ou cancelada.");
        if (Participantes.Count == 0) throw new RegraDeNegocioException("Inclua os participantes.");
        foreach (var p in Participantes)
        {
            if (!p.Presenca) { p.Resultado = ResultadoParticipante.NaoCompareceu; p.ValidoAte = null; continue; }
            if (t.MetodoEficacia != MetodoEficaciaTreinamento.NaoSeAplica && string.IsNullOrWhiteSpace(p.Avaliacao))
                throw new RegraDeNegocioException($"Informe a avaliação de eficácia de todos os presentes (SiAC 7.2).");
            p.Resultado ??= ResultadoParticipante.Aprovado;
            var data = DateOnly.FromDateTime(Data.Date);
            p.ValidoAte = p.Resultado == ResultadoParticipante.Aprovado ? (t.ValidadeMeses is { } m ? data.AddMonths(m) : DateOnly.MaxValue) : null;
        }
        Status = StatusTurma.Concluida;
    }
}
