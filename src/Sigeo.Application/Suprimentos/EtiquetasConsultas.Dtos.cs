using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Suprimentos;

/// <summary>Parâmetros da folha e itens para as etiquetas (só uma das listas vem preenchida).</summary>
/// <param name="Instrumentos">Instrumentos de aferição (etiqueta da qualidade).</param>
/// <param name="NomesFornecedores">Nome do fornecedor dono de cada instrumento de terceiro.</param>
/// <param name="Equipamentos">Equipamentos da obra com documentos.</param>
/// <param name="Familias">Nome de cada família de equipamento.</param>
public sealed record EtiquetasDados(ParametrosVigentes Parametros, List<EquipamentoMedicao> Instrumentos, Dictionary<Guid, string> NomesFornecedores,
    List<Domain.Suprimentos.Equipamento> Equipamentos, Dictionary<Guid, string> Familias);
