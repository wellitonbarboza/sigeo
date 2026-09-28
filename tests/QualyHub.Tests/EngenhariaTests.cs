using QualyHub.Domain.Engenharia;
using Xunit;

namespace QualyHub.Tests;

/// <summary>Cenário fictício: Quadra C, SPE "IRE" (16 pav.) e "IVE" (14 pav.), mix por pavimento 2×56 + 1×75 + 1×99.</summary>
public class EngenhariaTests
{
    private static readonly Guid Obra = Guid.NewGuid(), SpeA = Guid.NewGuid(), SpeB = Guid.NewGuid();
    private static readonly Tipologia T56 = new() { Codigo = "2Q-56", AreaPrivativa = 56 }, T75 = new() { Codigo = "1Q1S-75", AreaPrivativa = 75 }, T99 = new() { Codigo = "3S-99", AreaPrivativa = 99 };
    private static readonly List<(Tipologia, int)> Mix = [(T56, 2), (T75, 1), (T99, 1)];
    private static List<PredioGerar> Predios() => [new("Prédio A", SpeA, "IRE", 16, Mix), new("Prédio B", SpeB, "IVE", 14, Mix)];

    [Fact]
    public void Gera_arvore_e_quantidades()
    {
        var l = GeradorEmpreendimento.Gerar(Obra, "Quadra C", "QC", Predios());
        Assert.Single(l, x => x.Tipo == GeradorEmpreendimento.TipoQuadra);
        Assert.Equal(2, l.Count(x => x.Tipo == GeradorEmpreendimento.TipoPredio));
        Assert.Equal(30, l.Count(x => x.Tipo == GeradorEmpreendimento.TipoPavimento));
        Assert.Equal(64, l.Count(x => x.Tipo == GeradorEmpreendimento.TipoUnidade && x.SpeId == SpeA));
        Assert.Equal(56, l.Count(x => x.Tipo == GeradorEmpreendimento.TipoUnidade && x.SpeId == SpeB));
        Assert.Contains(l, x => x.Codigo == "QC-IRE-P16-1604");
        Assert.Equal(64 / 4 * (56 * 2 + 75 + 99), l.Where(x => x.SpeId == SpeA && x.Tipo == GeradorEmpreendimento.TipoUnidade).Sum(x => x.AreaPrivativa));
    }

    [Fact]
    public void Fracao_ideal_fecha_100_por_predio()
    {
        var l = GeradorEmpreendimento.Gerar(Obra, "Quadra C", "QC", Predios());
        Assert.Empty(GeradorEmpreendimento.ConferirFracao(l));
        foreach (var s in new[] { SpeA, SpeB })
            Assert.Equal(1m, l.Where(x => x.SpeId == s && x.Tipo == GeradorEmpreendimento.TipoUnidade).Sum(x => x.FracaoIdeal));
        var u99 = l.First(x => x.SpeId == SpeA && x.AreaPrivativa == 99);
        Assert.Equal(Math.Round(99m / 4576m, 6), u99.FracaoIdeal);
    }

    [Fact]
    public void Conferencia_aponta_predio_fora_de_100()
    {
        var l = GeradorEmpreendimento.Gerar(Obra, "Quadra C", "QC", Predios());
        l.First(x => x.SpeId == SpeB && x.Tipo == GeradorEmpreendimento.TipoUnidade).FracaoIdeal += 0.01m;
        var e = GeradorEmpreendimento.ConferirFracao(l);
        Assert.Single(e);
        Assert.Contains("Prédio B", e[0]);
    }

    [Fact]
    public void Valida_uma_spe_por_predio_e_limites()
    {
        var e = GeradorEmpreendimento.Validar("Quadra C", [new("A", SpeA, "IRE", 0, Mix), new("B", SpeA, "IRE", 5, [])]);
        Assert.Contains(e, x => x.Contains("pavimentos"));
        Assert.Contains(e, x => x.Contains("unidades por pavimento"));
        Assert.Contains(e, x => x.Contains("1 prédio = 1 SPE"));
        Assert.Empty(GeradorEmpreendimento.Validar("Quadra C", Predios()));
    }
}
