using Sigeo.Domain.Common;

namespace Sigeo.Domain.Engenharia;

/// <summary>Rateio proporcional em centésimos pelo método do maior resto.</summary>
public static class Rateio
{
    /// <summary>
    /// Divide <paramref name="total"/> proporcionalmente aos pesos (todos zero: partes iguais), com 2 casas.
    /// A soma das partes é exatamente o total (arredondado a 2 casas) e nenhuma parte fica negativa.
    /// </summary>
    public static decimal[] Proporcional(decimal total, IReadOnlyList<decimal> pesos)
    {
        if (pesos.Count == 0) return [];
        var soma = pesos.Sum(p => Math.Max(0, p));
        var centavos = (long)Math.Round(total * 100m, MidpointRounding.AwayFromZero);
        var sinal = Math.Sign(centavos); centavos = Math.Abs(centavos);
        var brutas = pesos.Select(p => soma <= 0 ? centavos / (decimal)pesos.Count : centavos * Math.Max(0, p) / soma).ToList();
        var inteiras = brutas.Select(b => (long)Math.Floor(b)).ToArray();
        var falta = (int)(centavos - inteiras.Sum());
        foreach (var k in brutas.Select((b, k) => (Resto: b - Math.Floor(b), k)).OrderByDescending(x => x.Resto).ThenBy(x => x.k).Take(falta).Select(x => x.k))
            inteiras[k]++;
        return inteiras.Select(c => sinal * c / 100m).ToArray();
    }
}
