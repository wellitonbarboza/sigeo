using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela do mapa comparativo da cotação.</summary>
public sealed class MapaCotacaoConsultas(IFabricaDb fabrica)
{
    public async Task<ParametrosVigentes> ParametrosAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await ParametrosServico.CarregarAsync(db, ct);
    }

    /// <summary>Dados do mapa; null quando a cotação não existe (ou não é visível ao usuário).</summary>
    public async Task<MapaCotacaoDados?> MapaAsync(Guid id, ParametrosVigentes parametros, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var c = await db.Cotacoes.AsNoTracking().Include(c => c.Fornecedores).Include(c => c.Itens).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (c is null) return null;
        var obra = await db.Obras.Where(o => o.Id == c.ObraId).Select(o => o.Nome).FirstOrDefaultAsync(ct) ?? "";
        var ids = c.Fornecedores.Select(f => f.FornecedorId).ToList();
        var nomes = await db.Fornecedores.AsNoTracking().Where(f => ids.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.NomeFantasia ?? f.RazaoSocial, ct);
        var rids = c.Itens.Where(i => i.RequisicaoItemId != null).Select(i => i.RequisicaoItemId!.Value).Distinct().ToList();
        var locaisItem = await QuadroLocais.CarregarAsync(db, rids, ct);
        var pedidas = await db.Set<RequisicaoItem>().AsNoTracking().Where(r => rids.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => (r.Marca, r.MesmaMarca), ct);
        c.Recalcular(parametros);
        await CotacoesServico.PrecosReferenciaAsync(db, c, ct);
        return new(c, obra, nomes, locaisItem, pedidas);
    }
}
