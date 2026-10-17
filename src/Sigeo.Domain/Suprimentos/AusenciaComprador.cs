using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

public class AusenciaComprador : Entidade
{
    public Guid CarteiraId { get; set; }
    public DateOnly Inicio { get; set; }
    public DateOnly Fim { get; set; }
    public string? Motivo { get; set; }
    /// <summary>Na ausência, os lotes ainda não iniciados também vão para o substituto (senão só os novos).</summary>
    public bool TransferirAbertos { get; set; } = true;
}
