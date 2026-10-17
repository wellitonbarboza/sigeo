using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Sistema;

/// <summary>Leituras da tela de parâmetros e listas (Sistema/ParametrosListas).</summary>
public sealed class ParametrosListasConsultas(IFabricaDb fabrica)
{
    /// <summary>Parâmetros, listas, tipos de documento, regras de notificação e os 100 avisos por e-mail mais recentes.</summary>
    public async Task<ParametrosListasDados> CarregarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var parametros = await db.Parametros.OrderBy(p => p.Grupo).ThenBy(p => p.Chave).ToListAsync(ct);
        var listas = await db.Listas.OrderBy(l => l.Tipo).ThenBy(l => l.Ordem).ThenBy(l => l.Nome).ToListAsync(ct);
        var tipos = await db.TiposDocumento.OrderBy(t => t.AplicaA).ThenBy(t => t.Nome).ToListAsync(ct);
        var notifs = await db.RegrasNotificacao.OrderBy(n => n.Evento).ToListAsync(ct);
        var avisos = await db.AvisosEmail.OrderByDescending(a => a.CriadoEm).Take(100).ToListAsync(ct);
        return new(parametros, listas, tipos, notifs, avisos);
    }
}
