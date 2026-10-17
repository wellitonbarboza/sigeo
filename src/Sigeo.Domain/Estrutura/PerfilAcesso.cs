using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>
/// Perfil de acesso da construtora: por tela, as ações liberadas ("rota=VIEX;…"). O padrão de cada perfil de usuário
/// (<see cref="Base"/>) nasce igual ao padrão do sistema e pode ser ajustado; perfis criados (cópias) não têm base.
/// </summary>
public class PerfilAcesso : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    /// <summary>Perfil de usuário de que este é o padrão (usuários sem perfil de acesso escolhido caem aqui).</summary>
    public PerfilUsuario? Base { get; set; }
    public string Regras { get; set; } = string.Empty;
    /// <summary>Versão do catálogo de telas quando o perfil foi gravado: telas criadas depois herdam o padrão do perfil base.</summary>
    public int CatalogoVersao { get; set; }
}
