using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Quantidades de um item da requisição ao longo do atendimento (calculadas, nunca gravadas).</summary>
public sealed record SaldoItem(Guid ItemId, decimal Requisitado, decimal EmCotacao, decimal Pedido, decimal Recebido, decimal Cancelado)
{
    public decimal APedir => Math.Max(0, Requisitado - Pedido - Cancelado);
    public decimal AReceber => Math.Max(0, Pedido - Recebido);
    /// <summary>O que ainda pode entrar numa nova cotação (desconta o que já está em cotação aberta).</summary>
    public decimal DisponivelCotacao => Math.Max(0, APedir - EmCotacao);
    public bool Atendido => Requisitado > 0 && Recebido + Cancelado >= Requisitado;
    public string Situacao => Cancelado >= Requisitado && Requisitado > 0 ? "Cancelado"
        : Atendido ? (Cancelado > 0 ? "Encerrado com saldo" : "Atendido")
        : Pedido > 0 && APedir == 0 ? "A receber"
        : Pedido > 0 || Recebido > 0 ? "Parcial"
        : EmCotacao > 0 ? "Em cotação" : "A pedir";
}
