using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Colaboracao;

public sealed record AutorizacoesDados(SolicitacaoAprovacao? Solicitacao, List<Alcada> Alcadas, bool PodeDecidir);
