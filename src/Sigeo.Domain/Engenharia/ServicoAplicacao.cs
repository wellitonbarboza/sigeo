using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Engenharia;

/// <summary>
/// Serviço marcado na tipologia: em que ambiente (cômodo/área comum) ou nível (apartamento, pavimento, torre, condomínio) ele entra.
/// O orçamento explode isso para todos os locais gerados.
/// </summary>
public class ServicoAplicacao : EntidadeAuditavel, IPertenceObra
{
    public Guid ObraId { get; set; }
    public Guid TipologiaId { get; set; }
    public Guid ComposicaoId { get; set; }
    public AlvoServico Alvo { get; set; } = AlvoServico.Ambiente;
    /// <summary>Alvo = Ambiente: o ambiente-modelo da tipologia.</summary>
    public Guid? AmbienteModeloId { get; set; }
    /// <summary>Regra "Fixa": quantidade por local.</summary>
    public decimal? QuantidadeFixa { get; set; }
}
