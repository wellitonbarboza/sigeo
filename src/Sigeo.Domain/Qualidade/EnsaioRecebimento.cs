using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

/// <summary>Q14 · Ensaio executado num lote recebido.</summary>
public class EnsaioRecebimento : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid RecebimentoId { get; set; }
    public Guid EnsaioModeloId { get; set; }
    public string Ensaio { get; set; } = string.Empty;
    public string Criterio { get; set; } = string.Empty;
    public AcaoReprovacaoEnsaio AcaoReprovacao { get; set; }
    public string Amostra { get; set; } = string.Empty;
    public DateOnly DataColeta { get; set; }
    public DateOnly? DataPrevista { get; set; }
    public Guid? LaboratorioId { get; set; }
    public string? Resultado { get; set; }
    public SituacaoEnsaio Situacao { get; set; } = SituacaoEnsaio.Aguardando;
    public Guid? RncId { get; set; }

    public bool Atrasado(DateOnly hoje) => Situacao == SituacaoEnsaio.Aguardando && DataPrevista < hoje;
}
