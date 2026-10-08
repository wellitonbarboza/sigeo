using Microsoft.EntityFrameworkCore;
using QualyHub.Application.Abstracoes;

namespace QualyHub.Web.Servicos;

/// <summary>
/// Às 6h30 (segunda a sábado, horário de Brasília): roda a rotina de cadastros de cada empresa. Dois efeitos:
/// os processos em aberto amanhecem atualizados (qualificação vencida etc.) e o Azure SQL serverless acorda
/// antes da equipe chegar — o primeiro acesso do dia não espera o banco "acordar". Fora disso o banco pode pausar.
/// Idempotente: com mais de uma instância, a segunda passada não muda nada.
/// </summary>
public sealed class RotinaMatinal(IFabricaSistema sistema, RotinaCadastros rotina, TimeProvider relogio, ILogger<RotinaMatinal> log) : BackgroundService
{
    private static readonly TimeOnly Hora = new(6, 30);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var espera = QualyHub.Application.Comum.Agenda.AteProxima(relogio.GetLocalNow(), Hora);
            try { await Task.Delay(espera, relogio, ct); } catch (OperationCanceledException) { return; }
            try
            {
                List<Guid> empresas;
                await using (var db = sistema.Criar(Guid.Empty, "RotinaMatinal"))
                    empresas = await db.Empresas.AsNoTracking().Select(e => e.Id).ToListAsync(ct);
                foreach (var e in empresas) await rotina.GarantirHojeAsync(e, ct);
                log.LogInformation("Rotina matinal: {N} empresa(s)", empresas.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { log.LogWarning(ex, "Rotina matinal falhou"); }
        }
    }

}
