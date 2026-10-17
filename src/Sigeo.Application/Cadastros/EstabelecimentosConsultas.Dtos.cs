using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Cadastros;

/// <summary>Estabelecimentos e fluxos entre eles.</summary>
public sealed record EstabelecimentosDados(List<Estabelecimento> Estabelecimentos, List<FluxoEstabelecimento> Fluxos);
