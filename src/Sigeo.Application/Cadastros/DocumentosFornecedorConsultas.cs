using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;

namespace Sigeo.Application.Cadastros;

/// <summary>Leituras da tela de documentos de fornecedor (Cadastros/DocumentosFornecedor).</summary>
public sealed class DocumentosFornecedorConsultas(IFabricaDb fabrica)
{
    /// <summary>Todos os documentos, com tipo e fornecedor.</summary>
    public async Task<List<FornecedorDocumento>> ListarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.FornecedorDocumentos.AsNoTracking().Include(d => d.TipoDocumento).Include(d => d.Fornecedor).ToListAsync(ct);
    }

    public async Task<FornecedorDocumento> ObterAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.FornecedorDocumentos.AsNoTracking().FirstAsync(d => d.Id == id, ct);
    }
}
