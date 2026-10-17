using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;

namespace Sigeo.Application.Cadastros;

/// <summary>
/// Aplica o padrão do catálogo de uma construtora a partir de uma planilha (abas Grupos, Reclassificar e Produtos):
/// <list type="number">
/// <item>reclassifica produtos por regra (grupo/subgrupo atual e palavras do nome) — une grupos repetidos e separa o que não é do grupo;</item>
/// <item>define em cada grupo o tipo de custo e a fase do orçamento herdados e o material controlado ligado;</item>
/// <item>cadastra (ou completa) os insumos da construtora já classificados, sem duplicar (código do ERP ou nome + unidade);</item>
/// <item>lista oficial: quando a planilha liga materiais controlados, os demais materiais deixam de ligar grupos e produtos;</item>
/// <item>desativa grupos/subgrupos que ficaram vazios; recodifica os produtos ainda não usados e refaz o vínculo com os materiais controlados.</item>
/// </list>
/// Nada é excluído: produto só muda de grupo e categoria vazia só é desativada.
/// </summary>
public sealed class PadraoCatalogoServico(IFabricaDb fabrica, CodigosProdutoServico codigos)
{
    public Task<ResumoPadraoCatalogo> AnalisarAsync(PadraoCatalogo p, CancellationToken ct = default) => ProcessarAsync(p, false, ct);
    public Task<ResumoPadraoCatalogo> AplicarAsync(PadraoCatalogo p, CancellationToken ct = default) => ProcessarAsync(p, true, ct);

