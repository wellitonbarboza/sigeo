using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

/// <summary>D03 · Etapa de aprovação da revisão (com assinatura eletrônica).</summary>
public class EtapaRevisao : Entidade
{
    public Guid DocumentoRevisaoId { get; set; }
    public DocumentoRevisao Revisao { get; set; } = null!;
    public EtapaRevisaoTipo Etapa { get; set; }
    public Guid? ResponsavelUsuarioId { get; set; }
    public string? ResponsavelNome { get; set; }
    public DateOnly? Prazo { get; set; }
    public DecisaoEtapa Decisao { get; set; } = DecisaoEtapa.Pendente;
    public string? Comentario { get; set; }
    public DateTimeOffset? DataDecisao { get; set; }
    public string? Assinatura { get; set; }
    /// <summary>Registro de uma decisão anterior (devolução) — mantido para a trilha.</summary>
    public bool Historico { get; set; }
}
