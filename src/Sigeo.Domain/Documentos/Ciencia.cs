using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

/// <summary>D06 · Leitura obrigatória / ciência — evidência de comunicação da mudança.</summary>
public class Ciencia : EntidadeAuditavel
{
    public Guid DocumentoRevisaoId { get; set; }
    public DocumentoRevisao Revisao { get; set; } = null!;
    public Guid UsuarioId { get; set; }
    public string UsuarioNome { get; set; } = string.Empty;
    public string? Funcao { get; set; }
    public DateOnly Prazo { get; set; }
    public DateTimeOffset? LidoEm { get; set; }
    public bool ExigeTreinamento { get; set; }

    public StatusCiencia Status(DateOnly hoje) => LidoEm is not null ? StatusCiencia.Lido : hoje > Prazo ? StatusCiencia.Atrasado : StatusCiencia.Pendente;
}
