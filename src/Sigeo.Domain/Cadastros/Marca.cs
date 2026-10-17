using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Cadastros;

/// <summary>
/// Marca (fabricante comercial). Opcional na requisição; obrigatória do item cotado em diante (cotação, pedido, recebimento),
/// para comparar preço por especificação + marca.
/// </summary>
public class Marca : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    public string? Fabricante { get; set; }
    public string? Observacao { get; set; }
    public bool Ativa { get; set; } = true;

    /// <summary>Chave de comparação: sem acento, maiúsculas, espaços simples.</summary>
    public static string Chave(string? nome) =>
        string.Join(' ', new string((nome ?? "").Normalize(System.Text.NormalizationForm.FormD)
            .Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray())
            .ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
