using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>
/// Mapa comparativo: cotação (recalculada e com preços de referência), nome da obra e dos fornecedores,
/// quebra por local e a marca pedida em cada item da requisição.
/// </summary>
public sealed record MapaCotacaoDados(Cotacao Cotacao, string Obra, Dictionary<Guid, string> Nomes,
    Dictionary<Guid, List<LocalQuantidade>> LocaisItem, Dictionary<Guid, (string? Marca, bool Mesma)> Pedidas);
