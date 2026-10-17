using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela de economia (o painel vem de EconomiaServico).</summary>
public sealed class EconomiaConsultas(IFabricaDb fabrica)
{
    public async Task<ParametrosVigentes> ParametrosAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await ParametrosServico.CarregarAsync(db, ct);
    }
}
