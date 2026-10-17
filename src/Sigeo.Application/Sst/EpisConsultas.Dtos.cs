using Sigeo.Domain.Sst;

namespace Sigeo.Application.Sst;

/// <summary>Resumo da ficha de EPI de um colaborador.</summary>
public sealed record EpiFichaLinha(Guid Id, string Nome, string? Empresa, DateOnly? Ultima, DateOnly? Troca, string? CaVencido, string Situacao);

public sealed record EpisDados(List<Epi> Epis, List<EpiFichaLinha> Linhas);

public sealed record EpiFicha(Colaborador Colaborador, List<EntregaEpi> Entregas);
