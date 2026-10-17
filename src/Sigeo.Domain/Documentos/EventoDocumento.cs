using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

/// <summary>D09 · Log de eventos do controle de documentos (permanente, append-only).</summary>
public class EventoDocumento : Entidade, IPertenceEmpresa
{
    public Guid EmpresaId { get; set; }
    public DateTimeOffset DataHora { get; set; }
    public Guid? UsuarioId { get; set; }
    public string UsuarioNome { get; set; } = string.Empty;
    public EventoDocumentoTipo Evento { get; set; }
    public Guid? DocumentoId { get; set; }
    public Guid? RevisaoId { get; set; }
    public Guid? CopiaId { get; set; }
    public string? Referencia { get; set; }
    public string? Detalhe { get; set; }
    public string? Origem { get; set; }
}
