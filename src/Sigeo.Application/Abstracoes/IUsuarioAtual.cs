namespace Sigeo.Application.Abstracoes;

/// <summary>
/// Contexto do usuário da requisição. Na Web vem das claims do Entra ID (oid, name, grupos);
/// no Azure Functions/importação vem de uma identidade de sistema.
/// </summary>
public interface IUsuarioAtual
{
    Guid EmpresaId { get; }
    Guid? UsuarioId { get; }
    string Nome { get; }
    /// <summary>Diretoria/Qualidade corporativa enxergam todas as obras.</summary>
    bool AcessoTodasObras { get; }
    IReadOnlyCollection<Guid> ObrasPermitidas { get; }
    /// <summary>Web, Campo, Importacao, Function.</summary>
    string Origem { get; }
    string? CorrelationId { get; }
}
