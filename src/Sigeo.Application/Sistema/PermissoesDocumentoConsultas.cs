using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Sistema;

/// <summary>Leituras da tela de permissões de documentos do SGQ (Sistema/Permissoes).</summary>
public sealed class PermissoesDocumentoConsultas(IFabricaDb fabrica)
{
    /// <summary>Pastas e documentos (para os alvos) e as regras de permissão.</summary>
    public async Task<PermissoesDocumentoDados> CarregarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var pastas = await db.Pastas.OrderBy(p => p.Ordem).Select(p => new Opcao(p.Id, p.Nome)).ToListAsync(ct);
        var docs = await db.Documentos.OrderBy(d => d.Codigo).Select(d => new Opcao(d.Id, d.Codigo + " – " + d.Titulo)).ToListAsync(ct);
        var regras = await db.Permissoes.ToListAsync(ct);
        return new(pastas, docs, regras);
    }
}
