using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Recebimentos da lista (com pedido e itens) e o nome de exibição dos fornecedores.</summary>
public sealed record RecebimentosLista(List<Recebimento> Recebimentos, Dictionary<Guid, string> Fornecedores);

/// <summary>Recebimento aberto: marca pedida por item do pedido, número do pedido (0 sem pedido) e transferência gerada.</summary>
public sealed record RecebimentoCarregado(Recebimento Recebimento, Dictionary<Guid, string?> MarcaPedido, int NumeroPedido, Guid? Transferencia);
