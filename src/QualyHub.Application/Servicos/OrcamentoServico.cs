using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Application.Servicos;

/// <summary>
/// Serviços e composições, serviços por tipologia, matriz serviço × local (orçamento) e demanda de materiais/serviços
/// por local para a requisição e a contratação.
/// </summary>
public sealed class OrcamentoServico(IFabricaDb fabrica)
{
    public async Task SalvarComposicaoAsync(Composicao c, CancellationToken ct = default)
    {
        c.Codigo = (c.Codigo ?? "").Trim().ToUpperInvariant();
        var ordem = 0;
        foreach (var i in c.Itens.OrderBy(i => i.Ordem)) { i.Ordem = ++ordem; i.ComposicaoId = c.Id; }
        await using var db = fabrica.Criar();
        var prods = c.Itens.Where(i => i.ProdutoId != null).Select(i => i.ProdutoId!.Value).Distinct().ToList();
        var ps = await db.Produtos.AsNoTracking().Where(p => prods.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        foreach (var i in c.Itens.Where(i => i.ProdutoId is { } pid && ps.ContainsKey(pid)))
        {
            var p = ps[i.ProdutoId!.Value];
            if (string.IsNullOrWhiteSpace(i.Descricao)) i.Descricao = p.Nome;
            if (string.IsNullOrWhiteSpace(i.Unidade) || i.Unidade == "un") i.Unidade = p.Unidade;
            if (i.CustoUnitario == 0 && p.PrecoReferencia is { } pr) i.CustoUnitario = pr;
        }
        ValidacaoException.Se(c.Validar());
        if (await db.Composicoes.AnyAsync(x => x.Id != c.Id && x.Codigo == c.Codigo, ct)) throw new RegraDeNegocioException("Código de serviço já usado.");
        await Grafo.SalvarAsync(db, c, nameof(Composicao.Itens));
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> CopiarComposicaoAsync(Guid id, string codigo, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var o = await db.Composicoes.AsNoTracking().Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
        var n = new Composicao { Codigo = codigo, Descricao = o.Descricao + " (cópia)", Unidade = o.Unidade, Grupo = o.Grupo, Regra = o.Regra, Fator = o.Fator, ServicoControladoId = o.ServicoControladoId, Observacao = o.Observacao };
        foreach (var i in o.Itens)
            n.Itens.Add(new ComposicaoItem { ComposicaoId = n.Id, Ordem = i.Ordem, Tipo = i.Tipo, ProdutoId = i.ProdutoId, FuncaoObraId = i.FuncaoObraId, Descricao = i.Descricao, Unidade = i.Unidade, Coeficiente = i.Coeficiente, PerdaPerc = i.PerdaPerc, CustoUnitario = i.CustoUnitario });
        await SalvarComposicaoAsync(n, ct);
        return n.Id;
    }

    /// <summary>Marca/desmarca o serviço num ambiente ou nível da tipologia.</summary>
    public async Task MarcarAplicacaoAsync(Guid tipologiaId, Guid composicaoId, AlvoServico alvo, Guid? ambienteId, bool marcar, decimal? fixa = null, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var t = await db.Tipologias.Include(x => x.Ambientes).FirstOrDefaultAsync(x => x.Id == tipologiaId, ct) ?? throw new RegraDeNegocioException("Tipologia não encontrada.");
        if (!await db.Composicoes.AnyAsync(c => c.Id == composicaoId && c.Ativa, ct)) throw new RegraDeNegocioException("Serviço (composição) não encontrado ou inativo.");
        var permitido = t.Tipo switch
        {
            TipoTipologia.Apartamento => alvo is AlvoServico.Ambiente or AlvoServico.Apartamento,
            TipoTipologia.Torre => alvo is AlvoServico.Ambiente or AlvoServico.Pavimento or AlvoServico.Torre,
            _ => alvo is AlvoServico.Ambiente or AlvoServico.Condominio
        };
        if (!permitido) throw new RegraDeNegocioException($"Nível {alvo} não se aplica a tipologia de {t.Tipo}.");
        if (alvo == AlvoServico.Ambiente && (ambienteId is null || t.Ambientes.All(a => a.Id != ambienteId))) throw new RegraDeNegocioException("Escolha o ambiente da tipologia.");
        if (alvo != AlvoServico.Ambiente) ambienteId = null;
        var atual = await db.ServicoAplicacoes.FirstOrDefaultAsync(a => a.TipologiaId == tipologiaId && a.ComposicaoId == composicaoId && a.Alvo == alvo && a.AmbienteModeloId == ambienteId, ct);
        if (marcar && atual is null)
            db.ServicoAplicacoes.Add(new ServicoAplicacao { ObraId = t.ObraId, TipologiaId = tipologiaId, ComposicaoId = composicaoId, Alvo = alvo, AmbienteModeloId = ambienteId, QuantidadeFixa = fixa });
        else if (marcar && atual is not null) atual.QuantidadeFixa = fixa;
        else if (!marcar && atual is not null) db.Remove(atual);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Gera/atualiza a matriz serviço × local da obra a partir das tipologias. Células travadas (editadas à mão) ficam como estão;
    /// as que deixaram de existir são excluídas (exclusão lógica).
    /// </summary>
    public async Task<ResultadoOrcamento> GerarAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var locais = await db.Locais.AsNoTracking().Where(l => l.ObraId == obraId).ToListAsync(ct);
        var aps = await db.ServicoAplicacoes.AsNoTracking().Where(a => a.ObraId == obraId).ToListAsync(ct);
        var usadas = aps.Select(a => a.ComposicaoId).Distinct().ToList();
        var comps = await db.Composicoes.AsNoTracking().Include(c => c.Itens).Where(c => usadas.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
        var novas = MotorOrcamento.Gerar(locais, aps, comps);
        var atuais = await db.ServicosLocais.Where(s => s.ObraId == obraId).ToListAsync(ct);
        var porChave = atuais.ToDictionary(s => (s.ComposicaoId, s.LocalId));
        int criadas = 0, atualizadas = 0, removidas = 0, travadas = 0;
        foreach (var n in novas)
        {
            var cu = comps[n.ComposicaoId].CustoUnitario;
            if (porChave.TryGetValue((n.ComposicaoId, n.LocalId), out var s))
            {
                if (s.Travado) { travadas++; continue; }
                if (s.Quantidade != n.Quantidade || s.CustoUnitario != cu || s.SpeId != n.SpeId) atualizadas++;
                s.Quantidade = n.Quantidade; s.CustoUnitario = cu; s.Custo = Math.Round(n.Quantidade * cu, 2); s.SpeId = n.SpeId;
            }
            else
            {
                db.ServicosLocais.Add(new ServicoLocal { ObraId = obraId, ComposicaoId = n.ComposicaoId, LocalId = n.LocalId, SpeId = n.SpeId, Quantidade = n.Quantidade, CustoUnitario = cu, Custo = Math.Round(n.Quantidade * cu, 2) });
                criadas++;
            }
        }
        var chaves = novas.Select(n => (n.ComposicaoId, n.LocalId)).ToHashSet();
        foreach (var s in atuais.Where(s => !s.Travado && s.Origem == OrigemCelula.Tipologia && !chaves.Contains((s.ComposicaoId, s.LocalId)))) { db.Remove(s); removidas++; }
        await db.SaveChangesAsync(ct);
        var total = await db.ServicosLocais.Where(s => s.ObraId == obraId).SumAsync(s => s.Custo, ct);
        return new ResultadoOrcamento(criadas, atualizadas, removidas, travadas, total);
    }

    /// <summary>Edição manual de uma célula (trava contra o gerador).</summary>
    public async Task EditarCelulaAsync(Guid id, decimal quantidade, bool travar, CancellationToken ct = default)
    {
        if (quantidade < 0) throw new RegraDeNegocioException("Quantidade não pode ser negativa.");
        await using var db = fabrica.Criar();
        var s = await db.ServicosLocais.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new RegraDeNegocioException("Célula não encontrada (o orçamento foi gerado de novo?). Atualize a tela.");
        s.Quantidade = quantidade; s.Custo = Math.Round(quantidade * s.CustoUnitario, 2); s.Travado = travar;
        // Destravada volta a ser do gerador (pode ser recalculada ou removida na próxima geração).
        s.Origem = travar ? OrigemCelula.Manual : OrigemCelula.Tipologia;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Demanda para os locais escolhidos (torre, pavimentos, apartamentos…): materiais por produto (composição × coeficiente × perda)
    /// ou serviços por composição, com orçado, já requisitado e saldo por local.
    /// </summary>
    public async Task<List<LinhaDemanda>> DemandaAsync(Guid obraId, IReadOnlyCollection<Guid> composicoes, IReadOnlyCollection<Guid> locaisEscolhidos, TipoRequisicao tipo,
        Guid? ignorarRequisicaoId = null, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var locais = await db.Locais.AsNoTracking().Where(l => l.ObraId == obraId).ToListAsync(ct);
        var filhos = locais.ToLookup(l => l.LocalPaiId);
        var alvo = locaisEscolhidos.SelectMany(l => MotorOrcamento.Subarvore(l, filhos)).ToHashSet();
        var cels = await db.ServicosLocais.AsNoTracking().Where(s => s.ObraId == obraId && composicoes.Contains(s.ComposicaoId)).ToListAsync(ct);
        cels = cels.Where(c => alvo.Contains(c.LocalId)).ToList();
        var comps = await db.Composicoes.AsNoTracking().Include(c => c.Itens).Where(c => composicoes.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
        cels = cels.Where(c => comps.ContainsKey(c.ComposicaoId)).ToList(); // composição excluída/inativa fica de fora
        var servico = tipo == TipoRequisicao.Servico;
        var reqLocais = (await (from rl in db.RequisicaoItemLocais.AsNoTracking()
                                join ri in db.RequisicaoItens.AsNoTracking() on rl.RequisicaoItemId equals ri.Id
                                join rq in db.Requisicoes.AsNoTracking() on ri.RequisicaoId equals rq.Id
                                where rq.ObraId == obraId && rq.Id != ignorarRequisicaoId && ri.Status != StatusItemRequisicao.Cancelado
                                      && rq.Status != StatusRequisicao.Cancelada && rq.Status != StatusRequisicao.NaoAutorizada
                                      && (servico ? ri.ProdutoId == null : ri.ProdutoId != null)
                                select new { rl.LocalId, rl.ComposicaoId, ri.ProdutoId, rl.Quantidade }).ToListAsync(ct))
                        .Where(x => alvo.Contains(x.LocalId)).ToList();
        var r = new List<LinhaDemanda>();
        if (tipo == TipoRequisicao.Servico)
        {
            var req = reqLocais.GroupBy(x => (x.ComposicaoId, x.LocalId)).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantidade));
            foreach (var g in cels.GroupBy(c => c.ComposicaoId))
            {
                var comp = comps[g.Key];
                var porLocal = g.Select(c => (c.LocalId, c.ComposicaoId, c.Quantidade, req.GetValueOrDefault(((Guid?)c.ComposicaoId, c.LocalId)))).ToList();
                var refUnit = comp.Custo(TipoInsumo.MaoObraEmpreitada) + comp.Custo(TipoInsumo.ServicoTerceiro);
                r.Add(new LinhaDemanda(null, comp.Id, comp.Descricao, comp.Unidade, porLocal.Sum(x => x.Quantidade), porLocal.Sum(x => x.Item4),
                    refUnit > 0 ? refUnit : comp.CustoUnitario, comp.ServicoControladoId, comp.Grupo, porLocal.Select(x => new LocalDemanda(x.LocalId, x.ComposicaoId, x.Quantidade, x.Item4)).ToList()));
            }
        }
        else
        {
            var linhas = from c in cels
                         from i in comps[c.ComposicaoId].Itens
                         where i.Tipo == TipoInsumo.Material && i.ProdutoId != null
                         select new { Prod = i.ProdutoId!.Value, i.Descricao, i.Unidade, i.CustoUnitario, c.LocalId, c.ComposicaoId, Qtd = i.QuantidadePor(c.Quantidade) };
            var prodIds = linhas.Select(x => x.Prod).Distinct().ToList();
            var prods = await db.Produtos.AsNoTracking().Where(p => prodIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
            // Requisitado por produto × local × composição; o que foi requisitado sem composição (manual) abate uma vez só,
            // na primeira composição que usa o produto naquele local (antes contava em todas → saldo subestimado).
            var reqComp = reqLocais.Where(y => y.ComposicaoId != null).GroupBy(y => (y.ProdutoId!.Value, y.LocalId, y.ComposicaoId!.Value)).ToDictionary(x => x.Key, x => x.Sum(y => y.Quantidade));
            var reqSem = reqLocais.Where(y => y.ComposicaoId == null).GroupBy(y => (y.ProdutoId!.Value, y.LocalId)).ToDictionary(x => x.Key, x => x.Sum(y => y.Quantidade));
            foreach (var g in linhas.GroupBy(x => x.Prod))
            {
                var semUsado = new HashSet<Guid>();
                var porLocal = g.GroupBy(x => (x.LocalId, x.ComposicaoId)).OrderBy(x => x.Key.LocalId).ThenBy(x => x.Key.ComposicaoId)
                    .Select(x => (x.Key.LocalId, x.Key.ComposicaoId, x.Sum(y => y.Qtd),
                        reqComp.GetValueOrDefault((g.Key, x.Key.LocalId, x.Key.ComposicaoId))
                        + (semUsado.Add(x.Key.LocalId) ? reqSem.GetValueOrDefault((g.Key, x.Key.LocalId)) : 0))).ToList();
                var p = prods.GetValueOrDefault(g.Key);
                r.Add(new LinhaDemanda(g.Key, null, p?.Nome ?? g.First().Descricao, p?.Unidade ?? g.First().Unidade, porLocal.Sum(x => x.Item3), porLocal.Sum(x => x.Item4),
                    p?.PrecoReferencia ?? g.First().CustoUnitario, null, p?.Grupo, porLocal.Select(x => new LocalDemanda(x.LocalId, x.ComposicaoId, x.Item3, x.Item4)).ToList()));
            }
        }
        return r.OrderBy(x => x.Grupo).ThenBy(x => x.Descricao).ToList();
    }

    /// <summary>SPE dos locais escolhidos (uma só; mais de uma = erro).</summary>
    public async Task<Guid?> SpeDosLocaisAsync(Guid obraId, IReadOnlyCollection<Guid> locais, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var spes = await db.Locais.AsNoTracking().Where(l => l.ObraId == obraId && locais.Contains(l.Id)).Select(l => l.SpeId).Distinct().ToListAsync(ct);
        if (spes.Count(s => s != null) > 1) throw new RegraDeNegocioException("Os locais escolhidos são de SPEs (condomínios) diferentes: faça uma requisição por SPE.");
        return spes.FirstOrDefault(s => s != null);
    }

    /// <summary>Converte a demanda em itens de requisição (quantidade = saldo, quebrada por local).</summary>
    public static List<RequisicaoItem> Itens(Guid requisicaoId, IEnumerable<LinhaDemanda> linhas, IReadOnlyDictionary<Guid, Domain.Cadastros.Produto> produtos)
    {
        var r = new List<RequisicaoItem>();
        foreach (var l in linhas.Where(l => l.Saldo > 0))
        {
            var i = new RequisicaoItem { RequisicaoId = requisicaoId, Descricao = l.Descricao, Unidade = l.Unidade, ValorReferencia = l.ValorReferencia, ComposicaoId = l.ComposicaoId, ServicoControladoId = l.ServicoControladoId };
            if (l.ProdutoId is { } pid && produtos.TryGetValue(pid, out var p)) i.HerdarDoProduto(p);
            foreach (var x in l.Locais)
            {
                var q = Math.Round(Math.Max(0, x.Orcado - x.Requisitado), 3);
                if (q > 0) i.Locais.Add(new RequisicaoItemLocal { RequisicaoItemId = i.Id, LocalId = x.LocalId, ComposicaoId = x.ComposicaoId, Quantidade = q });
            }
            i.Quantidade = Math.Round(i.Locais.Sum(x => x.Quantidade), 3);
            if (i.Quantidade > 0) r.Add(i);
        }
        return r;
    }
}
