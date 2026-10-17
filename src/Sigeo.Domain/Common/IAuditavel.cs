namespace Sigeo.Domain.Common;

public interface IAuditavel
{
    DateTimeOffset CriadoEm { get; set; }
    string CriadoPor { get; set; }
    DateTimeOffset? AlteradoEm { get; set; }
    string? AlteradoPor { get; set; }
}
