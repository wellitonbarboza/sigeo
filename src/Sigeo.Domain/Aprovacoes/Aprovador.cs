using Sigeo.Domain.Common;

namespace Sigeo.Domain.Aprovacoes;

/// <summary>
/// Quem está agindo numa etapa de aprovação, com os limites cadastrados no login dele.
/// Montado pela camada de aplicação a partir de <see cref="Estrutura.Usuario"/> + <see cref="LimiteAprovacao"/>.
/// </summary>
public sealed record Aprovador(Guid UsuarioId, string Nome, PerfilUsuario Perfil, IReadOnlyCollection<LimiteAprovacao> Limites)
{
    public Aprovador(Guid usuarioId, string nome, PerfilUsuario perfil) : this(usuarioId, nome, perfil, []) { }

    /// <summary>
    /// Limite vigente do usuário para o tipo/obra. O limite específico da obra prevalece sobre o geral.
    /// Nulo = nenhum limite cadastrado (o usuário fica restrito apenas às alçadas).
    /// </summary>
    public decimal? LimitePara(EntidadeAprovavel entidade, Guid? obraId)
    {
        var ativos = Limites.Where(l => l.Ativo && !l.Excluido && l.UsuarioId == UsuarioId && l.Entidade == entidade).ToList();
        var daObra = obraId is null ? null : ativos.FirstOrDefault(l => l.ObraId == obraId);
        return (daObra ?? ativos.FirstOrDefault(l => l.ObraId is null))?.ValorMaximo;
    }
}
