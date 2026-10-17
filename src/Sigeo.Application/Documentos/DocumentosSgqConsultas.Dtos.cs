using Sigeo.Domain.Documentos;

namespace Sigeo.Application.Documentos;

public sealed record DocumentosSgqLista(List<Pasta> Pastas, List<Documento> Documentos, int ARecolher);

public sealed record DocumentoSgqAberto(Documento Documento, DocumentoRevisao? EmAndamento, PermissaoEfetiva Permissao, CheckoutDocumento? Checkout,
    List<Ciencia> Ciencias, List<EventoDocumento> Eventos);
