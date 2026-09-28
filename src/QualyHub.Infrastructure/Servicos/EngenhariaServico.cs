using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;
using QualyHub.Infrastructure.Importacao;

namespace QualyHub.Infrastructure.Servicos;

public sealed record LinhaResumoSpe(Guid SpeId, string Spe, string? Quadra, string Cnpj, int Pavimentos, int Unidades, decimal AreaPrivativa, decimal FracaoSoma,
    IReadOnlyList<(string Tipologia, int Unidades, decimal Area)> PorTipologia);

/// <summary>Fase 1 do módulo de engenharia: SPE, tipologias, gerador do empreendimento e fração ideal.</summary>
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
        if (s.QuadraId is { } qid && !await db.Quadras.AnyAsync(q => q.Id == qid && q.ObraId == s.ObraId, ct)) throw new RegraDeNegocioException("Quadra/condomínio não pertence a esta obra.");
        await Grafo.SalvarAsync(db, s);
        await db.SaveChangesAsync(ct);
    }

    public async Task SalvarQuadraAsync(Quadra q, CancellationToken ct = default)
    {
        var e = new List<string>();
        q.Codigo = (q.Codigo ?? "").Trim().ToUpperInvariant();
        q.Nome = (q.Nome ?? "").Trim();
        if (q.Nome.Length == 0) e.Add("Informe o nome (ex.: Quadra C).");
        if (q.Codigo.Length is < 1 or > 6 || !q.Codigo.All(char.IsLetterOrDigit)) e.Add("Sigla de 1 a 6 letras/números (ex.: QC).");
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
            throw new RegraDeNegocioException("Já existe quadra/condomínio com esta sigla ou nome nesta obra.");
        var antiga = await db.Quadras.AsNoTracking().Where(x => x.Id == q.Id).Select(x => x.Codigo).FirstOrDefaultAsync(ct);
        if (q.LocalId is { } lid)
        {
            if (antiga is not null && antiga != q.Codigo) throw new RegraDeNegocioException("A sigla não pode mudar depois de gerar prédios e unidades (os códigos já estão em uso).");
            var local = await db.Locais.FirstOrDefaultAsync(l => l.Id == lid, ct);
            if (local is not null) local.Nome = q.Nome;
        }
        await Grafo.SalvarAsync(db, q);
        await db.SaveChangesAsync(ct);
    }

    public async Task SalvarTipologiaAsync(Tipologia t, CancellationToken ct = default)
    {
        var e = new List<string>();
        t.Codigo = (t.Codigo ?? "").Trim().ToUpperInvariant();
        if (t.Codigo.Length == 0) e.Add("Informe o código (ex.: 2Q-56).");
        if (string.IsNullOrWhiteSpace(t.Nome)) e.Add("Informe o nome.");
        if (t.AreaPrivativa <= 0) e.Add("Área privativa deve ser maior que zero.");
        if (t.Dormitorios < 0 || t.Suites < 0 || t.Suites > t.Dormitorios) e.Add("Suítes não podem passar do número de dormitórios.");
        if (t.ObraId == Guid.Empty) e.Add("Selecione a obra.");
        ValidacaoException.Se(e);
        await using var db = fabrica.Criar();
        if (await db.Tipologias.AnyAsync(x => x.Id != t.Id && x.ObraId == t.ObraId && x.Codigo == t.Codigo, ct)) throw new RegraDeNegocioException("Código já usado nesta obra.");
        await Grafo.SalvarAsync(db, t);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Gera prédios › pavimentos › unidades dentro da quadra/condomínio cadastrado (pavimentos iguais) e distribui a fração ideal.</summary>
    public async Task<int> GerarAsync(Guid obraId, Guid quadraId, IReadOnlyList<(string Nome, Guid SpeId, int Pavimentos, IReadOnlyList<(Guid TipologiaId, int Qtd)> Mix)> predios, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var quadra = await db.Quadras.FirstOrDefaultAsync(q => q.Id == quadraId && q.ObraId == obraId, ct) ?? throw new RegraDeNegocioException("Selecione a quadra/condomínio.");
        var spes = await db.Spes.Where(s => s.ObraId == obraId).ToDictionaryAsync(s => s.Id, ct);
        var tips = await db.Tipologias.Where(t => t.ObraId == obraId).ToDictionaryAsync(t => t.Id, ct);
        var specs = predios.Select(p => new PredioGerar(p.Nome, p.SpeId, spes.TryGetValue(p.SpeId, out var sp) ? sp.Codigo : "?", p.Pavimentos,
            p.Mix.Where(m => m.Qtd > 0 && tips.ContainsKey(m.TipologiaId)).Select(m => (tips[m.TipologiaId], m.Qtd)).ToList())).ToList();
        ValidacaoException.Se(GeradorEmpreendimento.Validar(quadra.Nome, specs));
        var ocupadas = await db.Locais.Where(l => l.ObraId == obraId && l.Tipo == GeradorEmpreendimento.TipoPredio && l.SpeId != null).Select(l => l.SpeId).ToListAsync(ct);
        if (specs.Any(p => ocupadas.Contains(p.SpeId))) throw new RegraDeNegocioException("Uma das SPEs já tem prédio cadastrado (1 prédio = 1 SPE).");
        if (specs.Any(p => spes[p.SpeId].QuadraId is { } outra && outra != quadra.Id)) throw new RegraDeNegocioException("Uma das SPEs está vinculada a outra quadra/condomínio.");

        Local? localQuadra = quadra.LocalId is { } lid ? await db.Locais.FirstOrDefaultAsync(l => l.Id == lid, ct) : null;
        var ordem = localQuadra is null ? 0 : await db.Locais.CountAsync(l => l.LocalPaiId == localQuadra.Id && l.Tipo == GeradorEmpreendimento.TipoPredio, ct);
        var locais = GeradorEmpreendimento.Gerar(obraId, quadra.Nome, quadra.Codigo, specs, localQuadra, ordem);
        if (localQuadra is null) quadra.LocalId = locais[0].Id;
        foreach (var p in specs) spes[p.SpeId].QuadraId = quadra.Id;
        db.Locais.AddRange(locais);
        await db.SaveChangesAsync(ct);
        return locais.Count;
    }

    /// <summary>Resumo por SPE: pavimentos, unidades, área privativa, soma da fração ideal e quebra por tipologia.</summary>
    public async Task<List<LinhaResumoSpe>> ResumoAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var spes = await db.Spes.AsNoTracking().Where(s => s.ObraId == obraId && s.Ativa).OrderBy(s => s.Codigo).ToListAsync(ct);
        var quadras = await db.Quadras.AsNoTracking().Where(q => q.ObraId == obraId).ToDictionaryAsync(q => q.Id, q => q.Nome, ct);
        var tips = await db.Tipologias.AsNoTracking().Where(t => t.ObraId == obraId).ToDictionaryAsync(t => t.Id, t => t.Codigo, ct);
        var locais = await db.Locais.AsNoTracking().Where(l => l.ObraId == obraId && l.SpeId != null).ToListAsync(ct);
        return spes.Select(s =>
        {
            var us = locais.Where(l => l.SpeId == s.Id && l.Tipo == GeradorEmpreendimento.TipoUnidade).ToList();
            return new LinhaResumoSpe(s.Id, s.NomeExibicao, s.QuadraId is { } qd ? quadras.GetValueOrDefault(qd) : null, s.Cnpj, locais.Count(l => l.SpeId == s.Id && l.Tipo == GeradorEmpreendimento.TipoPavimento), us.Count,
                us.Sum(u => u.AreaPrivativa ?? 0), us.Sum(u => u.FracaoIdeal ?? 0),
                us.GroupBy(u => u.TipologiaId).Select(g => (g.Key is { } k ? tips.GetValueOrDefault(k, "?") : "—", g.Count(), g.Sum(u => u.AreaPrivativa ?? 0))).OrderBy(x => x.Item1).ToList());
        }).ToList();
    }

    /// <summary>Grava frações ideais informadas (ex.: do memorial de incorporação) — exige soma 100% por prédio.</summary>
    public async Task SalvarFracoesAsync(Guid obraId, IReadOnlyDictionary<Guid, decimal> fracoes, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var locais = await db.Locais.Where(l => l.ObraId == obraId).ToListAsync(ct);
        foreach (var l in locais) if (fracoes.TryGetValue(l.Id, out var f)) { if (f is < 0 or > 1) throw new RegraDeNegocioException($"{l.Nome}: fração fora de 0–100%."); l.FracaoIdeal = f; }
        ValidacaoException.Se(GeradorEmpreendimento.ConferirFracao(locais));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Recalcula a fração ideal do prédio pela área privativa (usar até ter o memorial).</summary>
    public async Task RecalcularFracaoAsync(Guid predioId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var todos = await db.Locais.Where(l => l.SpeId != null).ToListAsync(ct);
        var pred = todos.First(l => l.Id == predioId);
        var pavs = todos.Where(l => l.LocalPaiId == pred.Id).Select(l => l.Id).ToHashSet();
        var us = todos.Where(l => l.Tipo == GeradorEmpreendimento.TipoUnidade && l.LocalPaiId is { } p && pavs.Contains(p)).OrderBy(l => l.Codigo).ToList();
        GeradorEmpreendimento.DistribuirFracao(us);
        await db.SaveChangesAsync(ct);
    }
}
