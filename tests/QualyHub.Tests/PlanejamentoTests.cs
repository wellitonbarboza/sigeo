using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Planejamento;

namespace QualyHub.Tests;

/// <summary>Calendário de dias úteis, motor de rede (CPM), "arrastar só move", predecessoras por EAP, gerador de redes e desembolso.</summary>
public class PlanejamentoTests
{
    private static readonly Calendario Cal = new();
    private static readonly Guid V = Guid.NewGuid();

    private static AtividadeCronograma A(string nome, int dur, int ordem = 0, Guid? pai = null, bool resumo = false, decimal custo = 0) =>
        new() { VersaoId = V, Nome = nome, Duracao = dur, Ordem = ordem, PaiId = pai, Resumo = resumo, Custo = custo };

    private static VinculoAtividade L(AtividadeCronograma p, AtividadeCronograma s, TipoVinculo t = TipoVinculo.TerminoInicio, int lag = 0) =>
        new() { VersaoId = V, PredecessoraId = p.Id, SucessoraId = s.Id, Tipo = t, Lag = lag };

    [Fact]
    public void Calendario_pula_fim_de_semana_e_feriados_nacionais()
    {
        // 09/10/2026 é sexta; 12/10 (segunda) é Nossa Senhora Aparecida → próximo dia útil 13/10.
        var sexta = new DateOnly(2026, 10, 9);
        Assert.True(Cal.EhUtil(sexta));
        Assert.False(Cal.EhUtil(new DateOnly(2026, 10, 12)));
        Assert.Equal(new DateOnly(2026, 10, 13), Cal.Data(Cal.Indice(sexta) + 1));
        Assert.Equal(new DateOnly(2027, 3, 28), Calendario.Pascoa(2027));
        Assert.False(Cal.EhUtil(new DateOnly(2027, 3, 26))); // Sexta-feira Santa
        Assert.False(Cal.EhUtil(new DateOnly(2027, 2, 8)));  // Carnaval
        Assert.Equal(new DateOnly(2026, 10, 13), Cal.ProximoUtil(new DateOnly(2026, 10, 10)));
        for (var i = 6000; i < 6400; i++) Assert.Equal(i, Cal.Indice(Cal.Data(i)));
    }

    [Fact]
    public void Feriado_municipal_cadastrado_entra_no_calendario()
    {
        var c = new Calendario([new DateOnly(2026, 10, 14)]);
        Assert.False(c.EhUtil(new DateOnly(2026, 10, 14)));
        Assert.Equal(new DateOnly(2026, 10, 15), c.Data(c.Indice(new DateOnly(2026, 10, 13)) + 1));
    }

    [Fact]
    public void Motor_calcula_TI_II_TT_IT_com_folga_e_caminho_critico()
    {
        var a = A("Fundação", 5, 1); var b = A("Estrutura", 10, 2); var c = A("Instalações", 3, 3); var d = A("Pintura", 2, 4); var e = A("Marco", 0, 5);
        var ats = new List<AtividadeCronograma> { a, b, c, d, e };
        var vs = new List<VinculoAtividade> { L(a, b), L(a, c, TipoVinculo.InicioInicio, 2), L(b, d, TipoVinculo.TerminoTermino, 1), L(d, e) };
        var inicio = new DateOnly(2026, 3, 2); // segunda
        var r = MotorCronograma.Calcular(ats, vs, inicio, Cal);

        Assert.Equal(inicio, a.Inicio);
        Assert.Equal(new DateOnly(2026, 3, 6), a.Termino);       // 5 dias úteis
        Assert.Equal(new DateOnly(2026, 3, 9), b.Inicio);         // TI: dia útil seguinte
        Assert.Equal(new DateOnly(2026, 3, 20), b.Termino);
        Assert.Equal(new DateOnly(2026, 3, 4), c.Inicio);         // II+2
        Assert.Equal(new DateOnly(2026, 3, 23), d.Termino);       // TT+1: termina 1 dia útil após a estrutura
        Assert.Equal(d.Termino, e.Inicio);                        // marco no término da predecessora
        Assert.Equal(e.Termino, r.Termino);
        Assert.True(a.Critica && b.Critica && d.Critica);
        Assert.False(c.Critica);
        Assert.True(c.Folga > 0);
        Assert.Equal(0, a.Folga);
    }

    [Fact]
    public void Concluida_nao_entra_no_caminho_critico_e_agrupador_soma()
    {
        var r = A("Rede", 0, 1, resumo: true);
        var a = A("A", 4, 1, r.Id, custo: 1000); var b = A("B", 4, 2, r.Id, custo: 3000);
        a.Avanco = 100; b.Avanco = 50;
        var ats = new List<AtividadeCronograma> { r, a, b };
        MotorCronograma.Calcular(ats, [L(a, b)], new DateOnly(2026, 3, 2), Cal);
        Assert.False(a.Critica);
        Assert.True(b.Critica);
        Assert.Equal(4000, r.Custo);
        Assert.Equal(62.5m, r.Avanco); // (100×1000 + 50×3000) / 4000
        Assert.Equal(a.Inicio, r.Inicio);
        Assert.Equal(b.Termino, r.Termino);
        Assert.Equal(8, r.Duracao);
    }

