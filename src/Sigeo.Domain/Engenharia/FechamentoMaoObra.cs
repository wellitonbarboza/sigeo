using Sigeo.Domain.Common;

namespace Sigeo.Domain.Engenharia;

/// <summary>Fechamento do mês: custo-hora real por equipe (custo do mês ÷ horas apropriadas) e custo por serviço × local.</summary>
public static class FechamentoMaoObra
{
    public sealed record CustoEquipe(Guid? EquipeId, decimal CustoMes, decimal HorasApropriadas, decimal CustoHoraReal);
    public sealed record CustoCelula(Guid ComposicaoId, Guid LocalId, decimal Horas, decimal Custo);

    public static (List<CustoEquipe> Equipes, List<CustoCelula> Celulas, decimal SemApropriacao) Calcular(IEnumerable<CustoMaoObraMes> custos, IEnumerable<Apropriacao> apropriacoes)
    {
        var aps = apropriacoes.ToList();
        // Chave Guid.Empty = sem equipe (custo lançado por colaborador/função, rateado pelas horas apropriadas sem equipe).
        static Guid K(Guid? e) => e ?? Guid.Empty;
        var porEquipe = custos.GroupBy(c => K(c.EquipeId)).ToDictionary(g => g.Key, g => g.Sum(c => c.Total));
        var horas = aps.GroupBy(a => K(a.EquipeId)).ToDictionary(g => g.Key, g => g.Sum(a => a.Horas));
        var equipes = porEquipe.Keys.Union(horas.Keys).Select(k =>
        {
            var custo = porEquipe.GetValueOrDefault(k); var h = horas.GetValueOrDefault(k);
            return new CustoEquipe(k == Guid.Empty ? null : k, custo, h, h <= 0 ? 0 : Math.Round(custo / h, 4));
        }).ToList();
        // Custo de cada célula = custo do mês do grupo (equipe) × horas da célula ÷ horas do grupo, rateado em centavos
        // (a soma das células fecha exatamente com o custo do mês dos grupos que tiveram horas).
        var custoCelula = new Dictionary<(Guid, Guid), decimal>();
        foreach (var g in aps.GroupBy(a => K(a.EquipeId)))
        {
            var cels = g.GroupBy(a => (a.ComposicaoId, a.LocalId)).Select(c => (Chave: c.Key, Horas: c.Sum(a => a.Horas))).ToList();
            var rateio = Rateio.Proporcional(porEquipe.GetValueOrDefault(g.Key), cels.Select(c => c.Horas).ToList());
            for (var k = 0; k < cels.Count; k++) custoCelula[cels[k].Chave] = custoCelula.GetValueOrDefault(cels[k].Chave) + rateio[k];
        }
        var celulas = aps.GroupBy(a => (a.ComposicaoId, a.LocalId))
            .Select(g => new CustoCelula(g.Key.ComposicaoId, g.Key.LocalId, g.Sum(a => a.Horas), custoCelula.GetValueOrDefault(g.Key)))
            .ToList();
        var sem = equipes.Where(e => e.HorasApropriadas <= 0).Sum(e => e.CustoMes);
        return (equipes, celulas, sem);
    }

    /// <summary>Rateia horas entre locais proporcionalmente aos pesos (sem peso: partes iguais). Ver <see cref="Rateio.Proporcional"/>.</summary>
    public static List<(Guid LocalId, decimal Horas)> Ratear(decimal horas, IReadOnlyList<(Guid LocalId, decimal Peso)> locais)
    {
        var partes = Rateio.Proporcional(horas, locais.Select(l => l.Peso).ToList());
        return locais.Select((l, k) => (l.LocalId, partes[k])).ToList();
    }
}
