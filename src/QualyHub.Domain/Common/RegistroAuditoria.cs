namespace QualyHub.Domain.Common;

/// <summary>
/// Log de auditoria append-only (quem, quando, o quê, antes/depois). Gravado pelo AuditInterceptor.
/// Complementa as temporal tables: o log é legível para o auditor; a temporal table prova o estado em qualquer data.
/// </summary>
public class RegistroAuditoria
{
    public long Id { get; set; }
    public Guid? EmpresaId { get; set; }
    public DateTimeOffset Quando { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string? UsuarioId { get; set; }
    public string Entidade { get; set; } = string.Empty;
    public string EntidadeId { get; set; } = string.Empty;
    public string Operacao { get; set; } = string.Empty;   // Inclusao, Alteracao, Exclusao
    public string? ValoresAnterioresJson { get; set; }
    public string? ValoresNovosJson { get; set; }
    public string? CamposAlterados { get; set; }
    public string? Origem { get; set; }                     // Web, Campo, Importacao, Function
    public string? CorrelationId { get; set; }
}
