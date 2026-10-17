namespace Sigeo.Application.Abstracoes;

/// <summary>Armazenamento de arquivos. Produção: Azure Blob Storage (Managed Identity). Desenvolvimento: pasta local.</summary>
public interface IArmazenamento
{
    Task<ArquivoSalvo> SalvarAsync(Stream conteudo, string nomeOriginal, string pasta, string? contentType, CancellationToken ct = default);
    Task<Stream> AbrirAsync(string blob, CancellationToken ct = default);
}
