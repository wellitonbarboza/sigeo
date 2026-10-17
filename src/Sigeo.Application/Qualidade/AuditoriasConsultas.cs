using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Qualidade;

/// <summary>Leituras da tela de auditorias internas (só leitura).</summary>
public sealed class AuditoriasConsultas(IFabricaDb fabrica)
{
    public async Task<List<AuditoriaInterna>> ListarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.AuditoriasInternas.AsNoTracking().Include(a => a.Constatacoes).OrderByDescending(a => a.Inicio).ToListAsync(ct);
    }

    public async Task<AuditoriaInterna> AbrirAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.AuditoriasInternas.AsNoTracking().Include(a => a.Constatacoes).FirstAsync(a => a.Id == id, ct);
    }
}
