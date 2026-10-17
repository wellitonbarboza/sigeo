namespace Sigeo.Application.Comum;

/// <summary>Janela do expediente para rotinas frequentes (deixa o Azure SQL serverless pausar à noite e no domingo).</summary>
public static class Expediente
{
    public static bool Dentro(DateTimeOffset agoraLocal) =>
        agoraLocal.DayOfWeek != DayOfWeek.Sunday && agoraLocal.Hour >= 6 && agoraLocal.Hour < 21;
}
