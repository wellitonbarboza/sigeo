using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela de cotações (só consulta; gravações ficam em CotacoesServico).</summary>
public sealed class CotacoesConsultas(IFabricaDb fabrica, FornecedoresServico forn)
{
    public async Task<ParametrosVigentes> ParametrosAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await ParametrosServico.CarregarAsync(db, ct);
    }

    public async Task<List<CotacaoLinha>> ListaAsync(int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Cotacoes.AsNoTracking().OrderByDescending(c => c.Numero).Take(limite)
            .Select(c => new CotacaoLinha(c.Id, c.Numero, c.Nome, db.Obras.Where(o => o.Id == c.ObraId).Select(o => o.Nome).FirstOrDefault() ?? "", c.Tipo,
                c.PropostasRecebidas, c.PropostasExigidas, c.ValorEstimado, c.FornecedorVencedor != null ? c.FornecedorVencedor.RazaoSocial : null, c.Status, c.CriadoPor, c.CriadoEm)).ToListAsync(ct);
    }

    /// <summary>Fornecedores para incluir na cotação (sugeridos pelos itens da requisição, quando houver).</summary>
    public async Task<FornecedoresSugeridos> FornecedoresParaAdicionarAsync(Guid? requisicao, TipoRequisicao tipo, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var itens = requisicao is { } rq ? await db.RequisicaoItens.AsNoTracking().Where(i => i.RequisicaoId == rq).ToListAsync(ct) : [];
        return await ConvitesSugeridos.ParaAsync(db, forn, tipo, itens, ct);
    }

    /// <summary>Cotação com fornecedores e itens, já recalculada com os parâmetros e com os preços de referência.</summary>
    public async Task<CotacaoCarregada> CotacaoAsync(Guid id, ParametrosVigentes parametros, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var c = await db.Cotacoes.AsNoTracking().Include(c => c.Fornecedores).Include(c => c.Itens).FirstAsync(c => c.Id == id, ct);
        var ids = c.Fornecedores.Select(f => f.FornecedorId).ToList();
        var nomes = await db.Fornecedores.AsNoTracking().Where(f => ids.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.NomeFantasia ?? f.RazaoSocial, ct);
        var rids = c.Itens.Where(i => i.RequisicaoItemId != null).Select(i => i.RequisicaoItemId!.Value).Distinct().ToList();
        var locaisItem = await QuadroLocais.CarregarAsync(db, rids, ct);
        c.Recalcular(parametros);
        await CotacoesServico.PrecosReferenciaAsync(db, c, ct);
        return new(c, nomes, locaisItem);
    }
}
