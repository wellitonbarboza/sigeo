using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>T27 · Regra de notificação por evento (quem recebe, quando, por qual canal).</summary>
public class RegraNotificacao : EntidadeAuditavel
{
    public string Evento { get; set; } = string.Empty;
    public string? Antecedencia { get; set; }
    public string? Repeticao { get; set; }
    public string Destinatarios { get; set; } = string.Empty;
    public CanalNotificacao Canal { get; set; } = CanalNotificacao.Pendencias;
    public string ModeloMensagem { get; set; } = string.Empty;
    public bool Ativa { get; set; } = true;
}