    [Fact]
    public void Ciclo_e_barrado()
    {
        var a = A("A", 2); var b = A("B", 2);
        var ex = Assert.Throws<RegraDeNegocioException>(() => MotorCronograma.Calcular([a, b], [L(a, b), L(b, a)], new DateOnly(2026, 3, 2), Cal));
        Assert.Contains("ciclo", ex.Message);
    }

    [Fact]
    public void Arrastar_so_move_vira_folga_no_vinculo()
    {
        var a = A("A", 5, 1); var b = A("B", 3, 2); var c = A("C", 2, 3);
        var ats = new List<AtividadeCronograma> { a, b, c };
        var vs = new List<VinculoAtividade> { L(a, b), L(b, c) };
        var ini = new DateOnly(2026, 3, 2);
        MotorCronograma.Calcular(ats, vs, ini, Cal);
        var termCAntes = c.Termino;

        // B para 3 dias úteis depois: vira +3 no vínculo A→B, sem restrição de data; C acompanha.
        var mudados = MotorCronograma.Mover(b, Cal.Data(Cal.Indice(b.Inicio) + 3), ats, vs, Cal);
        Assert.Single(mudados);
        Assert.Equal(3, vs[0].Lag);
        MotorCronograma.Calcular(ats, vs, ini, Cal);
        Assert.Equal(Cal.Data(Cal.Indice(termCAntes) + 3), c.Termino);
        Assert.Null(b.InicioMinimo);

        // Voltar 5 dias: folga negativa (antecipação) no vínculo
        MotorCronograma.Mover(b, Cal.Data(Cal.Indice(b.Inicio) - 5), ats, vs, Cal);
        Assert.Equal(-2, vs[0].Lag);

        // Sem predecessora: muda o início mínimo
        MotorCronograma.Mover(a, new DateOnly(2026, 3, 9), ats, vs, Cal);
        Assert.Equal(new DateOnly(2026, 3, 9), a.InicioMinimo);
        MotorCronograma.Calcular(ats, vs, ini, Cal);
        Assert.Equal(new DateOnly(2026, 3, 9), a.Inicio);
    }

    [Fact]
    public void Predecessoras_por_EAP_em_portugues_e_ingles()
    {
        var p = MotorCronograma.LerPredecessoras("1.2.3TI+2; 4II, 2.1TT-1;5 ; 3.1SS+3 d");
        Assert.Equal(5, p.Count);
        Assert.Equal(new MotorCronograma.Predecessora("1.2.3", TipoVinculo.TerminoInicio, 2), p[0]);
        Assert.Equal(TipoVinculo.InicioInicio, p[1].Tipo);
        Assert.Equal(-1, p[2].Lag);
        Assert.Equal(TipoVinculo.TerminoInicio, p[3].Tipo);
        Assert.Equal(new MotorCronograma.Predecessora("3.1", TipoVinculo.InicioInicio, 3), p[4]);
        Assert.Throws<RegraDeNegocioException>(() => MotorCronograma.LerPredecessoras("abc"));

        var a = A("A", 1, 1); var b = A("B", 1, 2); var c = A("C", 1, 3);
        MotorCronograma.Renumerar([a, b, c]);
        var vs = new List<VinculoAtividade> { L(a, c, lag: 2), L(b, c, TipoVinculo.InicioInicio, -1) };
        Assert.Equal("1TI+2; 2II-1", MotorCronograma.FormatarPredecessoras(c.Id, vs, new[] { a, b, c }.ToDictionary(x => x.Id)));
    }

    private static (GeradorRedes.RedeDef Torre, GeradorRedes.RedeDef Estrutura, GeradorRedes.PacoteDef Alv, GeradorRedes.PacoteDef Est) Redes()
    {
        var aptos = new List<GeradorRedes.LocalDef>();
        for (var pav = 1; pav <= 3; pav++) for (var ap = 1; ap <= 2; ap++) aptos.Add(new(Guid.NewGuid(), $"Apto {pav}0{ap}", pav));
        var comp = Guid.NewGuid();
        var alv = new GeradorRedes.PacoteDef(Guid.NewGuid(), "Alvenaria", comp, 4, 1, aptos.Select(a => a.Id).ToHashSet());
        var reb = new GeradorRedes.PacoteDef(Guid.NewGuid(), "Reboco", null, 2, 1, aptos.Where(a => a.Nome != "Apto 302").Select(a => a.Id).ToHashSet());
        var torre = new GeradorRedes.RedeDef(Guid.NewGuid(), "Torre A", TipoRede.PorApartamento, 1, aptos, [alv, reb]);
        var pavs = Enumerable.Range(1, 5).Select(p => new GeradorRedes.LocalDef(Guid.NewGuid(), $"Pav. {p}", p)).ToList();
        var est = new GeradorRedes.PacoteDef(Guid.NewGuid(), "Estrutura", null, 10, 1, pavs.Select(p => p.Id).ToHashSet());
        var estrutura = new GeradorRedes.RedeDef(Guid.NewGuid(), "Estrutura", TipoRede.PorPavimento, 0, pavs, [est]);
        return (torre, estrutura, alv, est);
    }

