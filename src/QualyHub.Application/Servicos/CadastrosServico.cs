using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;

namespace QualyHub.Application.Servicos;

public enum OrdemProdutos { Codigo, Nome }

/// <summary>Filtro da lista de produtos. Texto procura em código e nome; Grupo é exato; Ativo nulo = todos.</summary>
public sealed record FiltroProdutos(string? Texto = null, string? Grupo = null, bool? Ativo = null, OrdemProdutos Ordem = OrdemProdutos.Codigo, bool Decrescente = false);

/// <summary>Linha da grade de produtos: só as colunas exibidas (o catálogo tem milhares de itens).</summary>
public sealed record LinhaProduto(Guid Id, string Codigo, string Nome, string Unidade, string? Grupo, string? SubGrupo, string? Ncm,
    int LeadTimeDias, decimal? PrecoReferencia, string? MaterialControlado, bool Ativo);

/// <summary>Página da consulta; <see cref="Pagina"/> volta ajustada quando a pedida passou do fim (ex.: filtro reduziu o total).</summary>
public sealed record PaginaProdutos(IReadOnlyList<LinhaProduto> Itens, int Total, int Pagina, int Tamanho);

/// <summary>Linha completa para a planilha exportada (inclui as colunas que não cabem na grade).</summary>
public sealed record LinhaProdutoExportacao(string Codigo, string Nome, string Unidade, string? Grupo, string? SubGrupo, string? Ncm, string? DescricaoCompleta,
    string? Marca, string? Referencia, string? Ean, decimal? QuantidadePorEmbalagem, string? CodigoAnterior, string? OrigemSistema,
    int LeadTimeDias, decimal? PrecoReferencia, string? MaterialControlado, bool Ativo);

public sealed record ResumoProdutos(int Ativos, int AClassificar, int SemNcm, int Vinculados);

/// <summary>
/// Cadastros de suprimentos: produtos (insumos), categorias e materiais/serviços controlados.
/// O código do produto segue as regras de <see cref="CodigosProdutoServico"/>.
/// </summary>
public sealed class CadastrosServico(IFabricaDb fabrica)
{
    public const int TamanhoMaximoPagina = 500;

    // ───────────── Produtos ─────────────

    private static IQueryable<Produto> Filtrar(IQueryable<Produto> q, FiltroProdutos f)
    {
        if (!string.IsNullOrWhiteSpace(f.Texto))
        {
            var t = f.Texto.Trim();
            // Sem distinção de acento e caixa ("conexao" acha "conexão"), como a busca antiga em memória fazia.
            q = q.Where(p => EF.Functions.Collate(p.Codigo, "Latin1_General_CI_AI").Contains(t)
                          || EF.Functions.Collate(p.Nome, "Latin1_General_CI_AI").Contains(t));
        }
        if (!string.IsNullOrWhiteSpace(f.Grupo)) q = q.Where(p => p.Grupo == f.Grupo);
        if (f.Ativo is { } a) q = q.Where(p => p.Ativo == a);
        // Id no desempate: sem ele a mesma linha pode aparecer em duas páginas (ordem instável no SQL).
        return (f.Ordem, f.Decrescente) switch
        {
            (OrdemProdutos.Nome, false) => q.OrderBy(p => p.Nome).ThenBy(p => p.Id),
            (OrdemProdutos.Nome, true) => q.OrderByDescending(p => p.Nome).ThenBy(p => p.Id),
            (_, false) => q.OrderBy(p => p.Codigo).ThenBy(p => p.Id),
            (_, true) => q.OrderByDescending(p => p.Codigo).ThenBy(p => p.Id),
        };
    }

    /// <summary>Consulta paginada no banco (Skip/Take no SQL) — a tela nunca recebe o catálogo inteiro.</summary>
    public async Task<PaginaProdutos> ListarProdutosAsync(FiltroProdutos f, int pagina, int tamanho, CancellationToken ct)
    {
        tamanho = Math.Clamp(tamanho, 1, TamanhoMaximoPagina);
        await using var db = fabrica.CriarLeitura();
        var q = Filtrar(db.Produtos, f);
        var total = await q.CountAsync(ct);
        pagina = Math.Clamp(pagina, 0, Math.Max(0, (total - 1) / tamanho));
        var itens = await q.Skip(pagina * tamanho).Take(tamanho)
            .Select(p => new LinhaProduto(p.Id, p.Codigo, p.Nome, p.Unidade, p.Grupo, p.SubGrupo, p.Ncm, p.LeadTimeDias, p.PrecoReferencia,
                p.MaterialControlado == null ? null : p.MaterialControlado.Nome, p.Ativo))
            .ToListAsync(ct);
        return new PaginaProdutos(itens, total, pagina, tamanho);
    }

