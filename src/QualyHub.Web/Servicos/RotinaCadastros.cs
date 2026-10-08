using System.Collections.Concurrent;
using QualyHub.Application.Abstracoes;
using QualyHub.Application.Servicos;

namespace QualyHub.Web.Servicos;

/// <summary>
/// Atualização "de tempo" dos cadastros nos processos em andamento (qualificação que venceu hoje, por exemplo).
/// Roda uma vez por dia por empresa, no primeiro acesso ao início — sem tarefa agendada, para não acordar o banco à toa.
/// </summary>
public sealed class RotinaCadastros(IFabricaSistema sistema, TimeProvider relogio, ILogger<RotinaCadastros> log)
{
    private readonly ConcurrentDictionary<Guid, DateOnly> _ultima = new();

    public async Task GarantirHojeAsync(Guid empresaId, CancellationToken ct = default)
    {
        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().Date);
        if (empresaId == Guid.Empty || (_ultima.TryGetValue(empresaId, out var d) && d == hoje)) return;
        _ultima[empresaId] = hoje;
        try
        {
            await using var db = sistema.Criar(empresaId, "RotinaCadastros");
            var n = await SincronizacaoCadastros.RotinaDiariaAsync(db, hoje, ct);
            if (n > 0) log.LogInformation("Rotina de cadastros: {N} fornecedor(es) mudaram de qualificação na empresa {Empresa}", n, empresaId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ultima.TryRemove(empresaId, out _);
            log.LogWarning(ex, "Rotina de cadastros falhou na empresa {Empresa}", empresaId);
        }
    }
}