    [Fact]
    public void Gerador_monta_rede_por_apartamento_com_equipes_pulmao_e_vinculo_entre_redes()
    {
        var (torre, estrutura, alv, est) = Redes();
        var g = GeradorRedes.Gerar(V, [estrutura, torre], [new(est.Id, alv.Id, ModoVinculoRede.PorPavimento, 2, 3)], (c, l) => 1000m);

        var folhas = g.Atividades.Where(a => !a.Resumo).ToList();
        Assert.Equal(5 + 6 + 5, folhas.Count);                    // 5 pavs + 6 aptos alvenaria + 5 aptos reboco (302 fora)
        Assert.Equal(6000, folhas.Where(f => f.PacoteId == alv.Id).Sum(f => f.Custo)); // custo do orçamento por apto
        Assert.All(folhas.Where(f => f.PacoteId != alv.Id), f => Assert.Equal(0, f.Custo));

        var ini = new DateOnly(2026, 3, 2);
        MotorCronograma.Renumerar(g.Atividades);
        MotorCronograma.Calcular(g.Atividades, g.Vinculos, ini, Cal);
        AtividadeCronograma F(string nome) => folhas.Single(f => f.Nome == nome);
        // Alvenaria do pav. 1 espera a estrutura do pav. 3 (+3)
        Assert.Equal(Cal.Data(Cal.Indice(F("Estrutura · Pav. 3").Termino) + 1 + 3), F("Alvenaria · Apto 101").Inicio);
        // 1 equipe: apto 102 depois do 101
        Assert.Equal(Cal.Data(Cal.Indice(F("Alvenaria · Apto 101").Termino) + 1), F("Alvenaria · Apto 102").Inicio);
        // Reboco do 101 depois da alvenaria do 101 + pulmão 1
        Assert.True(F("Reboco · Apto 101").Inicio >= Cal.Data(Cal.Indice(F("Alvenaria · Apto 101").Termino) + 2));
        // Alvenaria do pav. 3 (3+2 = 5) espera a estrutura do pav. 5
        Assert.True(F("Alvenaria · Apto 301").Inicio > F("Estrutura · Pav. 5").Termino);
    }

    [Fact]
    public void Regerar_mantem_avanco_base_e_folga_ajustada()
    {
        var (torre, estrutura, alv, est) = Redes();
        var vr = new GeradorRedes.VinculoRedeDef[] { new(est.Id, alv.Id, ModoVinculoRede.PorPavimento, 2, 0) };
        var g1 = GeradorRedes.Gerar(V, [estrutura, torre], vr, (c, l) => 10m);
        var f1 = g1.Atividades.First(a => a.Nome == "Alvenaria · Apto 102");
        f1.Avanco = 40; f1.InicioBase = new DateOnly(2026, 5, 4);
        var v1 = g1.Vinculos.First(v => v.SucessoraId == f1.Id && g1.Atividades.First(a => a.Id == v.PredecessoraId).Nome == "Alvenaria · Apto 101");
        v1.Lag = 4;
        var manual = new AtividadeCronograma { VersaoId = V, Nome = "Ligação definitiva de energia", Duracao = 1, Ordem = 9 };
        var vm = new VinculoAtividade { VersaoId = V, PredecessoraId = f1.Id, SucessoraId = manual.Id };

        var g2 = GeradorRedes.Gerar(V, [estrutura, torre], vr, (c, l) => 10m);
        var m = GeradorRedes.Mesclar([.. g1.Atividades, manual], [.. g1.Vinculos, vm], g2);
        var f2 = m.Atividades.First(a => a.Nome == "Alvenaria · Apto 102");
        Assert.NotEqual(f1.Id, f2.Id);
        Assert.Equal(40, f2.Avanco);
        Assert.Equal(new DateOnly(2026, 5, 4), f2.InicioBase);
        Assert.Equal(4, m.Vinculos.First(v => v.SucessoraId == f2.Id && m.Atividades.First(a => a.Id == v.PredecessoraId).Nome == "Alvenaria · Apto 101").Lag);
        Assert.Contains(m.Atividades, a => a.Id == manual.Id);
        Assert.Contains(m.Vinculos, v => v.PredecessoraId == f2.Id && v.SucessoraId == manual.Id);
    }

    [Fact]
    public void Desembolso_distribui_custo_pelos_dias_uteis_do_mes()
    {
        // 10 dias úteis: 26/10 a 06/11/2026 (02/11 é Finados) → 5 dias em outubro, 5 em novembro
        var a = A("A", 10, 1, custo: 10000);
        MotorCronograma.Calcular([a], [], new DateOnly(2026, 10, 26), Cal);
        Assert.Equal(new DateOnly(2026, 11, 9), a.Termino);
        var d = Desembolso.PorMes([a], Cal);
        Assert.Equal(5000, d[new DateOnly(2026, 10, 1)]);
        Assert.Equal(5000, d[new DateOnly(2026, 11, 1)]);
        Assert.Empty(Desembolso.PorMes([a], Cal, usarBase: true));
    }
}
