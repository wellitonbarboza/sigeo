using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Sst;

namespace Sigeo.Application.Sst;

/// <summary>Leituras da tela de documentos de colaborador (Sst/DocumentosColaborador).</summary>
public sealed class DocumentosColaboradorConsultas(IFabricaDb fabrica)
{
    public async Task<List<Exigencia>> ExigenciasAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Exigencias.AsNoTracking().OrderBy(e => e.Ordem).ToListAsync(ct);
    }

    /// <summary>Documentos (mais recentes primeiro; com colaborador, só os dele), com nomes de fornecedores e colaboradores para a grade.</summary>
    public async Task<DocumentosColaboradorDados> ListarAsync(Guid? colaborador, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var forn = await db.Fornecedores.AsNoTracking().ToDictionaryAsync(f => f.Id, f => f.NomeExibicao, ct);
        var cols = await db.Colaboradores.AsNoTracking().Select(c => new ColaboradorResumo(c.Id, c.Nome, c.FornecedorId)).ToDictionaryAsync(c => c.Id, ct);
        var q = db.DocumentosColaborador.AsNoTracking();
        if (colaborador is { } fc) q = q.Where(d => d.ColaboradorId == fc);
        var docs = await q.OrderByDescending(d => d.CriadoEm).ToListAsync(ct);
        return new(forn, cols, docs);
    }

    public async Task<DocumentoColaborador> ObterAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.DocumentosColaborador.AsNoTracking().FirstAsync(x => x.Id == id, ct);
    }
}
