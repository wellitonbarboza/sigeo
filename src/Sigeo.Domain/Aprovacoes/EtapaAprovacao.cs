using Sigeo.Domain.Common;

namespace Sigeo.Domain.Aprovacoes;

public class EtapaAprovacao : Entidade
{
    public Guid SolicitacaoAprovacaoId { get; set; }
    public SolicitacaoAprovacao Solicitacao { get; set; } = null!;
    public int Ordem { get; set; }
    public PerfilUsuario PerfilAprovador { get; set; }
    public Guid? UsuarioAprovadorId { get; set; }
    public DecisaoAprovacao Decisao { get; set; } = DecisaoAprovacao.Pendente;
    public Guid? DecididoPorUsuarioId { get; set; }
    public string? DecididoPorNome { get; set; }
    public DateTimeOffset? DecididoEm { get; set; }
    public string? Justificativa { get; set; }
    /// <summary>Preenchido quando o substituto decide em nome do titular (evidência da delegação).</summary>
    public Guid? EmNomeDeUsuarioId { get; set; }
    public string? EmNomeDeNome { get; set; }
    public Guid? DelegacaoAprovacaoId { get; set; }

    /// <summary>Aprovador nominal prevalece sobre o perfil.</summary>
    public bool Autoriza(Aprovador a) => UsuarioAprovadorId is { } uid ? uid == a.UsuarioId : PerfilAprovador == a.Perfil;
}
