using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

// ═══════════════════════ Regras ═══════════════════════

/// <summary>Resultado da análise das exigências de uma pessoa.</summary>
public sealed record SituacaoCalculada(SituacaoColaborador Situacao, AtividadeRisco Restricoes, IReadOnlyList<(string Exigencia, string Situacao, DateOnly? Validade, bool Ok)> Detalhe);
