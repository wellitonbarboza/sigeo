using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Qualidade;

/// <summary>Leituras da FVS no celular (tela de campo), só leitura.</summary>
public sealed class FvsCampoConsultas(IFabricaDb fabrica)
{
    /// <summary>Equipamentos de medição usáveis na data.</summary>
    public async Task<List<Opcao>> EquipamentosAsync(DateOnly hoje, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return (await db.Equipamentos.AsNoTracking().ToListAsync(ct)).Where(e => e.UsavelEm(hoje)).Select(e => new Opcao(e.Id, $"{e.Codigo} – {e.Descricao}")).ToList();
    }

    /// <summary>FVS com itens, modelo e local, mais a razão social do prestador.</summary>
    public async Task<FvsCampoDados> FvsAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var f = await db.Fvss.AsNoTracking().Include(f => f.Itens).Include(f => f.Modelo).Include(f => f.Local).FirstAsync(f => f.Id == id, ct);
        var prestador = await db.Fornecedores.Where(x => x.Id == f.FornecedorId).Select(x => x.RazaoSocial).FirstOrDefaultAsync(ct);
        return new(f, prestador);
    }
}
