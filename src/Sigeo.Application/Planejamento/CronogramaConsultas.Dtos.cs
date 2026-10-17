using Sigeo.Domain.Planejamento;

namespace Sigeo.Application.Planejamento;

public sealed record CronogramaObraDados(List<VersaoCronograma> Versoes, Calendario Calendario, List<Feriado> Feriados);

public sealed record CronogramaVersaoDados(VersaoCronograma Versao, List<RedePlanejamento> Redes);
