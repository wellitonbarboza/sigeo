using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Qualidade;

public sealed record FvssInicio(decimal Meta, List<Opcao> Equipamentos);

public sealed record FvssLista(Dictionary<Guid, string> Locais, Dictionary<Guid, string> Fornecedores, List<Fvs> Fvss);

/// <param name="EquipamentoFora">Equipamento da FVS que não estava na lista de usáveis (null quando não há ou já estava).</param>
public sealed record FvsAberta(Fvs Fvs, Rnc? Rnc, EquipamentoMedicao? EquipamentoFora);
