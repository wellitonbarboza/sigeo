namespace Groundkey.Application.Abstracoes;

/// <summary>Arquivo salvo: caminho no armazenamento + SHA-256 (prova de integridade).</summary>
public sealed record ArquivoSalvo(string Blob, string Hash, long Tamanho, string NomeOriginal, string? ContentType);

/// <summary>Armazenamento de arquivos. Produção: Azure Blob Storage (Managed Identity). Desenvolvimento: pasta local.</summary>
public interface IArmazenamento
{
    Task<ArquivoSalvo> SalvarAsync(Stream conteudo, string nomeOriginal, string pasta, string? contentType, CancellationToken ct = default);
    Task<Stream> AbrirAsync(string blob, CancellationToken ct = default);
}
