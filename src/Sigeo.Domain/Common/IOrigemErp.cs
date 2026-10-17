namespace Sigeo.Domain.Common;

/// <summary>Registro originado no ERP — a chave externa é usada no upsert da importação.</summary>
public interface IOrigemErp
{
    string? CodigoErp { get; set; }
    Guid? UltimaImportacaoId { get; set; }
}
