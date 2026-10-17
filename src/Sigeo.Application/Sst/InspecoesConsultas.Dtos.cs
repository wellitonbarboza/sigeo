using Sigeo.Domain.Sst;

namespace Sigeo.Application.Sst;

public sealed record InspecoesDados(List<InspecaoSeguranca> Itens, Dictionary<Guid, string> NomesLocais);
