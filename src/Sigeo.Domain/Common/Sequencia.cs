namespace Sigeo.Domain.Common;

/// <summary>Numeração sequencial por empresa e chave (ex.: "Requisicao", "Contrato-2026", "Copia-P.SUP.01-R01").</summary>
public class Sequencia : Entidade, IPertenceEmpresa
{
    public Guid EmpresaId { get; set; }
    public string Chave { get; set; } = string.Empty;
    public int Ultimo { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public int Proximo() => ++Ultimo;
}
