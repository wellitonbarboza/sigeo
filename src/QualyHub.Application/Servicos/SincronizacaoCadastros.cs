using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Common;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Application.Servicos;

/// <summary>
/// Cadastro alterado vale também para os processos em andamento: produto nas requisições/cotações abertas e
/// qualificação do fornecedor nas cotações abertas. (Alçadas e engenheiro: <see cref="AprovacaoServico.RecalcularPendentesAsync"/>.)
/// Processos encerrados (pedido emitido, cotação aprovada, requisição concluída) mantêm a foto da época.
/// </summary>
public static class SincronizacaoCadastros
{
    public static readonly StatusRequisicao[] RequisicoesAbertas =
    [
        StatusRequisicao.Rascunho, StatusRequisicao.DevolvidaAjuste, StatusRequisicao.AguardandoEngenheiro,
        StatusRequisicao.AguardandoDiretorTecnico, StatusRequisicao.Autorizada, StatusRequisicao.EmCotacao
    ];

    public static readonly StatusCotacao[] CotacoesAbertas =
        [StatusCotacao.Aberto, StatusCotacao.AguardandoPropostas, StatusCotacao.EmAnalise, StatusCotacao.ReprovadoVoltaCotacao];

    /// <summary>Produto alterado: descrição, unidade, prazo e material controlado nos itens em andamento. Retorna itens alterados.</summary>
    public static async Task<int> ProdutoAsync(IQualyHubDb db, Guid produtoId, CancellationToken ct = default)
    {
        var p = await db.Produtos.AsNoTracking().Include(x => x.MaterialControlado).FirstOrDefaultAsync(x => x.Id == produtoId, ct);
        if (p is null) return 0;
        var itens = await db.RequisicaoItens.Where(i => i.ProdutoId == produtoId && RequisicoesAbertas.Contains(i.Requisicao.Status)).ToListAsync(ct);
        var n = 0;
        foreach (var i in itens)
        {
            var antes = (i.Descricao, i.Unidade, i.LeadTimeDias, i.MaterialControladoId, i.EspecificacaoTecnica, i.DocumentosExigidos);
            i.Descricao = p.Nome; i.Unidade = p.Unidade; i.LeadTimeDias = p.LeadTimeDias;
            i.MaterialControladoId = p.MaterialControladoId; i.MaterialControlado = p.MaterialControladoId is not null;
            // Especificação/documentos editados na requisição prevalecem; só preenche se estiverem vazios.
            if (p.MaterialControlado is { } mc) { i.EspecificacaoTecnica ??= mc.EspecificacaoModelo; i.DocumentosExigidos ??= mc.DocumentosExigidos; }
            if (antes != (i.Descricao, i.Unidade, i.LeadTimeDias, i.MaterialControladoId, i.EspecificacaoTecnica, i.DocumentosExigidos)) n++;
        }
        var ids = itens.Select(i => (Guid?)i.Id).ToList();
        var cot = await db.CotacaoItens.Where(c => ids.Contains(c.RequisicaoItemId) && CotacoesAbertas.Contains(c.Cotacao.Status)).ToListAsync(ct);
        foreach (var c in cot)
        {
            c.Descricao = p.Nome; c.Unidade = p.Unidade; c.MaterialControlado = p.MaterialControladoId is not null;
            if (!c.MaterialControlado) c.AtendeEspecificacao = null;
        }
        return n + cot.Count;
    }

    /// <summary>Qualificação do fornecedor (calculada hoje) nos convites das cotações em andamento. Retorna convites alterados.</summary>
    public static async Task<int> QualificacaoNasCotacoesAsync(IQualyHubDb db, IReadOnlyCollection<Guid> fornecedores, DateOnly hoje, CancellationToken ct = default)
    {
        if (fornecedores.Count == 0) return 0;
        var convites = await db.CotacaoFornecedores.Where(c => fornecedores.Contains(c.FornecedorId) && CotacoesAbertas.Contains(c.Cotacao.Status)).ToListAsync(ct);
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
}