    /// <summary>Todas as linhas do filtro (não só a página) para a planilha.</summary>
    public async Task<List<LinhaProdutoExportacao>> ExportarProdutosAsync(FiltroProdutos f, CancellationToken ct)
    {
        await using var db = fabrica.CriarLeitura();
        return await Filtrar(db.Produtos, f)
            .Select(p => new LinhaProdutoExportacao(p.Codigo, p.Nome, p.Unidade, p.Grupo, p.SubGrupo, p.Ncm, p.DescricaoCompleta, p.Marca, p.Referencia, p.Ean,
                p.QuantidadePorEmbalagem, p.CodigoAnterior, p.OrigemSistema, p.LeadTimeDias, p.PrecoReferencia,
                p.MaterialControlado == null ? null : p.MaterialControlado.Nome, p.Ativo))
            .ToListAsync(ct);
    }

    /// <summary>Indicadores do catálogo inteiro (independem do filtro e da página).</summary>
    public async Task<ResumoProdutos> ResumoProdutosAsync(CancellationToken ct)
    {
        await using var db = fabrica.CriarLeitura();
        var ativos = db.Produtos.Where(p => p.Ativo);
        return new(
            await ativos.CountAsync(ct),
            await ativos.CountAsync(p => p.Grupo == null || p.Grupo.Trim() == "" || p.Grupo == "A Cadastrar", ct),
            await ativos.CountAsync(p => p.Ncm == null || p.Ncm == "", ct),
            await db.Produtos.CountAsync(p => p.MaterialControladoId != null, ct));
    }

    /// <summary>Grupos e subgrupos já usados no catálogo (sugestões do formulário e filtro por grupo).</summary>
    public async Task<(List<string> Grupos, List<(string? Grupo, string Sub)> Subgrupos)> GruposProdutoAsync(CancellationToken ct)
    {
        await using var db = fabrica.CriarLeitura();
        var pares = await db.Produtos.Where(p => p.Grupo != null && p.Grupo != "")
            .Select(p => new { p.Grupo, p.SubGrupo }).Distinct().ToListAsync(ct);
        var grupos = pares.Select(x => x.Grupo!).Distinct().Order().ToList();
        var subs = pares.Where(x => !string.IsNullOrWhiteSpace(x.SubGrupo)).Select(x => (x.Grupo, x.SubGrupo!)).Distinct().OrderBy(x => x.Item2).ToList();
        return (grupos, subs);
    }

