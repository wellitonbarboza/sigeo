namespace Sigeo.Application.Planejamento;

/// <summary>Almoxarifado da obra (se houver) e os equipamentos em uso ou disponíveis.</summary>
public sealed record DiarioEquipamentosDados(Guid? AlmoxarifadoObra, List<DiarioEquipamentoItem> Equipamentos);

public sealed record DiarioEquipamentoItem(Guid Id, string Patrimonio, string Nome, Guid? AlmoxarifadoId);
