using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Engenharia;

public sealed record LinhaCustoRealizado(Guid Id, DateOnly Data, TipoCusto Tipo, string Fase, Guid? LocalId, string? Local, string? Fornecedor,
    string Descricao, string? Unidade, decimal Quantidade, decimal Valor, string? Documento, OrigemCusto Origem, bool Historico, string? Observacao, string? Pedido);

public sealed record LinhaOrcadoRealizado(string Fase, decimal Orcado, decimal Realizado)
{
    public decimal Saldo => Orcado - Realizado;
    public decimal? Perc => Orcado == 0 ? null : Math.Round(Realizado / Orcado * 100, 1);
}

public sealed record PainelCusto(decimal Total, decimal Historico, decimal AposCorte, decimal AApropriar, DateOnly? Corte,
    IReadOnlyList<LinhaOrcadoRealizado> PorFase, IReadOnlyDictionary<TipoCusto, decimal> PorTipo, IReadOnlyList<(DateOnly Mes, decimal Valor)> PorMes);
