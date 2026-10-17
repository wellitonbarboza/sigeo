using Sigeo.Domain.Planejamento;

namespace Sigeo.Application.Planejamento;

public sealed record PlanejamentoTelaObraDados(List<VersaoCronograma> Versoes, Calendario Calendario);
