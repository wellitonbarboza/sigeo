using Microsoft.EntityFrameworkCore;
using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Cadastros;

namespace Groundkey.Application.Servicos;

public sealed record ResultadoPadronizacao(int Recodificados, int Provisorios, int MantidosPorUso, IReadOnlyList<(string De, string Para, string Produto)> Amostra);

/// <summary>
/// Código padronizado de produto: SIGLA-GRUPO + SIGLA-SUBGRUPO + sequência (HID-CNX-0001), continuando do último
/// código emitido na mesma categoria. Sem classificação ("A Cadastrar") → provisório TMP-000001.
/// Regras: produto já usado (requisição, cotação/pedido, EPI) mantém o código; antes do 1º uso o código acompanha
/// a reclassificação; provisório sempre vira definitivo ao ser classificado. O código anterior fica guardado.
/// </summary>
public sealed class CodigosProdutoServico(IFabricaDb fabrica)
{
    /// <summary>Cache por contexto das categorias (evita ida ao banco a cada produto na importação).</summary>
    public sealed class Cache
    {
        internal List<CategoriaProduto>? Todas;
        internal List<(Guid Id, Guid? CategoriaProdutoId)>? Ligados;
    }

    private static async Task<List<CategoriaProduto>> CategoriasAsync(IGroundkeyDb db, Cache cache, CancellationToken ct) =>
        cache.Todas ??= await db.CategoriasProduto.ToListAsync(ct);

    public static bool MesmoNome(string a, string b) =>
        string.Equals(string.Join(' ', CodigoProduto.Normalizar(a).Split(' ', StringSplitOptions.RemoveEmptyEntries)),
                      string.Join(' ', CodigoProduto.Normalizar(b).Split(' ', StringSplitOptions.RemoveEmptyEntries)), StringComparison.Ordinal);

    /// <summary>Encontra (ou cria com sigla sugerida) o grupo e o subgrupo pelo nome.</summary>
    public static async Task<(CategoriaProduto Grupo, CategoriaProduto Sub)> GarantirCategoriasAsync(IGroundkeyDb db, string grupo, string sub, Cache cache, CancellationToken ct = default)
    {
        var todas = await CategoriasAsync(db, cache, ct);
        var g = todas.FirstOrDefault(c => c.PaiId == null && !c.Excluido && MesmoNome(c.Nome, grupo));
        if (g is null)
        {
            g = new CategoriaProduto { Nome = grupo.Trim(), Sigla = CodigoProduto.SugerirSigla(grupo, todas.Where(c => c.PaiId == null && !c.Excluido).Select(c => c.Sigla)) };
            db.CategoriasProduto.Add(g); todas.Add(g);
        }
        var s = todas.FirstOrDefault(c => c.PaiId == g.Id && !c.Excluido && MesmoNome(c.Nome, sub));
        if (s is null)
        {
            s = new CategoriaProduto { PaiId = g.Id, Nome = sub.Trim(), Sigla = CodigoProduto.SugerirSigla(sub, todas.Where(c => c.PaiId == g.Id && !c.Excluido).Select(c => c.Sigla)) };
            db.CategoriasProduto.Add(s); todas.Add(s);
        }
        return (g, s);
    }

