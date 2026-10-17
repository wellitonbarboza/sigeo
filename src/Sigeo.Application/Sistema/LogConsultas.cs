using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Sistema;

/// <summary>Leituras da tela de log (Sistema/Log): trilha de auditoria e eventos de documentos, paginadas.</summary>
public sealed class LogConsultas(IFabricaDb fabrica)
{
    /// <summary>Página <paramref name="pagina"/> (base 0) da auditoria no período, com filtro de texto opcional, e o total.</summary>
    public async Task<LogAuditoriaPagina> AuditoriaAsync(DateTimeOffset ini, DateTimeOffset fim, string? texto, int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var qa = db.Auditoria.Where(a => a.Quando >= ini && a.Quando <= fim);
        if (!string.IsNullOrWhiteSpace(texto))
            qa = qa.Where(a => a.Usuario.Contains(texto) || a.Entidade.Contains(texto) || (a.CamposAlterados != null && a.CamposAlterados.Contains(texto)));
        var total = await qa.CountAsync(ct);
        var itens = await qa.OrderByDescending(a => a.Quando).ThenByDescending(a => a.Id).Skip(pagina * tamanhoPagina).Take(tamanhoPagina).ToListAsync(ct);
        return new(total, itens);
    }

    /// <summary>Página de eventos de documento no período, o total e o código de cada documento citado.</summary>
    public async Task<LogEventosPagina> EventosAsync(DateTimeOffset ini, DateTimeOffset fim, string? texto, int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var qe = db.EventosDocumento.Where(e => e.DataHora >= ini && e.DataHora <= fim);
        if (!string.IsNullOrWhiteSpace(texto))
            qe = qe.Where(e => e.UsuarioNome.Contains(texto) || (e.Detalhe != null && e.Detalhe.Contains(texto)));
        var total = await qe.CountAsync(ct);
        var eventos = await qe.OrderByDescending(e => e.DataHora).ThenByDescending(e => e.Id).Skip(pagina * tamanhoPagina).Take(tamanhoPagina).ToListAsync(ct);
        var ids = eventos.Where(e => e.DocumentoId != null).Select(e => e.DocumentoId!.Value).Distinct().ToList();
        var docs = await db.Documentos.Where(d => ids.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Codigo, ct);
        return new(total, eventos, docs);
    }
}
