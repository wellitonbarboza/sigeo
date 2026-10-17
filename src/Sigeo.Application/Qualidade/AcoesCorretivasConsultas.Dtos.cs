using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Qualidade;

public sealed record AcaoCorretivaAberta(AcaoCorretiva Acao, List<Rnc> Rncs);
