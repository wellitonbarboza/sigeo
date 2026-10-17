using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras do quadro de fornecedores da cotação na tela da requisição.</summary>
public sealed class FornecedoresCotacaoConsultas(IFabricaDb fabrica, FornecedoresServico forn)
{
    /// <summary>Cotação mais recente (não cancelada) da requisição.</summary>
    public async Task<Guid?> CotacaoDaRequisicaoAsync(Guid requisicao, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Cotacoes.Where(c => c.RequisicaoId == requisicao && c.Status != StatusCotacao.Cancelado).OrderByDescending(c => c.Numero).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct);
    }

    public async Task<CotacaoComNomes> CotacaoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var c = await db.Cotacoes.AsNoTracking().Include(c => c.Fornecedores).Include(c => c.Itens).FirstAsync(c => c.Id == id, ct);
        var ids = c.Fornecedores.Select(f => f.FornecedorId).ToList();
        var nomes = await db.Fornecedores.AsNoTracking().Where(f => ids.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.NomeFantasia ?? f.RazaoSocial, ct);
        return new(c, nomes);
    }

    /// <summary>Fornecedores para incluir na cotação, sugeridos pelos itens da requisição.</summary>
    public async Task<FornecedoresSugeridos> ParaAdicionarAsync(Guid requisicao, TipoRequisicao tipo, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var itens = await db.RequisicaoItens.AsNoTracking().Where(i => i.RequisicaoId == requisicao).ToListAsync(ct);
        return await ConvitesSugeridos.ParaAsync(db, forn, tipo, itens, ct);
    }
}
