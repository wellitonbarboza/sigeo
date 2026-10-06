using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;

namespace QualyHub.Application.Servicos;

public sealed record LinhaResumoCondominio(Guid QuadraId, string Condominio, string? Spe, string? Cnpj, int Torres, int Pavimentos, int Apartamentos, int Comodos, int AreasComuns,
    decimal AreaPrivativa, decimal FracaoSoma, IReadOnlyList<(string Tipologia, int Unidades, decimal Area)> PorTipologia);

public sealed record ResultadoSincronizacao(int Atualizados, int Criados);

/// <summary>
/// Estrutura do empreendimento: condomínio (= SPE), tipologias de apartamento/torre/implantação, gerador de locais,
/// sincronização com as tipologias e fração ideal por condomínio.
/// </summary>
public sealed class EngenhariaServico(IFabricaDb fabrica)
{
    /// <summary>Validação dos dados da obra (regra pura). Normaliza a UF.</summary>
    public static List<string> PrepararObra(Obra o)
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(o.Codigo)) e.Add("Informe o código.");
        if (string.IsNullOrWhiteSpace(o.Nome)) e.Add("Informe o nome.");
        if (string.IsNullOrWhiteSpace(o.EnderecoEntrega)) e.Add("Informe o endereço de entrega padrão.");
        o.Uf = o.Uf?.Trim().ToUpperInvariant();
        return e;
    }

    /// <summary>Grava só os dados da obra; os locais têm tela e regras próprias (gerador/sincronização).</summary>
    public async Task SalvarObraAsync(Obra o, CancellationToken ct = default)
    {
        ValidacaoException.Se(PrepararObra(o));
        await using var db = fabrica.Criar();
        o.Locais = []; // coleção não sincronizada aqui: vazia evita que o Grafo tente inserir locais carregados
        await Grafo.SalvarAsync(db, o);
        await db.SaveChangesAsync(ct);
        // Engenheiro da obra (obras sem SPE) vale também para as aprovações em andamento.
        if (await AprovacaoServico.RecalcularPendentesAsync(db, x => x.ObraId == o.Id, ct) > 0) await db.SaveChangesAsync(ct);
    }

    public async Task SalvarSpeAsync(Spe s, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await SalvarSpeAsync(db, s, ct);
        await db.SaveChangesAsync(ct);
        // Engenheiro da SPE vale também para as aprovações em andamento (etapas ainda não decididas).
        if (await AprovacaoServico.RecalcularPendentesAsync(db, x => x.ObraId == s.ObraId, ct) > 0) await db.SaveChangesAsync(ct);
    }

    /// <summary>Condomínio + SPE numa transação só (a tela de condomínio grava os dois juntos).</summary>
    public Task SalvarCondominioAsync(Quadra q, Spe? spe, CancellationToken ct = default) =>
        fabrica.EmTransacaoAsync(async db =>
        {
            await SalvarQuadraAsync(db, q, ct);
            await db.SaveChangesAsync(ct);
            if (spe is not null)
            {
                spe.ObraId = q.ObraId; spe.QuadraId = q.Id;
                await SalvarSpeAsync(db, spe, ct);
                await db.SaveChangesAsync(ct);
                if (await AprovacaoServico.RecalcularPendentesAsync(db, x => x.ObraId == spe.ObraId, ct) > 0) await db.SaveChangesAsync(ct);
            }
        }, ct);

    private static async Task SalvarSpeAsync(IQualyHubDb db, Spe s, CancellationToken ct)
    {
        var e = new List<string>();
        var doc = Conversor.SomenteDigitos(s.Cnpj);
        if (!Conversor.CnpjValido(doc)) e.Add("CNPJ inválido.");
        if (string.IsNullOrWhiteSpace(s.RazaoSocial)) e.Add("Informe a razão social.");
        s.Codigo = (s.Codigo ?? "").Trim().ToUpperInvariant();
        if (s.Codigo.Length is < 2 or > 10 || !s.Codigo.All(char.IsLetterOrDigit)) e.Add("Sigla de 2 a 10 letras/números (ex.: IRE).");
        if (s.ObraId == Guid.Empty) e.Add("Selecione a obra (empreendimento).");
        ValidacaoException.Se(e);
        s.Cnpj = Conversor.FormatarCnpj(doc);
        if (await db.Spes.AnyAsync(x => x.Id != s.Id && x.Cnpj == s.Cnpj, ct)) throw new RegraDeNegocioException("Já existe SPE com este CNPJ.");
        if (await db.Spes.AnyAsync(x => x.Id != s.Id && x.ObraId == s.ObraId && x.Codigo == s.Codigo, ct)) throw new RegraDeNegocioException("Sigla já usada nesta obra.");
        if (s.QuadraId is { } qid)
        {
            if (!await db.Quadras.AnyAsync(q => q.Id == qid && q.ObraId == s.ObraId, ct)) throw new RegraDeNegocioException("Condomínio não pertence a esta obra.");
            if (await db.Spes.AnyAsync(x => x.Id != s.Id && x.QuadraId == qid, ct)) throw new RegraDeNegocioException("Este condomínio já tem SPE (1 condomínio = 1 SPE).");
        }
        var quadraAntiga = await db.Spes.AsNoTracking().Where(x => x.Id == s.Id).Select(x => x.QuadraId).FirstOrDefaultAsync(ct);
        await Grafo.SalvarAsync(db, s);
        await EstabelecimentosServico.SincronizarSpeAsync(db, s, ct);
        // Locais (e células do orçamento) do condomínio passam a ser da SPE; se a SPE saiu de outro condomínio, os locais de lá ficam sem SPE.
        var locais = await db.Locais.Where(l => l.ObraId == s.ObraId).ToListAsync(ct);
        var filhos = locais.ToLookup(l => l.LocalPaiId);
        async Task<HashSet<Guid>> Arvore(Guid? quadraId) =>
            quadraId is { } qid && await db.Quadras.Where(q => q.Id == qid).Select(q => q.LocalId).FirstOrDefaultAsync(ct) is { } raiz
                ? MotorOrcamento.Subarvore(raiz, filhos) : [];
        if (quadraAntiga is { } qa && qa != s.QuadraId)
        {
            var velha = await Arvore(qa);
            foreach (var l in locais.Where(l => velha.Contains(l.Id) && l.SpeId == s.Id)) l.SpeId = null;
            await db.ServicosLocais.Where(c => velha.Contains(c.LocalId) && c.SpeId == s.Id).ExecuteUpdateAsync(u => u.SetProperty(c => c.SpeId, (Guid?)null), ct);
        }
        var nova = await Arvore(s.QuadraId);
        foreach (var l in locais.Where(l => nova.Contains(l.Id))) l.SpeId = s.Id;
        if (nova.Count > 0) await db.ServicosLocais.Where(c => nova.Contains(c.LocalId)).ExecuteUpdateAsync(u => u.SetProperty(c => c.SpeId, (Guid?)s.Id), ct);
    }

    public async Task SalvarQuadraAsync(Quadra q, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await SalvarQuadraAsync(db, q, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SalvarQuadraAsync(IQualyHubDb db, Quadra q, CancellationToken ct)
    {
        var e = new List<string>();
        q.Codigo = (q.Codigo ?? "").Trim().ToUpperInvariant();
        q.Nome = (q.Nome ?? "").Trim();
        if (q.Nome.Length == 0) e.Add("Informe o nome (ex.: Condomínio Dona Irene).");
        if (q.Codigo.Length is < 1 or > 6 || !q.Codigo.All(char.IsLetterOrDigit)) e.Add("Sigla de 1 a 6 letras/números (ex.: QA).");
        if (string.IsNullOrWhiteSpace(q.Tipo)) e.Add("Informe o tipo (quadra, condomínio…).");
        if (q.ObraId == Guid.Empty) e.Add("Selecione a obra.");
        if (q.AreaTerreno is < 0) e.Add("Área do terreno não pode ser negativa.");
        if (!string.IsNullOrWhiteSpace(q.CnpjCondominio))
        {
            var doc = Conversor.SomenteDigitos(q.CnpjCondominio);
            if (!Conversor.CnpjValido(doc)) e.Add("CNPJ do condomínio inválido.");
            else q.CnpjCondominio = Conversor.FormatarCnpj(doc);
        }
        ValidacaoException.Se(e);
        if (await db.Quadras.AnyAsync(x => x.Id != q.Id && x.ObraId == q.ObraId && (x.Codigo == q.Codigo || x.Nome == q.Nome), ct))
            throw new RegraDeNegocioException("Já existe condomínio com esta sigla ou nome nesta obra.");
        if (q.ImplantacaoId is { } im && !await db.Tipologias.AnyAsync(t => t.Id == im && t.Tipo == TipoTipologia.Implantacao, ct))
            throw new RegraDeNegocioException("Escolha uma tipologia do tipo Implantação.");
        // Sigla e vínculo com a árvore de locais vêm do banco (a tela pode estar desatualizada).
        var antiga = await db.Quadras.AsNoTracking().Where(x => x.Id == q.Id).Select(x => new { x.Codigo, x.LocalId }).FirstOrDefaultAsync(ct);
        q.LocalId = antiga?.LocalId;
        if (q.LocalId is { } lid)
        {
            if (antiga!.Codigo != q.Codigo) throw new RegraDeNegocioException("A sigla não pode mudar depois de gerar as torres (os códigos já estão em uso).");
            var local = await db.Locais.FirstOrDefaultAsync(l => l.Id == lid, ct);
            if (local is not null) { local.Nome = q.Nome; local.TipologiaId = q.ImplantacaoId; }
        }
        await Grafo.SalvarAsync(db, q);
    }

    public async Task SalvarTipologiaAsync(Tipologia t, CancellationToken ct = default)
    {
        t.Codigo = (t.Codigo ?? "").Trim().ToUpperInvariant();
        var ordem = 0;
        foreach (var a in t.Ambientes.OrderBy(a => a.Ordem)) { a.Ordem = ++ordem; a.TipologiaId = t.Id; a.Nome = (a.Nome ?? "").Trim(); }
        ordem = 0;
        foreach (var m in t.Mix.OrderBy(m => m.Ordem)) { m.Ordem = ++ordem; m.TipologiaId = t.Id; }
        if (t.Tipo == TipoTipologia.Apartamento) foreach (var a in t.Ambientes) a.Escopo = EscopoAmbiente.Comodo;
        if (t.Tipo == TipoTipologia.Implantacao) foreach (var a in t.Ambientes) a.Escopo = EscopoAmbiente.AreaComumCondominio;
        if (t.Tipo != TipoTipologia.Torre) t.Mix.Clear();
        t.CompletarSiglas();
        var e = t.Validar().ToList();
        if (t.ObraId == Guid.Empty) e.Add("Selecione a obra.");
        ValidacaoException.Se(e);
        await using var db = fabrica.Criar();
        if (await db.Tipologias.AnyAsync(x => x.Id != t.Id && x.ObraId == t.ObraId && x.Codigo == t.Codigo, ct)) throw new RegraDeNegocioException("Código já usado nesta obra.");
        var ids = t.Mix.Select(m => m.ApartamentoTipologiaId).Distinct().ToList();
        if (ids.Count > 0 && await db.Tipologias.CountAsync(x => ids.Contains(x.Id) && x.Tipo == TipoTipologia.Apartamento && x.ObraId == t.ObraId, ct) != ids.Count)
            throw new RegraDeNegocioException("Mix: use tipologias de apartamento desta obra.");
        var anterior = await db.Tipologias.AsNoTracking().Where(x => x.Id == t.Id).Select(x => (TipoTipologia?)x.Tipo).FirstOrDefaultAsync(ct);
        if (anterior is { } ant && ant != t.Tipo && await db.Locais.AnyAsync(l => l.TipologiaId == t.Id, ct))
            throw new RegraDeNegocioException("O tipo da tipologia não pode mudar depois de gerar locais.");
        // Ambiente removido da tipologia não pode estar em uso (locais gerados ou serviços marcados), senão vira referência órfã.
        var mantidos = t.Ambientes.Select(a => a.Id).ToList();
        var removidos = await db.TipologiaAmbientes.Where(a => a.TipologiaId == t.Id && !mantidos.Contains(a.Id)).Select(a => new { a.Id, a.Nome }).ToListAsync(ct);
        if (removidos.Count > 0)
        {
            var rids = removidos.Select(r => r.Id).ToList();
            var usados = await db.Locais.Where(l => l.AmbienteModeloId != null && rids.Contains(l.AmbienteModeloId.Value)).Select(l => l.AmbienteModeloId!.Value)
                .Union(db.ServicoAplicacoes.Where(a => a.AmbienteModeloId != null && rids.Contains(a.AmbienteModeloId.Value)).Select(a => a.AmbienteModeloId!.Value))
                .Distinct().ToListAsync(ct);
            if (usados.Count > 0)
                throw new RegraDeNegocioException($"Não é possível remover {string.Join(", ", removidos.Where(r => usados.Contains(r.Id)).Select(r => r.Nome))}: " +
                                                  "já há locais gerados ou serviços marcados para o ambiente. Desmarque os serviços e mantenha o ambiente (os locais podem ter FVS e medições).");
        }
        await Grafo.SalvarAsync(db, t, nameof(Tipologia.Ambientes), nameof(Tipologia.Mix));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Duplica a tipologia (ambientes, mix) com novo código — base para variações.</summary>
    public async Task<Guid> CopiarTipologiaAsync(Guid id, string codigo, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var o = await db.Tipologias.AsNoTracking().Include(x => x.Ambientes).Include(x => x.Mix).FirstAsync(x => x.Id == id, ct);
        var n = new Tipologia
        {
            ObraId = o.ObraId, Tipo = o.Tipo, Codigo = codigo, Nome = o.Nome + " (cópia)", Dormitorios = o.Dormitorios, Suites = o.Suites, AreaPrivativa = o.AreaPrivativa,
            AreaEquivalente = o.AreaEquivalente, Pavimentos = o.Pavimentos, PrimeiroTerreo = o.PrimeiroTerreo, NumeracaoPorAndar = o.NumeracaoPorAndar, PeDireitoPadrao = o.PeDireitoPadrao, Observacoes = o.Observacoes
        };
        foreach (var a in o.Ambientes) n.Ambientes.Add(new TipologiaAmbiente
        {
            TipologiaId = n.Id, Ordem = a.Ordem, Escopo = a.Escopo, Nome = a.Nome, Sigla = a.Sigla, Categoria = a.Categoria, AreaPiso = a.AreaPiso, Perimetro = a.Perimetro,
            PeDireito = a.PeDireito, AreaVaos = a.AreaVaos, PavimentoDe = a.PavimentoDe, PavimentoAte = a.PavimentoAte
        });
        foreach (var m in o.Mix) n.Mix.Add(new TipologiaMix { TipologiaId = n.Id, Ordem = m.Ordem, PavimentoDe = m.PavimentoDe, PavimentoAte = m.PavimentoAte, ApartamentoTipologiaId = m.ApartamentoTipologiaId, Quantidade = m.Quantidade });
        await SalvarTipologiaAsync(n, ct);
        return n.Id;
    }

    /// <summary>Gera as torres do condomínio (com pavimentos, apartamentos, cômodos e áreas comuns) e redistribui a fração ideal do condomínio.</summary>
    public async Task<int> GerarAsync(Guid obraId, Guid quadraId, IReadOnlyList<(string Nome, Guid TipologiaTorreId)> torres, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var quadra = await db.Quadras.FirstOrDefaultAsync(q => q.Id == quadraId && q.ObraId == obraId, ct) ?? throw new RegraDeNegocioException("Selecione o condomínio.");
        var spe = await db.Spes.FirstOrDefaultAsync(s => s.QuadraId == quadra.Id, ct);
        var tips = await db.Tipologias.AsNoTracking().Include(t => t.Ambientes).Include(t => t.Mix).Where(t => t.ObraId == obraId).ToDictionaryAsync(t => t.Id, ct);
        foreach (var t in tips.Values) t.CompletarSiglas();
        var specs = torres.Select(t => new TorreGerar(t.Nome, tips.TryGetValue(t.TipologiaTorreId, out var tt) ? tt : new Tipologia { Nome = "?", Tipo = TipoTipologia.Apartamento })).ToList();
        var e = GeradorEmpreendimento.Validar(quadra, spe?.Id, specs, tips).ToList();
        Tipologia? impl = quadra.ImplantacaoId is { } im ? tips.GetValueOrDefault(im) : null;
        Local? cond = quadra.LocalId is { } lid ? await db.Locais.FirstOrDefaultAsync(l => l.Id == lid, ct) : null;
        var existentes = cond is null ? [] : await db.Locais.Where(l => l.LocalPaiId == cond.Id && l.Tipo == GeradorEmpreendimento.TipoTorre).Select(l => l.Nome).ToListAsync(ct);
        if (specs.Any(s => existentes.Contains(s.Nome.Trim(), StringComparer.OrdinalIgnoreCase))) e.Add("Já existe torre com este nome no condomínio.");
        if (spe is null) e.Add("Cadastre a SPE do condomínio antes de gerar as torres.");
        ValidacaoException.Se(e);
        // Próximo número = maior já usado + 1 (torre excluída não libera o código, evita QA-T03 repetido).
        var ordem = cond is null ? 0 : await db.Locais.IgnoreQueryFilters([FiltrosNomes.Obra]).Where(l => l.LocalPaiId == cond.Id && l.Tipo == GeradorEmpreendimento.TipoTorre).MaxAsync(l => (int?)l.Ordem, ct) ?? 0;
        var locais = GeradorEmpreendimento.Gerar(obraId, quadra, spe!.Id, impl, specs, tips, cond, ordem);
        if (cond is null) quadra.LocalId = locais[0].Id;
        var idQuadra = quadra.Id; var raiz = quadra.LocalId;
        await fabrica.EmTransacaoAsync(async tdb =>
        {
            var q = await tdb.Quadras.FirstAsync(x => x.Id == idQuadra, ct);
            q.LocalId = raiz;
            tdb.Locais.AddRange(locais);
            await tdb.SaveChangesAsync(ct);
            await RecalcularFracaoAsync(tdb, q, ct);
            await tdb.SaveChangesAsync(ct);
        }, ct);
        return locais.Count;
    }

    /// <summary>
    /// Sincroniza os locais gerados com as tipologias: atualiza medidas/categoria dos cômodos e áreas comuns e cria os ambientes
    /// que foram incluídos na tipologia depois da geração. Não apaga locais (podem ter FVS, requisição e medição).
    /// </summary>
    public async Task<ResultadoSincronizacao> SincronizarAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var tips = await db.Tipologias.AsNoTracking().Include(t => t.Ambientes).Include(t => t.Mix).Where(t => t.ObraId == obraId).ToDictionaryAsync(t => t.Id, ct);
        foreach (var t in tips.Values) t.CompletarSiglas();
        var amb = tips.Values.SelectMany(t => t.Ambientes.Select(a => (Tip: t, Amb: a))).ToDictionary(x => x.Amb.Id);
        var locais = await db.Locais.Where(l => l.ObraId == obraId).ToListAsync(ct);
        int atual = 0, criados = 0;
        foreach (var l in locais.Where(l => l.AmbienteModeloId is { } a && amb.ContainsKey(a)))
        {
            var (t, a) = amb[l.AmbienteModeloId!.Value];
            // Só medidas e categoria: o nome do local pode ter sido ajustado na obra e é preservado.
            var antes = (l.AreaPiso, l.Perimetro, l.PeDireito, l.AreaParede, l.Categoria);
            GeradorEmpreendimento.CopiarMedidas(l, a, t.PeDireitoPadrao);
            if (antes != (l.AreaPiso, l.Perimetro, l.PeDireito, l.AreaParede, l.Categoria)) atual++;
        }
        var filhos = locais.ToLookup(l => l.LocalPaiId);
        var codigos = locais.Select(l => l.Codigo).ToHashSet(StringComparer.OrdinalIgnoreCase);
        void Garantir(Local pai, IEnumerable<TipologiaAmbiente> modelos, decimal? pd, string tipo)
        {
            foreach (var a in modelos.Where(a => filhos[pai.Id].All(f => f.AmbienteModeloId != a.Id)))
            {
                var n = GeradorEmpreendimento.Ambiente(obraId, pai, a, pd, tipo);
                if (!codigos.Add(n.Codigo)) continue;
                db.Locais.Add(n); locais.Add(n); criados++;
            }
        }
        foreach (var l in locais.ToList())
        {
            if (l.TipologiaId is not { } tid || !tips.TryGetValue(tid, out var t)) continue;
            if (l.Tipo == GeradorEmpreendimento.TipoApartamento) Garantir(l, t.Ambientes.Where(a => a.Escopo == EscopoAmbiente.Comodo), t.PeDireitoPadrao, GeradorEmpreendimento.TipoComodo);
            else if (l.Tipo == GeradorEmpreendimento.TipoTorre)
            {
                Garantir(l, t.Ambientes.Where(a => a.Escopo == EscopoAmbiente.AreaComumTorre), t.PeDireitoPadrao, GeradorEmpreendimento.TipoAreaComum);
                foreach (var pav in filhos[l.Id].Where(p => p.Tipo == GeradorEmpreendimento.TipoPavimento))
                    Garantir(pav, t.Ambientes.Where(a => a.Escopo == EscopoAmbiente.AreaComumPavimento && a.NoPavimento(pav.Ordem)), t.PeDireitoPadrao, GeradorEmpreendimento.TipoAreaComum);
            }
            else if (l.Tipo == GeradorEmpreendimento.TipoCondominio)
                Garantir(l, t.Ambientes.Where(a => a.Escopo == EscopoAmbiente.AreaComumCondominio), t.PeDireitoPadrao, GeradorEmpreendimento.TipoAreaComum);
        }
        await db.SaveChangesAsync(ct);
        return new ResultadoSincronizacao(atual, criados);
    }

    /// <summary>Resumo por condomínio (SPE): torres, pavimentos, apartamentos, cômodos, área privativa, fração ideal e mix por tipologia.</summary>
    public async Task<List<LinhaResumoCondominio>> ResumoAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var quadras = await db.Quadras.AsNoTracking().Where(q => q.ObraId == obraId).OrderBy(q => q.Ordem).ThenBy(q => q.Codigo).ToListAsync(ct);
        var spes = await db.Spes.AsNoTracking().Where(s => s.ObraId == obraId).ToListAsync(ct);
        var tips = await db.Tipologias.AsNoTracking().Where(t => t.ObraId == obraId).ToDictionaryAsync(t => t.Id, t => t.Codigo, ct);
        var locais = await db.Locais.AsNoTracking().Where(l => l.ObraId == obraId).ToListAsync(ct);
        var filhos = locais.ToLookup(l => l.LocalPaiId);
        return quadras.Select(q =>
        {
            var spe = spes.FirstOrDefault(s => s.QuadraId == q.Id);
            var sub = q.LocalId is { } r ? MotorOrcamento.Subarvore(r, filhos) : [];
            var ls = locais.Where(l => sub.Contains(l.Id)).ToList();
            var us = ls.Where(l => l.Tipo == GeradorEmpreendimento.TipoApartamento).ToList();
            return new LinhaResumoCondominio(q.Id, q.Nome, spe?.NomeExibicao, spe?.Cnpj, ls.Count(l => l.Tipo == GeradorEmpreendimento.TipoTorre),
                ls.Count(l => l.Tipo == GeradorEmpreendimento.TipoPavimento), us.Count, ls.Count(l => l.Tipo == GeradorEmpreendimento.TipoComodo),
                ls.Count(l => l.Tipo == GeradorEmpreendimento.TipoAreaComum), us.Sum(u => u.AreaPrivativa ?? 0), us.Sum(u => u.FracaoIdeal ?? 0),
                us.GroupBy(u => u.TipologiaId).Select(g => (g.Key is { } k ? tips.GetValueOrDefault(k, "?") : "—", g.Count(), g.Sum(u => u.AreaPrivativa ?? 0))).OrderBy(x => x.Item1).ToList());
        }).ToList();
    }

    /// <summary>Grava frações ideais informadas (ex.: do memorial de incorporação) — exige soma 100% por condomínio.</summary>
    public async Task SalvarFracoesAsync(Guid obraId, IReadOnlyDictionary<Guid, decimal> fracoes, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var locais = await db.Locais.Where(l => l.ObraId == obraId).ToListAsync(ct);
        foreach (var l in locais) if (fracoes.TryGetValue(l.Id, out var f)) { if (f is < 0 or > 1) throw new RegraDeNegocioException($"{l.Nome}: fração fora de 0–100%."); l.FracaoIdeal = f; }
        ValidacaoException.Se(GeradorEmpreendimento.ConferirFracao(locais));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Recalcula a fração ideal do condomínio pela área privativa (usar até ter o memorial).</summary>
    public async Task RecalcularFracaoAsync(Guid quadraId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var q = await db.Quadras.FirstOrDefaultAsync(x => x.Id == quadraId, ct) ?? throw new RegraDeNegocioException("Condomínio não encontrado.");
        await RecalcularFracaoAsync(db, q, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task RecalcularFracaoAsync(IQualyHubDb db, Quadra q, CancellationToken ct)
    {
        if (q.LocalId is not { } raiz) return;
        var locais = await db.Locais.Where(l => l.ObraId == q.ObraId).ToListAsync(ct);
        var sub = MotorOrcamento.Subarvore(raiz, locais.ToLookup(l => l.LocalPaiId));
        var us = locais.Where(l => sub.Contains(l.Id) && l.Tipo == GeradorEmpreendimento.TipoApartamento).OrderBy(l => l.Codigo).ToList();
        GeradorEmpreendimento.DistribuirFracao(us);
    }
}
