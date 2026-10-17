using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>
/// Senha do usuário (hash PBKDF2, nunca a senha em si), bloqueio por tentativas e troca obrigatória.
/// Separada do cadastro para que editar o usuário nunca toque na senha.
/// </summary>
public class CredencialUsuario : EntidadeAuditavel
{
    public Guid UsuarioId { get; set; }
    public string SenhaHash { get; set; } = string.Empty;
    public bool TrocarSenhaNoProximoAcesso { get; set; } = true;
    public int TentativasFalhas { get; set; }
    public DateTimeOffset? BloqueadoAte { get; set; }
    public DateTimeOffset? UltimoAcessoEm { get; set; }
    public DateTimeOffset? SenhaAlteradaEm { get; set; }
}
