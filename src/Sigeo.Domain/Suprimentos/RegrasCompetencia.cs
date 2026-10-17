using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Regras da documentação mensal ligadas à medição.</summary>
public static class RegrasCompetencia
{
    /// <summary>Bloqueios da medição: competência do mês não conferida e adiamentos vencidos sem entrega no contrato.</summary>
    public static IEnumerable<string> BloqueiosMedicao(DateOnly mesMedicao, IReadOnlyCollection<CompetenciaDocumental> doContrato, DateOnly hoje)
    {
        var mes = new DateOnly(mesMedicao.Year, mesMedicao.Month, 1);
        var c = doContrato.FirstOrDefault(x => x.Mes == mes);
        if (c is not null && c.Itens.Count > 0 && !c.LiberaMedicao)
            yield return $"Documentação mensal {mes:MM/yyyy} não conferida ({c.Entregues} de {c.Itens.Count} itens entregues).";
        foreach (var i in doContrato.SelectMany(x => x.Itens.Select(i => (x.Mes, i))).Where(t => t.i.AdiamentoVencido(hoje)))
            yield return $"Documento adiado vencido: {i.i.Descricao} ({i.Mes:MM/yyyy}), prometido para {i.i.AdiadoAte:dd/MM/yyyy}.";
    }

    /// <summary>Meses de competência do contrato até o mês anterior a hoje (competência do mês M abre no dia 1 do mês M+1).</summary>
    public static IEnumerable<DateOnly> Meses(DateOnly inicioContrato, DateOnly? fimContrato, DateOnly hoje)
    {
        var m = new DateOnly(inicioContrato.Year, inicioContrato.Month, 1);
        var ultimo = new DateOnly(hoje.Year, hoje.Month, 1).AddMonths(-1);
        if (fimContrato is { } f && new DateOnly(f.Year, f.Month, 1) < ultimo) ultimo = new DateOnly(f.Year, f.Month, 1);
        for (; m <= ultimo; m = m.AddMonths(1)) yield return m;
    }
}
