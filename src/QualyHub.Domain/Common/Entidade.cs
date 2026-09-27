namespace QualyHub.Domain.Common;

/// <summary>Base de todas as entidades. Guid v7 (sequencial) evita fragmentação de índice no Azure SQL.</summary>
public abstract class Entidade
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
}

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

/// <summary>Entidade vinculada a uma obra (base do filtro de acesso por obra).</summary>
public abstract class EntidadeDaObra : EntidadeAuditavel, IPertenceObra
{
    public Guid ObraId { get; set; }
}

public interface IAuditavel
{
    DateTimeOffset CriadoEm { get; set; }
    string CriadoPor { get; set; }
    DateTimeOffset? AlteradoEm { get; set; }
    string? AlteradoPor { get; set; }
}

public interface IExclusaoLogica
{
    bool Excluido { get; set; }
    DateTimeOffset? ExcluidoEm { get; set; }
    string? ExcluidoPor { get; set; }
}

public interface IPertenceEmpresa { Guid EmpresaId { get; set; } }
public interface IPertenceObra { Guid ObraId { get; set; } }

/// <summary>Entidade com histórico completo em temporal table (evidência de rastreabilidade para a auditoria SiAC).</summary>
public interface IHistoricoTemporal { }

/// <summary>Registro originado no Obra Prima — a chave externa é usada no upsert da importação.</summary>
public interface IOrigemObraPrima
{
    string? CodigoObraPrima { get; set; }
    Guid? UltimaImportacaoId { get; set; }
}
