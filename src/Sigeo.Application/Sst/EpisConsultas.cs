using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Sst;

/// <summary>Leituras da tela de EPI (Sst/Epis).</summary>
public sealed class EpisConsultas(IFabricaDb fabrica)
{
    /// <summary>Dias de antecedência do alerta de CA vencendo (parâmetro).</summary>
    public async Task<int> AlertaCaAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return (await ParametrosServico.CarregarAsync(db, ct)).Int(Parametros.AlertaCaDias);
    }

    /// <summary>Catálogo de EPI ativo e o resumo da ficha de cada colaborador sem saída.</summary>
    public async Task<EpisDados> CarregarAsync(DateOnly hoje, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var epis = await db.Epis.AsNoTracking().Where(e => e.Ativo).OrderBy(e => e.Descricao).ToListAsync(ct);
        var forn = await db.Fornecedores.AsNoTracking().ToDictionaryAsync(f => f.Id, f => f.NomeExibicao, ct);
        var cols = await db.Colaboradores.AsNoTracking().Where(c => c.DataSaida == null).OrderBy(c => c.Nome)
            .Select(c => new { c.Id, c.Nome, c.FornecedorId }).ToListAsync(ct);
        // Só os campos usados no resumo: a tabela de entregas cresce a cada troca de EPI.
        var ent = await db.EntregasEpi.AsNoTracking().Select(x => new { x.ColaboradorId, x.EpiId, x.DataEntrega, x.TrocaPrevista }).ToListAsync(ct);
        var linhas = cols.Select(c =>
        {
            var e = ent.Where(x => x.ColaboradorId == c.Id).ToList();
            var caV = e.Select(x => epis.FirstOrDefault(p => p.Id == x.EpiId)).Where(p => p is not null && p.ValidadeCa < hoje).Select(p => p!.Descricao).FirstOrDefault();
            var troca = e.Where(x => x.TrocaPrevista is not null).Select(x => x.TrocaPrevista).Min();
            var sit = e.Count == 0 ? "Sem entrega" : caV is not null ? "CA vencido" : troca <= hoje ? "Troca hoje" : "Em dia";
            return new EpiFichaLinha(c.Id, c.Nome, c.FornecedorId is { } f ? forn.GetValueOrDefault(f) : "Própria", e.Select(x => (DateOnly?)x.DataEntrega).Max(), troca, caV, sit);
        }).ToList();
        return new(epis, linhas);
    }

    /// <summary>Colaborador e suas entregas de EPI, mais recentes primeiro.</summary>
    public async Task<EpiFicha> FichaAsync(Guid colaborador, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var colab = await db.Colaboradores.AsNoTracking().FirstAsync(c => c.Id == colaborador, ct);
        var entregas = await db.EntregasEpi.AsNoTracking().Where(e => e.ColaboradorId == colaborador).OrderByDescending(e => e.DataEntrega).ToListAsync(ct);
        return new(colab, entregas);
    }
}
