using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

public sealed record LinhaCompetencia(Guid Id, Guid ContratoId, string Contrato, string Fornecedor, string Obra, DateOnly Mes, SituacaoCompetencia Situacao, bool Atrasada,
    int Entregues, int Total, int AdiamentosPendentes, string? Medicao);

public sealed record ItemCompetenciaDto(ItemCompetencia Item, string? Colaborador, int? DiasNaObra, string? Observacao);

public sealed record CompetenciaDetalhe(CompetenciaDocumental Competencia, string Contrato, string Fornecedor, string Obra, IReadOnlyList<ItemCompetenciaDto> Itens,
    IReadOnlyList<LinhaCompetencia> Historico, string? Medicao, bool Atrasada, DateOnly Limite);
