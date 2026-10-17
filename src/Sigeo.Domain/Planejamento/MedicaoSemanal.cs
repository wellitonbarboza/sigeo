using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>
/// Medição semanal: percentual executado acumulado de um serviço (atividade do cronograma) num local.
/// Uma por obra × atividade × semana — medir de novo na mesma semana sobrepõe (a anterior fica no histórico temporal).
/// Quem mede é a Engenharia de obra; 100% é aceito com FVS aberta/não concluída, guardando o aviso.
/// </summary>
public class MedicaoSemanal : EntidadeDaObra, IHistoricoTemporal
{
    /// <summary>Chave estável da atividade (a mesma do gerador de redes), vale para todas as versões do cronograma.</summary>
    public string AtividadeChave { get; set; } = string.Empty;
    public Guid LocalId { get; set; }
    public Guid? PacoteId { get; set; }
    public Guid? ComposicaoId { get; set; }
    public string Servico { get; set; } = string.Empty;
    /// <summary>Segunda-feira da semana medida.</summary>
    public DateOnly Semana { get; set; }
    /// <summary>Data considerada como realização da medição (escolhida por quem mede).</summary>
    public DateOnly DataMedicao { get; set; }
    public decimal Percentual { get; set; }
    public decimal? PercentualAnterior { get; set; }
    /// <summary>Medido 100% com FVS do serviço aberta ou não concluída no local.</summary>
    public bool FvsPendente { get; set; }
    public string? Observacao { get; set; }
    public string MedidoPor { get; set; } = string.Empty;

    public static DateOnly SegundaDe(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
}
