using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Colaboracao;

/// <summary>Leituras dos componentes de anexos e fotos (Shared/Anexos, Fotos e FotosRegistro).</summary>
public sealed class AnexosConsultas(IFabricaDb fabrica)
{
    /// <summary>Anexos do registro na categoria, mais recentes primeiro.</summary>
    public async Task<List<AnexoLinha>> AnexosAsync(string entidade, Guid entidadeId, string categoria, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Anexos.Where(a => a.Entidade == entidade && a.EntidadeId == entidadeId && a.Categoria == categoria)
                              .OrderByDescending(a => a.CriadoEm)
                              .Select(a => new AnexoLinha(a.Id, a.NomeOriginal, a.Categoria, a.TamanhoBytes, a.ArquivoHashSha256, a.CriadoPor, a.CriadoEm))
                              .ToListAsync(ct);
    }

    /// <summary>Todos os arquivos do registro (qualquer categoria), na ordem de envio — usado pelas fotos de item.</summary>
    public async Task<List<FotoLinha>> FotosAsync(string entidade, Guid entidadeId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Anexos.Where(a => a.Entidade == entidade && a.EntidadeId == entidadeId).OrderBy(a => a.CriadoEm)
                              .Select(a => new FotoLinha(a.Id, a.NomeOriginal, a.CriadoPor, a.CriadoEm)).ToListAsync(ct);
    }

    /// <summary>Fotos (categoria "Foto") do registro, na ordem de envio.</summary>
    public async Task<List<FotoRegistro>> FotosRegistroAsync(string entidade, Guid entidadeId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Anexos.Where(a => a.Entidade == entidade && a.EntidadeId == entidadeId && a.Categoria == "Foto")
            .OrderBy(a => a.CriadoEm).Select(a => new FotoRegistro(a.Id, a.NomeOriginal)).ToListAsync(ct);
    }
}
