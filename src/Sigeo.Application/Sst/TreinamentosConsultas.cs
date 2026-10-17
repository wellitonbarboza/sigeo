using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Sst;

namespace Sigeo.Application.Sst;

/// <summary>Leituras da tela de treinamentos e turmas (Sst/Treinamentos).</summary>
public sealed class TreinamentosConsultas(IFabricaDb fabrica)
{
    /// <summary>Turmas (com participantes), mais recentes primeiro, e catálogo de treinamentos ativos.</summary>
    public async Task<TreinamentosDados> CarregarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var turmas = await db.Turmas.AsNoTracking().Include(t => t.Participantes).OrderByDescending(t => t.Data).ToListAsync(ct);
        var cat = await db.Treinamentos.AsNoTracking().Where(t => t.Ativo).OrderBy(t => t.Descricao).ToListAsync(ct);
        return new(turmas, cat);
    }

    public async Task<Turma> TurmaAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Turmas.AsNoTracking().Include(t => t.Participantes).FirstAsync(t => t.Id == id, ct);
    }
}
