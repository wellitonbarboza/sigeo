using Sigeo.Domain.Common;
using Sigeo.Domain.Financeiro;

namespace Sigeo.Domain.Incorporacao;

/// <summary>
/// Correção monetária das parcelas de venda: índice "até as chaves" (INCC) e "após as chaves" (IPCA), mês a mês desde a data-base
/// até o mês anterior ao do vencimento (defasagem de 1 mês, prática de mercado). Após as chaves soma os juros do contrato.
/// Mês sem índice informado conta 0% (o financeiro vê o aviso).
/// </summary>
public static class MotorCorrecao
{
    public sealed record Resultado(decimal Fator, decimal ValorCorrigido, decimal Correcao, decimal Juros, IReadOnlyList<DateOnly> MesesSemIndice);

    public static Resultado Corrigir(decimal valor, DateOnly dataBase, DateOnly vencimento, DateOnly? chaves, string indiceAntes, string indiceDepois,
        decimal jurosMes, IReadOnlyDictionary<(string Indice, DateOnly Mes), decimal> indices)
    {
        var fator = 1m; var fatorJuros = 1m;
        var faltam = new List<DateOnly>();
        var mes = new DateOnly(dataBase.Year, dataBase.Month, 1);
        var ate = new DateOnly(vencimento.Year, vencimento.Month, 1).AddMonths(-1);
        while (mes <= ate)
        {
            var depois = chaves is { } c && mes >= new DateOnly(c.Year, c.Month, 1);
            var idx = depois ? indiceDepois : indiceAntes;
            if (indices.TryGetValue((idx, mes), out var v)) fator *= 1 + v / 100m;
            else faltam.Add(mes);
            if (depois && jurosMes > 0) fatorJuros *= 1 + jurosMes / 100m;
            mes = mes.AddMonths(1);
        }
        var corrigido = Math.Round(valor * fator, 2);
        var comJuros = Math.Round(corrigido * fatorJuros, 2);
        return new Resultado(Math.Round(fator, 8), comJuros, corrigido - valor, comJuros - corrigido, faltam);
    }
}
