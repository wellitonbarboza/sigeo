using Sigeo.Domain.Common;
using Sigeo.Domain.Documentos;

namespace Sigeo.Application.Sistema;

public sealed record LogAuditoriaPagina(int Total, List<RegistroAuditoria> Itens);

public sealed record LogEventosPagina(int Total, List<EventoDocumento> Eventos, Dictionary<Guid, string> CodigosDocumentos);
