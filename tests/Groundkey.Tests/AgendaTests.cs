using Groundkey.Application.Comum;

namespace Groundkey.Tests;

public class AgendaTests
{
    private static readonly TimeSpan Brasilia = TimeSpan.FromHours(-3);
    private static readonly TimeOnly SeisEMeia = new(6, 30);

    [Fact]
    public void Antes_das_6h30_roda_no_mesmo_dia()
    {
        var segunda5h = new DateTimeOffset(2026, 10, 5, 5, 0, 0, Brasilia);
        Assert.Equal(TimeSpan.FromMinutes(90), Agenda.AteProxima(segunda5h, SeisEMeia));
    }

    [Fact]
    public void Depois_das_6h30_de_sabado_pula_o_domingo()
    {
        var sabado7h = new DateTimeOffset(2026, 10, 10, 7, 0, 0, Brasilia);
        var proxima = sabado7h + Agenda.AteProxima(sabado7h, SeisEMeia);
        Assert.Equal(DayOfWeek.Monday, proxima.DayOfWeek);
        Assert.Equal(new DateTimeOffset(2026, 10, 12, 6, 30, 0, Brasilia), proxima);
    }
}
