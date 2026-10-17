using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Comum;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.PortalFornecedor;

// Propostas
public sealed partial class PortalServico
{
    public static bool CotacaoAberta(Cotacao c) => c.Status is StatusCotacao.Aberto or StatusCotacao.AguardandoPropostas or StatusCotacao.EmAnalise or StatusCotacao.ReprovadoVoltaCotacao;

    private async Task<(Guid EmpresaId, Guid CotacaoId)?> LocalizarConviteAsync(Guid token, CancellationToken ct)
    {
        await using var busca = sistema.Criar(Guid.Empty, "PortalCotacao");
        var r = await (from cf in busca.Set<CotacaoFornecedor>().IgnoreQueryFilters().AsNoTracking()
                       join c in busca.Cotacoes.IgnoreQueryFilters().AsNoTracking() on cf.CotacaoId equals c.Id
                       where cf.LinkResposta == token && !c.Excluido
                       select new { c.EmpresaId, c.Id }).FirstOrDefaultAsync(ct);
        return r is null ? null : (r.EmpresaId, r.Id);
    }

    public async Task<PropostaPortal?> AbrirPropostaAsync(Guid token, CancellationToken ct = default)
    {
        if (await LocalizarConviteAsync(token, ct) is not { } loc) return null;
        await using var db = sistema.Criar(loc.EmpresaId, "PortalCotacao");
        var c = await db.Cotacoes.IgnoreQueryFilters().AsNoTracking().Include(x => x.Fornecedores).Include(x => x.Itens).FirstAsync(x => x.Id == loc.CotacaoId, ct);
        var conv = c.Fornecedores.First(f => f.LinkResposta == token);
        var forn = await db.Fornecedores.IgnoreQueryFilters().AsNoTracking().Where(f => f.Id == conv.FornecedorId).Select(f => f.RazaoSocial).FirstAsync(ct);
        var emp = await db.Empresas.AsNoTracking().Where(e => e.Id == loc.EmpresaId).Select(e => e.NomeFantasia != "" ? e.NomeFantasia : e.RazaoSocial).FirstAsync(ct);
        var itens = c.Itens.Where(i => i.FornecedorId == conv.FornecedorId).OrderBy(i => i.Descricao).ToList();
        var req = c.RequisicaoId is { } rq ? await db.Requisicoes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == rq, ct) : null;
        var fat = req?.EstabelecimentoFaturamentoId is { } est
            ? await db.Estabelecimentos.AsNoTracking().Where(x => x.Id == est).Select(x => x.RazaoSocial + " — CNPJ " + x.Cnpj).FirstOrDefaultAsync(ct)
            : req?.SpeId is { } sp ? await db.Spes.AsNoTracking().Where(s => s.Id == sp).Select(s => s.RazaoSocial + " — CNPJ " + s.Cnpj).FirstOrDefaultAsync(ct) : null;
        var destino = req?.SpeId is { } sp2 && req.EstabelecimentoFaturamentoId is not null
            ? await db.Spes.AsNoTracking().Where(s => s.Id == sp2).Select(s => "SPE " + (s.NomeFantasia ?? s.RazaoSocial)).FirstOrDefaultAsync(ct) + $" · requisição nº {req.Numero}"
            : null;
        var reqItens = itens.Where(i => i.RequisicaoItemId != null).Select(i => i.RequisicaoItemId!.Value).ToList();
        var locs = await Comum.QuadroLocais.CarregarAsync(db, reqItens, ct);
        var porItem = itens.Where(i => i.RequisicaoItemId is { } r && locs.ContainsKey(r)).ToDictionary(i => i.Id, i => locs[i.RequisicaoItemId!.Value]);
        var pedidas = await db.Set<RequisicaoItem>().AsNoTracking().Where(r => reqItens.Contains(r.Id) && r.Marca != null).ToDictionaryAsync(r => r.Id, r => (r.Marca!, r.MesmaMarca), ct);
        var marcas = itens.Where(i => i.RequisicaoItemId is { } r && pedidas.ContainsKey(r)).ToDictionary(i => i.Id, i => pedidas[i.RequisicaoItemId!.Value]);
        var agora = relogio.GetUtcNow();
        if (conv.VisualizadoEm is null)
            await db.Set<CotacaoFornecedor>().Where(x => x.Id == conv.Id && x.VisualizadoEm == null).ExecuteUpdateAsync(u => u.SetProperty(x => x.VisualizadoEm, agora), ct);
        var versoes = await db.PropostasVersao.AsNoTracking().Where(v => v.CotacaoFornecedorId == conv.Id).OrderByDescending(v => v.Versao).ToListAsync(ct);
        conv.Versoes = versoes;
        var decidida = c.Status is StatusCotacao.AguardandoAprovacao or StatusCotacao.Aprovado or StatusCotacao.PedidoGerado;
        var venc = decidida ? (c.VencedorPorItem ? itens.Count(i => i.Vencedor) : c.FornecedorVencedorId == conv.FornecedorId ? itens.Count : 0) : 0;
        var sit = SituacaoPropostaFornecedor.Calcular(c.Status, c.PrazoResposta, conv, venc, itens.Count, decidida, agora);
        RascunhoProposta? rasc = null;
        if (!string.IsNullOrWhiteSpace(conv.RascunhoJson))
            try { rasc = System.Text.Json.JsonSerializer.Deserialize<RascunhoProposta>(conv.RascunhoJson); } catch (System.Text.Json.JsonException) { }
        return new PropostaPortal(emp, forn, c.Numero, c.Nome, c.EnderecoEntrega, c.Frete, sit.PodeEditar && conv.Status != StatusConvite.Declinou, conv, itens, fat, destino)
            { Locais = porItem, Material = c.Tipo == TipoRequisicao.Material, MarcasPedidas = marcas, PrazoResposta = c.PrazoResposta, Rascunho = rasc, Versoes = versoes, Situacao = sit };
    }

    public async Task EnviarPropostaAsync(Guid token, IReadOnlyList<ItemPropostaPortal> itens, decimal? frete, string? condicao, DateOnly? validade,
        DateOnly? prazoEntrega, CancellationToken ct = default, string? enviadoPor = null)
    {
        if (await LocalizarConviteAsync(token, ct) is not { } loc) throw new RegraDeNegocioException("Link inválido.");
        await using var db = sistema.Criar(loc.EmpresaId, "PortalCotacao");
        var c = await db.Cotacoes.IgnoreQueryFilters().Include(x => x.Fornecedores).Include(x => x.Itens).FirstAsync(x => x.Id == loc.CotacaoId, ct);
        if (!CotacaoAberta(c)) throw new RegraDeNegocioException("Esta cotação já foi encerrada. Fale com o setor de Suprimentos.");
        if (c.PrazoResposta is { } pz && pz <= relogio.GetUtcNow()) throw new RegraDeNegocioException($"O prazo de resposta terminou em {pz.ToLocalTime():dd/MM/yyyy HH:mm}.");
        var conv = c.Fornecedores.First(f => f.LinkResposta == token);
        if (conv.Status == StatusConvite.Declinou) throw new RegraDeNegocioException("Você declinou desta cotação. Fale com Suprimentos para reabrir o convite.");
        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().Date);
        var erros = new List<string>();
        var meus = c.Itens.Where(i => i.FornecedorId == conv.FornecedorId).ToDictionary(i => i.Id);
        foreach (var it in itens)
        {
            if (!meus.TryGetValue(it.ItemId, out var ci)) { erros.Add("Item não pertence a esta proposta."); continue; }
            if (it.ValorUnitario < 0) erros.Add($"{ci.Descricao}: valor negativo.");
            if (it.Entrega is { } e && e < hoje) erros.Add($"{ci.Descricao}: entrega no passado.");
        }
        if (!itens.Any(i => i.ValorUnitario > 0)) erros.Add("Informe o preço de pelo menos um item.");
        var reqIds = meus.Values.Where(i => i.RequisicaoItemId != null).Select(i => i.RequisicaoItemId!.Value).ToList();
        var pedidas = await db.Set<RequisicaoItem>().IgnoreQueryFilters().AsNoTracking().Where(r => reqIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => (r.Marca, r.MesmaMarca), ct);
        foreach (var it in itens.Where(i => meus.ContainsKey(i.ItemId)))
        {
            var ci = meus[it.ItemId];
            var pr = ci.RequisicaoItemId is { } rq && pedidas.TryGetValue(rq, out var x) ? x : (null, false);
            var teste = new CotacaoItem { Descricao = ci.Descricao, ValorOfertado = it.ValorUnitario, Marca = it.Marca };
            if (teste.ErroMarca(c.Tipo == TipoRequisicao.Material, pr.Item1, pr.Item2) is { } em) erros.Add(em);
        }
        if (frete is < 0) erros.Add("Frete negativo.");
        if (validade is { } v && v < hoje) erros.Add("Validade da proposta no passado.");
        ValidacaoException.Se(erros);

        foreach (var it in itens)
        {
            var ci = meus[it.ItemId];
            ci.ValorOfertado = Math.Round(it.ValorUnitario, 4);
            ci.Entrega = it.Entrega ?? prazoEntrega;
            ci.Marca = string.IsNullOrWhiteSpace(it.Marca) ? ci.Marca : it.Marca.Trim();
        }
        var marcas = await MarcasServico.NormalizarAsync(db, meus.Values.Select(i => i.Marca), ct);
        foreach (var ci in meus.Values) ci.Marca = MarcasServico.Oficial(marcas, ci.Marca);
        conv.ValorFrete = c.Frete == TipoFrete.NaoSeAplica ? null : frete; conv.CondicaoPagamento = string.IsNullOrWhiteSpace(condicao) ? null : condicao.Trim();
        conv.ValidadeProposta = validade; conv.PrazoEntregaOfertado = prazoEntrega;
        conv.Status = StatusConvite.Respondido; conv.DataResposta = hoje;
        conv.RascunhoEm = null; conv.RascunhoJson = null;
        // Cada envio guarda uma versão (o comprador vê a última e o histórico).
        var nVersao = await db.PropostasVersao.CountAsync(v => v.CotacaoFornecedorId == conv.Id, ct) + 1;
        db.PropostasVersao.Add(new PropostaVersao
        {
            CotacaoFornecedorId = conv.Id, Versao = nVersao, EnviadaEm = relogio.GetUtcNow(), EnviadaPor = enviadoPor,
            ValorItens = meus.Values.Sum(i => Math.Round(i.ValorOfertado * i.Quantidade, 2)), ValorFrete = conv.ValorFrete, PrazoEntrega = prazoEntrega,
            CondicaoPagamento = conv.CondicaoPagamento, Validade = validade,
            ItensJson = System.Text.Json.JsonSerializer.Serialize(meus.Values.OrderBy(i => i.Descricao).Select(i => new { i.Descricao, i.Unidade, i.Quantidade, i.ValorOfertado, i.Marca, i.Entrega }))
        });
        if (c.Status is StatusCotacao.Aberto or StatusCotacao.AguardandoPropostas) c.Status = StatusCotacao.EmAnalise;
        c.Recalcular(await ParametrosServico.CarregarAsync(db, ct));
        await db.SaveChangesAsync(ct);
    }
    // Proposta: rascunho e declínio

    public async Task SalvarRascunhoAsync(Guid token, RascunhoProposta r, CancellationToken ct = default)
    {
        if (await LocalizarConviteAsync(token, ct) is not { } loc) throw new RegraDeNegocioException("Link inválido.");
        await using var db = sistema.Criar(loc.EmpresaId, "PortalCotacao");
        var c = await db.Cotacoes.IgnoreQueryFilters().Include(x => x.Fornecedores).FirstAsync(x => x.Id == loc.CotacaoId, ct);
        if (!CotacaoAberta(c) || c.PrazoResposta is { } pz && pz <= relogio.GetUtcNow()) throw new RegraDeNegocioException("Esta cotação não está mais recebendo propostas.");
        var conv = c.Fornecedores.First(f => f.LinkResposta == token);
        conv.RascunhoJson = System.Text.Json.JsonSerializer.Serialize(r);
        conv.RascunhoEm = relogio.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Fornecedor informa que não vai cotar (com motivo) ou volta atrás enquanto a cotação está aberta.</summary>
    public async Task DeclinarAsync(Guid token, bool declinar, string? motivo, CancellationToken ct = default)
    {
        if (await LocalizarConviteAsync(token, ct) is not { } loc) throw new RegraDeNegocioException("Link inválido.");
        await using var db = sistema.Criar(loc.EmpresaId, "PortalCotacao");
        var c = await db.Cotacoes.IgnoreQueryFilters().Include(x => x.Fornecedores).Include(x => x.Itens).FirstAsync(x => x.Id == loc.CotacaoId, ct);
        if (!CotacaoAberta(c) || c.PrazoResposta is { } pz && pz <= relogio.GetUtcNow()) throw new RegraDeNegocioException("Esta cotação já foi encerrada.");
        var conv = c.Fornecedores.First(f => f.LinkResposta == token);
        if (declinar)
        {
            if (string.IsNullOrWhiteSpace(motivo)) throw new RegraDeNegocioException("Informe o motivo (ex.: sem estoque, sem equipe disponível).");
            conv.Status = StatusConvite.Declinou; conv.MotivoDeclinio = motivo.Trim().Length > 300 ? motivo.Trim()[..300] : motivo.Trim();
        }
        else
        {
            var temPreco = c.Itens.Any(i => i.FornecedorId == conv.FornecedorId && i.ValorOfertado > 0);
            conv.Status = temPreco ? StatusConvite.Respondido : StatusConvite.Convidado; conv.MotivoDeclinio = null;
        }
        c.Recalcular(await ParametrosServico.CarregarAsync(db, ct));
        await db.SaveChangesAsync(ct);
    }
}
