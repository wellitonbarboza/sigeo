using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;

namespace Sigeo.Application.Cadastros;

/// <summary>Leituras da matriz de documentos exigidos de fornecedor (Cadastros/MatrizDocumentos).</summary>
public sealed class MatrizDocumentosConsultas(IFabricaDb fabrica)
{
    public async Task<List<TipoDocumentoExigido>> TiposAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.TiposDocumento.AsNoTracking().Where(t => t.AplicaA == AplicaA.Fornecedor).OrderBy(t => t.Categoria).ThenBy(t => t.Ordem).ThenBy(t => t.Nome).ToListAsync(ct);
    }
}
