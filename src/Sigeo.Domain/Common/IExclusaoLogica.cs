namespace Sigeo.Domain.Common;

public interface IExclusaoLogica
{
    bool Excluido { get; set; }
    DateTimeOffset? ExcluidoEm { get; set; }
    string? ExcluidoPor { get; set; }
}
