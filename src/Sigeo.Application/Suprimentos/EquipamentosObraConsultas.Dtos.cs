using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Dados das abas da tela de equipamentos.</summary>
public sealed record EquipamentosObraLista(List<FamiliaEquipamento> Familias, List<EquipamentosServico.Situacao> Situacoes, List<PlanoManutencao> Planos, List<OrdemServico> Ordens);

/// <summary>Equipamento aberto com movimentos e OS.</summary>
public sealed record EquipamentoObraAberto(Domain.Suprimentos.Equipamento Equipamento, List<MovimentoEquipamento> Movimentos, List<OrdemServico> Ordens);

/// <summary>OS aberta e os itens do checklist do plano.</summary>
public sealed record OrdemServicoAberta(OrdemServico Os, IReadOnlyList<string> ItensChecklist);
