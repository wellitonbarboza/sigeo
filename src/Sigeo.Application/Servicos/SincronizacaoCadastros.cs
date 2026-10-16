using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Servicos;

/// <summary>
/// Cadastro alterado vale também para os processos em andamento ("tempo real" nos abertos):
/// produto e material controlado → itens de requisições, cotações e pedidos ainda não enviados;
/// qualificação do fornecedor → convites das cotações; parâmetros → propostas exigidas das cotações.
/// (Alçadas, engenheiro e usuários: <see cref="AprovacaoServico.RecalcularPendentesAsync"/>.)
/// Processos encerrados (pedido enviado, cotação com pedido gerado, requisição concluída) mantêm a foto da época.
/// </summary>
public static class SincronizacaoCadastros
{
    public static readonly StatusRequisicao[] RequisicoesAbertas =
    [
        StatusRequisicao.Rascunho, StatusRequisicao.DevolvidaAjuste, StatusRequisicao.AguardandoEngenheiro,
        StatusRequisicao.AguardandoDiretorTecnico, StatusRequisicao.Autorizada, StatusRequisicao.EmCotacao
    ];

    /// <summary>Cotação em edição (dá para incluir/retirar fornecedor e mexer em preço).</summary>
    public static readonly StatusCotacao[] CotacoesAbertas =
        [StatusCotacao.Aberto, StatusCotacao.AguardandoPropostas, StatusCotacao.EmAnalise, StatusCotacao.ReprovadoVoltaCotacao];

    /// <summary>Cotação ainda sem pedido/contrato (inclui aguardando aprovação e aprovada): recebe a qualificação atual.</summary>
    public static readonly StatusCotacao[] CotacoesEmAndamento =
        [.. CotacoesAbertas, StatusCotacao.AguardandoAprovacao, StatusCotacao.Aprovado];

    /// <summary>Pedido ainda não enviado ao fornecedor.</summary>
    public static readonly StatusPedido[] PedidosAbertos = [StatusPedido.Emitido, StatusPedido.AguardandoAprovacaoDiretor, StatusPedido.Aprovado];

