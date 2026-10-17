using Sigeo.Domain.Cadastros;

namespace Sigeo.Application.Cadastros;

/// <summary>Produto ativo ainda sem material controlado.</summary>
public sealed record ProdutoSemVinculo(string Codigo, string Nome, string? Grupo, string? SubGrupo);

/// <summary>O que a tela de controlados carrega de uma vez. Fvm/Fvs: modelo vigente ("código Rnn") por material/serviço.</summary>
public sealed record ControladosDados(List<MaterialControlado> Materiais, List<ServicoControlado> Servicos, Dictionary<Guid, string> Fvm, Dictionary<Guid, string> Fvs,
    List<ProdutoSemVinculo> SemVinculo, Dictionary<Guid, int> ProdutosPorMaterial);
