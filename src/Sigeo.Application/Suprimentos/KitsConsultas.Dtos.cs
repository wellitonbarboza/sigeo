using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Kits visíveis na obra e etapas disponíveis.</summary>
public sealed record KitsLista(List<KitMaterial> Kits, List<string> Etapas);

/// <summary>Saldos para a separação do kit e locais já entregues.</summary>
public sealed record KitSaldos(Dictionary<Guid, decimal> Saldos, Dictionary<Guid, decimal> Central, HashSet<Guid> Entregues);
