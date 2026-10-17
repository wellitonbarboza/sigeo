namespace Sigeo.Application.Abstracoes;

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
