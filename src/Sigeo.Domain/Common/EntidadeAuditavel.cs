namespace Sigeo.Domain.Common;

/// <summary>
/// Entidade com trilha de auditoria. Os campos de auditoria são preenchidos pelo AuditInterceptor,
/// nunca pela aplicação — assim ninguém "esquece" ou forja o registro.
/// </summary>
public abstract class EntidadeAuditavel : Entidade, IAuditavel, IExclusaoLogica, IPertenceEmpresa
{
    public Guid EmpresaId { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public string CriadoPor { get; set; } = string.Empty;
    public DateTimeOffset? AlteradoEm { get; set; }
    public string? AlteradoPor { get; set; }
    public bool Excluido { get; set; }
    public DateTimeOffset? ExcluidoEm { get; set; }
    public string? ExcluidoPor { get; set; }

    /// <summary>Concorrência otimista — essencial para a sincronização do app de campo (offline).</summary>
    public byte[] RowVersion { get; set; } = [];
}
