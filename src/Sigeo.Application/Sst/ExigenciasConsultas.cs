using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Sst;

/// <summary>Leituras da matriz função × exigência (Sst/Exigencias).</summary>
public sealed class ExigenciasConsultas(IFabricaDb fabrica)
{
    public async Task<ExigenciasDados> CarregarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var funcoes = await db.Funcoes.AsNoTracking().Include(f => f.Exigencias).Where(f => f.Ativa).OrderBy(f => f.Descricao).ToListAsync(ct);
        var exig = await db.Exigencias.AsNoTracking().OrderBy(e => e.Ordem).ToListAsync(ct);
        var treinamentos = await db.Treinamentos.AsNoTracking().Select(t => new Opcao(t.Id, t.Descricao)).ToListAsync(ct);
        return new(funcoes, exig, treinamentos);
    }
}
