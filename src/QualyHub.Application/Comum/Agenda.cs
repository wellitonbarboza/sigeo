namespace QualyHub.Application.Comum;

/// <summary>Horário das rotinas agendadas (sem dependência de infraestrutura, testável).</summary>
public static class Agenda
{
    /// <summary>Tempo até o próximo <paramref name="hora"/> de segunda a sábado, contado a partir de <paramref name="agora"/> (fuso do próprio valor).</summary>
    public static TimeSpan AteProxima(DateTimeOffset agora, TimeOnly hora)
    {
        var alvo = new DateTimeOffset(agora.Date + hora.ToTimeSpan(), agora.Offset);
        if (alvo <= agora) alvo = alvo.AddDays(1);
        while (alvo.DayOfWeek == DayOfWeek.Sunday) alvo = alvo.AddDays(1);
        return alvo - agora;
    }
}

/// <summary>Janela do expediente para rotinas frequentes (deixa o Azure SQL serverless pausar à noite e no domingo).</summary>
public static class Expediente
{
    public static bool Dentro(DateTimeOffset agoraLocal) =>
        agoraLocal.DayOfWeek != DayOfWeek.Sunday && agoraLocal.Hour >= 6 && agoraLocal.Hour < 21;
}
