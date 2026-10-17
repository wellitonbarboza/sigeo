using Sigeo.Domain.Common;
using Sigeo.Domain.Documentos;

namespace Sigeo.Application.Documentos;

/// <summary>Linha da grade de cópias controladas.</summary>
public sealed record CopiaLinha(Guid Id, string Numero, string Documento, string Rev, TipoCopia Tipo, string Destinatario, DateTimeOffset? Entregue, DateOnly? Prazo, StatusCopia Status, string Qr);

public sealed record CopiasDados(List<CopiaLinha> Copias, List<SolicitacaoCopia> Solicitacoes, List<Opcao> Vigentes);
