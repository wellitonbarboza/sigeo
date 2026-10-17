namespace Sigeo.Domain.Planejamento;

/// <summary>Desembolso: custo de cada atividade distribuído por igual nos seus dias úteis, somado por mês.</summary>
public static class Desembolso
{
    public static SortedDictionary<DateOnly, decimal> PorMes(IEnumerable<AtividadeCronograma> atividades, Calendario cal, bool usarBase = false)
    {
        var r = new SortedDictionary<DateOnly, decimal>();
        foreach (var a in atividades.Where(a => !a.Resumo && a.Custo != 0))
        {
            DateOnly? ini = usarBase ? a.InicioBase : a.Inicio, fim = usarBase ? a.TerminoBase : a.Termino;
            if (ini is null || fim is null) continue;
            var i0 = cal.Indice(ini.Value); var i1 = Math.Max(i0, cal.Indice(fim.Value) + (cal.EhUtil(fim.Value) ? 0 : -1));
            var n = i1 - i0 + 1;
            var porDia = a.Custo / n;
            for (var i = i0; i <= i1; i++)
            {
                var d = cal.Data(i); var mes = new DateOnly(d.Year, d.Month, 1);
                r[mes] = r.GetValueOrDefault(mes) + porDia;
            }
        }
        foreach (var k in r.Keys.ToList()) r[k] = Math.Round(r[k], 2);
        return r;
    }
}
