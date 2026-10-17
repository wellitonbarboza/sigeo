using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Saldo do produto no almoxarifado (atualizado junto com cada movimento) + mínimo/ponto de pedido/máximo.</summary>
public class SaldoEstoque : EntidadeAuditavel
{
    public Guid AlmoxarifadoId { get; set; }
    public Guid ProdutoId { get; set; }
    public decimal Disponivel { get; set; }
    /// <summary>Material em quarentena até o ensaio (não pode sair).</summary>
    public decimal Bloqueado { get; set; }
    /// <summary>Custo médio ponderado do almoxarifado.</summary>
    public decimal CustoMedio { get; set; }
    public decimal Minimo { get; set; }
    public decimal PontoPedido { get; set; }
    public decimal Maximo { get; set; }
    public DateTimeOffset? UltimoMovimento { get; set; }

    public decimal Valor => Math.Round((Disponivel + Bloqueado) * CustoMedio, 2);
    public bool AbaixoDoMinimo => Minimo > 0 && Disponivel <= Minimo;
    public bool NoPontoDePedido => PontoPedido > 0 && Disponivel <= PontoPedido;
}
