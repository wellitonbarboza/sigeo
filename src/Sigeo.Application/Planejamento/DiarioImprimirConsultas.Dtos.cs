using Sigeo.Domain.Planejamento;

namespace Sigeo.Application.Planejamento;

/// <summary>Diário para impressão; <see cref="Fotos"/> traz os anexos de foto por atividade, na ordem de envio.</summary>
public sealed record DiarioImpressaoDados(DiarioObra Diario, string Obra, Dictionary<Guid, string> Locais,
    Dictionary<Guid, string> Equipamentos, Dictionary<Guid, List<Guid>> Fotos);
