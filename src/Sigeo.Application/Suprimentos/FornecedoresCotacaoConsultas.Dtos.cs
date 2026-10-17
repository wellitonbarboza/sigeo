using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Cotação com fornecedores e itens e o nome dos fornecedores convidados.</summary>
public sealed record CotacaoComNomes(Cotacao Cotacao, Dictionary<Guid, string> Nomes);
