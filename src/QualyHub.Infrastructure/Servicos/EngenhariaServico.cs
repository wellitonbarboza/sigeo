using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;
using QualyHub.Infrastructure.Dados;
using QualyHub.Infrastructure.Importacao;

namespace QualyHub.Infrastructure.Servicos;

public sealed record LinhaResumoCondominio(Guid QuadraId, string Condominio, string? Spe, string? Cnpj, int Torres, int Pavimentos, int Apartamentos, int Comodos, int AreasComuns,
    decimal AreaPrivativa, decimal FracaoSoma, IReadOnlyList<(string Tipologia, int Unidades, decimal Area)> PorTipologia);

public sealed record ResultadoSincronizacao(int Atualizados, int Criados);

/// <summary>
/// Estrutura do empreendimento: condomínio (= SPE), tipologias de apartamento/torre/implantação, gerador de locais,
/// sincronização com as tipologias e fração ideal por condomínio.
/// </summary>
public sealed class EngenhariaServico(FabricaDb fabrica)
{
    public async Task SalvarSpeAsync(Spe s, CancellationToken ct = default)
    {
        var e = new List<string>();
        var doc = Conversor.SomenteDigitos(s.Cnpj);
        if (!Conversor.CnpjValido(doc)) e.Add("CNPJ inválido.");
        if (string.IsNullOrWhiteSpace(s.RazaoSocial)) e.Add("Informe a razão social.");
        s.Codigo = (s.Codigo ?? "").Trim().ToUpperInvariant();
        if (s.Codigo.Length is < 2 or > 10 || !s.Codigo.All(char.IsLetterOrDigit)) e.Add("Sigla de 2 a 10 letras/números (ex.: IRE).");
        if (s.ObraId == Guid.Empty) e.Add("Selecione a obra (empreendimento).");
        ValidacaoException.Se(e);
        s.Cnpj = Convert.ToUInt64(doc).ToString(@"00\.000\.000\/0000\-00");
        await using var db = fabrica.Criar();
        if (await db.Spes.AnyAsync(x => x.Id != s.Id && x.Cnpj == s.Cnpj, ct)) throw new RegraDeNegocioException("Já existe SPE com este CNPJ.");
        if (await db.Spes.AnyAsync(x => x.Id != s.Id && x.ObraId == s.ObraId && x.Codigo == s.Codigo, ct)) throw new RegraDeNegocioException("Sigla já usada nesta obra.");
        if (s.QuadraId is { } qid)
        {
            if (!await db.Quadras.AnyAsync(q => q.Id == qid && q.ObraId == s.ObraId, ct)) throw new RegraDeNegocioException("Condomínio não pertence a esta obra.");
            if (await db.Spes.AnyAsync(x => x.Id != s.Id && x.QuadraId == qid, ct)) throw new RegraDeNegocioException("Este condomínio já tem SPE (1 condomínio = 1 SPE).");
        }
        await Grafo.SalvarAsync(db, s);
        await EstabelecimentosServico.SincronizarSpeAsync(db, s, ct);
        // Locais já gerados do condomínio passam a ser da SPE.
        if (s.QuadraId is { } q2 && await db.Quadras.Where(q => q.Id == q2).Select(q => q.LocalId).FirstOrDefaultAsync(ct) is { } raiz)
        {
            var locais = await db.Locais.Where(l => l.ObraId == s.ObraId).ToListAsync(ct);
            var sub = MotorOrcamento.Subarvore(raiz, locais.ToLookup(l => l.LocalPaiId));
            foreach (var l in locais.Where(l => sub.Contains(l.Id))) l.SpeId = s.Id;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task SalvarQuadraAsync(Quadra q, CancellationToken ct = default)
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
            else q.CnpjCondominio = Convert.ToUInt64(doc).ToString(@"00\.000\.000\/0000\-00");
        }
        ValidacaoException.Se(e);
        await using var db = fabrica.Criar();
        if (await db.Quadras.AnyAsync(x => x.Id != q.Id && x.ObraId == q.ObraId && (x.Codigo == q.Codigo || x.Nome == q.Nome), ct))
            throw new RegraDeNegocioException("Já existe condomínio com esta sigla ou nome nesta obra.");
        if (q.ImplantacaoId is { } im && !await db.Tipologias.AnyAsync(t => t.Id == im && t.Tipo == TipoTipologia.Implantacao, ct))
            throw new RegraDeNegocioException("Escolha uma tipologia do tipo Implantação.");
        var antiga = await db.Quadras.AsNoTracking().Where(x => x.Id == q.Id).Select(x => x.Codigo).FirstOrDefaultAsync(ct);
        if (q.LocalId is { } lid)
        {
            if (antiga is not null && antiga != q.Codigo) throw new RegraDeNegocioException("A sigla não pode mudar depois de gerar as torres (os códigos já estão em uso).");
            var local = await db.Locais.FirstOrDefaultAsync(l => l.Id == lid, ct);
            if (local is not null) { local.Nome = q.Nome; local.TipologiaId = q.ImplantacaoId; }
        }
        await Grafo.SalvarAsync(db, q);
        await db.SaveChangesAsync(ct);
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
            AreaEquivalente = o.AreaEquivalente, Pavimentos = o.Pavimentos, PrimeiroTerreo = o.PrimeiroTerreo, PeDireitoPadrao = o.PeDireitoPadrao, Observacoes = o.Observacoes
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
        var tips = await db.Tipologias.Include(t => t.Ambientes).Include(t => t.Mix).Where(t => t.ObraId == obraId).ToDictionaryAsync(t => t.Id, ct);
        foreach (var t in tips.Values) t.CompletarSiglas();
        var specs = torres.Select(t => new TorreGerar(t.Nome, tips.TryGetValue(t.TipologiaTorreId, out var tt) ? tt : new Tipologia { Nome = "?", Tipo = TipoTipologia.Apartamento })).ToList();
        var e = GeradorEmpreendimento.Validar(quadra, spe?.Id, specs, tips).ToList();
        Tipologia? impl = quadra.ImplantacaoId is { } im ? tips.GetValueOrDefault(im) : null;
        Local? cond = quadra.LocalId is { } lid ? await db.Locais.FirstOrDefaultAsync(l => l.Id == lid, ct) : null;
        var existentes = cond is null ? [] : await db.Locais.Where(l => l.LocalPaiId == cond.Id && l.Tipo == GeradorEmpreendimento.TipoTorre).Select(l => l.Nome).ToListAsync(ct);
        if (specs.Any(s => existentes.Contains(s.Nome.Trim(), StringComparer.OrdinalIgnoreCase))) e.Add("Já existe torre com este nome no condomínio.");
        ValidacaoException.Se(e);
        var ordem = cond is null ? 0 : await db.Locais.CountAsync(l => l.LocalPaiId == cond.Id && l.Tipo == GeradorEmpreendimento.TipoTorre, ct);
        var locais = GeradorEmpreendimento.Gerar(obraId, quadra, spe!.Id, impl, specs, tips, cond, ordem);
        if (cond is null) quadra.LocalId = locais[0].Id;
        db.Locais.AddRange(locais);
        await db.SaveChangesAsync(ct);
        await RecalcularFracaoAsync(quadra.Id, ct);
        return locais.Count;
    }

