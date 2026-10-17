using Sigeo.Domain.Documentos;

namespace Sigeo.Application.Documentos;

public sealed record ListaMestraDados(Dictionary<Guid, string> Pastas, List<Documento> Documentos);
