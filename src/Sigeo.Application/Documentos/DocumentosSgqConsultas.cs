using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Documentos;

namespace Sigeo.Application.Documentos;

/// <summary>Leituras da tela de documentos do SGQ (pastas, documentos e ficha do documento), só leitura.</summary>
public sealed class DocumentosSgqConsultas(IFabricaDb fabrica, DocumentosServico docs)
{
    /// <summary>Regras de retenção para o seletor do documento.</summary>
    public async Task<List<Opcao>> RetencoesAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.RegrasRetencao.OrderBy(r => r.Identificacao).Select(r => new Opcao(r.Id, r.Identificacao + " – " + r.TempoRetencao)).ToListAsync(ct);
    }

    /// <summary>Pastas, documentos com revisões (por código) e quantas cópias aguardam recolhimento.</summary>
    public async Task<DocumentosSgqLista> ListarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var pastas = await db.Pastas.ToListAsync(ct);
        var documentos = await db.Documentos.Include(d => d.Revisoes).OrderBy(d => d.Codigo).ToListAsync(ct);
        var aRecolher = await db.Copias.CountAsync(c => c.Status == StatusCopia.RecolhimentoPendente, ct);
        return new(pastas, documentos, aRecolher);
    }

    /// <summary>Ficha do documento: revisões (etapas e cópias), permissão do usuário, checkout da revisão em andamento, leituras e log.</summary>
    public async Task<DocumentoSgqAberto> AbrirAsync(Guid id, int limiteEventos, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var d = await db.Documentos.Include(d => d.Revisoes).ThenInclude(r => r.Etapas).Include(d => d.Revisoes).ThenInclude(r => r.Copias).AsSplitQuery().FirstAsync(d => d.Id == id, ct);
        var emAndamento = d.Revisoes.FirstOrDefault(r => r.Status is < StatusRevisao.Vigente and not StatusRevisao.Recusada);
        var perm = await docs.PermissaoAsync(db, d, ct);
        var revIds = d.Revisoes.Select(r => r.Id).ToList();
        var checkout = emAndamento is null ? null : await db.Checkouts.FirstOrDefaultAsync(c => c.DocumentoRevisaoId == emAndamento.Id && c.CheckinEm == null && c.LiberadoForcadoPor == null, ct);
        var ciencias = await db.Ciencias.Where(c => revIds.Contains(c.DocumentoRevisaoId)).OrderBy(c => c.UsuarioNome).ToListAsync(ct);
        var eventos = await db.EventosDocumento.Where(e => e.DocumentoId == id).OrderByDescending(e => e.DataHora).Take(limiteEventos).ToListAsync(ct);
        return new(d, emAndamento, perm, checkout, ciencias, eventos);
    }
}