    /// <summary>
    /// Produtos alterados: descrição, unidade, prazo e material controlado nos itens em andamento
    /// (especificação/documentos editados no processo prevalecem; só preenche se vazios). Retorna itens alterados.
    /// </summary>
    public static async Task<int> ProdutosAsync(ISigeoDb db, IReadOnlyCollection<Guid> produtos, CancellationToken ct = default)
    {
        if (produtos.Count == 0) return 0;
        var ps = await db.Produtos.AsNoTracking().Include(x => x.MaterialControlado).Where(x => produtos.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var n = 0;
        var itens = await db.RequisicaoItens.Where(i => i.ProdutoId != null && produtos.Contains(i.ProdutoId.Value) && RequisicoesAbertas.Contains(i.Requisicao.Status)).ToListAsync(ct);
        foreach (var i in itens)
        {
            var p = ps[i.ProdutoId!.Value];
            var antes = (i.Descricao, i.Unidade, i.LeadTimeDias, i.MaterialControladoId, i.EspecificacaoTecnica, i.DocumentosExigidos);
            i.Descricao = p.Nome; i.Unidade = p.Unidade; i.LeadTimeDias = p.LeadTimeDias;
            i.MaterialControladoId = p.MaterialControladoId; i.MaterialControlado = p.MaterialControladoId is not null;
            if (p.MaterialControlado is { } mc) { i.EspecificacaoTecnica ??= mc.EspecificacaoModelo; i.DocumentosExigidos ??= mc.DocumentosExigidos; }
            if (antes != (i.Descricao, i.Unidade, i.LeadTimeDias, i.MaterialControladoId, i.EspecificacaoTecnica, i.DocumentosExigidos)) n++;
        }
        var porReq = itens.ToDictionary(i => (Guid?)i.Id, i => ps[i.ProdutoId!.Value]);
        var ids = porReq.Keys.ToList();
        foreach (var c in await db.CotacaoItens.Where(c => ids.Contains(c.RequisicaoItemId) && CotacoesAbertas.Contains(c.Cotacao.Status)).ToListAsync(ct))
        {
            var p = porReq[c.RequisicaoItemId];
            c.Descricao = p.Nome; c.Unidade = p.Unidade; c.MaterialControlado = p.MaterialControladoId is not null;
            if (!c.MaterialControlado) c.AtendeEspecificacao = null;
            n++;
        }
        foreach (var i in await db.PedidoItens.Where(i => i.ProdutoId != null && produtos.Contains(i.ProdutoId.Value) && PedidosAbertos.Contains(i.Pedido.Status)).ToListAsync(ct))
        {
            var p = ps[i.ProdutoId!.Value];
            i.Descricao = p.Nome; i.Unidade = p.Unidade; i.MaterialControladoId = p.MaterialControladoId;
            if (p.MaterialControlado is { } mc) { i.EspecificacaoTecnica ??= mc.EspecificacaoModelo; i.DocumentosExigidos ??= mc.DocumentosExigidos; }
            n++;
        }
        return n;
    }

    /// <summary>
    /// Material controlado alterado (especificação-modelo / documentos exigidos): itens em andamento que estavam com o texto
    /// antigo (ou vazio) recebem o novo; texto ajustado à mão no processo fica. Retorna itens alterados.
    /// </summary>
    public static async Task<int> MaterialControladoAsync(ISigeoDb db, Guid materialId, string? especAntes, string? docsAntes, CancellationToken ct = default)
    {
        var m = await db.MateriaisControlados.AsNoTracking().FirstOrDefaultAsync(x => x.Id == materialId, ct);
        if (m is null) return 0;
        static bool Seguir(string? atual, string? antes) => string.IsNullOrWhiteSpace(atual) || string.Equals(atual?.Trim(), antes?.Trim(), StringComparison.Ordinal);
        var n = 0;
        foreach (var i in await db.RequisicaoItens.Where(i => i.MaterialControladoId == materialId && RequisicoesAbertas.Contains(i.Requisicao.Status)).ToListAsync(ct))
        {
            if (Seguir(i.EspecificacaoTecnica, especAntes) && i.EspecificacaoTecnica != m.EspecificacaoModelo) { i.EspecificacaoTecnica = m.EspecificacaoModelo; n++; }
            if (Seguir(i.DocumentosExigidos, docsAntes) && i.DocumentosExigidos != m.DocumentosExigidos) { i.DocumentosExigidos = m.DocumentosExigidos; n++; }
        }
        foreach (var i in await db.PedidoItens.Where(i => i.MaterialControladoId == materialId && PedidosAbertos.Contains(i.Pedido.Status)).ToListAsync(ct))
        {
            if (Seguir(i.EspecificacaoTecnica, especAntes) && i.EspecificacaoTecnica != m.EspecificacaoModelo) { i.EspecificacaoTecnica = m.EspecificacaoModelo; n++; }
            if (Seguir(i.DocumentosExigidos, docsAntes) && i.DocumentosExigidos != m.DocumentosExigidos) { i.DocumentosExigidos = m.DocumentosExigidos; n++; }
        }
        return n;
    }

    /// <summary>Qualificação do fornecedor (calculada hoje) nos convites das cotações em andamento. Retorna convites alterados.</summary>
    public static async Task<int> QualificacaoNasCotacoesAsync(ISigeoDb db, IReadOnlyCollection<Guid> fornecedores, DateOnly hoje, CancellationToken ct = default)
    {
        if (fornecedores.Count == 0) return 0;
        var convites = await db.CotacaoFornecedores.Where(c => fornecedores.Contains(c.FornecedorId) && CotacoesEmAndamento.Contains(c.Cotacao.Status)).ToListAsync(ct);
        if (convites.Count == 0) return 0;
        var fids = convites.Select(c => c.FornecedorId).Distinct().ToList();
        var fs = await db.Fornecedores.AsNoTracking().Include(f => f.Qualificacoes).Where(f => fids.Contains(f.Id)).ToDictionaryAsync(f => f.Id, ct);
        var n = 0;
        foreach (var c in convites)
        {
            if (!fs.TryGetValue(c.FornecedorId, out var f)) continue;
            var st = f.CalcularStatus(hoje);
            if (c.QualificacaoNaData != st) { c.QualificacaoNaData = st; n++; }
        }
        return n;
    }

    /// <summary>Parâmetros de alçada mudaram: cotações em edição recalculam valor estimado e propostas exigidas.</summary>
    public static async Task<int> PropostasExigidasAsync(ISigeoDb db, CancellationToken ct = default)
    {
        var cs = await db.Cotacoes.Include(c => c.Itens).Include(c => c.Fornecedores).Where(c => CotacoesAbertas.Contains(c.Status)).ToListAsync(ct);
        if (cs.Count == 0) return 0;
        var p = await ParametrosServico.CarregarAsync(db, ct);
        var n = 0;
        foreach (var c in cs)
        {
            var antes = c.PropostasExigidas;
            c.Recalcular(p);
            if (antes != c.PropostasExigidas) n++;
        }
        return n;
    }

    /// <summary>
    /// Rotina do dia (qualificação vence com o tempo, sem ninguém salvar nada): recalcula o status gravado dos fornecedores
    /// e leva para as cotações em andamento. Retorna fornecedores que mudaram.
    /// </summary>
    public static async Task<int> RotinaDiariaAsync(ISigeoDb db, DateOnly hoje, CancellationToken ct = default)
    {
        var fs = await db.Fornecedores.Include(f => f.Qualificacoes).ToListAsync(ct);
        var mudaram = new List<Guid>();
        foreach (var f in fs)
        {
            var st = f.CalcularStatus(hoje);
            if (f.StatusQualificacao != st) { f.StatusQualificacao = st; mudaram.Add(f.Id); }
        }
        var conv = await db.CotacaoFornecedores.Where(c => CotacoesEmAndamento.Contains(c.Cotacao.Status)).Select(c => c.FornecedorId).Distinct().ToListAsync(ct);
        await QualificacaoNasCotacoesAsync(db, conv, hoje, ct);
        await db.SaveChangesAsync(ct);
        return mudaram.Count;
    }
}
