using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Cadastros;

/// <summary>Leituras da tela de estabelecimentos (Cadastros/Estabelecimentos).</summary>
public sealed class EstabelecimentosConsultas(IFabricaDb fabrica)
{
    public async Task<EstabelecimentosDados> CarregarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var lista = await db.Estabelecimentos.AsNoTracking().ToListAsync(ct);
        var fluxos = await db.FluxosEstabelecimento.AsNoTracking().ToListAsync(ct);
        return new(lista, fluxos);
    }
}
