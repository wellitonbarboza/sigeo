using System.Globalization;
using System.Text;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Fiscal;

/// <summary>Item do pedido candidato ao vínculo com o item da nota.</summary>
public sealed record ItemPedidoParaConferencia(Guid Id, string Descricao, string? Unidade, decimal Saldo, string? Codigo);
