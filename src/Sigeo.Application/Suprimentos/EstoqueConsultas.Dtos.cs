using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Saldos do almoxarifado e disponível no central por produto.</summary>
public sealed record EstoqueSaldos(List<SaldoEstoque> Saldos, Dictionary<Guid, decimal> Central);

/// <summary>Movimentos do kardex e, quando pedido, o nome dos locais citados.</summary>
public sealed record EstoqueMovimentos(List<MovimentoEstoque> Movimentos, Dictionary<Guid, string>? NomesLocais);
