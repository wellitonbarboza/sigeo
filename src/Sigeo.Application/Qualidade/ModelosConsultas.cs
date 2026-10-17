using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Qualidade;

/// <summary>Leituras da tela de modelos de inspeção (FVS, FVM e checklists de segurança), só leitura.</summary>
public sealed class ModelosConsultas(IFabricaDb fabrica)
{
    /// <summary>Modelos do tipo, com itens e ensaios, por código.</summary>
    public async Task<List<ModeloInspecao>> ListarAsync(TipoModelo tipo, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.ModelosInspecao.AsNoTracking().Include(m => m.Itens).Include(m => m.Ensaios).Where(m => m.Tipo == tipo).OrderBy(m => m.Codigo).ToListAsync(ct);
    }

    public async Task<ModeloInspecao> AbrirAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.ModelosInspecao.AsNoTracking().Include(x => x.Itens).Include(x => x.Ensaios).FirstAsync(x => x.Id == id, ct);
    }
}
