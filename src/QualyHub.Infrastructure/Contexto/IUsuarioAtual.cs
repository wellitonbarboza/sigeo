namespace QualyHub.Infrastructure.Contexto;

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

/// <summary>Identidade de sistema para rotinas (alertas, migrações, seed).</summary>
public sealed class UsuarioSistema(Guid empresaId, string origem = "Function") : IUsuarioAtual
{
    public Guid EmpresaId { get; } = empresaId;
    public Guid? UsuarioId => null;
    public string Nome => "sistema";
    public bool AcessoTodasObras => true;
    public IReadOnlyCollection<Guid> ObrasPermitidas { get; } = [];
    public string Origem { get; } = origem;
    public string? CorrelationId => null;
}
