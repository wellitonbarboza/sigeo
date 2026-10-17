using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Equipamento locado em campo (chegada → devolução) para o cálculo por diária.</summary>
public sealed record PeriodoLocacao(Guid ContratoItemId, string Descricao, decimal Quantidade, DateOnly Chegada, DateOnly? Devolucao);
