using Sigeo.Domain.Documentos;

namespace Sigeo.Application.Documentos;

public sealed record GrdsOpcoes(List<Opcao> Vigentes, List<Opcao> Contratos);

public sealed record GrdsLista(Dictionary<Guid, string> Fornecedores, List<Grd> Grds);
