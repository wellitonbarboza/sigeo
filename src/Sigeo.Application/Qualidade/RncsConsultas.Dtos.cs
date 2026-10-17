using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Qualidade;

public sealed record RncsLista(List<Rnc> Itens, Dictionary<Guid, string> Acoes, Dictionary<Guid, string> Locais);
