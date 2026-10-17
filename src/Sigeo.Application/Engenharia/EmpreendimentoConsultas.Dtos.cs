using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Engenharia;

/// <summary>Obras do cadastro e, com obra escolhida, a estrutura dela.</summary>
public sealed record EmpreendimentoDados(List<Obra> Obras, EmpreendimentoObraDados? DaObra);

public sealed record EmpreendimentoObraDados(List<Local> Locais, List<Quadra> Quadras, List<Spe> Spes, List<Tipologia> Tipologias);
