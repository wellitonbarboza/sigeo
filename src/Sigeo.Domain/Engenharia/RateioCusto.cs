using Sigeo.Domain.Common;

namespace Sigeo.Domain.Engenharia;

/// <summary>Divide um valor (e a quantidade) entre locais pelo peso de cada um, sem perder centavos no arredondamento.</summary>
public static class RateioCusto
{
    public sealed record Parte(Guid? LocalId, decimal Quantidade, decimal Valor);

    public static IReadOnlyList<Parte> Ratear(decimal quantidade, decimal valor, IReadOnlyList<(Guid LocalId, decimal Peso)> locais)
    {
        var validos = locais.Where(l => l.Peso > 0).GroupBy(l => l.LocalId).Select(g => (LocalId: g.Key, Peso: g.Sum(x => x.Peso))).ToList();
        if (validos.Count == 0) return [new Parte(null, quantidade, valor)];
        if (validos.Count == 1) return [new Parte(validos[0].LocalId, quantidade, valor)];
        var total = validos.Sum(l => l.Peso);
        var partes = new List<Parte>();
        decimal somaV = 0, somaQ = 0;
        for (var i = 0; i < validos.Count; i++)
        {
            var ultimo = i == validos.Count - 1;
            var v = ultimo ? valor - somaV : Math.Round(valor * validos[i].Peso / total, 2);
            var q = ultimo ? quantidade - somaQ : Math.Round(quantidade * validos[i].Peso / total, 4);
            somaV += v; somaQ += q;
            partes.Add(new Parte(validos[i].LocalId, q, v));
        }
        return partes;
    }
}
