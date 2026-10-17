using Sigeo.Domain.Sst;

namespace Sigeo.Application.Sst;

public sealed record PermissoesTrabalhoDados(List<PermissaoTrabalho> Itens, Dictionary<Guid, string> NomesLocais);
