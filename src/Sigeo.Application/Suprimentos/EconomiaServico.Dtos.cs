using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

public sealed record LinhaEconomia(string Chave, string Nome, decimal Comprado, decimal BaseTotal, decimal Economia, decimal Percentual, decimal? MetaPerc);

public sealed record MesEconomia(DateOnly Mes, decimal Economia, decimal Percentual, decimal BaseTotal);

public sealed record EconomiaDto(EconomiaReal.Resumo Resumo, decimal? MetaSetorPerc, IReadOnlyList<LinhaEconomia> PorComprador, IReadOnlyList<LinhaEconomia> PorCategoria,
    IReadOnlyList<MesEconomia> Mensal, IReadOnlyList<AvaliacaoCompra> Ganhos, IReadOnlyList<AvaliacaoCompra> Perdas, IReadOnlyList<(string Motivo, int Itens, decimal Valor)> SemBase,
    IReadOnlyList<AvaliacaoCompra> Aumentos, decimal LimiteAumento);

public sealed record HistoricoDto(Guid ProdutoId, string Produto, string Unidade, string? Grupo, HistoricoPreco.Resultado Resultado);
