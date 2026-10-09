using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Planejamento;

namespace QualyHub.Application.Servicos;

/// <summary>
/// Médio e curto prazo do planejamento (Last Planner):
/// • médio prazo (lookahead): atividades das próximas semanas do cronograma vigente e as restrições que impedem cada uma;
/// • curto prazo: plano semanal com compromissos tirados do cronograma (só atividade sem restrição aberta), apontamento
///   concluído/não concluído com causa, fechamento da semana com PPC, série de PPC e Pareto das causas.
/// O que entra no plano depois da segunda-feira conta como "fora da meta" (não entra no PPC).
/// </summary>
public sealed class CurtoPrazoServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    public sealed record AtividadeHorizonte(string Chave, string Eap, string Nome, string Grupo, DateOnly Inicio, DateOnly Termino, decimal Avanco, bool Critica,
        int RestricoesAbertas, int RestricoesVencidas, bool Livre);
    public sealed record Horizonte(Guid? VersaoId, string? VersaoCodigo, bool Vigente, DateOnly De, DateOnly Ate, IReadOnlyList<AtividadeHorizonte> Atividades);
    public sealed record SemanaVista(PlanoSemanal? Plano, DateOnly Semana, IReadOnlyList<AtividadeHorizonte> Candidatas, bool PlanejamentoAberto);
    public sealed record PontoPpc(DateOnly Semana, int Planejados, int Concluidos, int ForaDaMeta, decimal? Ppc, bool Fechada);
    public sealed record Indicadores(IReadOnlyList<PontoPpc> Serie, decimal? PpcAcumulado, IReadOnlyList<(CausaNaoCumprimento Causa, int Qtd)> Causas,
        int RestricoesAbertas, int RestricoesVencidas, int RestricoesRemovidas, decimal? RemovidasNoPrazo);

    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    private Task ExigirAsync(IQualyHubDb db, string acao) =>
        Permissoes.ExigirAsync(db, usuario, acao, PerfilUsuario.Engenharia, PerfilUsuario.Diretoria, PerfilUsuario.Campo);

    /// <summary>Chave estável da atividade entre versões: a do gerador por redes; atividade manual usa o nome.</summary>
    public static string ChaveDe(AtividadeCronograma a)
    {
        var k = a.Chave ?? $"N:{a.Nome.Trim().ToUpperInvariant()}";
        return k.Length <= 80 ? k : k[..80];
    }

    /// <summary>Versão de referência: a vigente; sem aprovação ainda, a mais recente (avisando na tela).</summary>
    private static async Task<VersaoCronograma?> ReferenciaAsync(IQualyHubDb db, Guid obraId, CancellationToken ct) =>
        await db.VersoesCronograma.AsNoTracking().Where(v => v.ObraId == obraId && v.Status == StatusVersao.Vigente).OrderByDescending(v => v.Numero).FirstOrDefaultAsync(ct)
        ?? await db.VersoesCronograma.AsNoTracking().Where(v => v.ObraId == obraId).OrderByDescending(v => v.Numero).FirstOrDefaultAsync(ct);

    private async Task<(VersaoCronograma? Versao, List<AtividadeHorizonte> Atividades)> AtividadesAsync(IQualyHubDb db, Guid obraId, DateOnly de, DateOnly ate, CancellationToken ct)
    {
        var v = await ReferenciaAsync(db, obraId, ct);
        if (v is null) return (null, []);
        var todas = await db.AtividadesCronograma.AsNoTracking().Where(a => a.VersaoId == v.Id).ToListAsync(ct);
        var porId = todas.ToDictionary(a => a.Id);
        var folhas = todas.Where(a => !a.Resumo && a.Avanco < 100 && a.Inicio <= ate && a.Termino >= de).OrderBy(a => a.Inicio).ThenBy(a => a.Eap).ToList();
        var chaves = folhas.Select(ChaveDe).Distinct().ToList();
        var hoje = Hoje;
        var restr = await db.Restricoes.AsNoTracking().Where(r => r.ObraId == obraId && r.Status == StatusRestricao.Aberta && chaves.Contains(r.AtividadeChave))
            .Select(r => new { r.AtividadeChave, r.DataLimite }).ToListAsync(ct);
        var porChave = restr.GroupBy(r => r.AtividadeChave).ToDictionary(g => g.Key, g => (Abertas: g.Count(), Vencidas: g.Count(x => x.DataLimite < hoje)));
        string Grupo(AtividadeCronograma a) => a.PaiId is { } p && porId.TryGetValue(p, out var pai) ? pai.Nome : "";
        var lista = folhas.Select(a =>
        {
            var k = ChaveDe(a);
            var (ab, ve) = porChave.GetValueOrDefault(k);
            return new AtividadeHorizonte(k, a.Eap, a.Nome, Grupo(a), a.Inicio, a.Termino, a.Avanco, a.Critica, ab, ve, ab == 0);
        }).ToList();
        return (v, lista);
    }

    // ───────── Médio prazo ─────────

    public async Task<Horizonte> MedioPrazoAsync(Guid obraId, int semanas = 6, CancellationToken ct = default)
    {
        semanas = Math.Clamp(semanas, 1, 16);
        await using var db = fabrica.CriarLeitura();
        var de = PlanoSemanal.SegundaDe(Hoje);
        var ate = de.AddDays(7 * semanas - 1);
        var (v, ativ) = await AtividadesAsync(db, obraId, de, ate, ct);
        return new Horizonte(v?.Id, v?.Codigo, v?.Status == StatusVersao.Vigente, de, ate, ativ);
    }

    public async Task<List<Restricao>> RestricoesAsync(Guid obraId, bool somenteAbertas, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var q = db.Restricoes.Where(r => r.ObraId == obraId);
        if (somenteAbertas) q = q.Where(r => r.Status == StatusRestricao.Aberta);
        return await q.OrderBy(r => r.Status).ThenBy(r => r.DataLimite).Take(1000).ToListAsync(ct);
    }

    public async Task<Guid> SalvarRestricaoAsync(Restricao r, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "registrar restrições");
        if (r.ResponsavelId is { } rid)
            r.ResponsavelNome = await db.Usuarios.AsNoTracking().Where(u => u.Id == rid && u.Ativo).Select(u => u.Nome).FirstOrDefaultAsync(ct)
                                ?? throw new RegraDeNegocioException("Responsável não encontrado ou inativo.");
        ValidacaoException.Se(r.Validar());
        if (!usuario.AcessoTodasObras && !usuario.ObrasPermitidas.Contains(r.ObraId)) throw new RegraDeNegocioException("Você não tem acesso a essa obra.");
        var atual = await db.Restricoes.FirstOrDefaultAsync(x => x.Id == r.Id, ct);
        if (atual is null)
        {
            if (r.DataLimite < Hoje) throw new RegraDeNegocioException("A data-limite não pode ser no passado.");
            r.Numero = await Numerador.ProximoAsync(db, $"Restricao:{r.ObraId:N}", ct);
            r.Status = StatusRestricao.Aberta;
            db.Restricoes.Add(r);
        }
        else
        {
            if (!atual.Aberta) throw new RegraDeNegocioException("Restrição encerrada não pode ser alterada.");
            atual.Tipo = r.Tipo; atual.Descricao = r.Descricao.Trim(); atual.AtividadeChave = r.AtividadeChave; atual.AtividadeNome = r.AtividadeNome;
            atual.InicioPrevisto = r.InicioPrevisto; atual.ResponsavelId = r.ResponsavelId; atual.ResponsavelNome = r.ResponsavelNome; atual.DataLimite = r.DataLimite;
        }
        await db.SaveChangesAsync(ct);
        return r.Id;
    }

    public async Task RemoverRestricaoAsync(Guid id, string como, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "remover restrições");
        var r = await db.Restricoes.FirstAsync(x => x.Id == id, ct);
        r.Remover(como, usuario.Nome, relogio.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelarRestricaoAsync(Guid id, string motivo, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "cancelar restrições");
        var r = await db.Restricoes.FirstAsync(x => x.Id == id, ct);
        r.Cancelar(motivo, usuario.Nome, relogio.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    // ───────── Curto prazo (plano semanal) ─────────

    public async Task<SemanaVista> SemanaAsync(Guid obraId, DateOnly qualquerDia, CancellationToken ct = default)
    {
        var semana = PlanoSemanal.SegundaDe(qualquerDia);
        await using var db = fabrica.CriarLeitura();
        var plano = await db.PlanosSemanais.Include(p => p.Compromissos).FirstOrDefaultAsync(p => p.ObraId == obraId && p.Semana == semana, ct);
        if (plano is not null) plano.Compromissos = plano.Compromissos.OrderBy(c => c.ForaDaMeta).ThenBy(c => c.Ordem).ToList();
        var (_, ativ) = await AtividadesAsync(db, obraId, semana, semana.AddDays(6), ct);
        return new SemanaVista(plano, semana, ativ, Hoje <= semana);
    }

    public async Task<Guid> CriarPlanoAsync(Guid obraId, DateOnly qualquerDia, CancellationToken ct = default)
    {
        var semana = PlanoSemanal.SegundaDe(qualquerDia);
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "montar o plano semanal");
        if (!usuario.AcessoTodasObras && !usuario.ObrasPermitidas.Contains(obraId)) throw new RegraDeNegocioException("Você não tem acesso a essa obra.");
        if (semana > PlanoSemanal.SegundaDe(Hoje).AddDays(28)) throw new RegraDeNegocioException("Monte o plano semanal no máximo 4 semanas antes.");
        if (await db.PlanosSemanais.FirstOrDefaultAsync(p => p.ObraId == obraId && p.Semana == semana, ct) is { } ja) return ja.Id;
        var p = new PlanoSemanal { ObraId = obraId, Semana = semana };
        db.PlanosSemanais.Add(p);
        await db.SaveChangesAsync(ct);
        return p.Id;
    }

    private static async Task<PlanoSemanal> PlanoAbertoAsync(IQualyHubDb db, Guid planoId, CancellationToken ct)
    {
        var p = await db.PlanosSemanais.Include(x => x.Compromissos).FirstOrDefaultAsync(x => x.Id == planoId, ct)
                ?? throw new RegraDeNegocioException("Plano semanal não encontrado.");
        if (!p.Editavel) throw new RegraDeNegocioException("Semana fechada não pode ser alterada.");
        return p;
    }

    /// <summary>Inclui atividades do cronograma no plano. Atividade com restrição aberta é recusada (precisa remover a restrição antes).</summary>
    public async Task<int> AdicionarAtividadesAsync(Guid planoId, IReadOnlyCollection<string> chaves, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "montar o plano semanal");
        var p = await PlanoAbertoAsync(db, planoId, ct);
        var (_, ativ) = await AtividadesAsync(db, p.ObraId, p.Semana, p.Fim, ct);
        var porChave = ativ.GroupBy(a => a.Chave).ToDictionary(g => g.Key, g => g.First());
        var bloqueadas = chaves.Where(k => porChave.TryGetValue(k, out var a) && !a.Livre).Select(k => porChave[k].Nome).ToList();
        if (bloqueadas.Count > 0)
            throw new RegraDeNegocioException($"Com restrição aberta (remova antes de comprometer): {string.Join("; ", bloqueadas.Take(5))}{(bloqueadas.Count > 5 ? $" (+{bloqueadas.Count - 5})" : "")}.");
        var fora = Hoje > p.Semana;
        var ordem = p.Compromissos.Select(c => c.Ordem).DefaultIfEmpty(0).Max();
        var n = 0;
        foreach (var k in chaves.Distinct())
        {
            if (!porChave.TryGetValue(k, out var a) || p.Compromissos.Any(c => c.AtividadeChave == k)) continue;
            var c = new CompromissoSemanal { PlanoId = p.Id, Ordem = ++ordem, AtividadeChave = k, Descricao = Limitar($"{a.Eap} {a.Nome}".Trim(), 300)!, ForaDaMeta = fora };
            p.Compromissos.Add(c); db.CompromissosSemanais.Add(c); n++;
        }
        await db.SaveChangesAsync(ct);
        return n;
    }

    /// <summary>Compromisso avulso (fora do cronograma, ex.: limpeza, mobilização).</summary>
    public async Task AdicionarAvulsoAsync(Guid planoId, string descricao, string? meta, string? responsavel, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(descricao)) throw new RegraDeNegocioException("Descreva o compromisso.");
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "montar o plano semanal");
        var p = await PlanoAbertoAsync(db, planoId, ct);
        var c = new CompromissoSemanal
        {
            PlanoId = p.Id, Ordem = p.Compromissos.Select(x => x.Ordem).DefaultIfEmpty(0).Max() + 1, Descricao = Limitar(descricao.Trim(), 300)!,
            Meta = Limitar(meta?.Trim(), 150), Responsavel = Limitar(responsavel?.Trim(), 150), ForaDaMeta = Hoje > p.Semana
        };
        db.CompromissosSemanais.Add(c);
        await db.SaveChangesAsync(ct);
    }

    public async Task EditarCompromissoAsync(Guid id, string? meta, string? responsavel, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "montar o plano semanal");
        var c = await db.CompromissosSemanais.FirstAsync(x => x.Id == id, ct);
        await PlanoAbertoAsync(db, c.PlanoId, ct);
        c.Meta = Limitar(meta?.Trim(), 150); c.Responsavel = Limitar(responsavel?.Trim(), 150);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Retirar do plano só antes da semana começar (ou o que entrou fora da meta) — senão o PPC deixaria de medir.</summary>
    public async Task RetirarCompromissoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "montar o plano semanal");
        var c = await db.CompromissosSemanais.FirstAsync(x => x.Id == id, ct);
        var p = await PlanoAbertoAsync(db, c.PlanoId, ct);
        if (!c.ForaDaMeta && Hoje > p.Semana) throw new RegraDeNegocioException("Depois de a semana começar, compromisso planejado não sai do plano: aponte como não concluído e informe a causa.");
        db.CompromissosSemanais.Remove(c);
        await db.SaveChangesAsync(ct);
    }

    public async Task ApontarAsync(Guid id, SituacaoCompromisso situacao, CausaNaoCumprimento? causa, string? observacao, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "apontar o plano semanal");
        var c = await db.CompromissosSemanais.FirstAsync(x => x.Id == id, ct);
        await PlanoAbertoAsync(db, c.PlanoId, ct);
        c.Apontar(situacao, causa, Limitar(observacao, 500));
        await db.SaveChangesAsync(ct);
    }

    public async Task FecharSemanaAsync(Guid planoId, string? observacao, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "fechar a semana");
        var p = await PlanoAbertoAsync(db, planoId, ct);
        if (Hoje < p.Fim.AddDays(-2)) throw new RegraDeNegocioException($"A semana só pode ser fechada a partir de {p.Fim.AddDays(-2):dd/MM} (sexta-feira).");
        p.Observacao = string.IsNullOrWhiteSpace(observacao) ? p.Observacao : observacao.Trim();
        p.Fechar(usuario.Nome, relogio.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    // ───────── Indicadores ─────────

    public async Task<Indicadores> IndicadoresAsync(Guid obraId, int semanas = 12, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var desde = PlanoSemanal.SegundaDe(Hoje).AddDays(-7 * (semanas - 1));
        var planos = await db.PlanosSemanais.Include(p => p.Compromissos).Where(p => p.ObraId == obraId && p.Semana >= desde).OrderBy(p => p.Semana).ToListAsync(ct);
        var serie = planos.Select(p =>
        {
            var (pl, ok, fora, ppc) = PlanoSemanal.Apurar(p.Compromissos);
            return new PontoPpc(p.Semana, pl, ok, fora, p.Status == StatusPlanoSemanal.Fechado ? p.Ppc : ppc, p.Status == StatusPlanoSemanal.Fechado);
        }).ToList();
        var fechados = serie.Where(s => s.Fechada).ToList();
        var plTot = fechados.Sum(s => s.Planejados);
        decimal? acum = plTot == 0 ? null : Math.Round(100m * fechados.Sum(s => s.Concluidos) / plTot, 1);
        var causas = planos.SelectMany(p => p.Compromissos).Where(c => c.Situacao == SituacaoCompromisso.NaoConcluido && c.Causa != null)
            .GroupBy(c => c.Causa!.Value).Select(g => (g.Key, g.Count())).OrderByDescending(x => x.Item2).ToList();
        var hoje = Hoje;
        var rs = await db.Restricoes.Where(r => r.ObraId == obraId).Select(r => new { r.Status, r.DataLimite, r.RemovidaEm }).ToListAsync(ct);
        var removidas = rs.Where(r => r.Status == StatusRestricao.Removida).ToList();
        decimal? noPrazo = removidas.Count == 0 ? null
            : Math.Round(100m * removidas.Count(r => r.RemovidaEm is { } em && DateOnly.FromDateTime(em.ToLocalTime().Date) <= r.DataLimite) / removidas.Count, 1);
        return new Indicadores(serie, acum, causas, rs.Count(r => r.Status == StatusRestricao.Aberta), rs.Count(r => r.Status == StatusRestricao.Aberta && r.DataLimite < hoje),
            removidas.Count, noPrazo);
    }

    private static string? Limitar(string? s, int max) => s is null ? null : s.Length <= max ? s : s[..max];
}
