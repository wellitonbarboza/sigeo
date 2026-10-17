using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Linha da grade de medições.</summary>
public sealed record MedicaoLinha(Guid Id, string Contrato, int Numero, string Fornecedor, DateOnly Inicio, DateOnly Fim, decimal Valor, decimal? Nota, FaixaNota Faixa, StatusMedicao Status, string CriadoPor);

/// <summary>Tudo o que a tela lê ao abrir uma medição (uma ida ao banco).</summary>
/// <param name="Fvs">Todas as FVS do prestador na obra (a tela filtra período e liberadas).</param>
/// <param name="RncsFvs">RNC de origem FVS do prestador.</param>
/// <param name="ColaboradoresBloqueados">Colaboradores bloqueados do prestador na obra durante o período.</param>
public sealed record MedicaoAberta(Medicao Medicao, List<Fvs> Fvs, Dictionary<Guid, string> NomesLocais, int RncsFvs,
    List<CompetenciaDocumental> Competencias, int ColaboradoresBloqueados);
