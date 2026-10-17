using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Colaboracao;

/// <summary>Leituras da tela de metas (Colaboracao/Metas) que não vêm do MetasServico.</summary>
public sealed class MetasConsultas(IFabricaDb fabrica)
{
    /// <summary>Equipes ativas, por nome, para o seletor.</summary>
    public async Task<List<Opcao>> EquipesAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Equipes.AsNoTracking().Where(e => e.Ativa).OrderBy(e => e.Nome).Select(e => new Opcao(e.Id, e.Nome, null)).ToListAsync(ct);
    }
}
