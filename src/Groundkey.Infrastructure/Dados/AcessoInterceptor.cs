using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Groundkey.Contratos;
using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Common;

namespace Groundkey.Infrastructure.Dados;

/// <summary>
/// Barreira do perfil de acesso na gravação: o que é gravado a partir de uma tela exige, nessa tela, "Incluir/Editar"
/// (ou "Excluir/Cancelar" quando a operação só exclui ou cancela). Vale para qualquer botão da tela, mesmo que ela
/// esqueça de esconder. Decisão de aprovação é conferida no motor de aprovação ("Aprovar" + alçada).
/// Fora de tela (rotinas, portal, login, importação) não se aplica.
/// </summary>
public sealed class AcessoInterceptor(IServiceProvider sp) : SaveChangesInterceptor
{
    /// <summary>Registros que qualquer um que vê a tela gera (leitura, ciência, conversa, log): não exigem "Incluir".</summary>
    private static readonly HashSet<string> Livres =
    [
        "RegistroAuditoria", "RegistroPlataforma", "Sequencia", "Ciencia", "EventoDocumento", "TarefaLeitura", "LeituraMensagem",
        "Conversa", "ParticipanteConversa", "MensagemConversa", "AvisoEmail", "UsoIa", "CredencialUsuario",
    ];

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData e, InterceptionResult<int> r, CancellationToken ct = default)
    {
        if (e.Context is not null) await ConferirAsync(e.Context, ct);
        return await base.SavingChangesAsync(e, r, ct);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData e, InterceptionResult<int> r)
    {
        if (e.Context is not null) ConferirAsync(e.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavingChanges(e, r);
    }

    private async Task ConferirAsync(DbContext ctx, CancellationToken ct)
    {
        var tela = CatalogoTelas.TelaDe(sp.GetService<Application.Servicos.IContextoTela>()?.Rota);
        if (tela is null || sp.GetService<IUsuarioAtual>()?.UsuarioId is null) return;
        var acao = Classificar(ctx.ChangeTracker.Entries());
        if (acao is null) return;
        var mapa = await sp.GetRequiredService<Application.Servicos.AcessoServico>().MapaAsync(ct);
        Application.Servicos.AcessoServico.Conferir(mapa, tela.Rota, acao.Value);
    }

    /// <summary>
    /// I = inclui/altera; E = só exclui ou cancela (status "Cancelado…"); null = nada a conferir aqui
    /// (sem mudança ou decisão de aprovação, conferida no motor de aprovação).
    /// </summary>
    public static char? Classificar(IEnumerable<EntityEntry> entradas)
    {
        bool grava = false, exclui = false, cancela = false;
        foreach (var x in entradas)
        {
            if (x.State is EntityState.Detached or EntityState.Unchanged || Livres.Contains(x.Metadata.ClrType.Name)) continue;
            if (x.Entity is EtapaAprovacao && x.State == EntityState.Modified) return null;
            if (x.State == EntityState.Deleted
                || (x.Entity is IExclusaoLogica { Excluido: true } && x.State == EntityState.Modified && x.Property(nameof(IExclusaoLogica.Excluido)).IsModified))
            { exclui = true; continue; }
            if (x.State == EntityState.Modified && x.Metadata.FindProperty("Status") is { } st && x.Property(st.Name) is { IsModified: true } p
                && p.CurrentValue?.ToString()?.StartsWith("Cancel", StringComparison.OrdinalIgnoreCase) == true)
            { cancela = true; continue; }
            grava = true;
        }
        if (cancela || (exclui && !grava)) return AcaoTela.Excluir;
        return grava || exclui ? AcaoTela.Incluir : null;
    }
}
