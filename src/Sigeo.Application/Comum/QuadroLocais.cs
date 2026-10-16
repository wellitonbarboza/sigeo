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

/// <summary>Monta e resume a quebra por local (sem depender da tela).</summary>
public static class QuadroLocais
{
    internal sealed record No(Guid Id, Guid? PaiId, string Tipo, string Nome, string Codigo, int Ordem);

    /// <summary>Quebra por local de cada item de requisição: item → linhas situadas na árvore da obra.</summary>
    public static async Task<Dictionary<Guid, List<LocalQuantidade>>> CarregarAsync(ISigeoDb db, IReadOnlyCollection<Guid> requisicaoItens, CancellationToken ct = default)
    {
        if (requisicaoItens.Count == 0) return [];
        var quebra = await db.RequisicaoItemLocais.AsNoTracking().Where(x => requisicaoItens.Contains(x.RequisicaoItemId))
                             .Select(x => new { x.RequisicaoItemId, x.LocalId, x.Quantidade }).ToListAsync(ct);
        if (quebra.Count == 0) return [];
        var locaisIds = quebra.Select(x => x.LocalId).Distinct().ToList();
        var obras = await db.Locais.AsNoTracking().Where(l => locaisIds.Contains(l.Id)).Select(l => l.ObraId).Distinct().ToListAsync(ct);
        var nos = await db.Locais.AsNoTracking().Where(l => obras.Contains(l.ObraId))
                          .Select(l => new No(l.Id, l.LocalPaiId, l.Tipo, l.Nome, l.Codigo, l.Ordem)).ToDictionaryAsync(n => n.Id, ct);
        return quebra.GroupBy(x => x.RequisicaoItemId)
                     .ToDictionary(g => g.Key, g => Montar(g.Select(x => (x.LocalId, x.Quantidade)), nos));
    }

    internal static List<LocalQuantidade> Montar(IEnumerable<(Guid LocalId, decimal Quantidade)> quebra, IReadOnlyDictionary<Guid, No> nos)
    {
        var r = new List<LocalQuantidade>();
        foreach (var (id, q) in quebra)
        {
            if (!nos.TryGetValue(id, out var l)) continue;
            No? torre = null, pav = null, apto = null;
            for (var n = l; n is not null; n = n.PaiId is { } p && nos.TryGetValue(p, out var pai) ? pai : null)
            {
                if (n.Tipo == GeradorEmpreendimento.TipoTorre) torre ??= n;
                else if (n.Tipo == GeradorEmpreendimento.TipoPavimento) pav ??= n;
                else if (n.Tipo == GeradorEmpreendimento.TipoApartamento) apto ??= n;
            }
            var comodo = l.Tipo is GeradorEmpreendimento.TipoComodo or GeradorEmpreendimento.TipoAreaComum ? l.Nome : null;
            r.Add(new LocalQuantidade(l.Codigo, l.Nome, l.Tipo, q, torre?.Nome, torre?.Ordem ?? 0, pav?.Nome, pav?.Ordem ?? 0, apto?.Nome, apto?.Ordem ?? 0, comodo));
        }
        return r.OrderBy(x => x.Codigo, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Caminho legível do local: "Torre 1 › 3º pavimento › Apto 302 › Sala de estar".</summary>
    public static string Caminho(LocalQuantidade l) =>
        string.Join(" › ", new[] { l.Torre, l.Pavimento, l.Apartamento, l.Comodo ?? (l.Apartamento is null && l.Pavimento is null && l.Torre is null ? l.Nome : null) }
            .Where(x => !string.IsNullOrEmpty(x))) is { Length: > 0 } d ? d : l.Nome;

    /// <summary>Resumo de uma linha: "Torre 1 · 4 pav. · 30 aptos · 4 cômodos/apto · 2 áreas comuns · total 7.546,5".</summary>
    public static string Resumo(IReadOnlyList<LocalQuantidade> linhas, string? unidade = null)
    {
        if (linhas.Count == 0) return "";
        var br = new System.Globalization.CultureInfo("pt-BR");
        var partes = new List<string>();
        var torres = linhas.Where(l => l.Torre is not null).Select(l => l.Torre!).Distinct().ToList();
        if (torres.Count == 1) partes.Add(torres[0]); else if (torres.Count > 1) partes.Add($"{torres.Count} torres");
        var pavs = linhas.Where(l => l.Pavimento is not null).Select(l => (l.Torre, l.PavimentoOrdem)).Distinct().Count();
        if (pavs > 0) partes.Add($"{pavs} pav.");
        var aptos = linhas.Where(l => l.Apartamento is not null).GroupBy(l => (l.Torre, l.PavimentoOrdem, l.Apartamento)).ToList();
        if (aptos.Count > 0)
        {
            partes.Add($"{aptos.Count} apto{(aptos.Count > 1 ? "s" : "")}");
            var comodos = aptos.Max(g => g.Count(l => l.Nivel == GeradorEmpreendimento.TipoComodo));
            if (comodos > 0) partes.Add($"{comodos} cômodo{(comodos > 1 ? "s" : "")}/apto");
        }
        var comuns = linhas.Count(l => l.Apartamento is null && l.Nivel == GeradorEmpreendimento.TipoAreaComum);
        if (comuns > 0) partes.Add($"{comuns} área{(comuns > 1 ? "s" : "")} comum{(comuns > 1 ? "s" : "")}");
        if (partes.Count == 0) partes.Add($"{linhas.Count} local(is)");
        partes.Add($"total {linhas.Sum(l => l.Quantidade).ToString("#,##0.###", br)}{(string.IsNullOrWhiteSpace(unidade) ? "" : " " + unidade)}");
        return string.Join(" · ", partes);
    }
}

/// <summary>Árvore de locais de uma obra já carregada na tela: situa a quebra por local de qualquer item sem ir ao banco.</summary>
public sealed class ArvoreLocais
{
    private readonly Dictionary<Guid, QuadroLocais.No> _nos;

    public ArvoreLocais(IEnumerable<Domain.Estrutura.Local> locais) =>
        _nos = locais.GroupBy(l => l.Id).ToDictionary(g => g.Key, g => { var l = g.First(); return new QuadroLocais.No(l.Id, l.LocalPaiId, l.Tipo, l.Nome, l.Codigo, l.Ordem); });

    public static readonly ArvoreLocais Vazia = new([]);

    /// <summary>Nome legível de cada local ("QA-T01-P03-302-SAL – Torre 1 › 3º pavimento › Apto 302 › Sala"); ordena pelo código.</summary>
    public Dictionary<Guid, string> Nomes() =>
        _nos.Keys.ToDictionary(id => id, id => Situar([(id, 0m)]) is [var l] ? $"{l.Codigo} – {QuadroLocais.Caminho(l)}" : "");

    public List<LocalQuantidade> Situar(IEnumerable<(Guid LocalId, decimal Quantidade)> quebra) => QuadroLocais.Montar(quebra, _nos);
}
