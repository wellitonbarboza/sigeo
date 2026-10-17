using Sigeo.Domain.Planejamento;

namespace Sigeo.Application.Planejamento;

public sealed record PlanejamentoImpressaoDados(VersaoCronograma Versao, string Obra, Calendario Calendario);
