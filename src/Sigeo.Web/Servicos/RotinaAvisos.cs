using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Abstracoes;
using Sigeo.Application.Comum;

namespace Sigeo.Web.Servicos;

/// <summary>
/// Avisos por e-mail das regras de notificação. A cada 15 min, no expediente (seg–sáb, 6h–21h), gera os lembretes
/// (aprovação pendente, prazos, tarefas, RNC, restrições) e manda a fila. Mensagem nova acorda a rotina na hora.
/// Fora do expediente só envia o que for acordado (não consulta o banco à toa — o Azure SQL pausa).
/// Sem e-mail configurado (ACS), os avisos ficam na fila e saem quando a comunicação for ligada.
/// </summary>
public sealed class RotinaAvisos(IFabricaSistema sistema, IEnvioMensagens envio, CanalAvisos canal, IConfiguration cfg,
    TimeProvider relogio, ILogger<RotinaAvisos> log) : BackgroundService
{
    private static readonly TimeSpan Ciclo = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(30), relogio, ct); } catch (OperationCanceledException) { return; }
        var ultimaGeracao = DateTimeOffset.MinValue;
        var acordada = true;
        while (!ct.IsCancellationRequested)
        {
            var agora = relogio.GetUtcNow();
            var expediente = Expediente.Dentro(relogio.GetLocalNow());
            var gerar = expediente && agora - ultimaGeracao >= Ciclo;
            try
            {
                if (gerar || acordada) await RodarAsync(gerar, ct);
                if (gerar) ultimaGeracao = agora;
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { log.LogWarning(ex, "Rotina de avisos falhou"); }
            acordada = await canal.EsperarAsync(expediente ? Ciclo : TimeSpan.FromHours(1), ct);
            if (ct.IsCancellationRequested) return;
        }
    }

    private async Task RodarAsync(bool gerar, CancellationToken ct)
    {
        List<(Guid Id, string Nome)> empresas;
        await using (var db = sistema.Criar(Guid.Empty, "RotinaAvisos"))
            empresas = (await db.Empresas.AsNoTracking().Select(e => new { e.Id, e.NomeFantasia, e.RazaoSocial }).ToListAsync(ct))
                .Select(e => (e.Id, string.IsNullOrWhiteSpace(e.NomeFantasia) ? e.RazaoSocial : e.NomeFantasia)).ToList();
        var site = EnderecoSite();
        foreach (var (id, nome) in empresas)
        {
            try
            {
                await using var db = sistema.Criar(id, "RotinaAvisos");
                var agora = relogio.GetUtcNow();
                if (gerar)
                {
                    var n = await AvisosServico.GerarAsync(db, agora, DateOnly.FromDateTime(relogio.GetLocalNow().Date), ct);
                    if (n > 0) { await db.SaveChangesAsync(ct); log.LogInformation("Avisos: {N} novo(s) na fila da empresa {Empresa}.", n, id); }
                }
                var (ok, falhas) = await AvisosServico.EnviarPendentesAsync(db, envio, site, nome, agora, ct);
                if (ok + falhas > 0) log.LogInformation("Avisos da empresa {Empresa}: {Ok} enviado(s), {Falhas} falha(s).", id, ok, falhas);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { log.LogWarning(ex, "Avisos falharam na empresa {Empresa}", id); }
        }
    }

    /// <summary>Endereço do site para os links: configurado (Sigeo:EnderecoSite) ou o do App Service (WEBSITE_HOSTNAME).</summary>
    private string EnderecoSite()
    {
        if (cfg["Sigeo:EnderecoSite"] is { Length: > 0 } e) return e.TrimEnd('/') + "/";
        if (Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") is { Length: > 0 } h) return $"https://{h}/";
        return (cfg["ASPNETCORE_URLS"]?.Split(';')[0] ?? "http://localhost:5080").TrimEnd('/') + "/";
    }
}
