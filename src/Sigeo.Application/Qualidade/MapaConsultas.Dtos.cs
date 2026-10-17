using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Qualidade;

public sealed record MapaInspecoes(List<Local> Torres, List<Local> Linhas, List<Fvs> Fvs);
