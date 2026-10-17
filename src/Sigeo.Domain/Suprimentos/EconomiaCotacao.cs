using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Resumo das 3 economias do vencedor (padrão ExpertData): valor e % sobre a base.</summary>
public sealed record EconomiaCotacao(decimal Negociada, decimal NegociadaPerc, decimal? VsUltimo, decimal? VsUltimoPerc, decimal? VsReferencia, decimal? VsReferenciaPerc, decimal Total)
{
    public static EconomiaCotacao Calcular(IEnumerable<CotacaoItem> itensVencedor)
    {
        var l = itensVencedor.Where(i => i.ValorUnitarioFinal > 0).ToList();
        var total = l.Sum(i => i.ValorTotal);
        static decimal Perc(decimal econ, decimal baseV) => baseV == 0 ? 0 : Math.Round(econ / baseV * 100, 1);
        var ofertado = l.Sum(i => Math.Round(i.ValorOfertado * i.Quantidade, 2));
        var neg = l.Sum(i => i.Economia);
        var comUlt = l.Where(i => i.EconomiaVsUltimo is not null).ToList();
        var comRef = l.Where(i => i.EconomiaVsReferencia is not null).ToList();
        decimal? ult = comUlt.Count == 0 ? null : comUlt.Sum(i => i.EconomiaVsUltimo!.Value);
        decimal? rf = comRef.Count == 0 ? null : comRef.Sum(i => i.EconomiaVsReferencia!.Value);
        var baseUlt = comUlt.Sum(i => Math.Round(i.PrecoUltimo!.Value * i.Quantidade, 2));
        var baseRef = comRef.Sum(i => Math.Round(i.PrecoReferencia!.Value * i.Quantidade, 2));
        return new EconomiaCotacao(neg, Perc(neg, ofertado), ult, ult is null ? null : Perc(ult.Value, baseUlt), rf, rf is null ? null : Perc(rf.Value, baseRef), total);
    }
}
