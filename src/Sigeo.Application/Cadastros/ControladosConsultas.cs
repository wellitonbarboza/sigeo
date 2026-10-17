using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;

namespace Sigeo.Application.Cadastros;

/// <summary>Leituras da tela de materiais e serviços controlados (Cadastros/Controlados).</summary>
public sealed class ControladosConsultas(IFabricaDb fabrica)
{
    /// <summary>Controlados, modelos FVM/FVS vigentes, produtos sem vínculo (até o limite + 1) e quantidade de produtos por material.</summary>
    public async Task<ControladosDados> CarregarAsync(int limiteSemVinculo, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var mats = await db.MateriaisControlados.OrderBy(m => m.Codigo).ToListAsync(ct);
        var srvs = await db.ServicosControlados.OrderBy(m => m.Codigo).ToListAsync(ct);
        var modelos = await db.ModelosInspecao.Where(m => m.Situacao == SituacaoModelo.Vigente)
            .Select(m => new { m.MaterialControladoId, m.ServicoControladoId, m.Tipo, m.Codigo, m.Revisao }).ToListAsync(ct);
        var fvm = modelos.Where(m => m.MaterialControladoId != null).GroupBy(m => m.MaterialControladoId!.Value).ToDictionary(g => g.Key, g => $"{g.First().Codigo} R{g.First().Revisao:00}");
        var fvs = modelos.Where(m => m.ServicoControladoId != null && m.Tipo == TipoModelo.FVS).GroupBy(m => m.ServicoControladoId!.Value).ToDictionary(g => g.Key, g => $"{g.First().Codigo} R{g.First().Revisao:00}");
        // Um a mais que o limite: só para saber se a lista foi cortada.
        var semVinculo = await db.Produtos.Where(p => p.Ativo && p.MaterialControladoId == null).OrderBy(p => p.Nome)
            .Select(p => new ProdutoSemVinculo(p.Codigo, p.Nome, p.Grupo, p.SubGrupo)).Take(limiteSemVinculo + 1).ToListAsync(ct);
        var qtdProd = await db.Produtos.Where(p => p.MaterialControladoId != null).GroupBy(p => p.MaterialControladoId!.Value).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);
        return new(mats, srvs, fvm, fvs, semVinculo, qtdProd);
    }
}
