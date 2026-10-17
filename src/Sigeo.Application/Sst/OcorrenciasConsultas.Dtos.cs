using Sigeo.Domain.Sst;

namespace Sigeo.Application.Sst;

public sealed record OcorrenciasDados(List<Ocorrencia> Itens, Dictionary<Guid, string> NomesLocais, Dictionary<Guid, string> NumerosAcoes, List<Opcao> Pts);
