using System.Globalization;
using System.Text;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Fiscal;

/// <summary>
/// Regra de conferência nota × pedido sem IA: semelhança de palavras da descrição (sem acento, sem palavras curtas),
/// código do insumo igual, unidade igual e quantidade dentro do saldo. Cada item do pedido é usado uma vez.
/// </summary>
public static class ConferenciaNota
{
    public const decimal ConfiancaMinima = 0.35m;

    public static Dictionary<int, (Guid PedidoItemId, decimal Confianca)> Parear(IReadOnlyList<NotaFiscalItem> nota, IReadOnlyList<ItemPedidoParaConferencia> pedido)
    {
        var pares = new List<(int Item, Guid Ped, decimal Nota)>();
        foreach (var n in nota)
            foreach (var p in pedido)
            {
                var s = Semelhanca(n.Descricao, p.Descricao);
                if (!string.IsNullOrWhiteSpace(n.Codigo) && string.Equals(n.Codigo?.Trim(), p.Codigo?.Trim(), StringComparison.OrdinalIgnoreCase)) s = Math.Max(s, 0.9m);
                if (MesmaUnidade(n.Unidade, p.Unidade)) s += 0.1m;
                if (p.Saldo > 0 && n.Quantidade > p.Saldo * 1.05m) s -= 0.1m;
                if (s >= ConfiancaMinima) pares.Add((n.Item, p.Id, Math.Min(1, Math.Round(s, 2))));
            }
        var r = new Dictionary<int, (Guid, decimal)>();
        var usados = new HashSet<Guid>();
        foreach (var (item, ped, nota_) in pares.OrderByDescending(x => x.Nota))
            if (!r.ContainsKey(item) && usados.Add(ped)) r[item] = (ped, nota_);
        return r;
    }

    /// <summary>Coeficiente de Jaccard das palavras significativas (0–1).</summary>
    public static decimal Semelhanca(string? a, string? b)
    {
        var x = Palavras(a); var y = Palavras(b);
        if (x.Count == 0 || y.Count == 0) return 0;
        var inter = x.Count(y.Contains);
        return Math.Round((decimal)inter / (x.Count + y.Count - inter), 4);
    }

    public static bool MesmaUnidade(string? a, string? b) => !string.IsNullOrWhiteSpace(a) && Normalizar(a) == Normalizar(b);

    private static HashSet<string> Palavras(string? s) =>
        Normalizar(s).Split([' ', '-', '/', ',', '.', '(', ')', ';', ':', '"', '\''], StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length > 2 || p.Any(char.IsDigit)).ToHashSet();

    private static string Normalizar(string? s)
    {
        var d = (s ?? "").Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(char.ToLowerInvariant(c));
        return sb.ToString().Trim();
    }
}
