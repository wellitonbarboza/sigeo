using Sigeo.Application.Comum;

namespace Sigeo.Tests;

public class QuadroLocaisTests
{
    private static LocalQuantidade Comodo(int pav, int apto, string comodo, decimal q) =>
        new($"QA-T01-P{pav:00}-{pav}{apto:00}-{comodo[..3].ToUpperInvariant()}", comodo, "Cômodo", q, "Torre 1", 1, $"{pav}º pavimento", pav, $"Apto {pav}{apto:00}", apto, comodo);

    [Fact]
    public void Resumo_conta_torre_pavimentos_aptos_comodos_e_areas_comuns()
    {
        var linhas = new List<LocalQuantidade>
        {
            Comodo(1, 1, "Cozinha", 10), Comodo(1, 1, "Sala", 5), Comodo(1, 2, "Cozinha", 10), Comodo(2, 1, "Cozinha", 10),
            new("QA-T01-P01-HAL", "Hall", "Área comum", 2.5m, "Torre 1", 1, "1º pavimento", 1, null, 0, "Hall")
        };
        Assert.Equal("Torre 1 · 2 pav. · 3 aptos · 2 cômodos/apto · 1 área comum · total 37,5 UN", QuadroLocais.Resumo(linhas, "UN"));
        Assert.Equal("302", Comodo(3, 2, "Sala", 1).NumeroApartamento);
    }
}
