using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Linha da grade de liberações de pagamento.</summary>
public sealed record PagamentoLinha(Guid Id, int Numero, OrigemPagamento Origem, string Fornecedor, string Nf, decimal ValorNf, decimal Valor, DateOnly Vencimento, StatusPagamento Status, string CriadoPor);

/// <summary>Opções de origem para uma nova liberação.</summary>
public sealed record PagamentoOpcoes(List<Opcao> Pedidos, List<Opcao> Medicoes);

/// <summary>Pedido com os recebimentos dele.</summary>
public sealed record PagamentoPedido(Pedido Pedido, List<Recebimento> Recebimentos);

/// <summary>Liberação aberta com o que ela referencia.</summary>
public sealed record PagamentoAberto(LiberacaoPagamento Liberacao, Pedido? Pedido, List<Recebimento> Recebimentos, Medicao? Medicao);
