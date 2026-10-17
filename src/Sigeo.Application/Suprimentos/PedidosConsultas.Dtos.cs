using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Pedidos da lista (com itens e fornecedor) e o nome das obras.</summary>
public sealed record PedidosLista(List<Pedido> Pedidos, Dictionary<Guid, string> Obras);

/// <summary>Pedido aberto, quebra por local dos itens e o fornecedor com qualificações e documentos (para as travas de qualidade).</summary>
public sealed record PedidoCarregado(Pedido Pedido, Dictionary<Guid, List<LocalQuantidade>> LocaisItem, Fornecedor Fornecedor);
