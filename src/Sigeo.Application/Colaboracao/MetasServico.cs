using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Common;
using Sigeo.Domain.Planejamento;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Colaboracao;

/// <summary>
/// Metas e entregas: metas do setor, individuais ou de equipe, com valor automático (indicadores do sistema) ou manual;
/// tarefas ligadas à meta com aceite do gestor; placar por pessoa, equipe e serviço. A nota do placar serve para acompanhar
/// e avaliar o desempenho — não altera a meta (de economia ou qualquer outra).
/// </summary>
public sealed class MetasServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    public async Task<Guid> SalvarAsync(Meta m, CancellationToken ct = default)
    {
        ValidacaoException.Se(m.Validar());
        await using var db = fabrica.Criar();
        if (m.UsuarioId is { } u) m.UsuarioNome = await db.Usuarios.AsNoTracking().Where(x => x.Id == u).Select(x => x.Nome).FirstOrDefaultAsync(ct);
        if (m.EquipeId is { } e) m.EquipeNome = await db.Equipes.AsNoTracking().Where(x => x.Id == e).Select(x => x.Nome).FirstOrDefaultAsync(ct);
        if (m.Dono != DonoMeta.Pessoa) { m.UsuarioId = null; m.UsuarioNome = null; }
        if (m.Dono != DonoMeta.Equipe) { m.EquipeId = null; m.EquipeNome = null; }
        await Grafo.SalvarAsync(db, m);
        await db.SaveChangesAsync(ct);
        return m.Id;
    }

    /// <summary>Valor realizado do indicador no período da meta (até hoje), do dono da meta.</summary>
    public static async Task<decimal?> ApurarAsync(ISigeoDb db, Meta m, DateOnly hoje, CancellationToken ct = default)
    {
        var fim = m.Fim < hoje ? m.Fim : hoje;
        if (fim < m.Inicio) return null;
        Guid? pessoa = m.Dono == DonoMeta.Pessoa ? m.UsuarioId : null;
        static decimal? P(int a, int t) => t == 0 ? null : Math.Round(100m * a / t, 1);
        switch (m.Indicador)
        {
            case IndicadorMeta.Manual: return m.ValorManual;
            case IndicadorMeta.EconomiaReal:
            {
                var l = await EconomiaServico.AvaliarPeriodoAsync(db, m.Inicio, fim, m.ObraId, ct);
                var r = EconomiaReal.Resumir(l.Where(a => pessoa is null || a.Compra.CompradorId == pessoa));
                return r.BaseTotal == 0 ? null : r.Percentual;
            }
            case IndicadorMeta.TempoCompra:
            {
                var t = (await PainelSuprimentosServico.TemposCompraAsync(db, m.Inicio, fim, m.ObraId, ct)).Where(x => pessoa is null || x.Comprador == pessoa).ToList();
                return t.Count == 0 ? null : Math.Round(t.Average(x => x.Dias), 1);
            }
            case IndicadorMeta.EntregaNoPrazo:
            {
                var e = (await PainelSuprimentosServico.EntregasAsync(db, m.Inicio, fim, hoje, m.ObraId, ct)).Where(x => pessoa is null || x.Comprador == pessoa).ToList();
                return P(e.Count(x => x.NoPrazo), e.Count);
            }
            case IndicadorMeta.CotacaoCompetitiva:
            {
                var c = (await PainelSuprimentosServico.CotacoesCompetitivasAsync(db, m.Inicio, fim, m.ObraId, ct)).Where(x => pessoa is null || x.Comprador == pessoa).ToList();
                return P(c.Count(x => x.Competitiva), c.Count);
            }
            case IndicadorMeta.AtendimentoNecessidade:
            {
                var n = (await PainelSuprimentosServico.NecessidadesAsync(db, m.Inicio, fim, hoje, m.ObraId, ct)).Where(x => pessoa is null || x.Comprador == pessoa).ToList();
                return P(n.Count(x => x.AteNecessidade), n.Count);
            }
            case IndicadorMeta.PpcLastPlanner:
            {
                var planos = await db.PlanosSemanais.AsNoTracking().Include(p => p.Compromissos)
                    .Where(p => p.Semana >= m.Inicio && p.Semana <= fim && p.Status == StatusPlanoSemanal.Fechado && (m.ObraId == null || p.ObraId == m.ObraId)).ToListAsync(ct);
                var comp = planos.SelectMany(p => p.Compromissos).ToList();
                var (pl, ok, _, _) = PlanoSemanal.Apurar(comp);
                return P(ok, pl);
            }
            case IndicadorMeta.TarefasNoPrazo:
            {
                var t = await TarefasDoDonoAsync(db, m, fim, ct);
                var vencidas = t.Where(x => x.PrazoOriginal <= fim && x.Status != StatusTarefa.Cancelada).ToList();
                return P(vencidas.Count(RegrasMeta.NoPrazo), vencidas.Count);
            }
        }
        return null;
    }

    private static async Task<List<Tarefa>> TarefasDoDonoAsync(ISigeoDb db, Meta m, DateOnly fim, CancellationToken ct)
    {
        var q = db.Tarefas.AsNoTracking().Where(t => t.PrazoOriginal >= m.Inicio && t.PrazoOriginal <= fim);
        q = m.Dono switch
        {
            DonoMeta.Pessoa => q.Where(t => t.ResponsavelId == m.UsuarioId),
            DonoMeta.Equipe => q.Where(t => t.EquipeId == m.EquipeId),
            _ => q.Where(t => t.MetaId == m.Id || (m.Setor != null && t.Tipo == m.Setor))
        };
        return await q.ToListAsync(ct);
    }

    public async Task<MetasDto> CarregarAsync(DateOnly inicio, DateOnly fim, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var metas = await db.Metas.AsNoTracking().Where(m => m.Ativa && m.Inicio <= fim && m.Fim >= inicio).OrderBy(m => m.Dono).ThenBy(m => m.Nome).ToListAsync(ct);
        var metaIds = metas.Select(m => m.Id).ToList();
        var tarefas = await db.Tarefas.AsNoTracking().Where(t => (t.MetaId != null && metaIds.Contains(t.MetaId.Value)) || (t.PrazoOriginal >= inicio && t.PrazoOriginal <= fim && t.MetaId != null))
            .OrderBy(t => t.Prazo).ToListAsync(ct);
        var linhas = new List<LinhaMeta>();
        foreach (var m in metas)
        {
            var v = await ApurarAsync(db, m, Hoje, ct);
            var at = RegrasMeta.Atingido(m.Alvo, v, m.MenorMelhor);
            var (txt, tom) = RegrasMeta.Situacao(at);
            var dela = tarefas.Where(t => t.MetaId == m.Id).ToList();
            linhas.Add(new LinhaMeta(m, v, at, txt, tom, dela.Count, dela.Count(t => t.Status == StatusTarefa.Concluida)));
        }
        // Tarefas das pessoas no período (todas, para o placar) — ligadas a meta ou não.
        var doPeriodo = await db.Tarefas.AsNoTracking().Where(t => t.PrazoOriginal >= inicio && t.PrazoOriginal <= fim && t.Status != StatusTarefa.Cancelada).ToListAsync(ct);
        var equipes = await db.Equipes.AsNoTracking().ToDictionaryAsync(e => e.Id, e => e.Nome, ct);
        LinhaPlacar Placar(string chave, string nome, IEnumerable<LinhaMeta> ms, IEnumerable<Tarefa> ts)
        {
            var lm = ms.ToList(); var lt = ts.Where(t => t.PrazoOriginal <= Hoje || t.Status == StatusTarefa.Concluida).ToList();
            var noPrazo = lt.Count(RegrasMeta.NoPrazo);
            return new LinhaPlacar(chave, nome, lm.Count, lm.Count(x => x.Atingido >= 1), noPrazo, lt.Count, lt.Count(t => t.Atrasada(Hoje)),
                RegrasMeta.Nota(lm.Select(x => (x.Atingido, x.Meta.Peso)), noPrazo, lt.Count));
        }
        var pessoas = linhas.Where(l => l.Meta.Dono == DonoMeta.Pessoa && l.Meta.UsuarioId != null).Select(l => (Id: l.Meta.UsuarioId!.Value, Nome: l.Meta.UsuarioNome ?? ""))
            .Concat(doPeriodo.Select(t => (Id: t.ResponsavelId, Nome: t.ResponsavelNome))).GroupBy(x => x.Id).Select(g => g.First()).ToList();
        var porPessoa = pessoas.Select(p => Placar(p.Id.ToString(), p.Nome, linhas.Where(l => l.Meta.Dono == DonoMeta.Pessoa && l.Meta.UsuarioId == p.Id), doPeriodo.Where(t => t.ResponsavelId == p.Id)))
            .OrderByDescending(x => x.Nota ?? -1).ToList();
        var eqIds = linhas.Where(l => l.Meta.EquipeId != null).Select(l => l.Meta.EquipeId!.Value).Concat(doPeriodo.Where(t => t.EquipeId != null).Select(t => t.EquipeId!.Value)).Distinct();
        var porEquipe = eqIds.Select(e => Placar(e.ToString(), equipes.GetValueOrDefault(e, "Equipe"), linhas.Where(l => l.Meta.EquipeId == e), doPeriodo.Where(t => t.EquipeId == e)))
            .OrderByDescending(x => x.Nota ?? -1).ToList();
        var setores = linhas.Where(l => l.Meta.Dono == DonoMeta.Setor).GroupBy(l => l.Meta.Setor ?? "Setor")
            .Select(g => Placar($"setor:{g.Key}", $"Setor {g.Key}", g, doPeriodo.Where(t => t.Tipo == g.Key)));
        porEquipe.InsertRange(0, setores);
        var porServico = linhas.Where(l => !string.IsNullOrWhiteSpace(l.Meta.Servico)).GroupBy(l => l.Meta.Servico!)
            .Select(g => Placar($"serv:{g.Key}", g.Key, g, tarefas.Where(t => g.Any(l => l.Meta.Id == t.MetaId)))).ToList();
        var metaNome = metas.ToDictionary(m => m.Id, m => m.Nome);
        var lista = tarefas.Select(t => new LinhaTarefaMeta(t.Id, t.Numero, t.Titulo, t.MetaId is { } mid ? metaNome.GetValueOrDefault(mid) : null, t.ResponsavelNome,
            t.EquipeId is { } e ? equipes.GetValueOrDefault(e) : null, t.Prazo, t.PrazoOriginal, t.Status, t.Aceite, t.Atrasada(Hoje), t.MotivoDevolucao)).ToList();
        return new MetasDto(linhas, porPessoa, porEquipe, porServico, lista);
    }

    /// <summary>Gestor aceita a entrega da tarefa da meta, ou devolve (reabre) com motivo.</summary>
    public async Task DecidirEntregaAsync(Guid tarefaId, bool aceitar, string? motivo, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var t = await db.Tarefas.FirstAsync(x => x.Id == tarefaId, ct);
        if (t.Aceite != AceiteTarefa.Aguardando) throw new RegraDeNegocioException("A tarefa não está aguardando aceite.");
        if (t.ResponsavelId == usuario.UsuarioId) throw new RegraDeNegocioException("Quem entregou não aceita a própria entrega.");
        if (aceitar) { t.Aceite = AceiteTarefa.Aceita; t.AceitaPor = usuario.Nome; t.AceitaEm = relogio.GetUtcNow(); t.MotivoDevolucao = null; }
        else
        {
            if (string.IsNullOrWhiteSpace(motivo)) throw new RegraDeNegocioException("Informe o motivo da devolução.");
            t.Aceite = AceiteTarefa.Devolvida; t.MotivoDevolucao = motivo.Trim(); t.Status = StatusTarefa.EmAndamento; t.ConcluidaEm = null; t.ConcluidaPor = null;
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Fechamento mensal: congela o valor de cada meta ativa no mês (para comparar meses e para a avaliação de desempenho).</summary>
    public async Task<int> FecharMesAsync(DateOnly mes, CancellationToken ct = default)
    {
        var m0 = new DateOnly(mes.Year, mes.Month, 1);
        var fimMes = m0.AddMonths(1).AddDays(-1);
        await using var db = fabrica.Criar();
        var metas = await db.Metas.Include(m => m.Apuracoes).Where(m => m.Ativa && m.Inicio <= fimMes && m.Fim >= m0).ToListAsync(ct);
        foreach (var m in metas)
        {
            var v = await ApurarAsync(db, m, fimMes < Hoje ? fimMes : Hoje, ct);
            var a = m.Apuracoes.FirstOrDefault(x => x.Mes == m0);
            if (a is null) { a = new MetaApuracao { MetaId = m.Id, Mes = m0 }; db.Add(a); }
            a.Valor = v; a.PercAtingido = RegrasMeta.Atingido(m.Alvo, v, m.MenorMelhor); a.ApuradoEm = relogio.GetUtcNow(); a.ApuradoPor = usuario.Nome;
        }
        await db.SaveChangesAsync(ct);
        return metas.Count;
    }
}
