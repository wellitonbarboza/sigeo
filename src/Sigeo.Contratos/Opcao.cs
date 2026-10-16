namespace Sigeo.Contratos;

/// <summary>Item de lista de seleção (id + texto + dado extra, ex.: código ou obra da SPE).</summary>
public sealed record Opcao(Guid Id, string Texto, string? Extra = null);
