using Sigeo.Domain.Common;

namespace Sigeo.Domain.Aprovacoes;

/// <summary>Titular que delegou a aprovação ao substituto, com a delegação que dá base ao ato.</summary>
public sealed record DelegacaoRecebida(DelegacaoAprovacao Delegacao, Aprovador Titular);
