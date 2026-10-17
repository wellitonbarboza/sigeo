using Sigeo.Domain.Common;

namespace Sigeo.Domain.Aprovacoes;

/// <summary>
/// Delegação temporária (férias, afastamento): o substituto decide em nome do titular durante o período.
/// Fica registrada em cada etapa decidida por delegação.
/// </summary>
public class DelegacaoAprovacao : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid TitularUsuarioId { get; set; }
    public Guid SubstitutoUsuarioId { get; set; }
    public DateTimeOffset Inicio { get; set; }
    public DateTimeOffset Fim { get; set; }
    /// <summary>Nulo = todos os tipos.</summary>
    public EntidadeAprovavel? Entidade { get; set; }
    /// <summary>Nulo = todas as obras.</summary>
    public Guid? ObraId { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public bool Revogada { get; set; }
    public DateTimeOffset? RevogadaEm { get; set; }

    public static DelegacaoAprovacao Criar(Guid empresaId, Guid titularId, Guid substitutoId, DateTimeOffset inicio, DateTimeOffset fim,
        string motivo, EntidadeAprovavel? entidade = null, Guid? obraId = null)
    {
        if (titularId == substitutoId)
            throw new RegraDeNegocioException("Titular e substituto devem ser pessoas diferentes.");
        if (fim <= inicio)
            throw new RegraDeNegocioException("O fim da delegação deve ser posterior ao início.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new RegraDeNegocioException("Informe o motivo da delegação.");
        return new DelegacaoAprovacao
        {
            EmpresaId = empresaId, TitularUsuarioId = titularId, SubstitutoUsuarioId = substitutoId,
            Inicio = inicio, Fim = fim, Motivo = motivo, Entidade = entidade, ObraId = obraId
        };
    }

    public void Revogar(DateTimeOffset agora) { Revogada = true; RevogadaEm = agora; }

    public bool Vigente(DateTimeOffset agora, EntidadeAprovavel entidade, Guid? obraId) =>
        !Revogada && !Excluido
        && agora >= Inicio && agora < Fim
        && (Entidade is null || Entidade == entidade)
        && (ObraId is null || ObraId == obraId);
}
