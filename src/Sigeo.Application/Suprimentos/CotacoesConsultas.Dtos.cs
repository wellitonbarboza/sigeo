using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Linha da lista de cotações.</summary>
public sealed record CotacaoLinha(Guid Id, int Numero, string Nome, string Obra, TipoRequisicao Tipo, int Recebidas, int Exigidas, decimal Valor, string? Vencedor, StatusCotacao Status, string CriadoPor, DateTimeOffset CriadoEm);

/// <summary>Cotação aberta (recalculada e com preços de referência), nome dos fornecedores e quebra por local de cada item da requisição.</summary>
public sealed record CotacaoCarregada(Cotacao Cotacao, Dictionary<Guid, string> Nomes, Dictionary<Guid, List<LocalQuantidade>> LocaisItem);