    /// <summary>
    /// Sincroniza os locais gerados com as tipologias: atualiza medidas/categoria dos cômodos e áreas comuns e cria os ambientes
    /// que foram incluídos na tipologia depois da geração. Não apaga locais (podem ter FVS, requisição e medição).
    /// </summary>
    public async Task<ResultadoSincronizacao> SincronizarAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var tips = await db.Tipologias.Include(t => t.Ambientes).Include(t => t.Mix).Where(t => t.ObraId == obraId).ToDictionaryAsync(t => t.Id, ct);
        foreach (var t in tips.Values) t.CompletarSiglas();
        var amb = tips.Values.SelectMany(t => t.Ambientes.Select(a => (Tip: t, Amb: a))).ToDictionary(x => x.Amb.Id);
        var locais = await db.Locais.Where(l => l.ObraId == obraId).ToListAsync(ct);
        int atual = 0, criados = 0;
        foreach (var l in locais.Where(l => l.AmbienteModeloId is { } a && amb.ContainsKey(a)))
        {
            var (t, a) = amb[l.AmbienteModeloId!.Value];
            var antes = (l.AreaPiso, l.Perimetro, l.PeDireito, l.AreaParede, l.Categoria, l.Nome);
            GeradorEmpreendimento.CopiarMedidas(l, a, t.PeDireitoPadrao);
            l.Nome = a.Nome;
            if (antes != (l.AreaPiso, l.Perimetro, l.PeDireito, l.AreaParede, l.Categoria, l.Nome)) atual++;
        }
        var filhos = locais.ToLookup(l => l.LocalPaiId);
        void Garantir(Local pai, IEnumerable<TipologiaAmbiente> modelos, decimal? pd, string tipo)
        {
            foreach (var a in modelos.Where(a => filhos[pai.Id].All(f => f.AmbienteModeloId != a.Id)))
            {
                var n = GeradorEmpreendimento.Ambiente(obraId, pai, a, pd, tipo);
                if (locais.Any(x => x.Codigo == n.Codigo)) continue;
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
        var q = await db.Quadras.FirstAsync(x => x.Id == quadraId, ct);
        if (q.LocalId is not { } raiz) return;
        var locais = await db.Locais.Where(l => l.ObraId == q.ObraId).ToListAsync(ct);
        var sub = MotorOrcamento.Subarvore(raiz, locais.ToLookup(l => l.LocalPaiId));
        var us = locais.Where(l => sub.Contains(l.Id) && l.Tipo == GeradorEmpreendimento.TipoApartamento).OrderBy(l => l.Codigo).ToList();
        GeradorEmpreendimento.DistribuirFracao(us);
        await db.SaveChangesAsync(ct);
    }
}
