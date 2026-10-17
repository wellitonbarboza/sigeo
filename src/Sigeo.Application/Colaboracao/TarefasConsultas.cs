using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Colaboracao;

namespace Sigeo.Application.Colaboracao;

/// <summary>Leituras da tela de tarefas (Colaboracao/Tarefas).</summary>
public sealed class TarefasConsultas(IFabricaDb fabrica)
{
    /// <summary>Tarefas mais recentes pelo número (até <paramref name="limite"/>), com prorrogações e leituras.</summary>
    public async Task<List<Tarefa>> ListarAsync(int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Tarefas.AsNoTracking().Include(t => t.Prorrogacoes).Include(t => t.Leituras).OrderByDescending(t => t.Numero).Take(limite).ToListAsync(ct);
    }
}
