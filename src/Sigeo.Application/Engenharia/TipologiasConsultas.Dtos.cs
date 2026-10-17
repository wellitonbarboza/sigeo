using Sigeo.Domain.Engenharia;

namespace Sigeo.Application.Engenharia;

public sealed record TipologiasDados(List<Tipologia> Tipologias, List<ServicoAplicacao> Aplicacoes, Dictionary<Guid, int> Usos, List<Composicao> Composicoes);
