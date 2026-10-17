using Sigeo.Domain.Common;

namespace Sigeo.Domain.Aprovacoes;

/// <summary>
/// Limite de aprovação cadastrado no login: "Fulano aprova pedidos até R$ X" (opcionalmente só numa obra).
/// Sem limite cadastrado, valem só as alçadas.
/// </summary>
public class LimiteAprovacao : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid UsuarioId { get; set; }
    public EntidadeAprovavel Entidade { get; set; }
    /// <summary>Nulo = vale para todas as obras. Limite da obra prevalece sobre o geral.</summary>
    public Guid? ObraId { get; set; }
    public decimal ValorMaximo { get; set; }
    public bool Ativo { get; set; } = true;
}
