using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Qualidade;

public sealed record EquipamentosMedicaoLista(List<EquipamentoMedicao> Itens, List<TipoEquipamentoMedicao> Tipos);

public sealed record EquipamentoMedicaoAberto(EquipamentoMedicao Equipamento, List<Calibracao> Calibracoes, List<CautelaFerramenta> Cautelas);