    private static string N(string? s) => string.Join(' ', CodigoProduto.Normalizar(s ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
    private static bool Igual(string? a, string? b) => N(a) == N(b);
    public static string ChaveProduto(string nome, string unidade) => N(nome) + "|" + UnidadesPadrao.Normalizar(unidade);

    public static List<string> Validar(PadraoCatalogo p)
    {
        var e = new List<string>();
        foreach (var g in p.Grupos.Where(g => string.IsNullOrWhiteSpace(g.Grupo))) e.Add($"Grupos linha {g.Linha}: informe o grupo.");
        foreach (var g in p.Grupos.Where(g => g.Sigla is { Length: > 0 } s && !CodigoProduto.EhSiglaValida(s.ToUpperInvariant())))
            e.Add($"Grupos linha {g.Linha}: sigla \"{g.Sigla}\" inválida (3 letras ou números).");
        foreach (var d in p.Grupos.Where(g => g.MaterialCodigo is { Length: > 0 }).GroupBy(g => N(g.MaterialCodigo)).Where(x => x.Count() > 1))
            e.Add($"Grupos: o material {d.First().MaterialCodigo} está em mais de um grupo (1 grupo = 1 material).");
        foreach (var r in p.Regras.Where(r => string.IsNullOrWhiteSpace(r.GrupoAtual) || string.IsNullOrWhiteSpace(r.GrupoNovo) || string.IsNullOrWhiteSpace(r.SubNovo)))
            e.Add($"Reclassificar linha {r.Linha}: informe grupo atual, grupo novo e subgrupo novo.");
        foreach (var x in p.Produtos.Where(x => string.IsNullOrWhiteSpace(x.Nome) || string.IsNullOrWhiteSpace(x.Grupo) || string.IsNullOrWhiteSpace(x.SubGrupo)))
            e.Add($"Produtos linha {x.Linha}: informe nome, grupo e subgrupo.");
        return e;
    }

    private async Task<ResumoPadraoCatalogo> ProcessarAsync(PadraoCatalogo p, bool aplicar, CancellationToken ct)
    {
        ValidacaoException.Se(Validar(p));
        var avisos = new List<string>();
        var cache = new CodigosProdutoServico.Cache();
        await using var db = fabrica.Criar();
        var cats = await db.CategoriasProduto.ToListAsync(ct);
        var produtos = await db.Produtos.ToListAsync(ct);
        var materiais = await db.MateriaisControlados.ToListAsync(ct);

        // 1) Reclassificação por regra (a primeira regra que pega o produto vale).
        var movidos = new HashSet<Guid>();
        var porRegra = new List<(string, int)>();
        foreach (var r in p.Regras)
        {
            var palavras = (r.Contem ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(N).Where(x => x.Length > 0).ToList();
            var alvo = produtos.Where(x => !movidos.Contains(x.Id) && Igual(x.Grupo, r.GrupoAtual)
                                           && (string.IsNullOrWhiteSpace(r.SubAtual) || r.SubAtual == "*" || Igual(x.SubGrupo, r.SubAtual))
                                           && (palavras.Count == 0 || palavras.Any(w => $" {N(x.Nome)} ".Contains($" {w}", StringComparison.Ordinal))))
                .Where(x => !(Igual(x.Grupo, r.GrupoNovo) && Igual(x.SubGrupo, r.SubNovo))).ToList();
            foreach (var x in alvo) { x.Grupo = r.GrupoNovo.Trim(); x.SubGrupo = r.SubNovo.Trim(); movidos.Add(x.Id); }
            porRegra.Add(($"{r.GrupoAtual} › {(string.IsNullOrWhiteSpace(r.SubAtual) ? "*" : r.SubAtual)}{(palavras.Count > 0 ? $" ({r.Contem})" : "")} → {r.GrupoNovo} › {r.SubNovo}", alvo.Count));
        }

        // 2) Insumos da construtora (código do ERP ou nome + unidade; nunca duplica).
        var porCodigoErp = produtos.Where(x => !string.IsNullOrWhiteSpace(x.CodigoErp)).GroupBy(x => x.CodigoErp!.Trim().ToUpperInvariant()).ToDictionary(g => g.Key, g => g.First());
        var porChave = produtos.GroupBy(x => ChaveProduto(x.Nome, x.Unidade)).ToDictionary(g => g.Key, g => g.First());
        int novos = 0, completados = 0;
        var criados = new List<Produto>();
        foreach (var x in p.Produtos)
        {
            var un = UnidadesPadrao.Normalizar(x.Unidade);
            var existente = (x.CodigoErp is { Length: > 0 } ce ? porCodigoErp.GetValueOrDefault(ce.Trim().ToUpperInvariant()) : null)
                            ?? porChave.GetValueOrDefault(ChaveProduto(x.Nome, un));
            if (existente is not null)
            {
                completados++;
                if (existente.AClassificar) { existente.Grupo = x.Grupo.Trim(); existente.SubGrupo = x.SubGrupo.Trim(); movidos.Add(existente.Id); }
                existente.TipoCusto ??= x.TipoCusto;
                existente.CodigoErp ??= x.CodigoErp;
                existente.PrecoReferencia ??= x.Preco is > 0 ? x.Preco : null;
                continue;
            }
            var novo = new Produto
            {
                Nome = x.Nome.Trim(), Unidade = un, Grupo = x.Grupo.Trim(), SubGrupo = x.SubGrupo.Trim(), TipoCusto = x.TipoCusto,
                CodigoErp = string.IsNullOrWhiteSpace(x.CodigoErp) ? null : x.CodigoErp.Trim(), PrecoReferencia = x.Preco is > 0 ? x.Preco : null, OrigemSistema = "ERP"
            };
            produtos.Add(novo); criados.Add(novo);
            porChave[ChaveProduto(novo.Nome, un)] = novo;
            if (novo.CodigoErp is { } c2) porCodigoErp[c2.ToUpperInvariant()] = novo;
            novos++;
        }

        // 3) Grupos da planilha que ainda não existem nascem com a sigla pedida; depois as categorias de destino existem e ficam ativas.
        cache = new CodigosProdutoServico.Cache { Todas = cats };
        var gruposAntes = cats.Where(c => c.PaiId == null && !c.Excluido).Select(c => c.Id).ToHashSet();
        foreach (var pg in p.Grupos.Where(pg => !cats.Any(c => c.PaiId == null && !c.Excluido && Igual(c.Nome, pg.Grupo))))
        {
            var livres = cats.Where(c => c.PaiId == null && !c.Excluido).Select(c => c.Sigla).ToList();
            var sigla = pg.Sigla is { Length: > 0 } sg && !livres.Contains(sg.ToUpperInvariant()) ? sg.ToUpperInvariant() : CodigoProduto.SugerirSigla(pg.Grupo, livres);
            var g = new CategoriaProduto { Nome = pg.Grupo.Trim(), Sigla = sigla };
            db.CategoriasProduto.Add(g); cats.Add(g);
        }
        foreach (var (g, s) in produtos.Where(x => movidos.Contains(x.Id) || criados.Contains(x)).Select(x => (x.Grupo!, x.SubGrupo!)).Distinct())
        {
            var (cg, cs) = await CodigosProdutoServico.GarantirCategoriasAsync(db, g, s, cache, ct);
            cg.Ativo = true; cs.Ativo = true;
        }

        // 4) Grupos: sigla (só ao criar), tipo de custo, fase do orçamento e material controlado.
        var todas = db.CategoriasProduto.Local.ToList();
        var gruposCriados = new List<string>();
        var materiaisLigados = new List<string>();
        var atualizados = 0;
        foreach (var pg in p.Grupos)
        {
            var g = todas.FirstOrDefault(c => c.PaiId == null && !c.Excluido && Igual(c.Nome, pg.Grupo));
            if (g is null)
            {
                var sigla = pg.Sigla is { Length: > 0 } sg && !todas.Any(c => c.PaiId == null && !c.Excluido && c.Sigla == sg.ToUpperInvariant())
                    ? sg.ToUpperInvariant() : CodigoProduto.SugerirSigla(pg.Grupo, todas.Where(c => c.PaiId == null && !c.Excluido).Select(c => c.Sigla));
                g = new CategoriaProduto { Nome = pg.Grupo.Trim(), Sigla = sigla };
                db.CategoriasProduto.Add(g); todas.Add(g);
            }
            if (!gruposAntes.Contains(g.Id)) gruposCriados.Add($"{g.Sigla} {g.Nome}");
            else atualizados++;
            g.Ativo = true;
            g.TipoCustoPadrao = pg.TipoCusto ?? g.TipoCustoPadrao;
            g.FasePadrao = string.IsNullOrWhiteSpace(pg.Fase) ? g.FasePadrao : pg.Fase.Trim();
            if (pg.MaterialCodigo is { Length: > 0 } mc)
            {
                var mat = materiais.FirstOrDefault(m => !m.Excluido && N(m.Codigo) == N(mc));
                if (mat is null) { avisos.Add($"Grupos linha {pg.Linha}: material controlado {mc} não cadastrado — importe a lista de materiais antes."); continue; }
                foreach (var outro in materiais.Where(m => m.Id != mat.Id && m.CategoriaProdutoId == g.Id))
                {
                    avisos.Add($"{g.Nome}: deixa de ligar \"{outro.Codigo} {outro.Nome}\" (1 grupo = 1 material).");
                    outro.CategoriaProdutoId = null;
                }
                if (mat.CategoriaProdutoId != g.Id) materiaisLigados.Add($"{mat.Codigo} {mat.Nome} → {g.Nome}");
                mat.CategoriaProdutoId = g.Id;
            }
        }

        // Lista oficial: quando a planilha liga materiais, só eles ficam ligados a grupos e marcando produtos como controlados.
        var oficiais = materiais.Where(m => m.CategoriaProdutoId != null && p.Grupos.Any(pg => pg.MaterialCodigo is { Length: > 0 } mc && N(mc) == N(m.Codigo))).Select(m => m.Id).ToHashSet();
        var foraDaLista = 0;
        if (p.ListaOficialMateriais && oficiais.Count > 0)
        {
            foreach (var m in materiais.Where(m => !oficiais.Contains(m.Id) && m.CategoriaProdutoId != null))
            {
                avisos.Add($"Fora da lista de materiais controlados: \"{m.Codigo} {m.Nome}\" deixa de ligar o grupo {todas.FirstOrDefault(c => c.Id == m.CategoriaProdutoId)?.Nome}.");
                m.CategoriaProdutoId = null;
            }
            foreach (var x in produtos.Where(x => x.MaterialControladoId is { } mid && !oficiais.Contains(mid)))
            {
                x.MaterialControladoId = null; foraDaLista++;
            }
            if (foraDaLista > 0) avisos.Add($"{foraDaLista} produto(s) ligados a materiais fora da lista deixam de ser controlados (voltam a ser se o grupo deles tiver material da lista).");
        }

        // 5) Categorias que ficaram vazias (sem produto e sem material ligado) são desativadas.
        var usadas = produtos.Where(x => !x.Excluido).Select(x => (N(x.Grupo), N(x.SubGrupo))).ToHashSet();
        var gruposUsados = usadas.Select(u => u.Item1).ToHashSet();
        var ligadosA = materiais.Where(m => m.CategoriaProdutoId != null).Select(m => m.CategoriaProdutoId!.Value).ToHashSet();
        var definidos = p.Grupos.Select(x => N(x.Grupo)).ToHashSet();
        var desativadas = new List<string>();
        foreach (var g in todas.Where(c => c.PaiId == null && !c.Excluido && c.Ativo))
        {
            foreach (var s in todas.Where(c => c.PaiId == g.Id && !c.Excluido && c.Ativo && !usadas.Contains((N(g.Nome), N(c.Nome)))))
            {
                s.Ativo = false; desativadas.Add($"{g.Nome} › {s.Nome}");
            }
            if (!gruposUsados.Contains(N(g.Nome)) && !ligadosA.Contains(g.Id) && !definidos.Contains(N(g.Nome)))
            {
                g.Ativo = false; desativadas.Add(g.Nome);
            }
        }

        var resumo = new ResumoPadraoCatalogo(movidos.Count, porRegra, gruposCriados, atualizados, materiaisLigados, novos, completados, desativadas, avisos);
        if (!aplicar) return resumo;

        // Grava categorias, reclassificação e vínculos; depois o código dos novos (SIGLA-SUB-0000) com as categorias já gravadas.
        await db.SaveChangesAsync(ct);
        cache = new CodigosProdutoServico.Cache();
        foreach (var x in criados)
        {
            await CodigosProdutoServico.VincularControladoAsync(db, x, cache, ct);
            x.Codigo = await CodigosProdutoServico.GerarAsync(db, x, cache, ct);
            db.Produtos.Add(x);
        }
        await db.SaveChangesAsync(ct);

        // Vínculo com material controlado e tipo de custo herdado para todo o catálogo; recodifica os ainda não usados.
        await using (var db2 = fabrica.Criar())
        {
            var c2 = new CodigosProdutoServico.Cache();
            foreach (var x in await db2.Produtos.ToListAsync(ct)) await CodigosProdutoServico.VincularControladoAsync(db2, x, c2, ct);
            await db2.SaveChangesAsync(ct);
        }
        await codigos.PadronizarAsync(true, ct);
        return resumo;
    }
}