    public async Task<Produto?> ObterProdutoAsync(Guid id, CancellationToken ct)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Produtos.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    /// <summary>Valida e normaliza (NCM/EAN só dígitos; NCM zerado vira vazio). Regra pura — coberta por teste.</summary>
    public static List<string> PrepararProduto(Produto p)
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(p.Nome)) e.Add("Informe a descrição genérica.");
        p.Unidade = UnidadesPadrao.Normalizar(p.Unidade);
        if (!string.IsNullOrWhiteSpace(p.Grupo) && !p.AClassificar && string.IsNullOrWhiteSpace(p.SubGrupo)) e.Add("Informe o subgrupo (o código usa grupo + subgrupo).");
        if (p.LeadTimeDias < 0) e.Add("Lead time não pode ser negativo.");
        if (p.PrecoReferencia is < 0) e.Add("Preço de referência não pode ser negativo.");
        p.Ncm = string.IsNullOrWhiteSpace(p.Ncm) ? null : new string(p.Ncm.Where(char.IsDigit).ToArray());
        if (p.Ncm is { Length: not 8 }) e.Add("NCM deve ter 8 dígitos.");
        else if (p.Ncm is not null && p.Ncm.Trim('0').Length == 0) p.Ncm = null;
        p.Ean = string.IsNullOrWhiteSpace(p.Ean) ? null : new string(p.Ean.Where(char.IsDigit).ToArray());
        if (p.Ean is { Length: not (8 or 12 or 13 or 14) }) e.Add("EAN deve ter 8, 12, 13 ou 14 dígitos.");
        if (p.QuantidadePorEmbalagem is <= 0) e.Add("Quantidade por embalagem deve ser maior que zero.");
        return e;
    }

    public async Task SalvarProdutoAsync(Produto p, CancellationToken ct)
    {
        ValidacaoException.Se(PrepararProduto(p));
        await using var db = fabrica.Criar();
        p.MaterialControlado = null; // só a FK conta; evita reinserir o material carregado na tela
        await CodigosProdutoServico.AplicarAoSalvarAsync(db, p, new CodigosProdutoServico.Cache(), ct);
        p.Marca = MarcasServico.Oficial(await MarcasServico.NormalizarAsync(db, [p.Marca], ct), p.Marca);
        await Grafo.SalvarAsync(db, p);
        await db.SaveChangesAsync(ct);
        // Requisições e cotações em andamento passam a mostrar o cadastro atualizado.
        if (await SincronizacaoCadastros.ProdutosAsync(db, [p.Id], ct) > 0) await db.SaveChangesAsync(ct);
    }

    // ───────────── Categorias ─────────────

    /// <summary>Cria os grupos/subgrupos que aparecem nos produtos, com sigla sugerida (não altera códigos). Retorna quantas criou.</summary>
    public async Task<int> GerarCategoriasDosProdutosAsync(CancellationToken ct)
    {
        await using var db = fabrica.Criar();
        var cache = new CodigosProdutoServico.Cache();
        var pares = await db.Produtos.AsNoTracking()
            .Where(p => p.Grupo != null && p.SubGrupo != null && p.Grupo != "A Cadastrar" && p.SubGrupo != "A Cadastrar")
            .Select(p => new { p.Grupo, p.SubGrupo }).Distinct().ToListAsync(ct);
        foreach (var x in pares) await CodigosProdutoServico.GarantirCategoriasAsync(db, x.Grupo!, x.SubGrupo!, cache, ct);
        var novas = db.CategoriasProduto.Local.Count(c => db.Entry(c).State == EntityState.Added);
        await db.SaveChangesAsync(ct);
        return novas;
    }

    // ───────────── Materiais e serviços controlados ─────────────

    public static List<string> ValidarBasico(string? codigo, string? nome)
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(codigo)) e.Add("Informe o código.");
        if (string.IsNullOrWhiteSpace(nome)) e.Add("Informe o nome.");
        return e;
    }

    /// <summary>Salva o material e já reaplica o vínculo pelo grupo (produtos do grupo ligado viram controlados), tudo numa transação.</summary>
    public Task SalvarMaterialControladoAsync(MaterialControlado m, CancellationToken ct)
    {
        ValidacaoException.Se(ValidarBasico(m.Codigo, m.Nome));
        return fabrica.EmTransacaoAsync(async db =>
        {
            if (m.CategoriaProdutoId is { } g && await db.MateriaisControlados.AnyAsync(x => x.Id != m.Id && x.CategoriaProdutoId == g, ct))
                throw new RegraDeNegocioException("Este grupo do catálogo já está ligado a outro material (1 grupo = 1 material).");
            var antes = await db.MateriaisControlados.AsNoTracking().Where(x => x.Id == m.Id).Select(x => new { x.EspecificacaoModelo, x.DocumentosExigidos }).FirstOrDefaultAsync(ct);
            await Grafo.SalvarAsync(db, m);
            await db.SaveChangesAsync(ct);
            var mudaram = await VincularAsync(db, ct);
            await db.SaveChangesAsync(ct);
            // Processos em andamento: produtos que entraram/saíram do controle e a especificação/documentos do material.
            await SincronizacaoCadastros.ProdutosAsync(db, mudaram, ct);
            await SincronizacaoCadastros.MaterialControladoAsync(db, m.Id, antes?.EspecificacaoModelo, antes?.DocumentosExigidos, ct);
            await db.SaveChangesAsync(ct);
        }, ct);
    }

    public async Task SalvarServicoControladoAsync(ServicoControlado s, CancellationToken ct)
    {
        ValidacaoException.Se(ValidarBasico(s.Codigo, s.Nome));
        await using var db = fabrica.Criar();
        await Grafo.SalvarAsync(db, s);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Reaplica o vínculo produto → material controlado pelo grupo do catálogo. Retorna quantos produtos mudaram.</summary>
    public async Task<int> VincularPorGrupoAsync(CancellationToken ct)
    {
        await using var db = fabrica.Criar();
        await Permissoes.ExigirTelaAsync(db, fabrica.Usuario, "cadastros/controlados", Contratos.AcaoTela.Incluir, ct);
        var mudaram = await VincularAsync(db, ct);
        await db.SaveChangesAsync(ct);
        if (await SincronizacaoCadastros.ProdutosAsync(db, mudaram, ct) > 0) await db.SaveChangesAsync(ct);
        return mudaram.Count;
    }

    private static async Task<List<Guid>> VincularAsync(IQualyHubDb db, CancellationToken ct)
    {
        var cache = new CodigosProdutoServico.Cache();
        var mudaram = new List<Guid>();
        foreach (var p in await db.Produtos.ToListAsync(ct))
        {
            var antes = p.MaterialControladoId;
            await CodigosProdutoServico.VincularControladoAsync(db, p, cache, ct);
            if (antes != p.MaterialControladoId) mudaram.Add(p.Id);
        }
        return mudaram;
    }
}
