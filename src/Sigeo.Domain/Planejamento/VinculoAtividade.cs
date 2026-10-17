using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>Vínculo entre duas atividades-folha da mesma versão, com folga (lag) em dias úteis (negativa = antecipação).</summary>
public class VinculoAtividade : Entidade
{
    public Guid VersaoId { get; set; }
    public Guid PredecessoraId { get; set; }
    public Guid SucessoraId { get; set; }
    public TipoVinculo Tipo { get; set; } = TipoVinculo.TerminoInicio;
    public int Lag { get; set; }
}
