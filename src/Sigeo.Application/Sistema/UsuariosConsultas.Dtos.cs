using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Sistema;

/// <summary>Usuários (com obras e limites), alçadas e delegações de aprovação.</summary>
public sealed record UsuariosDados(List<Usuario> Usuarios, List<Alcada> Alcadas, List<DelegacaoAprovacao> Delegacoes);
