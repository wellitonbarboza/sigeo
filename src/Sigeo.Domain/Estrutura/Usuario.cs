using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>Usuário do sistema (login e senha próprios do SIGEO).</summary>
public class Usuario : EntidadeAuditavel
{
    /// <summary>Login de acesso (minúsculo, único no sistema). A senha fica em <see cref="CredencialUsuario"/>.</summary>
    public string Login { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; }
    /// <summary>Função exibida e usada em distribuição/leitura obrigatória (ex.: Engenheiro da Obra, Comprador).</summary>
    public string? Funcao { get; set; }
    public bool AcessoTodasObras { get; set; }
    /// <summary>Administrador e Diretoria veem todas as obras; os demais perfis, só as liberadas (ou todas, se marcado).</summary>
    public static bool PerfilVeTodas(PerfilUsuario p) => p is PerfilUsuario.Administrador or PerfilUsuario.Diretoria;
    public bool VeTodasObras => AcessoTodasObras || PerfilVeTodas(Perfil);
    public bool Ativo { get; set; } = true;
    /// <summary>
    /// Acesso a outra construtora: este cadastro (nesta empresa, com perfil e obras próprios) usa o login e a senha da conta
    /// <see cref="ContaId"/> (o usuário principal, em outra construtora). Vínculo não tem login nem senha próprios.
    /// </summary>
    public Guid? ContaId { get; set; }
    /// <summary>Administra a plataforma (construtoras, usuários entre construtoras, auditoria). Só na conta principal.</summary>
    public bool AdministradorPlataforma { get; set; }
    /// <summary>Acesso de suporte da plataforma: vale até esta data/hora (depois some da lista de construtoras).</summary>
    public DateTimeOffset? SuporteAte { get; set; }
    /// <summary>Perfil de acesso (o que vê e faz em cada tela). Vazio = o perfil padrão do <see cref="Perfil"/>.</summary>
    public Guid? PerfilAcessoId { get; set; }
    /// <summary>Exceções só deste usuário sobre o perfil de acesso: "rota=+A-E;…" (+ libera, − bloqueia).</summary>
    public string? ExcecoesAcesso { get; set; }
    public ICollection<UsuarioObra> Obras { get; set; } = [];
    /// <summary>Limites de aprovação por tipo (e opcionalmente por obra) — "aprova até R$ X".</summary>
    public ICollection<Aprovacoes.LimiteAprovacao> LimitesAprovacao { get; set; } = [];
}
