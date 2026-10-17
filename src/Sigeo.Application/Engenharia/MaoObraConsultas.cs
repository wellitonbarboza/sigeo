using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;

namespace Sigeo.Application.Engenharia;

/// <summary>Leituras da tela de mão de obra (equipes, custos do mês, apropriações, locais e composições).</summary>
public sealed class MaoObraConsultas(IFabricaDb fabrica)
{
    /// <summary>Dados da obra na competência <paramref name="mes"/> (primeiro dia do mês).</summary>
    public async Task<MaoObraDados> CarregarAsync(Guid obra, DateOnly mes, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var equipes = await db.Equipes.AsNoTracking().Include(e => e.Membros).Where(e => e.ObraId == obra).OrderBy(e => e.Codigo).ToListAsync(ct);
        var colabs = await db.Colaboradores.AsNoTracking().Where(c => c.ObraId == obra && c.Vinculo == VinculoColaborador.Proprio && c.DataSaida == null).OrderBy(c => c.Nome).Select(c => new Opcao(c.Id, c.Nome)).ToListAsync(ct);
        var custos = await db.CustosMaoObra.AsNoTracking().Where(c => c.ObraId == obra && c.Competencia == mes).ToListAsync(ct);
        var fim = mes.AddMonths(1);
        var aps = await db.Apropriacoes.AsNoTracking().Where(a => a.ObraId == obra && a.Data >= mes && a.Data < fim).ToListAsync(ct);
        var locais = await db.Locais.AsNoTracking().Where(l => l.ObraId == obra).ToListAsync(ct);
        var usados = await db.ServicosLocais.AsNoTracking().Where(s => s.ObraId == obra).Select(s => s.ComposicaoId).Distinct().ToListAsync(ct);
        var comps = await db.Composicoes.AsNoTracking().Include(c => c.Itens).ToDictionaryAsync(c => c.Id, ct);
        return new(equipes, colabs, custos, aps, locais, usados, comps);
    }
}
