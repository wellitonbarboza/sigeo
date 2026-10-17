using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela de requisições (só consulta; gravações ficam em RequisicoesServico).</summary>
public sealed class RequisicoesConsultas(IFabricaDb fabrica, FornecedoresServico forn)
{
    public async Task<Dictionary<Guid, string?>> MarcasDosProdutosAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Produtos.AsNoTracking().Where(p => p.Marca != null).ToDictionaryAsync(p => p.Id, p => p.Marca, ct);
    }

    public async Task<List<RequisicaoLinha>> ListaAsync(int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Requisicoes.AsNoTracking().OrderByDescending(r => r.Numero).Take(limite)
            .Select(r => new RequisicaoLinha(r.Id, r.Numero, db.Obras.Where(o => o.Id == r.ObraId).Select(o => o.Nome).FirstOrDefault() ?? "",
                r.Tipo, r.Atividade, r.CentroCusto != null ? r.CentroCusto.Nome : null, r.DataEntrega, r.Itens.Count, r.Status, r.CriadoPor, r.CriadoEm)).ToListAsync(ct);
    }

    public async Task<EstoqueRequisicao> EstoqueAsync(Guid obra, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var propria = await db.Obras.AsNoTracking().AnyAsync(o => o.Id == obra && o.DonoCusto == DonoCustoObra.Construtora, ct);
        var central = await db.Almoxarifados.AsNoTracking().FirstOrDefaultAsync(a => a.Tipo == TipoAlmoxarifado.Central && a.Ativo, ct);
        var almoxObra = await db.Almoxarifados.AsNoTracking().FirstOrDefaultAsync(a => a.ObraId == obra, ct);
        Dictionary<Guid, decimal> saldoObra = [], saldoCentral = [];
        if (almoxObra is { } ao) saldoObra = await db.SaldosEstoque.AsNoTracking().Where(x => x.AlmoxarifadoId == ao.Id && x.Disponivel > 0).ToDictionaryAsync(x => x.ProdutoId, x => x.Disponivel, ct);
        if (central is { } c) saldoCentral = await db.SaldosEstoque.AsNoTracking().Where(x => x.AlmoxarifadoId == c.Id && x.Disponivel > 0).ToDictionaryAsync(x => x.ProdutoId, x => x.Disponivel, ct);
        return new(propria, central, almoxObra, saldoObra, saldoCentral);
    }

    /// <summary>Requisição com itens e a quebra por local.</summary>
    public async Task<Requisicao> RequisicaoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Requisicoes.AsNoTracking().Include(r => r.Itens).ThenInclude(i => i.Locais).FirstAsync(r => r.Id == id, ct);
    }

    public async Task<List<Local>> LocaisDaObraAsync(Guid obra, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Locais.AsNoTracking().Where(l => l.ObraId == obra).ToListAsync(ct);
    }

    public async Task<ObraEntregaRequisicao?> ObraEntregaAsync(Guid obra, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Obras.AsNoTracking().Where(o => o.Id == obra).Select(o => new ObraEntregaRequisicao(o.EnderecoEntrega, o.DonoCusto, o.EstabelecimentoDonoId)).FirstOrDefaultAsync(ct);
    }

    /// <summary>Composições ativas usadas no orçamento da obra e os locais da obra.</summary>
    public async Task<OrcamentoRequisicao> OrcamentoAsync(Guid obra, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var usados = await db.ServicosLocais.AsNoTracking().Where(s => s.ObraId == obra).Select(s => s.ComposicaoId).Distinct().ToListAsync(ct);
        var comps = await db.Composicoes.AsNoTracking().Where(c => c.Ativa && usados.Contains(c.Id)).OrderBy(c => c.Grupo).ThenBy(c => c.Codigo)
            .Select(c => new ServicoOrcado(c.Id, c.Codigo, c.Descricao, c.Unidade, c.Grupo)).ToListAsync(ct);
        var locais = await db.Locais.AsNoTracking().Where(l => l.ObraId == obra).ToListAsync(ct);
        return new(usados.Count, comps, locais);
    }

    public async Task<Dictionary<Guid, Produto>> ProdutosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Produtos.AsNoTracking().Include(p => p.MaterialControlado).Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
    }

    public async Task<Produto> ProdutoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Produtos.AsNoTracking().Include(x => x.MaterialControlado).FirstAsync(x => x.Id == id, ct);
    }

    /// <summary>Fornecedores para convidar (sugeridos primeiro) e os lotes da requisição ainda em compra.</summary>
    public async Task<ConvitesRequisicao> ConvitesAsync(Guid requisicao, TipoRequisicao tipo, IEnumerable<RequisicaoItem> itens, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var (lista, sugeridos) = await ConvitesSugeridos.ParaAsync(db, forn, tipo, itens, ct);
        var lotes = await db.LotesCompra.AsNoTracking().Where(l => l.RequisicaoId == requisicao && l.Situacao == SituacaoLote.EmCompra).OrderBy(l => l.Letra).ToListAsync(ct);
        return new(lista, sugeridos, lotes);
    }
}
