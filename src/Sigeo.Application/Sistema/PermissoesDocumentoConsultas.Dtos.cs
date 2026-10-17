using Sigeo.Domain.Documentos;

namespace Sigeo.Application.Sistema;

public sealed record PermissoesDocumentoDados(List<Opcao> Pastas, List<Opcao> Documentos, List<PermissaoDocumento> Regras);
