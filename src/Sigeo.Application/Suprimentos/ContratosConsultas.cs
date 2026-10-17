using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela de contratos (lista, abertura do contrato e dos aditivos).</summary>
public sealed class ContratosConsultas(IFabricaDb fabrica)
{
    /// <summary>Lista dos contratos, mais recentes primeiro, até o limite da grade.</summary>
    public async Task<List<ContratoLinha>> ListaAsync(int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var lista = await db.Contratos.AsNoTracking().Include(k => k.Itens).Include(k => k.Fornecedor).OrderByDescending(k => k.CriadoEm).Take(limite).ToListAsync(ct);
        var obras = await db.Obras.AsNoTracking().ToDictionaryAsync(o => o.Id, o => o.Nome, ct);
        return lista.Select(k => new ContratoLinha(k.Id, k.Numero, obras.GetValueOrDefault(k.ObraId, ""), k.Fornecedor.NomeExibicao, k.Categoria, k.Objeto, k.ValorTotal, k.VigenciaFim, k.Status, k.CriadoPor)).ToList();
    }

    /// <summary>Contrato do aditivo (Guid.Empty quando o aditivo não existe).</summary>
    public async Task<Guid> ContratoDoAditivoAsync(Guid aditivoId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Aditivos.Where(a => a.Id == aditivoId).Select(a => a.ContratoId).FirstOrDefaultAsync(ct);
    }

    /// <summary>Contrato aberto com itens, locais da obra, medições, aditivos e limite de aditivo (%).</summary>
    public async Task<ContratoAberto> AbrirAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var k = await db.Contratos.AsNoTracking().Include(k => k.Itens).ThenInclude(i => i.Locais).Include(k => k.ServicosVinculados).FirstAsync(k => k.Id == id, ct);
        var locs = await db.Locais.AsNoTracking().Where(l => l.ObraId == k.ObraId).ToListAsync(ct);
        var medicoes = await db.Medicoes.AsNoTracking().Where(m => m.ContratoId == id).OrderBy(m => m.NumeroMedicao).ToListAsync(ct);
        var aditivos = await db.Aditivos.AsNoTracking().Where(a => a.ContratoId == id).OrderBy(a => a.Numero).ToListAsync(ct);
        var limite = (await ParametrosServico.CarregarAsync(db, ct))[Parametros.LimiteAditivoPerc];
        return new(k, locs, medicoes, aditivos, limite);
    }

    /// <summary>Aditivo com itens e a lista de aditivos do mesmo contrato.</summary>
    public async Task<AditivoAberto> AditivoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var ad = await db.Aditivos.AsNoTracking().Include(a => a.Itens).FirstAsync(a => a.Id == id, ct);
        var aditivos = await db.Aditivos.AsNoTracking().Where(a => a.ContratoId == ad.ContratoId).OrderBy(a => a.Numero).ToListAsync(ct);
        return new(ad, aditivos);
    }
}
