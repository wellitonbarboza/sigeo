using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Sst;

namespace Sigeo.Application.Sst;

/// <summary>Leituras da tela de colaboradores (Sst/Colaboradores).</summary>
public sealed class ColaboradoresConsultas(IFabricaDb fabrica)
{
    public async Task<List<Opcao>> ExigenciasAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Exigencias.AsNoTracking().Select(e => new Opcao(e.Id, e.Descricao)).ToListAsync(ct);
    }

    /// <summary>Colaboradores ativos em <paramref name="hoje"/> e todos (ambos por nome).</summary>
    public async Task<ColaboradoresLista> ListarAsync(DateOnly hoje, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var ativos = await db.Colaboradores.AsNoTracking().Where(c => c.DataSaida == null || c.DataSaida > hoje).OrderBy(c => c.Nome).ToListAsync(ct);
        var todos = await db.Colaboradores.AsNoTracking().OrderBy(c => c.Nome).ToListAsync(ct);
        return new(ativos, todos);
    }

    /// <summary>Ficha do colaborador: cadastro, documentos, treinamentos e entregas de EPI.</summary>
    public async Task<ColaboradorFicha> AbrirAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var c = await db.Colaboradores.AsNoTracking().FirstAsync(c => c.Id == id, ct);
        var docs = await db.DocumentosColaborador.AsNoTracking().Where(d => d.ColaboradorId == id).OrderByDescending(d => d.DataEmissao).ToListAsync(ct);
        var treins = (await db.Participantes.AsNoTracking().Where(p => p.ColaboradorId == id)
            .Join(db.Turmas, p => p.TurmaId, t => t.Id, (p, t) => new { p, t })
            .Join(db.Treinamentos, x => x.t.TreinamentoId, tr => tr.Id, (x, tr) => new { tr.Descricao, x.t.Codigo, x.t.Data, x.p.Resultado, x.p.ValidoAte }).ToListAsync(ct))
            .Select(x => (x.Descricao, x.Codigo, x.Data, x.Resultado, x.ValidoAte)).ToList();
        var epis = (await db.EntregasEpi.AsNoTracking().Where(e => e.ColaboradorId == id).Join(db.Epis, e => e.EpiId, p => p.Id, (e, p) => new { p.Descricao, p.Ca, e.Quantidade, e.DataEntrega, e.TrocaPrevista, e.Assinatura }).ToListAsync(ct))
            .Select(x => ($"{x.Descricao} (CA {x.Ca})", x.Quantidade, x.DataEntrega, x.TrocaPrevista, x.Assinatura)).ToList();
        return new(c, docs, treins, epis);
    }
}
