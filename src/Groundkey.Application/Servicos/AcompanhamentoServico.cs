using Microsoft.EntityFrameworkCore;
using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Colaboracao;
using Groundkey.Domain.Common;
using Groundkey.Domain.Engenharia;
using Groundkey.Domain.Estrutura;
using Groundkey.Domain.Planejamento;

namespace Groundkey.Application.Servicos;

/// <summary>
/// Acompanhamento do planejamento: painel por SPE e macroetapa (1º nível da EAP = grupo do serviço), Gantt por local
/// (redes › locais › serviços, com ligações), medição semanal pelo mapa, programação semanal para imprimir e plano de ação (tarefas).
/// </summary>
public sealed class AcompanhamentoServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    public const string SemEtapa = "Sem etapa";
    public const string OrigemPlanoAcao = "Cronograma";
    public const string TipoPlanoAcao = "Plano de ação";
    private static readonly string[] Paleta = ["#4f46e5", "#0f8a6b", "#b45309", "#be185d", "#0369a1", "#7c3aed", "#4d7c0f", "#b91c1c", "#0e7490", "#a16207", "#6d28d9", "#15803d"];

    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── Leitura comum ─────────

    /// <summary>Versão de referência do acompanhamento: a vigente; sem aprovação ainda, a mais recente.</summary>
    public static async Task<VersaoCronograma?> ReferenciaAsync(IGroundkeyDb db, Guid obraId, CancellationToken ct) =>
        await db.VersoesCronograma.AsNoTracking().Where(v => v.ObraId == obraId && v.Status == StatusVersao.Vigente).OrderByDescending(v => v.Numero).FirstOrDefaultAsync(ct)
        ?? await db.VersoesCronograma.AsNoTracking().Where(v => v.ObraId == obraId).OrderByDescending(v => v.Numero).FirstOrDefaultAsync(ct);

    private sealed record Base(VersaoCronograma Versao, List<AtividadeCronograma> Folhas, Dictionary<Guid, AtividadeCronograma> PorId,
        Dictionary<Guid, Local> Locais, Dictionary<Guid, string> Grupos, Dictionary<Guid, RedePacote> Pacotes, Calendario Cal);

    private static async Task<Base?> CarregarAsync(IGroundkeyDb db, Guid obraId, Guid? versaoId, CancellationToken ct)
    {
        var v = versaoId is { } id ? await db.VersoesCronograma.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.ObraId == obraId, ct) : await ReferenciaAsync(db, obraId, ct);
        if (v is null) return null;
        var todas = await db.AtividadesCronograma.AsNoTracking().Where(a => a.VersaoId == v.Id).ToListAsync(ct);
        var locais = await db.Locais.AsNoTracking().Where(l => l.ObraId == obraId).ToDictionaryAsync(l => l.Id, ct);
        var comps = todas.Where(a => a.ComposicaoId != null).Select(a => a.ComposicaoId!.Value).Distinct().ToList();
        var grupos = await db.Composicoes.AsNoTracking().Where(c => comps.Contains(c.Id)).Select(c => new { c.Id, c.Grupo }).ToDictionaryAsync(c => c.Id, c => string.IsNullOrWhiteSpace(c.Grupo) ? SemEtapa : c.Grupo!.Trim(), ct);
        var pacIds = todas.Where(a => a.PacoteId != null).Select(a => a.PacoteId!.Value).Distinct().ToList();
        var pacotes = await db.RedePacotes.AsNoTracking().Where(p => pacIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var cal = await PlanejamentoServico.CalendarioAsync(db, obraId, ct);
        return new Base(v, todas.Where(a => !a.Resumo).ToList(), todas.ToDictionary(a => a.Id), locais, grupos, pacotes, cal);
    }

    private static string Macroetapa(Base b, AtividadeCronograma a) => a.ComposicaoId is { } c && b.Grupos.TryGetValue(c, out var g) ? g : SemEtapa;

    private static string Servico(Base b, AtividadeCronograma a) =>
        a.PacoteId is { } p && b.Pacotes.TryGetValue(p, out var pac) ? pac.Nome : a.Nome.Split(" · ")[0];

    /// <summary>Fração planejada (0–1) da atividade até a data: linear nos dias úteis entre início e término (base se houver).</summary>
    public static decimal Planejado(AtividadeCronograma a, DateOnly data, Calendario cal, bool usarBase = true)
    {
        var ini = usarBase && a.InicioBase is { } ib ? ib : a.Inicio;
        var fim = usarBase && a.TerminoBase is { } tb ? tb : a.Termino;
        if (data < ini) return 0;
        if (data >= fim) return 1;
        var total = Math.Max(1, cal.DiasUteisEntre(ini, fim) + (cal.EhUtil(fim) ? 1 : 0));
        var feitos = cal.DiasUteisEntre(ini, data) + (cal.EhUtil(data) ? 1 : 0);
        return Math.Clamp((decimal)feitos / total, 0, 1);
    }

    private static decimal Peso(AtividadeCronograma a) => a.Custo > 0 ? a.Custo : Math.Max(1, a.Duracao);

    /// <summary>SPE dona do local (o próprio local ou o primeiro ancestral com SPE).</summary>
    private static Guid? SpeDe(Base b, Guid? localId)
    {
        var guarda = 0;
        while (localId is { } id && b.Locais.TryGetValue(id, out var l) && guarda++ < 12)
        {
            if (l.SpeId is { } s) return s;
            localId = l.LocalPaiId;
        }
        return null;
    }

    /// <summary>Rótulo do local para a obra: "Torre 01 · Apto 301" (torre + local), ou só o nome.</summary>
    private static string RotuloLocal(Base b, Guid? localId)
    {
        if (localId is not { } id || !b.Locais.TryGetValue(id, out var l)) return "—";
        var p = l.LocalPaiId; var guarda = 0;
        while (p is { } pid && b.Locais.TryGetValue(pid, out var pai) && guarda++ < 12)
        {
            if (pai.Tipo == GeradorEmpreendimento.TipoTorre && l.Tipo != GeradorEmpreendimento.TipoTorre) return $"{pai.Nome} · {l.Nome}";
            p = pai.LocalPaiId;
        }
        return l.Nome;
    }

    // ───────── Painel ─────────

    public sealed record MacroetapaPainel(string Nome, decimal Previsto, decimal Real, decimal Peso, int Atividades);
    public sealed record PontoCurva(DateOnly Semana, decimal Previsto, decimal? Real);
    public sealed record DiasObra(int Uteis, int Trabalhados, int PerdidosChuva, int Assinados, decimal EfetivoMedio);
    public sealed record Painel(string? Versao, bool Vigente, DateOnly DataStatus, decimal Previsto, decimal Real, DateOnly? Termino, DateOnly? TerminoBase, int? Desvio,
        IReadOnlyList<MacroetapaPainel> Macroetapas, IReadOnlyList<(Guid Id, string Nome)> Spes, IReadOnlyList<PontoCurva> Curva, DiasObra Dias,
        int AcoesAbertas, int AcoesAtrasadas, int RestricoesAbertas, int Atrasadas);

    public async Task<Painel> PainelAsync(Guid obraId, Guid? speId, DateOnly? dataStatus, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var hoje = Hoje;
        var st = dataStatus ?? hoje;
        var spes = await db.Spes.AsNoTracking().Where(s => s.ObraId == obraId).OrderBy(s => s.Codigo).ToListAsync(ct);
        var listaSpes = spes.Select(s => (s.Id, string.IsNullOrWhiteSpace(s.NomeFantasia) ? s.RazaoSocial : s.NomeFantasia!)).ToList();
        var dias = await DiasAsync(db, obraId, new DateOnly(st.Year, st.Month, 1), ct);
        var acoes = await db.Tarefas.AsNoTracking().Where(t => t.ObraId == obraId && t.OrigemEntidade == OrigemPlanoAcao && t.Status != StatusTarefa.Concluida && t.Status != StatusTarefa.Cancelada)
            .Select(t => t.Prazo).ToListAsync(ct);
        var restr = await db.Restricoes.AsNoTracking().CountAsync(r => r.ObraId == obraId && r.Status == StatusRestricao.Aberta, ct);
        var b = await CarregarAsync(db, obraId, null, ct);
        if (b is null) return new Painel(null, false, st, 0, 0, null, null, null, [], listaSpes, [], dias, acoes.Count, acoes.Count(p => p < hoje), restr, 0);

        var folhas = b.Folhas.Where(a => speId is null || SpeDe(b, a.LocalId) == speId).ToList();
        decimal PesoTotal(IEnumerable<AtividadeCronograma> xs) => xs.Sum(Peso);
        decimal Prev(IEnumerable<AtividadeCronograma> xs, DateOnly d) { var l = xs.ToList(); var t = PesoTotal(l); return t == 0 ? 0 : Math.Round(l.Sum(a => Peso(a) * Planejado(a, d, b.Cal)) / t * 100, 1); }
        decimal Real(IEnumerable<AtividadeCronograma> xs) { var l = xs.ToList(); var t = PesoTotal(l); return t == 0 ? 0 : Math.Round(l.Sum(a => Peso(a) * a.Avanco) / t, 1); }

        var macro = folhas.GroupBy(a => Macroetapa(b, a))
            .Select(g => new MacroetapaPainel(g.Key, Prev(g, st), Real(g), PesoTotal(g), g.Count()))
            .OrderBy(m => folhas.Where(a => Macroetapa(b, a) == m.Nome).Min(a => a.InicioBase ?? a.Inicio)).ToList();

        // Curva física semanal: previsto pela base; real pelas medições semanais (última medição de cada atividade até a semana).
        var curva = new List<PontoCurva>();
        if (folhas.Count > 0)
        {
            var ini = MedicaoSemanal.SegundaDe(folhas.Min(a => a.InicioBase ?? a.Inicio));
            var fim = folhas.Max(a => a.TerminoBase ?? a.Termino);
            var chaves = folhas.ToDictionary(a => a, CurtoPrazoServico.ChaveDe);
            var setChaves = chaves.Values.ToHashSet();
            var med = (await db.MedicoesSemanais.AsNoTracking().Where(m => m.ObraId == obraId).Select(m => new { m.AtividadeChave, m.Semana, m.Percentual }).ToListAsync(ct))
                .Where(m => setChaves.Contains(m.AtividadeChave)).GroupBy(m => m.AtividadeChave).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Semana).ToList());
            var total = PesoTotal(folhas);
            for (var s = ini; s <= fim.AddDays(6) && curva.Count < 260; s = s.AddDays(7))
            {
                var fimSemana = s.AddDays(5);
                decimal? real = null;
                if (s <= st)
                {
                    real = total == 0 ? 0 : Math.Round(folhas.Sum(a =>
                    {
                        var ms = med.GetValueOrDefault(chaves[a]);
                        var ult = ms?.LastOrDefault(x => x.Semana <= s);
                        return Peso(a) * (ult?.Percentual ?? (ms is null && a.Avanco > 0 && fimSemana >= st ? a.Avanco : 0));
                    }) / total, 1);
                }
                curva.Add(new PontoCurva(s, Prev(folhas, fimSemana), real));
            }
        }
        var termino = folhas.Count > 0 ? folhas.Max(a => a.Termino) : b.Versao.TerminoPrevisto;
        var terminoBase = folhas.Where(a => a.TerminoBase != null).Select(a => a.TerminoBase).DefaultIfEmpty(null).Max();
        int? desvio = termino is { } t1 && terminoBase is { } t0 ? b.Cal.DiasUteisEntre(t0, t1) : null;
        var atrasadas = folhas.Count(a => a.Avanco < 100 && a.Termino < st);
        return new Painel(b.Versao.Codigo, b.Versao.Status == StatusVersao.Vigente, st, Prev(folhas, st), Real(folhas), termino, terminoBase, desvio,
            macro, listaSpes, curva, dias, acoes.Count, acoes.Count(p => p < hoje), restr, atrasadas);
    }

    private static async Task<DiasObra> DiasAsync(IGroundkeyDb db, Guid obraId, DateOnly mes, CancellationToken ct)
    {
        var cal = await PlanejamentoServico.CalendarioAsync(db, obraId, ct);
        var fim = mes.AddMonths(1).AddDays(-1);
        var uteis = Enumerable.Range(0, fim.Day).Select(i => mes.AddDays(i)).Count(cal.EhUtil);
        var ds = await db.DiariosObra.AsNoTracking().Include(d => d.Efetivo).Where(d => d.ObraId == obraId && d.Data >= mes && d.Data <= fim).ToListAsync(ct);
        var trab = ds.Where(d => d.Trabalhado == DiaTrabalhado.Sim).ToList();
        return new DiasObra(uteis, trab.Count, ds.Count(d => d.Trabalhado == DiaTrabalhado.NaoChuva), ds.Count(d => d.Status == StatusDiario.Assinado),
            trab.Count == 0 ? 0 : Math.Round((decimal)trab.Average(d => d.Efetivo.Sum(e => e.Quantidade)), 1));
    }

    // ───────── Gantt por local ─────────

    public sealed record LocalGL(Guid Id, string Nome, string? Grupo, int Ordem);
    public sealed record RedeGL(Guid Id, string Nome, string? Spe, IReadOnlyList<LocalGL> Locais);
    public sealed record TarefaGL(Guid Id, Guid RedeId, Guid LinhaId, string Servico, string Cor, string Eap, DateOnly Inicio, DateOnly Termino, int Duracao,
        decimal Avanco, bool Critica, int? Folga, string Nome);
    public sealed record LigacaoGL(Guid Id, Guid De, Guid Para, string Tipo, int Lag);
    public sealed record GanttLocal(Guid VersaoId, string Codigo, bool Editavel, DateOnly Inicio, DateOnly Fim, DateOnly Hoje, DateOnly? DataStatus,
        IReadOnlyList<RedeGL> Redes, IReadOnlyList<(string Nome, string Cor)> Servicos, IReadOnlyList<TarefaGL> Tarefas, IReadOnlyList<LigacaoGL> Ligacoes);

    /// <summary>
    /// Gantt por local: cada rede é um local macro (torre, estrutura, área comum) com os seus locais nas linhas; cada barra é um serviço
    /// (pacote) no local. Atividades fora das redes entram em "Atividades avulsas", uma por linha.
    /// </summary>
    public async Task<GanttLocal?> GanttLocalAsync(Guid obraId, Guid versaoId, bool podeEditar, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var b = await CarregarAsync(db, obraId, versaoId, ct);
        if (b is null) return null;
        var vinculos = await db.VinculosAtividade.AsNoTracking().Where(x => x.VersaoId == b.Versao.Id).ToListAsync(ct);
        var redes = await db.RedesPlanejamento.AsNoTracking().Include(r => r.Locais).Include(r => r.Pacotes).AsSplitQuery()
            .Where(r => r.ObraId == obraId).OrderBy(r => r.Ordem).ThenBy(r => r.Nome).ToListAsync(ct);
        var spes = await db.Spes.AsNoTracking().Where(s => s.ObraId == obraId).ToDictionaryAsync(s => s.Id, s => string.IsNullOrWhiteSpace(s.NomeFantasia) ? s.RazaoSocial : s.NomeFantasia!, ct);

        // Cores por serviço na ordem em que aparecem nas redes.
        var servicos = new List<(string Nome, string Cor)>();
        string Cor(string servico)
        {
            var i = servicos.FindIndex(x => x.Nome == servico);
            if (i >= 0) return servicos[i].Cor;
            servicos.Add((servico, Paleta[servicos.Count % Paleta.Length]));
            return servicos[^1].Cor;
        }
        foreach (var p in redes.SelectMany(r => r.Pacotes.OrderBy(p => p.Ordem))) Cor(p.Nome);

        string? GrupoDe(Local l) => l.LocalPaiId is { } p && b.Locais.TryGetValue(p, out var pai) && pai.Tipo == GeradorEmpreendimento.TipoPavimento ? pai.Nome : null;
        var lista = new List<RedeGL>();
        var tarefas = new List<TarefaGL>();
        var usadas = new HashSet<Guid>();
        foreach (var r in redes)
        {
            var folhas = b.Folhas.Where(a => a.RedeId == r.Id && a.LocalId != null).ToList();
            if (folhas.Count == 0) continue;
            var locais = r.Locais.OrderBy(l => l.Ordem).Where(l => b.Locais.ContainsKey(l.LocalId)).Select(l => b.Locais[l.LocalId])
                .Select((l, i) => new LocalGL(l.Id, l.Nome, GrupoDe(l), i)).ToList();
            foreach (var extra in folhas.Select(a => a.LocalId!.Value).Distinct().Where(id => locais.All(l => l.Id != id) && b.Locais.ContainsKey(id)))
                locais.Add(new LocalGL(extra, b.Locais[extra].Nome, GrupoDe(b.Locais[extra]), locais.Count));
            var spe = folhas.Select(a => SpeDe(b, a.LocalId)).Where(s => s != null).GroupBy(s => s!.Value).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
            lista.Add(new RedeGL(r.Id, r.Nome, spe is { } sid ? spes.GetValueOrDefault(sid) : null, locais));
            foreach (var a in folhas)
            {
                var sv = Servico(b, a);
                tarefas.Add(new TarefaGL(a.Id, r.Id, a.LocalId!.Value, sv, Cor(sv), a.Eap, a.Inicio, a.Termino, a.Duracao, a.Avanco, a.Critica, a.Folga, a.Nome));
                usadas.Add(a.Id);
            }
        }
        var avulsas = b.Folhas.Where(a => !usadas.Contains(a.Id)).OrderBy(a => a.Eap, StringComparer.Ordinal).ToList();
        if (avulsas.Count > 0)
        {
            var id = Guid.Empty;
            lista.Add(new RedeGL(id, "Atividades avulsas", null, avulsas.Select((a, i) => new LocalGL(a.Id, $"{a.Eap} {a.Nome}", null, i)).ToList()));
            foreach (var a in avulsas)
            {
                var sv = a.ComposicaoId is not null ? Macroetapa(b, a) : "Avulsa";
                tarefas.Add(new TarefaGL(a.Id, id, a.Id, sv, Cor(sv), a.Eap, a.Inicio, a.Termino, a.Duracao, a.Avanco, a.Critica, a.Folga, a.Nome));
            }
        }
        var ids = tarefas.Select(t => t.Id).ToHashSet();
        var ligacoes = vinculos.Where(x => ids.Contains(x.PredecessoraId) && ids.Contains(x.SucessoraId))
            .Select(x => new LigacaoGL(x.Id, x.PredecessoraId, x.SucessoraId, MotorCronograma.Sigla(x.Tipo), x.Lag)).ToList();
        var ini = tarefas.Count > 0 ? tarefas.Min(t => t.Inicio) : b.Versao.InicioProjeto;
        var fim = tarefas.Count > 0 ? tarefas.Max(t => t.Termino) : b.Versao.InicioProjeto.AddMonths(3);
        return new GanttLocal(b.Versao.Id, b.Versao.Codigo, podeEditar && b.Versao.Editavel, ini, fim, Hoje, b.Versao.DataStatus, lista, servicos, tarefas, ligacoes);
    }

    // ───────── Medição semanal ─────────

    public sealed record OpcaoMedicao(Guid RedeId, string Rede, IReadOnlyList<(Guid PacoteId, string Servico)> Servicos);
    public sealed record CelulaMedicao(Guid LocalId, string Local, string? Grupo, int Ordem, decimal Avanco, DateOnly? MedidoEm, decimal? MedidoPercentual, bool TemControle, bool FvsAprovada);
    public sealed record MapaMedicao(DateOnly Semana, IReadOnlyList<OpcaoMedicao> Opcoes, IReadOnlyList<CelulaMedicao> Celulas, decimal AvancoServico, bool PodeMedir);

    /// <summary>Mapa da medição: os locais da rede para o serviço escolhido, com o avanço atual, a medição da semana e a situação da FVS.</summary>
    public async Task<MapaMedicao> MapaMedicaoAsync(Guid obraId, Guid? redeId, Guid? pacoteId, DateOnly data, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var semana = MedicaoSemanal.SegundaDe(data);
        var pode = await PodeMedirAsync(db, ct);
        var b = await CarregarAsync(db, obraId, null, ct);
        if (b is null) return new MapaMedicao(semana, [], [], 0, pode);
        var redes = await db.RedesPlanejamento.AsNoTracking().Include(r => r.Pacotes).Include(r => r.Locais).AsSplitQuery()
            .Where(r => r.ObraId == obraId).OrderBy(r => r.Ordem).ThenBy(r => r.Nome).ToListAsync(ct);
        var opcoes = redes.Where(r => b.Folhas.Any(a => a.RedeId == r.Id))
            .Select(r => new OpcaoMedicao(r.Id, r.Nome, r.Pacotes.OrderBy(p => p.Ordem).Where(p => b.Folhas.Any(a => a.PacoteId == p.Id)).Select(p => (p.Id, p.Nome)).ToList()))
            .Where(o => o.Servicos.Count > 0).ToList();
        var rede = opcoes.FirstOrDefault(o => o.RedeId == redeId) ?? opcoes.FirstOrDefault();
        if (rede is null) return new MapaMedicao(semana, opcoes, [], 0, pode);
        var pac = rede.Servicos.Any(s => s.PacoteId == pacoteId) ? pacoteId!.Value : rede.Servicos[0].PacoteId;
        var ordem = redes.First(r => r.Id == rede.RedeId).Locais.ToDictionary(l => l.LocalId, l => l.Ordem);
        var ats = b.Folhas.Where(a => a.RedeId == rede.RedeId && a.PacoteId == pac && a.LocalId != null).ToList();
        var chaves = ats.Select(CurtoPrazoServico.ChaveDe).ToList();
        var med = await db.MedicoesSemanais.AsNoTracking().Where(m => m.ObraId == obraId && m.Semana == semana && chaves.Contains(m.AtividadeChave)).ToListAsync(ct);
        var comp = ats.Select(a => a.ComposicaoId).FirstOrDefault(c => c != null);
        var servCtrl = comp is { } c ? await db.Composicoes.AsNoTracking().Where(x => x.Id == c).Select(x => x.ServicoControladoId).FirstOrDefaultAsync(ct) : null;
        var aprovadas = servCtrl is { } sc ? await FvsAprovadasAsync(db, obraId, sc, b, ct) : [];
        var cel = ats.Select(a =>
        {
            var l = b.Locais[a.LocalId!.Value];
            var m = med.FirstOrDefault(x => x.AtividadeChave == CurtoPrazoServico.ChaveDe(a));
            var grupo = l.LocalPaiId is { } p && b.Locais.TryGetValue(p, out var pai) && pai.Tipo == GeradorEmpreendimento.TipoPavimento ? pai.Nome : null;
            return new CelulaMedicao(l.Id, l.Nome, grupo, ordem.GetValueOrDefault(l.Id, l.Ordem), a.Avanco, m?.DataMedicao, m?.Percentual, servCtrl != null, aprovadas.Contains(l.Id));
        }).OrderBy(x => x.Ordem).ToList();
        var peso = ats.Sum(Peso);
        return new MapaMedicao(semana, opcoes, cel, peso == 0 ? 0 : Math.Round(ats.Sum(a => Peso(a) * a.Avanco) / peso, 1), pode);
    }

    /// <summary>Locais (e os ancestrais deles) com FVS aprovada/liberada do serviço controlado.</summary>
    private static async Task<HashSet<Guid>> FvsAprovadasAsync(IGroundkeyDb db, Guid obraId, Guid servicoControladoId, Base b, CancellationToken ct)
    {
        var locais = await db.Fvss.AsNoTracking().Where(f => f.ObraId == obraId && f.ServicoControladoId == servicoControladoId && (f.Status == StatusFvs.Aprovada || f.Status == StatusFvs.Liberada))
            .Select(f => f.LocalId).Distinct().ToListAsync(ct);
        var r = new HashSet<Guid>();
        foreach (var id in locais)
        {
            Guid? x = id; var guarda = 0;
            while (x is { } g && guarda++ < 12 && r.Add(g)) x = b.Locais.TryGetValue(g, out var l) ? l.LocalPaiId : null;
        }
        return r;
    }

    private async Task<bool> PodeMedirAsync(IGroundkeyDb db, CancellationToken ct)
    {
        if (usuario.UsuarioId is not { } id) return true;
        var p = await db.Usuarios.AsNoTracking().Where(u => u.Id == id).Select(u => (PerfilUsuario?)u.Perfil).FirstOrDefaultAsync(ct);
        return p is PerfilUsuario.Engenharia or PerfilUsuario.Administrador;
    }

    public sealed record ResultadoMedicao(int Gravadas, int Sobrepostas, int ComAvisoFvs);

    /// <summary>
    /// Grava a medição dos locais (percentual acumulado) na semana da data escolhida. Medir de novo na mesma semana sobrepõe.
    /// Atualiza o avanço das atividades na versão vigente e na reprogramação em elaboração (mesma chave).
    /// </summary>
    public async Task<ResultadoMedicao> MedirAsync(Guid obraId, Guid pacoteId, IReadOnlyCollection<Guid> locais, decimal percentual, DateOnly data, string? observacao, CancellationToken ct = default)
    {
        var e = new List<string>();
        if (locais.Count == 0) e.Add("Escolha ao menos um local no mapa.");
        if (percentual is < 0 or > 100) e.Add("Percentual entre 0 e 100%.");
        if (data > Hoje) e.Add("A data da medição não pode ser futura.");
        ValidacaoException.Se(e);
        var res = new ResultadoMedicao(0, 0, 0);
        await fabrica.EmTransacaoAsync(async db =>
        {
            if (!await PodeMedirAsync(db, ct)) throw new RegraDeNegocioException("Só a Engenharia de obra registra a medição semanal.");
            var b = await CarregarAsync(db, obraId, null, ct) ?? throw new RegraDeNegocioException("A obra ainda não tem cronograma.");
            var ats = b.Folhas.Where(a => a.PacoteId == pacoteId && a.LocalId is { } l && locais.Contains(l)).ToList();
            if (ats.Count == 0) throw new RegraDeNegocioException("Nenhuma atividade desse serviço nos locais escolhidos.");
            var semana = MedicaoSemanal.SegundaDe(data);
            var chaves = ats.ToDictionary(a => a.Id, CurtoPrazoServico.ChaveDe);
            var lista = chaves.Values.ToList();
            var existentes = await db.MedicoesSemanais.Where(m => m.ObraId == obraId && m.Semana == semana && lista.Contains(m.AtividadeChave)).ToListAsync(ct);
            var comp = ats.Select(a => a.ComposicaoId).FirstOrDefault(c => c != null);
            var servCtrl = comp is { } c ? await db.Composicoes.AsNoTracking().Where(x => x.Id == c).Select(x => x.ServicoControladoId).FirstOrDefaultAsync(ct) : null;
            var aprovadas = servCtrl is { } sc ? await FvsAprovadasAsync(db, obraId, sc, b, ct) : [];
            var nome = await NomeAsync(db, ct);
            int sobre = 0, aviso = 0;
            foreach (var a in ats)
            {
                var k = chaves[a.Id];
                var m = existentes.FirstOrDefault(x => x.AtividadeChave == k);
                if (m is null)
                {
                    m = new MedicaoSemanal { ObraId = obraId, AtividadeChave = k, LocalId = a.LocalId!.Value, PacoteId = a.PacoteId, ComposicaoId = a.ComposicaoId, Semana = semana, PercentualAnterior = a.Avanco };
                    db.MedicoesSemanais.Add(m);
                }
                else sobre++;
                m.Servico = Servico(b, a); m.DataMedicao = data; m.Percentual = percentual; m.Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(); m.MedidoPor = nome;
                m.FvsPendente = percentual >= 100 && servCtrl != null && !aprovadas.Contains(a.LocalId!.Value);
                if (m.FvsPendente) aviso++;
            }
            // Avanço nas versões em uso (vigente e em elaboração) pela chave da atividade.
            var versoes = await db.VersoesCronograma.Include(v => v.Atividades).Include(v => v.Vinculos).AsSplitQuery()
                .Where(v => v.ObraId == obraId && (v.Status == StatusVersao.Vigente || v.Status == StatusVersao.EmElaboracao)).ToListAsync(ct);
            var setChaves = lista.ToHashSet();
            foreach (var v in versoes)
            {
                var mudou = false;
                foreach (var a in v.Atividades.Where(a => !a.Resumo && setChaves.Contains(CurtoPrazoServico.ChaveDe(a)))) { a.Avanco = percentual; mudou = true; }
                if (mudou) await PlanejamentoServico.RecalcularAsync(db, v, ct);
            }
            await db.SaveChangesAsync(ct);
            res = new ResultadoMedicao(ats.Count, sobre, aviso);
        }, ct);
        return res;
    }

    private async Task<string> NomeAsync(IGroundkeyDb db, CancellationToken ct) =>
        usuario.UsuarioId is { } id ? await db.Usuarios.AsNoTracking().Where(u => u.Id == id).Select(u => u.Nome).FirstOrDefaultAsync(ct) ?? usuario.Nome : usuario.Nome;

    // ───────── Programação semanal ─────────

    public sealed record LinhaProgramacao(string Local, string Servico, string Macroetapa, decimal De, decimal Ate, IReadOnlyList<bool> Dias, string? Restricao);
    public sealed record SemanaProgramacao(DateOnly Segunda, IReadOnlyList<LinhaProgramacao> Linhas);
    public sealed record Programacao(string? Versao, string Obra, string? Spe, IReadOnlyList<SemanaProgramacao> Semanas);

    /// <summary>
    /// Programação semanal (segunda a sábado): local, atividade/serviço, avanço previsto na semana (de → até) e os dias programados.
    /// Quando a semana tem plano semanal (Last Planner), usa só os compromissos dele; senão, as atividades do cronograma na semana.
    /// </summary>
    public async Task<Programacao> ProgramacaoAsync(Guid obraId, Guid? speId, DateOnly de, DateOnly ate, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var obra = await db.Obras.AsNoTracking().Where(o => o.Id == obraId).Select(o => o.Codigo + " – " + o.Nome).FirstOrDefaultAsync(ct) ?? "";
        var spe = speId is { } s ? await db.Spes.AsNoTracking().Where(x => x.Id == s).Select(x => x.NomeFantasia ?? x.RazaoSocial).FirstOrDefaultAsync(ct) : null;
        var b = await CarregarAsync(db, obraId, null, ct);
        if (b is null) return new Programacao(null, obra, spe, []);
        var hoje = Hoje;
        var primeira = MedicaoSemanal.SegundaDe(de);
        if (ate < de) ate = de;
        var restr = await db.Restricoes.AsNoTracking().Where(r => r.ObraId == obraId && r.Status == StatusRestricao.Aberta)
            .Select(r => new { r.AtividadeChave, r.Descricao }).ToListAsync(ct);
        var restrPorChave = restr.GroupBy(r => r.AtividadeChave).ToDictionary(g => g.Key, g => g.First().Descricao);
        var planos = await db.PlanosSemanais.AsNoTracking().Include(p => p.Compromissos).Where(p => p.ObraId == obraId && p.Semana >= primeira && p.Semana <= ate).ToListAsync(ct);
        var folhas = b.Folhas.Where(a => speId is null || SpeDe(b, a.LocalId) == speId).ToList();
        var semanas = new List<SemanaProgramacao>();
        for (var seg = primeira; seg <= ate && semanas.Count < 12; seg = seg.AddDays(7))
        {
            var sab = seg.AddDays(5);
            var plano = planos.FirstOrDefault(p => p.Semana == seg);
            var chavesPlano = plano?.Compromissos.Where(c => c.AtividadeChave != null).Select(c => c.AtividadeChave!).ToHashSet();
            var ats = folhas.Where(a => a.Inicio <= sab && a.Termino >= seg && (a.Avanco < 100 || seg <= hoje))
                .Where(a => chavesPlano is null || chavesPlano.Count == 0 || chavesPlano.Contains(CurtoPrazoServico.ChaveDe(a))).ToList();
            var linhas = ats.Select(a =>
            {
                var k = CurtoPrazoServico.ChaveDe(a);
                var corrente = seg <= hoje && hoje <= sab.AddDays(1);
                var deP = corrente ? a.Avanco : Math.Round(Planejado(a, seg.AddDays(-1), b.Cal, usarBase: false) * 100, 0);
                var ateP = Math.Max(deP, Math.Round(Planejado(a, sab, b.Cal, usarBase: false) * 100, 0));
                var dias = Enumerable.Range(0, 6).Select(i => { var d = seg.AddDays(i); return d >= a.Inicio && d <= a.Termino && b.Cal.EhUtil(d); }).ToList();
                return new LinhaProgramacao(RotuloLocal(b, a.LocalId) is "—" ? a.Nome : RotuloLocal(b, a.LocalId), Servico(b, a), Macroetapa(b, a), deP, ateP, dias, restrPorChave.GetValueOrDefault(k));
            }).OrderBy(l => l.Local, StringComparer.CurrentCulture).ThenBy(l => l.Servico).ToList();
            semanas.Add(new SemanaProgramacao(seg, linhas));
        }
        return new Programacao(b.Versao.Codigo, obra, spe, semanas);
    }

    // ───────── Plano de ação (tarefas) ─────────

    public async Task<List<Tarefa>> PlanoAcaoAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Tarefas.AsNoTracking().Where(t => t.ObraId == obraId && t.OrigemEntidade == OrigemPlanoAcao)
            .OrderBy(t => t.Status == StatusTarefa.Concluida || t.Status == StatusTarefa.Cancelada).ThenBy(t => t.Prazo).ToListAsync(ct);
    }

    /// <summary>Monta a tarefa de uma ação de recuperação da versão (gravada pelo serviço de tarefas, que avisa o responsável).</summary>
    public static Tarefa NovaAcao(Guid obraId, VersaoCronograma versao, string acao, string? atividade, int? recupera, Guid responsavelId, string responsavelNome, DateOnly prazo)
    {
        var desc = string.Join(" · ", new[] { atividade is { Length: > 0 } ? $"Atividade/macroetapa: {atividade.Trim()}" : null, recupera is > 0 ? $"Recupera {recupera} d.ú." : null }.Where(x => x != null));
        return new Tarefa
        {
            ObraId = obraId, Titulo = acao.Trim(), Descricao = desc.Length == 0 ? null : desc, Tipo = TipoPlanoAcao, ResponsavelId = responsavelId, ResponsavelNome = responsavelNome,
            Prazo = prazo, PrazoOriginal = prazo, OrigemEntidade = OrigemPlanoAcao, OrigemId = versao.Id, OrigemDescricao = $"Cronograma {versao.Codigo}"
        };
    }
}
