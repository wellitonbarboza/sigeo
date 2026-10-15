using Microsoft.EntityFrameworkCore;
using Groundkey.Application.Abstracoes;
using Groundkey.Application.Fiscal;
using Groundkey.Application.IA;
using Groundkey.Domain.Fiscal;

namespace Groundkey.Web.Servicos;

/// <summary>
/// Às 6h30 (segunda a sábado, horário de Brasília), para cada construtora:
/// 1. rotina de cadastros (qualificação vencida etc. nos processos em aberto);
/// 2. notas fiscais dos últimos 5 dias no SIEG (se configurado) — ligadas ao pedido pela regra;
/// 3. notas que a regra não ligou passam pela IA (se IA:AnalisarNotasAutomatico = true), até 20 por dia.
/// Também acorda o Azure SQL serverless antes da equipe chegar. Idempotente: nota repetida não entra de novo.
/// </summary>
public sealed class RotinaMatinal(IFabricaSistema sistema, RotinaCadastros rotina, IFonteNotas fonte, IArmazenamento armazenamento,
    IAssistenteIa ia, ILeitorDocumentos leitor, LimitesIa limites, IConfiguration cfg, TimeProvider relogio, ILogger<RotinaMatinal> log) : BackgroundService
{
    private static readonly TimeOnly Hora = new(6, 30);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var espera = Groundkey.Application.Comum.Agenda.AteProxima(relogio.GetLocalNow(), Hora);
            try { await Task.Delay(espera, relogio, ct); } catch (OperationCanceledException) { return; }
            try
            {
                List<Guid> empresas;
                await using (var db = sistema.Criar(Guid.Empty, "RotinaMatinal"))
                    empresas = await db.Empresas.AsNoTracking().Select(e => e.Id).ToListAsync(ct);
                foreach (var e in empresas)
                {
                    await rotina.GarantirHojeAsync(e, ct);
                    await NotasAsync(e, ct);
                }
                log.LogInformation("Rotina matinal: {N} empresa(s)", empresas.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { log.LogWarning(ex, "Rotina matinal falhou"); }
        }
    }

    private async Task NotasAsync(Guid empresa, CancellationToken ct)
    {
        if (!fonte.Configurada(empresa)) return;
        try
        {
            var fab = new FabricaDbSistema(sistema, empresa, "RotinaSieg");
            var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().Date);
            var r = await new NotasFiscaisServico(fab, armazenamento, relogio).BuscarNaFonteAsync(fonte, empresa, hoje.AddDays(-5), hoje, ct);
            log.LogInformation("SIEG empresa {Empresa}: {Novas} nota(s) nova(s), {Vinculadas} vinculada(s), {Canceladas} cancelada(s).", empresa, r.Novas, r.Vinculadas, r.Canceladas);
            if (!ia.Configurado || !cfg.GetValue<bool>("IA:AnalisarNotasAutomatico")) return;
            List<Guid> pendentes;
            await using (var db = fab.CriarLeitura())
            {
                var desde = relogio.GetUtcNow().AddDays(-1);
                pendentes = await db.NotasFiscais.Where(n => n.Situacao == SituacaoNota.Recebida && n.FornecedorId != null && n.AnaliseIa == null && n.CriadoEm >= desde)
                    .OrderBy(n => n.CriadoEm).Select(n => n.Id).Take(20).ToListAsync(ct);
            }
            var analises = new AnalisesIaServico(fab, ia, leitor, fab.Usuario, relogio, limites);
            foreach (var id in pendentes)
            {
                try { await analises.AnalisarNotaAsync(id, ct); }
                catch (Exception ex) when (ex is not OperationCanceledException) { log.LogWarning(ex, "IA não analisou a nota {Nota}", id); }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { log.LogWarning(ex, "Busca de notas no SIEG falhou na empresa {Empresa}", empresa); }
    }
}
