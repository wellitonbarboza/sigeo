namespace Sigeo.Application.Cadastros;

/// <summary>Linha da grade de marcas.</summary>
public sealed record MarcaLinha(Guid Id, string Nome, string? Fabricante, string? Observacao, bool Ativa, bool Automatica, int Cotados, int Comprados);
