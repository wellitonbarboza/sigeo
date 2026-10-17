using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Cadastros;

/// <summary>Centros de custo e valor comprometido por centro.</summary>
public sealed record CentrosCustoDados(List<CentroCusto> CentrosCusto, Dictionary<Guid, decimal> Comprometido);
