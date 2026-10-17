using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Qualidade;

/// <summary>Leituras da tela de ações corretivas (só leitura).</summary>
public sealed class AcoesCorretivasConsultas(IFabricaDb fabrica)
{
    /// <summary>Ações da mais recente para a mais antiga, até o limite da grade.</summary>
    public async Task<List<AcaoCorretiva>> ListarAsync(int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.AcoesCorretivas.AsNoTracking().OrderByDescending(a => a.Numero).Take(limite).ToListAsync(ct);
    }

    /// <summary>Ação com os itens do plano e as RNCs ligadas a ela.</summary>
    public async Task<AcaoCorretivaAberta> AbrirAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var a = await db.AcoesCorretivas.AsNoTracking().Include(a => a.Itens).FirstAsync(a => a.Id == id, ct);
        var rncs = await db.Rncs.AsNoTracking().Where(r => r.AcaoCorretivaId == id).ToListAsync(ct);
        return new(a, rncs);
    }
}
