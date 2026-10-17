using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

/// <summary>Q15 · Onde o lote de material controlado foi aplicado (rastreabilidade).</summary>
public class AplicacaoLote : Entidade
{
    public Guid RecebimentoId { get; set; }
    public Guid LocalId { get; set; }
    public decimal Quantidade { get; set; }
    public DateOnly Data { get; set; }
    public Guid? FvsId { get; set; }
    public string RegistradoPor { get; set; } = string.Empty;
    public DateTimeOffset RegistradoEm { get; set; }
}
