namespace Sigeo.Application.Colaboracao;

/// <summary>Linha da lista de anexos de um registro.</summary>
public sealed record AnexoLinha(Guid Id, string NomeOriginal, string Categoria, long TamanhoBytes, string Hash, string CriadoPor, DateTimeOffset CriadoEm);

/// <summary>Foto de um item (ex.: item de FVS).</summary>
public sealed record FotoLinha(Guid Id, string NomeOriginal, string CriadoPor, DateTimeOffset CriadoEm);

/// <summary>Foto de um registro (miniatura).</summary>
public sealed record FotoRegistro(Guid Id, string Nome);
