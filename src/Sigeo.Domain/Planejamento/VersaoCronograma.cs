using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>
/// Versão do cronograma da obra (LB0 = linha de base, R1, R2… = reprogramações). O engenheiro elabora e envia;
/// o diretor técnico aprova (motor de alçadas). Aprovada vira Vigente e grava o desembolso mensal; a anterior fica Substituída.
/// </summary>
public class VersaoCronograma : EntidadeDaObra, IHistoricoTemporal
{
    public int Numero { get; set; }
    public string Codigo { get; set; } = "LB0";
    public StatusVersao Status { get; set; } = StatusVersao.EmElaboracao;
    public string? Motivo { get; set; }
    public string? Descricao { get; set; }
    public DateOnly InicioProjeto { get; set; }
    public DateOnly? DataStatus { get; set; }
    public DateOnly? TerminoPrevisto { get; set; }
    public DateOnly? TerminoBase { get; set; }
    public decimal CustoTotal { get; set; }
    public DateTimeOffset? EnviadoEm { get; set; }
    public string? EnviadoPor { get; set; }
    public string? AprovadoPor { get; set; }
    public DateTimeOffset? AprovadoEm { get; set; }
    public string? MotivoDevolucao { get; set; }
    public ICollection<AtividadeCronograma> Atividades { get; set; } = [];
    public ICollection<VinculoAtividade> Vinculos { get; set; } = [];
    public ICollection<DesembolsoVersao> Desembolso { get; set; } = [];

    public static string CodigoDe(int numero) => numero == 0 ? "LB0" : $"R{numero}";
    public bool Editavel => Status == StatusVersao.EmElaboracao;
}
