using Sigeo.Domain.Fiscal;

namespace Sigeo.Application.Suprimentos;

/// <summary>Notas da lista, número do pedido de cada nota, indicadores da fila e últimas importações.</summary>
public sealed record NotasFiscaisLista(List<NotaFiscalEntrada> Notas, Dictionary<Guid, int> NumeroPedido, int SemPedido, int Vinculadas, decimal ValorFila,
    int Canceladas, List<ImportacaoNota> Importacoes);

/// <summary>Nota aberta com os pedidos em aberto do fornecedor e o saldo dos itens do pedido vinculado.</summary>
public sealed record NotaFiscalCarregada(NotaFiscalEntrada Nota, List<Opcao> PedidosFornecedor, List<ItemPedidoParaConferencia> ItensPedido);