    private static bool Classificado(Produto p) => !p.AClassificar && !string.IsNullOrWhiteSpace(p.SubGrupo) && !p.SubGrupo.Equals("A Cadastrar", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gera o próximo código para a classificação atual do produto (continua do último da categoria).</summary>
    public static async Task<string> GerarAsync(IGroundkeyDb db, Produto p, Cache cache, CancellationToken ct = default)
    {
        if (!Classificado(p))
        {
            var n = await Numerador.ProximoAPartirDeAsync(db, "Prod:" + CodigoProduto.PrefixoProvisorio, () => MaiorAsync(db, CodigoProduto.PrefixoProvisorio + "-", ct), ct);
            return CodigoProduto.Provisorio(n);
        }
        var (g, s) = await GarantirCategoriasAsync(db, p.Grupo!, p.SubGrupo!, cache, ct);
        var prefixo = CodigoProduto.Prefixo(g.Sigla, s.Sigla);
        var seq = await Numerador.ProximoAPartirDeAsync(db, "Prod:" + prefixo.TrimEnd('-'), () => MaiorAsync(db, prefixo, ct), ct);
        return CodigoProduto.Montar(g.Sigla, s.Sigla, seq);
    }

    private static async Task<int> MaiorAsync(IGroundkeyDb db, string prefixo, CancellationToken ct)
    {
        var codigos = await db.Produtos.IgnoreQueryFilters().Where(x => x.Codigo.StartsWith(prefixo)).Select(x => x.Codigo).ToListAsync(ct);
        codigos.AddRange(db.Produtos.Local.Where(x => x.Codigo.StartsWith(prefixo, StringComparison.Ordinal)).Select(x => x.Codigo));
        return codigos.Select(CodigoProduto.Sequencia).DefaultIfEmpty(0).Max();
    }

    public static async Task<HashSet<Guid>> UsadosAsync(IGroundkeyDb db, CancellationToken ct = default)
    {
        var r = await db.RequisicaoItens.Where(i => i.ProdutoId != null).Select(i => i.ProdutoId!.Value).Distinct().ToListAsync(ct);
        var p = await db.PedidoItens.Where(i => i.ProdutoId != null).Select(i => i.ProdutoId!.Value).Distinct().ToListAsync(ct);
        return [.. r, .. p];
    }

    /// <summary>Define o código ao salvar: novo → gera; reclassificado → recodifica se provisório ou ainda não usado.</summary>
    public static async Task AplicarAoSalvarAsync(IGroundkeyDb db, Produto p, Cache cache, CancellationToken ct = default)
    {
        await VincularControladoAsync(db, p, cache, ct);
        var anterior = p.CriadoEm == default ? null : await db.Produtos.AsNoTracking().Where(x => x.Id == p.Id).Select(x => new { x.Codigo, x.Grupo, x.SubGrupo }).FirstOrDefaultAsync(ct);
        if (anterior is null) { if (string.IsNullOrWhiteSpace(p.Codigo) || !CodigoProduto.EhPadrao(p.Codigo)) p.Codigo = await GerarAsync(db, p, cache, ct); return; }
        p.Codigo = anterior.Codigo; // código não é editado à mão
        var mudou = !string.Equals(anterior.Grupo, p.Grupo, StringComparison.OrdinalIgnoreCase) || !string.Equals(anterior.SubGrupo, p.SubGrupo, StringComparison.OrdinalIgnoreCase);
        var provisorioClassificado = CodigoProduto.EhProvisorio(p.Codigo) && Classificado(p);
        if (!mudou && !provisorioClassificado) return;
        var usado = await db.RequisicaoItens.AnyAsync(i => i.ProdutoId == p.Id, ct) || await db.PedidoItens.AnyAsync(i => i.ProdutoId == p.Id, ct);
        if (usado && !provisorioClassificado) return; // congelado após o 1º uso
        var novo = await GerarAsync(db, p, cache, ct);
        if (novo != p.Codigo) { p.CodigoAnterior = p.Codigo; p.Codigo = novo; }
    }

    /// <summary>
    /// Material controlado pelo grupo (1 grupo do catálogo = 1 material da Tabela de Especificação): produto no grupo ligado vira controlado;
    /// produto que saiu do grupo perde o vínculo automático.
    /// </summary>
    public static async Task VincularControladoAsync(IGroundkeyDb db, Produto p, Cache cache, CancellationToken ct = default)
    {
        // Uma consulta por lote (Cache), não uma por produto.
        var ligados = cache.Ligados ??= (await db.MateriaisControlados.Where(m => m.CategoriaProdutoId != null && m.Ativo).Select(m => new { m.Id, m.CategoriaProdutoId }).ToListAsync(ct))
                                       .Select(m => (m.Id, m.CategoriaProdutoId)).ToList();
        var todas = await CategoriasAsync(db, cache, ct);
        var g = string.IsNullOrWhiteSpace(p.Grupo) ? null : todas.FirstOrDefault(c => c.PaiId == null && !c.Excluido && MesmoNome(c.Nome, p.Grupo));
        // Produto sem tipo de custo herda o do grupo (padrão do catálogo).
        if (p.TipoCusto is null && g?.TipoCustoPadrao is { } tc) p.TipoCusto = tc;
        if (ligados.Count == 0) return;
        var mat = g is null ? null : ligados.Where(m => m.CategoriaProdutoId == g.Id).Select(m => (Guid?)m.Id).FirstOrDefault();
        if (mat is { } mid) p.MaterialControladoId = mid;
        else if (p.MaterialControladoId is { } atual && ligados.Any(m => m.Id == atual)) p.MaterialControladoId = null;
    }

    /// <summary>Padroniza os códigos existentes (prévia ou aplicação). Usados e já padronizados na categoria certa ficam como estão.</summary>
    public async Task<ResultadoPadronizacao> PadronizarAsync(bool aplicar, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var cache = new Cache();
        db.ChangeTracker.AutoDetectChangesEnabled = false; // lote grande: evita varrer o rastreador a cada produto
        var usados = await UsadosAsync(db, ct);
        var produtos = await db.Produtos.OrderBy(x => x.CriadoEm).ThenBy(x => x.Codigo).ToListAsync(ct);
        int rec = 0, prov = 0, mant = 0;
        var amostra = new List<(string, string, string)>();
        foreach (var p in produtos)
        {
            string? prefixoCerto = null;
            if (Classificado(p))
            {
                var (g, s) = await GarantirCategoriasAsync(db, p.Grupo!, p.SubGrupo!, cache, ct);
                prefixoCerto = CodigoProduto.Prefixo(g.Sigla, s.Sigla);
            }
            var ok = prefixoCerto is null ? CodigoProduto.EhProvisorio(p.Codigo) : p.Codigo.StartsWith(prefixoCerto, StringComparison.Ordinal) && CodigoProduto.EhPadrao(p.Codigo);
            if (ok) continue;
            if (usados.Contains(p.Id) && !CodigoProduto.EhProvisorio(p.Codigo)) { mant++; continue; }
            var novo = await GerarAsync(db, p, cache, ct);
            if (amostra.Count < 30) amostra.Add((p.Codigo, novo, p.Nome));
            p.CodigoAnterior = p.Codigo; p.Codigo = novo;
            if (CodigoProduto.EhProvisorio(novo)) prov++; else rec++;
        }
        db.ChangeTracker.AutoDetectChangesEnabled = true;
        if (aplicar) await db.SaveChangesAsync(ct);
        return new ResultadoPadronizacao(rec, prov, mant, amostra);
    }

    /// <summary>Altera a sigla de um grupo/subgrupo (validada e única no nível) e recodifica os produtos ainda não usados.</summary>
    public async Task<int> AlterarSiglaAsync(Guid categoriaId, string novaSigla, CancellationToken ct = default)
    {
        novaSigla = (novaSigla ?? "").Trim().ToUpperInvariant();
        if (!CodigoProduto.EhSiglaValida(novaSigla)) throw new RegraDeNegocioException("A sigla deve ter exatamente 3 letras ou números (ex.: HID).");
        await using var db = fabrica.Criar();
        var c = await db.CategoriasProduto.FirstAsync(x => x.Id == categoriaId, ct);
        if (c.Sigla == novaSigla) return 0;
        if (await db.CategoriasProduto.AnyAsync(x => x.Id != c.Id && x.PaiId == c.PaiId && x.Sigla == novaSigla, ct))
            throw new RegraDeNegocioException($"A sigla {novaSigla} já é usada {(c.PaiId is null ? "por outro grupo" : "neste grupo")}.");
        c.Sigla = novaSigla;
        await db.SaveChangesAsync(ct);
        return (await PadronizarAsync(true, ct)).Recodificados;
    }

    /// <summary>Tipo de custo e fase do orçamento herdados pelos produtos do grupo (padrão do catálogo).</summary>
    public async Task SalvarPadraoGrupoAsync(Guid grupoId, Domain.Common.TipoCusto? tipo, string? fase, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var g = await db.CategoriasProduto.FirstAsync(x => x.Id == grupoId, ct);
        if (g.PaiId is not null) throw new RegraDeNegocioException("Tipo de custo e fase são definidos no grupo.");
        g.TipoCustoPadrao = tipo;
        g.FasePadrao = string.IsNullOrWhiteSpace(fase) ? null : fase.Trim();
        if (tipo is { } t)
            foreach (var p in await db.Produtos.Where(p => p.TipoCusto == null && p.Grupo == g.Nome).ToListAsync(ct)) p.TipoCusto = t;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Mescla um subgrupo em outro do mesmo grupo (ex.: "Eletrica" → "Elétricas"): move os produtos e recodifica os não usados.</summary>
    public async Task<int> MesclarAsync(Guid origemId, Guid destinoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var o = await db.CategoriasProduto.Include(x => x.Pai).FirstAsync(x => x.Id == origemId, ct);
        var d = await db.CategoriasProduto.Include(x => x.Pai).FirstAsync(x => x.Id == destinoId, ct);
        if (o.PaiId is null || d.PaiId is null || o.PaiId != d.PaiId || o.Id == d.Id) throw new RegraDeNegocioException("Só é possível mesclar subgrupos do mesmo grupo.");
        var prods = await db.Produtos.Where(p => p.Grupo == o.Pai!.Nome && p.SubGrupo == o.Nome).ToListAsync(ct);
        foreach (var p in prods) p.SubGrupo = d.Nome;
        db.CategoriasProduto.Remove(o); // exclusão lógica (interceptor)
        await db.SaveChangesAsync(ct);
        await PadronizarAsync(true, ct);
        return prods.Count;
    }
}
