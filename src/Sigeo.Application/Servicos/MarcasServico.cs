using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Application.Servicos;

/// <summary>Cadastro de marcas e o histórico de compra por marca (comparar preço por especificação + marca).</summary>
public sealed class MarcasServico(IFabricaDb fabrica)
{
    public async Task SalvarAsync(Marca m, CancellationToken ct = default)
    {
        m.Nome = string.Join(' ', (m.Nome ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(m.Nome)) throw new RegraDeNegocioException("Informe o nome da marca.");
        await using var db = fabrica.Criar();
        var chave = Marca.Chave(m.Nome);
        var nomes = await db.Marcas.AsNoTracking().Where(x => x.Id != m.Id).Select(x => x.Nome).ToListAsync(ct);
        if (nomes.Any(n => Marca.Chave(n) == chave)) throw new RegraDeNegocioException($"Marca {m.Nome} já cadastrada.");
        await Grafo.SalvarAsync(db, m);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Troca cada nome digitado pelo nome do cadastro (mesma grafia em todo lugar) e cadastra as marcas novas.
    /// Devolve chave → nome oficial. Chamar antes do SaveChanges do documento.
    /// </summary>
    public static async Task<Dictionary<string, string>> NormalizarAsync(ISigeoDb db, IEnumerable<string?> nomes, CancellationToken ct = default)
    {
        var digitados = nomes.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => string.Join(' ', n!.Split(' ', StringSplitOptions.RemoveEmptyEntries))).ToList();
        var r = new Dictionary<string, string>();
        if (digitados.Count == 0) return r;
        foreach (var m in await db.Marcas.AsNoTracking().Select(x => x.Nome).ToListAsync(ct)) r.TryAdd(Marca.Chave(m), m);
        foreach (var n in digitados)
        {
            var k = Marca.Chave(n);
            if (r.ContainsKey(k)) continue;
            db.Marcas.Add(new Marca { Nome = n.Length > 120 ? n[..120] : n, Observacao = "Cadastrada automaticamente na cotação/compra" });
            r[k] = n;
        }
        return r;
    }

    public static string? Oficial(Dictionary<string, string> mapa, string? nome) =>
        string.IsNullOrWhiteSpace(nome) ? null : mapa.TryGetValue(Marca.Chave(nome), out var o) ? o : nome.Trim();

    public sealed record CompraPorMarca(string Marca, int Pedidos, decimal Quantidade, decimal PrecoMedio, decimal UltimoPreco, DateTimeOffset UltimaCompra, decimal? MenorCotado);

    /// <summary>Compras do produto agrupadas por marca: preço médio, último preço e o menor preço cotado da marca.</summary>
    public async Task<List<CompraPorMarca>> ComprasPorMarcaAsync(Guid produtoId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var compras = await (from pi in db.PedidoItens
                             join p in db.Pedidos on pi.PedidoId equals p.Id
                             where pi.ProdutoId == produtoId && p.Status != StatusPedido.Cancelado && pi.ValorUnitario > 0
                             select new { Marca = pi.Marca ?? "(sem marca)", pi.Quantidade, pi.ValorUnitario, p.CriadoEm }).ToListAsync(ct);
        var cotados = await (from ci in db.Set<Domain.Suprimentos.CotacaoItem>()
                             join ri in db.Set<Domain.Suprimentos.RequisicaoItem>() on ci.RequisicaoItemId equals ri.Id
                             where ri.ProdutoId == produtoId && ci.ValorOfertado > 0 && ci.Marca != null
                             select new { ci.Marca, Valor = ci.ValorNegociado ?? ci.ValorOfertado }).ToListAsync(ct);
        var menor = cotados.GroupBy(x => Marca.Chave(x.Marca)).ToDictionary(g => g.Key, g => g.Min(x => x.Valor));
        return compras.GroupBy(x => Marca.Chave(x.Marca)).Select(g =>
        {
            var q = g.Sum(x => x.Quantidade);
            var ult = g.OrderByDescending(x => x.CriadoEm).First();
            return new CompraPorMarca(g.First().Marca, g.Count(), q, q == 0 ? 0 : Math.Round(g.Sum(x => x.Quantidade * x.ValorUnitario) / q, 4), ult.ValorUnitario, ult.CriadoEm,
                menor.TryGetValue(g.Key, out var m) ? m : null);
        }).OrderBy(x => x.PrecoMedio).ToList();
    }
}
