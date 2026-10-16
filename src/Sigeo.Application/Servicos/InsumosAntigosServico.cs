using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Abstracoes;
using Sigeo.Application.Comum;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Fiscal;

namespace Sigeo.Application.Servicos;

/// <summary>
/// Situação do insumo frente ao catálogo padronizado: grupo e subgrupo ativos em Categorias de Produto = padronizado;
/// grupo/subgrupo "A Cadastrar" ou vazio = a classificar; o resto (grupo/subgrupo que saiu do catálogo) = fora do catálogo.
/// </summary>
public static class SituacaoCadastro
{
    public const string ACadastrar = "A Cadastrar";

    /// <summary>Grupos ativos do catálogo e seus subgrupos ativos (nomes).</summary>
    public static async Task<Dictionary<string, HashSet<string>>> CatalogoAsync(ISigeoDb db, CancellationToken ct)
    {
        var cats = await db.CategoriasProduto.AsNoTracking().Where(c => c.Ativo).Select(c => new { c.Id, c.PaiId, c.Nome }).ToListAsync(ct);
        return cats.Where(c => c.PaiId == null)
            .GroupBy(g => g.Nome.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => cats.Where(s => g.Any(x => x.Id == s.PaiId)).Select(s => s.Nome.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
    }

    public static SituacaoCadastroProduto De(string? grupo, string? subgrupo, IReadOnlyDictionary<string, HashSet<string>> catalogo)
    {
        if (string.IsNullOrWhiteSpace(grupo) || grupo.Trim().Equals(ACadastrar, StringComparison.OrdinalIgnoreCase)
            || (subgrupo?.Trim().Equals(ACadastrar, StringComparison.OrdinalIgnoreCase) ?? false))
            return SituacaoCadastroProduto.AClassificar;
        if (!catalogo.TryGetValue(grupo.Trim(), out var subs)) return SituacaoCadastroProduto.ForaDoCatalogo;
        return string.IsNullOrWhiteSpace(subgrupo) || subs.Contains(subgrupo.Trim()) ? SituacaoCadastroProduto.Padronizado : SituacaoCadastroProduto.ForaDoCatalogo;
    }

    /// <summary>Mesma regra de <see cref="De"/>, traduzida para SQL.</summary>
    public static IQueryable<Produto> Filtrar(IQueryable<Produto> q, IQueryable<CategoriaProduto> cats, FiltroCadastroProduto filtro)
    {
        var ativas = cats.Where(c => c.Ativo);
        IQueryable<Produto> aClassificar(IQueryable<Produto> x) =>
            x.Where(p => p.Grupo == null || p.Grupo.Trim() == "" || p.Grupo == ACadastrar || p.SubGrupo == ACadastrar);
        IQueryable<Produto> padronizados(IQueryable<Produto> x) =>
            x.Where(p => p.Grupo != null && p.Grupo.Trim() != "" && p.Grupo != ACadastrar && (p.SubGrupo == null || p.SubGrupo != ACadastrar)
                && ativas.Any(g => g.PaiId == null && g.Nome == p.Grupo
                    && (p.SubGrupo == null || p.SubGrupo.Trim() == "" || ativas.Any(s => s.PaiId == g.Id && s.Nome == p.SubGrupo))));
        return filtro switch
        {
            FiltroCadastroProduto.Padronizados => padronizados(q),
            FiltroCadastroProduto.AClassificar => aClassificar(q),
            FiltroCadastroProduto.ForaDoCatalogo => q.Where(p => p.Grupo != null && p.Grupo.Trim() != "" && p.Grupo != ACadastrar && (p.SubGrupo == null || p.SubGrupo != ACadastrar)
                && !ativas.Any(g => g.PaiId == null && g.Nome == p.Grupo
                    && (p.SubGrupo == null || p.SubGrupo.Trim() == "" || ativas.Any(s => s.PaiId == g.Id && s.Nome == p.SubGrupo)))),
            FiltroCadastroProduto.Antigos => q.Where(p => !(p.Grupo != null && p.Grupo.Trim() != "" && p.Grupo != ACadastrar && (p.SubGrupo == null || p.SubGrupo != ACadastrar)
                && ativas.Any(g => g.PaiId == null && g.Nome == p.Grupo
                    && (p.SubGrupo == null || p.SubGrupo.Trim() == "" || ativas.Any(s => s.PaiId == g.Id && s.Nome == p.SubGrupo))))),
            _ => q,
        };
    }
}

public sealed record InsumoAntigo(Guid Id, string Codigo, string Nome, string Unidade, string? Grupo);
public sealed record PreviaInativacao(int Antigos, int ComUso, int Inativar, IReadOnlyList<InsumoAntigo> Amostra);
public sealed record EquivalenteSugerido(Guid Id, string Codigo, string Nome, string Unidade, string? Grupo, string? SubGrupo, decimal Semelhanca, bool MesmaUnidade);
public sealed record ResultadoSubstituicao(string De, string Para, int RequisicoesAtualizadas, int RequisicoesNaoAlteradas, int KitsAtualizados,
    int VinculosImportacao, int ComposicoesComAntigo, int SubstituicoesEncadeadas);

/// <summary>
/// Limpeza dos insumos antigos depois da padronização do catálogo:
/// inativar os antigos sem uso recente e substituir um antigo pelo padronizado equivalente (os próximos usos vão para o novo).
/// Nada é apagado: o histórico (pedidos, recebimentos, custo realizado) continua apontando para o código antigo.
/// </summary>
public sealed class InsumosAntigosServico(IFabricaDb fabrica, TimeProvider relogio)
{
    public const int MesesSemUso = 12;

    /// <summary>Grupos e subgrupos ativos do catálogo (para mostrar a situação de um produto aberto na tela).</summary>
    public async Task<Dictionary<string, HashSet<string>>> CatalogoAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await SituacaoCadastro.CatalogoAsync(db, ct);
    }

    /// <summary>Código e nome do produto que substituiu este (para a tela).</summary>
    public async Task<(string Codigo, string Nome)?> SubstitutoAsync(Guid? id, CancellationToken ct = default)
    {
        if (id is null) return null;
        await using var db = fabrica.CriarLeitura();
        var p = await db.Produtos.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.Codigo, x.Nome }).FirstOrDefaultAsync(ct);
        return p is null ? null : (p.Codigo, p.Nome);
    }

    /// <summary>
    /// Antigos ativos (a classificar ou fora do catálogo) sem uso nos últimos 12 meses: sem requisição, pedido, recebimento,
    /// movimento de estoque ou custo realizado no período, sem saldo em estoque e fora de composição, kit e EPI.
    /// </summary>
    private async Task<(List<InsumoAntigo> Antigos, HashSet<Guid> ComUso)> LevantarAsync(ISigeoDb db, CancellationToken ct)
    {
        var antigos = await SituacaoCadastro.Filtrar(db.Produtos.Where(p => p.Ativo), db.CategoriasProduto, FiltroCadastroProduto.Antigos)
            .OrderBy(p => p.Codigo).Select(p => new InsumoAntigo(p.Id, p.Codigo, p.Nome, p.Unidade, p.Grupo)).ToListAsync(ct);
        var desde = relogio.GetUtcNow().AddMonths(-MesesSemUso);
        var ids = antigos.Select(a => a.Id).ToList();
        var uso = new HashSet<Guid>();
        // Consultas em blocos (limite de parâmetros do SQL Server) e sem o filtro por obra: uso em qualquer obra conta.
        foreach (var bloco in ids.Chunk(1000))
        {
            var b = bloco.ToList();
            uso.UnionWith(await db.RequisicaoItens.IgnoreQueryFilters([FiltrosNomes.Obra]).Where(i => i.ProdutoId != null && b.Contains(i.ProdutoId.Value) && i.Requisicao.CriadoEm >= desde).Select(i => i.ProdutoId!.Value).ToListAsync(ct));
            uso.UnionWith(await db.PedidoItens.IgnoreQueryFilters([FiltrosNomes.Obra]).Where(i => i.ProdutoId != null && b.Contains(i.ProdutoId.Value) && i.Pedido.CriadoEm >= desde).Select(i => i.ProdutoId!.Value).ToListAsync(ct));
            uso.UnionWith(await db.RecebimentoItens.IgnoreQueryFilters([FiltrosNomes.Obra]).Where(i => i.ProdutoId != null && b.Contains(i.ProdutoId.Value) && i.Recebimento.CriadoEm >= desde).Select(i => i.ProdutoId!.Value).ToListAsync(ct));
            uso.UnionWith(await db.MovimentosEstoque.IgnoreQueryFilters([FiltrosNomes.Obra]).Where(m => b.Contains(m.ProdutoId) && m.CriadoEm >= desde).Select(m => m.ProdutoId).ToListAsync(ct));
            uso.UnionWith(await db.SaldosEstoque.IgnoreQueryFilters([FiltrosNomes.Obra]).Where(s => b.Contains(s.ProdutoId) && (s.Disponivel != 0 || s.Bloqueado != 0)).Select(s => s.ProdutoId).ToListAsync(ct));
            uso.UnionWith(await db.CustosRealizados.IgnoreQueryFilters([FiltrosNomes.Obra]).Where(c => c.ProdutoId != null && b.Contains(c.ProdutoId.Value) && c.CriadoEm >= desde).Select(c => c.ProdutoId!.Value).ToListAsync(ct));
            uso.UnionWith(await db.ComposicaoItens.IgnoreQueryFilters([FiltrosNomes.Obra]).Where(c => c.ProdutoId != null && b.Contains(c.ProdutoId.Value)).Select(c => c.ProdutoId!.Value).ToListAsync(ct));
            uso.UnionWith(await db.KitMaterialItens.IgnoreQueryFilters([FiltrosNomes.Obra]).Where(k => b.Contains(k.ProdutoId)).Select(k => k.ProdutoId).ToListAsync(ct));
            uso.UnionWith(await db.Epis.IgnoreQueryFilters([FiltrosNomes.Obra]).Where(e => e.ProdutoId != null && b.Contains(e.ProdutoId.Value)).Select(e => e.ProdutoId!.Value).ToListAsync(ct));
        }
        return (antigos, uso);
    }

    public async Task<PreviaInativacao> PreverInativacaoAsync(CancellationToken ct = default) => await PreverInativacaoAsync(null, ct);

    public async Task<PreviaInativacao> PreverInativacaoAsync(IReadOnlyCollection<Guid>? somente, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var (antigos, uso) = await LevantarAsync(db, ct);
        if (somente is not null) antigos = antigos.Where(a => somente.Contains(a.Id)).ToList();
        var inativar = antigos.Where(a => !uso.Contains(a.Id)).ToList();
        return new PreviaInativacao(antigos.Count, antigos.Count - inativar.Count, inativar.Count, inativar.Take(200).ToList());
    }

    /// <summary>Inativa os antigos sem uso (recalcula na hora: o que ganhou uso desde a prévia fica ativo).</summary>
    public async Task<int> InativarSemUsoAsync(CancellationToken ct = default) => await InativarSemUsoAsync(null, ct);

    /// <param name="somente">Restringe a estes produtos (testes); nulo = todos os antigos sem uso.</param>
    public async Task<int> InativarSemUsoAsync(IReadOnlyCollection<Guid>? somente, CancellationToken ct = default)
    {
        var n = 0;
        await fabrica.EmTransacaoAsync(async db =>
        {
            var (antigos, uso) = await LevantarAsync(db, ct);
            var alvo = antigos.Where(a => !uso.Contains(a.Id) && (somente is null || somente.Contains(a.Id))).Select(a => a.Id).ToList();
            foreach (var bloco in alvo.Chunk(1000))
            {
                var b = bloco.ToList();
                foreach (var p in await db.Produtos.Where(p => b.Contains(p.Id) && p.Ativo).ToListAsync(ct)) { p.Ativo = false; n++; }
            }
            await db.SaveChangesAsync(ct);
        }, ct);
        return n;
    }

    /// <summary>Padronizados ativos mais parecidos com o antigo (nome + unidade); com <paramref name="texto"/>, procura por ele.</summary>
    public async Task<IReadOnlyList<EquivalenteSugerido>> SugerirEquivalentesAsync(Guid antigoId, string? texto, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var antigo = await db.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == antigoId, ct) ?? throw new RegraDeNegocioException("Produto não encontrado.");
        var q = SituacaoCadastro.Filtrar(db.Produtos.AsNoTracking().Where(p => p.Ativo && p.Id != antigoId), db.CategoriasProduto, FiltroCadastroProduto.Padronizados);
        if (!string.IsNullOrWhiteSpace(texto))
        {
            var t = texto.Trim();
            q = q.Where(p => EF.Functions.Collate(p.Codigo, "Latin1_General_CI_AI").Contains(t) || EF.Functions.Collate(p.Nome, "Latin1_General_CI_AI").Contains(t));
        }
        var candidatos = await q.Select(p => new { p.Id, p.Codigo, p.Nome, p.Unidade, p.Grupo, p.SubGrupo }).Take(5000).ToListAsync(ct);
        return candidatos
            .Select(c => (c, s: ConferenciaNota.Semelhanca(antigo.Nome, c.Nome), u: ConferenciaNota.MesmaUnidade(antigo.Unidade, c.Unidade)))
            .Where(x => !string.IsNullOrWhiteSpace(texto) || x.s >= 0.2m)
            .OrderByDescending(x => x.s + (x.u ? 0.1m : 0)).ThenBy(x => x.c.Nome)
            .Take(10)
            .Select(x => new EquivalenteSugerido(x.c.Id, x.c.Codigo, x.c.Nome, x.c.Unidade, x.c.Grupo, x.c.SubGrupo, Math.Round(x.s, 2), x.u))
            .ToList();
    }

    /// <summary>
    /// Substitui o antigo pelo padronizado: o antigo fica inativo e marcado "substituído por"; requisições em rascunho, kits
    /// e vínculos da importação do ERP passam para o novo. Requisições já enviadas e composições do orçamento não mudam
    /// (alterariam documento aprovado ou custo orçado): voltam contadas para revisão.
    /// </summary>
    public async Task<ResultadoSubstituicao> SubstituirAsync(Guid antigoId, Guid novoId, CancellationToken ct = default)
    {
        if (antigoId == novoId) throw new RegraDeNegocioException("Escolha um produto diferente do atual.");
        ResultadoSubstituicao? r = null;
        await fabrica.EmTransacaoAsync(async db =>
        {
            var antigo = await db.Produtos.FirstOrDefaultAsync(p => p.Id == antigoId, ct) ?? throw new RegraDeNegocioException("Produto antigo não encontrado.");
            var novo = await db.Produtos.FirstOrDefaultAsync(p => p.Id == novoId, ct) ?? throw new RegraDeNegocioException("Produto novo não encontrado.");
            if (!novo.Ativo) throw new RegraDeNegocioException($"{novo.Codigo} está inativo: escolha um produto ativo.");
            if (novo.SubstituidoPorId is not null) throw new RegraDeNegocioException($"{novo.Codigo} já foi substituído por outro produto.");

            antigo.Ativo = false;
            antigo.SubstituidoPorId = novo.Id;
            // Quem já apontava para o antigo passa a apontar direto para o novo (sem cadeia).
            var encadeados = await db.Produtos.Where(p => p.SubstituidoPorId == antigo.Id).ToListAsync(ct);
            foreach (var p in encadeados) p.SubstituidoPorId = novo.Id;

            var itensReq = await db.RequisicaoItens.IgnoreQueryFilters([FiltrosNomes.Obra]).Include(i => i.Requisicao)
                .Where(i => i.ProdutoId == antigo.Id && i.Status == StatusItemRequisicao.Aberto).ToListAsync(ct);
            var trocados = 0;
            foreach (var i in itensReq.Where(i => i.Requisicao.Status == StatusRequisicao.Rascunho))
            {
                i.ProdutoId = novo.Id;
                if (i.Descricao == antigo.Nome) i.Descricao = novo.Nome;
                if (i.Unidade == antigo.Unidade) i.Unidade = novo.Unidade;
                trocados++;
            }
            var reqNaoAlteradas = itensReq.Where(i => i.Requisicao.Status != StatusRequisicao.Rascunho).Select(i => i.RequisicaoId).Distinct().Count();

            var kits = 0;
            var itensKit = await db.KitMaterialItens.IgnoreQueryFilters([FiltrosNomes.Obra]).Where(k => k.ProdutoId == antigo.Id).ToListAsync(ct);
            foreach (var k in itensKit)
            {
                var jaTem = await db.KitMaterialItens.IgnoreQueryFilters([FiltrosNomes.Obra]).FirstOrDefaultAsync(x => x.KitId == k.KitId && x.ProdutoId == novo.Id, ct);
                if (jaTem is not null) { jaTem.Quantidade += k.Quantidade; db.KitMaterialItens.Remove(k); }
                else k.ProdutoId = novo.Id;
                kits++;
            }

            var vinculos = await db.VinculosOrigem.Where(v => v.Tipo == TipoVinculoOrigem.Produto && v.DestinoId == antigo.Id).ToListAsync(ct);
            foreach (var v in vinculos) { v.DestinoId = novo.Id; v.DestinoTexto = $"{novo.Nome} ({novo.Unidade})"; }

            var composicoes = await db.ComposicaoItens.IgnoreQueryFilters([FiltrosNomes.Obra]).CountAsync(c => c.ProdutoId == antigo.Id, ct);
            await db.SaveChangesAsync(ct);
            r = new ResultadoSubstituicao(antigo.Codigo, novo.Codigo, trocados, reqNaoAlteradas, kits, vinculos.Count, composicoes, encadeados.Count);
        }, ct);
        return r!;
    }
}
