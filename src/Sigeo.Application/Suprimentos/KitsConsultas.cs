using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela de kits de material (cadastro, separação e consumo).</summary>
public sealed class KitsConsultas(IFabricaDb fabrica)
{
    /// <summary>Kits gerais e da obra, e etapas (grupos das composições ativas) quando há obra.</summary>
    public async Task<KitsLista> ListaAsync(Guid? obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var kits = await db.KitsMaterial.AsNoTracking().Include(k => k.Itens).Where(k => k.ObraId == null || k.ObraId == obraId).OrderBy(k => k.Codigo).ToListAsync(ct);
        List<string> etapas = obraId is null ? [] : await db.Composicoes.AsNoTracking().Where(c => c.Ativa && c.Grupo != null && c.Grupo != "").Select(c => c.Grupo!).Distinct().OrderBy(g => g).ToListAsync(ct);
        return new(kits, etapas);
    }

    /// <summary>Textos dos compromissos da programação semanal da semana de <paramref name="hoje"/>.</summary>
    public async Task<List<string>> CompromissosDaSemanaAsync(Guid? obraId, DateOnly hoje, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var seg = hoje.AddDays(-(((int)hoje.DayOfWeek + 6) % 7));
        return await db.PlanosSemanais.AsNoTracking().Where(p => p.ObraId == obraId && p.Semana >= seg.AddDays(-6) && p.Semana <= seg)
            .SelectMany(p => p.Compromissos).Select(c => c.Descricao + " " + c.Meta).ToListAsync(ct);
    }

    /// <summary>
    /// Saldos dos produtos no almoxarifado da separação e no central (só quando <paramref name="centralId"/> é outro almoxarifado),
    /// e os locais marcados que já receberam o kit.
    /// </summary>
    public async Task<KitSaldos> SaldosSeparacaoAsync(List<Guid> ids, Guid? almoxSep, Guid? centralId, Guid? kitSep, List<Guid> marcados, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var saldos = await db.SaldosEstoque.AsNoTracking().Where(s => s.AlmoxarifadoId == almoxSep && ids.Contains(s.ProdutoId)).ToDictionaryAsync(s => s.ProdutoId, s => s.Disponivel, ct);
        Dictionary<Guid, decimal> central = centralId is { } c
            ? await db.SaldosEstoque.AsNoTracking().Where(s => s.AlmoxarifadoId == c && ids.Contains(s.ProdutoId)).ToDictionaryAsync(s => s.ProdutoId, s => s.Disponivel, ct)
            : [];
        var entregues = (await db.MovimentosEstoque.AsNoTracking().Where(m => m.KitId == kitSep && m.LocalId != null && marcados.Contains(m.LocalId.Value)).Select(m => m.LocalId!.Value).Distinct().ToListAsync(ct)).ToHashSet();
        return new(saldos, central, entregues);
    }

    /// <summary>Locais que já receberam o kit (quando nenhum foi marcado).</summary>
    public async Task<List<Guid>> LocaisComEntregaAsync(Guid? kitId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.MovimentosEstoque.AsNoTracking().Where(m => m.KitId == kitId && m.LocalId != null).Select(m => m.LocalId!.Value).Distinct().ToListAsync(ct);
    }
}
