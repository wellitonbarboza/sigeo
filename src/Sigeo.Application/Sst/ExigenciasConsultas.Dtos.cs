using Sigeo.Domain.Sst;

namespace Sigeo.Application.Sst;

/// <summary>Funções ativas (com exigências), exigências e treinamentos.</summary>
public sealed record ExigenciasDados(List<FuncaoObra> Funcoes, List<Exigencia> Exigencias, List<Opcao> Treinamentos);
