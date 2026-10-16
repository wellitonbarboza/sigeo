namespace Sigeo.Domain.Planejamento;

/// <summary>
/// Calendário de trabalho da obra: dias da semana trabalhados + feriados (nacionais calculados + municipais/paradas cadastrados).
/// Converte data ↔ índice de dia útil para o motor de rede trabalhar só com inteiros.
/// </summary>
public sealed class Calendario
{
    private static readonly DateOnly Base = new(2000, 1, 3); // segunda-feira
    private readonly bool[] _util = new bool[7];
    private readonly int _porSemana;
    private readonly DateOnly[] _feriados; // só os que caem em dia trabalhado, ordenados

    public Calendario(IEnumerable<DateOnly>? feriados = null, bool sabado = false, bool incluirNacionais = true)
    {
        for (var d = DayOfWeek.Monday; d <= DayOfWeek.Friday; d++) _util[(int)d] = true;
        _util[(int)DayOfWeek.Saturday] = sabado;
        _porSemana = _util.Count(x => x);
        var todos = new HashSet<DateOnly>(feriados ?? []);
        if (incluirNacionais)
            for (var a = 2020; a <= 2040; a++) foreach (var f in FeriadosNacionais(a)) todos.Add(f.Data);
        _feriados = [.. todos.Where(f => _util[(int)f.DayOfWeek] && f >= Base).OrderBy(f => f)];
    }

    public bool EhUtil(DateOnly d) => _util[(int)d.DayOfWeek] && Array.BinarySearch(_feriados, d) < 0;

    /// <summary>Quantidade de dias úteis em [Base, d).</summary>
    private int AntesDe(DateOnly d)
    {
        var dias = d.DayNumber - Base.DayNumber;
        if (dias <= 0) return 0;
        var semanas = dias / 7;
        var n = semanas * _porSemana;
        for (var i = 0; i < dias % 7; i++) if (_util[(int)Base.AddDays(semanas * 7 + i).DayOfWeek]) n++;
        var idx = Array.BinarySearch(_feriados, d);
        var feriadosAntes = idx >= 0 ? idx : ~idx;
        return n - feriadosAntes;
    }

    /// <summary>Índice do dia útil (se d não é útil, o índice do próximo dia útil).</summary>
    public int Indice(DateOnly d) => AntesDe(d);

    /// <summary>Data do dia útil de índice i.</summary>
    public DateOnly Data(int i)
    {
        if (i < 0) i = 0;
        // menor d tal que AntesDe(d + 1) > i  (o (i+1)-ésimo dia útil)
        long lo = 0, hi = (long)(i + 1) * 7 / Math.Max(1, _porSemana) + _feriados.Length + 14;
        while (lo < hi)
        {
            var meio = (lo + hi) / 2;
            if (AntesDe(Base.AddDays((int)meio + 1)) > i) hi = meio; else lo = meio + 1;
        }
        return Base.AddDays((int)lo);
    }

    /// <summary>Próximo dia útil (o próprio, se já é útil).</summary>
    public DateOnly ProximoUtil(DateOnly d) => Data(Indice(d));

    /// <summary>Dias úteis entre duas datas (b − a em índices; negativo se b antes de a).</summary>
    public int DiasUteisEntre(DateOnly a, DateOnly b) => Indice(b) - Indice(a);

    /// <summary>Feriados nacionais (Lei 662/49 e 14.759/23) + Carnaval (seg/ter), Sexta-feira Santa e Corpus Christi, que param a obra.</summary>
    public static IEnumerable<(DateOnly Data, string Nome)> FeriadosNacionais(int ano)
    {
        var pascoa = Pascoa(ano);
        yield return (new DateOnly(ano, 1, 1), "Confraternização Universal");
        yield return (pascoa.AddDays(-48), "Carnaval");
        yield return (pascoa.AddDays(-47), "Carnaval");
        yield return (pascoa.AddDays(-2), "Sexta-feira Santa");
        yield return (new DateOnly(ano, 4, 21), "Tiradentes");
        yield return (new DateOnly(ano, 5, 1), "Dia do Trabalho");
        yield return (pascoa.AddDays(60), "Corpus Christi");
        yield return (new DateOnly(ano, 9, 7), "Independência");
        yield return (new DateOnly(ano, 10, 12), "Nossa Senhora Aparecida");
        yield return (new DateOnly(ano, 11, 2), "Finados");
        yield return (new DateOnly(ano, 11, 15), "Proclamação da República");
        yield return (new DateOnly(ano, 11, 20), "Consciência Negra");
        yield return (new DateOnly(ano, 12, 25), "Natal");
    }

    /// <summary>Domingo de Páscoa (algoritmo de Meeus/Jones/Butcher).</summary>
    public static DateOnly Pascoa(int ano)
    {
        int a = ano % 19, b = ano / 100, c = ano % 100, d = b / 4, e = b % 4, f = (b + 8) / 25, g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30, i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7, m = (a + 11 * h + 22 * l) / 451;
        int mes = (h + l - 7 * m + 114) / 31, dia = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(ano, mes, dia);
    }
}
