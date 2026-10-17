namespace Sigeo.Application.Abstracoes;

/// <summary>Arquivo salvo: caminho no armazenamento + SHA-256 (prova de integridade).</summary>
public sealed record ArquivoSalvo(string Blob, string Hash, long Tamanho, string NomeOriginal, string? ContentType);
