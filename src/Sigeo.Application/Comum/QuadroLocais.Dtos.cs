using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Abstracoes;
using Sigeo.Domain.Engenharia;

namespace Sigeo.Application.Comum;

/// <summary>
/// Uma linha da quebra por local de um item, já situada na árvore da obra (torre › pavimento › apartamento › cômodo).
/// Base do quadro visual dos locais (mapa da torre, por cômodo, lista) na cotação e no portal do fornecedor.
/// </summary>
public sealed record LocalQuantidade(
    string Codigo, string Nome, string Nivel, decimal Quantidade,
    string? Torre, int TorreOrdem, string? Pavimento, int PavimentoOrdem,
    string? Apartamento, int ApartamentoOrdem, string? Comodo)
{
    /// <summary>Número do apartamento sem o prefixo ("Apto 302" → "302").</summary>
    public string? NumeroApartamento => Apartamento?.Replace("Apto", "", StringComparison.OrdinalIgnoreCase).Trim();
}
