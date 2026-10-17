using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Cadastros;

/// <summary>Leituras da tela de marcas (Cadastros/Marcas).</summary>
public sealed class MarcasConsultas(IFabricaDb fabrica)
{
    /// <summary>Marcas com quantos itens cotados e comprados citam cada uma (pela chave normalizada do nome).</summary>
    public async Task<List<MarcaLinha>> ListarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var marcas = await db.Marcas.AsNoTracking().OrderBy(m => m.Nome).ToListAsync(ct);
        var cot = (await db.Set<CotacaoItem>().AsNoTracking().Where(i => i.Marca != null && i.ValorOfertado > 0).Select(i => i.Marca!).ToListAsync(ct))
            .GroupBy(Marca.Chave).ToDictionary(g => g.Key, g => g.Count());
        var com = (await db.PedidoItens.AsNoTracking().Where(i => i.Marca != null).Select(i => i.Marca!).ToListAsync(ct))
            .GroupBy(Marca.Chave).ToDictionary(g => g.Key, g => g.Count());
        return marcas.Select(m => new MarcaLinha(m.Id, m.Nome, m.Fabricante, m.Observacao, m.Ativa, m.Observacao?.StartsWith("Cadastrada automaticamente") == true,
            cot.GetValueOrDefault(Marca.Chave(m.Nome)), com.GetValueOrDefault(Marca.Chave(m.Nome)))).ToList();
    }

    public async Task<Marca> ObterAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Marcas.AsNoTracking().FirstAsync(m => m.Id == id, ct);
    }
}
