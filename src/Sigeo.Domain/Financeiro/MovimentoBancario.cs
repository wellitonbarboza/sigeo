using Sigeo.Domain.Common;

namespace Sigeo.Domain.Financeiro;

/// <summary>Movimento da conta bancária: baixa de parcela, transferência entre contas ou lançamento avulso (tarifa, rendimento).</summary>
public class MovimentoBancario : EntidadeAuditavel
{
    public Guid ContaBancariaId { get; set; }
    public DateOnly Data { get; set; }
    /// <summary>Entrada positiva, saída negativa.</summary>
    public decimal Valor { get; set; }
    public string Historico { get; set; } = string.Empty;
    public Guid? TituloParcelaId { get; set; }
    public Guid? TransferenciaId { get; set; }
    public Guid? ContaGerencialId { get; set; }
    public Guid? ObraId { get; set; }
    public bool Conciliado { get; set; }
}
