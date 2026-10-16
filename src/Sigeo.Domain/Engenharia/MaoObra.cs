using Sigeo.Domain.Common;

namespace Sigeo.Domain.Engenharia;

/// <summary>Equipe de mão de obra própria (ex.: Equipe de alvenaria 1). Os membros são colaboradores próprios do cadastro de pessoas.</summary>
public class Equipe : EntidadeDaObra
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Encarregado { get; set; }
    public bool Ativa { get; set; } = true;
    public ICollection<EquipeMembro> Membros { get; set; } = [];
}

public class EquipeMembro : Entidade
{
    public Guid EquipeId { get; set; }
    public Guid ColaboradorId { get; set; }
    public DateOnly Desde { get; set; }
    public DateOnly? Ate { get; set; }
}

/// <summary>
/// Custo mensal de mão de obra própria (por equipe, colaborador ou função): salário, encargos, benefícios e outros.
/// Pode ser replicado do mês anterior e ajustado. Base do custo-hora real da apropriação.
/// </summary>
public class CustoMaoObraMes : EntidadeDaObra
{
    /// <summary>1º dia do mês de competência.</summary>
    public DateOnly Competencia { get; set; }
    public Guid? EquipeId { get; set; }
    public Guid? ColaboradorId { get; set; }
    public Guid? FuncaoObraId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int Pessoas { get; set; } = 1;
    /// <summary>Salário base por pessoa.</summary>
    public decimal Salario { get; set; }
    public decimal EncargosPerc { get; set; }
    /// <summary>Benefícios por pessoa (VT, VA, cesta, plano…).</summary>
    public decimal Beneficios { get; set; }
    /// <summary>Outros custos do mês (horas extras, alojamento, EPI…).</summary>
    public decimal Outros { get; set; }
    public decimal HorasMes { get; set; } = 220;

    public decimal Total => Math.Round(Pessoas * (Salario * (1 + EncargosPerc / 100m) + Beneficios) + Outros, 2);
    /// <summary>Custo-hora teórico (total ÷ horas disponíveis).</summary>
    public decimal CustoHora => Pessoas * HorasMes <= 0 ? 0 : Math.Round(Total / (Pessoas * HorasMes), 4);

    public static DateOnly Mes(DateOnly d) => new(d.Year, d.Month, 1);

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (EquipeId is null && ColaboradorId is null && FuncaoObraId is null && string.IsNullOrWhiteSpace(Descricao)) e.Add("Informe a equipe, o colaborador, a função ou a descrição.");
        if (Pessoas < 1) e.Add("Pessoas deve ser pelo menos 1.");
        if (Salario < 0 || Beneficios < 0 || Outros < 0) e.Add("Valores não podem ser negativos.");
        if (EncargosPerc is < 0 or > 300) e.Add("Encargos entre 0 e 300%.");
        if (HorasMes <= 0) e.Add("Horas no mês devem ser maiores que zero.");
        return e;
    }

    /// <summary>Cópia para outra competência (replicar o mês anterior).</summary>
    public CustoMaoObraMes CopiarPara(DateOnly competencia) => new()
    {
        ObraId = ObraId, Competencia = Mes(competencia), EquipeId = EquipeId, ColaboradorId = ColaboradorId, FuncaoObraId = FuncaoObraId, Descricao = Descricao,
        Pessoas = Pessoas, Salario = Salario, EncargosPerc = EncargosPerc, Beneficios = Beneficios, Outros = Outros, HorasMes = HorasMes
    };
}

/// <summary>Apropriação: horas da equipe/colaborador num serviço × local, num dia (e a quantidade executada, quando informada).</summary>
public class Apropriacao : EntidadeDaObra
{
    public DateOnly Data { get; set; }
    public Guid? EquipeId { get; set; }
    public Guid? ColaboradorId { get; set; }
    public Guid ComposicaoId { get; set; }
    public Guid LocalId { get; set; }
    public decimal Horas { get; set; }
    public decimal? QuantidadeExecutada { get; set; }
    public string? Observacao { get; set; }
    /// <summary>Agrupa as linhas criadas no mesmo lançamento (um lançamento pode ratear horas em vários locais).</summary>
    public Guid Lancamento { get; set; }
}

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
