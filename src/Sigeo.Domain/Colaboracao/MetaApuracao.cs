using Sigeo.Domain.Common;

namespace Sigeo.Domain.Colaboracao;

/// <summary>Retrato da apuração (fechamento mensal congela o valor para comparar e avaliar).</summary>
public class MetaApuracao : Entidade
{
    public Guid MetaId { get; set; }
    public DateOnly Mes { get; set; }
    public decimal? Valor { get; set; }
    public decimal? PercAtingido { get; set; }
    public DateTimeOffset ApuradoEm { get; set; }
    public string? ApuradoPor { get; set; }
}
