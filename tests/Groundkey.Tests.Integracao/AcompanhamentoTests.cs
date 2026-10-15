using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Common;
using Groundkey.Domain.Estrutura;
using Groundkey.Domain.Planejamento;

namespace Groundkey.Tests.Integracao;

/// <summary>
/// Acompanhamento do planejamento no banco real: Gantt por local (redes › locais › serviços com ligações), ligar/alterar/excluir
/// pelo editor, medição semanal (sobrepõe na mesma semana, aviso de FVS), painel por macroetapa, programação semanal e diário de obra.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public sealed class AcompanhamentoTests(BancoFixture f)
{
    private sealed class Logado(Guid empresa, Guid id, string nome) : IUsuarioAtual
    {
        public Guid EmpresaId => empresa;
        public Guid? UsuarioId => id;
        public string Nome => nome;
        public bool AcessoTodasObras => true;
        public IReadOnlyCollection<Guid> ObrasPermitidas { get; } = [];
        public string Origem => "Teste";
        public string? CorrelationId => null;
    }

    private async Task<Usuario> UsuarioAsync(string nome, PerfilUsuario perfil)
    {
        var u = new Usuario { Login = $"{nome.Replace(" ", "").ToLowerInvariant()}.{Guid.NewGuid():N}"[..30], Nome = nome, Email = $"{Guid.NewGuid():N}@exemplo.com", Perfil = perfil, AcessoTodasObras = true };
        await using var db = f.Fabrica.Criar();
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        return u;
    }

    private Logado L(Usuario u) => new(f.EmpresaId, u.Id, u.Nome);
    private PlanejamentoServico Plan(Usuario u) { var l = L(u); return new PlanejamentoServico(f.Fabrica, l, new AprovacaoServico(l, f.Relogio, f.Fabrica), f.Relogio); }
    private AcompanhamentoServico Acomp(Usuario u) => new(f.Fabrica, L(u), f.Relogio);

    private async Task<(Empreendimento E, Guid Versao, Usuario Eng)> MontarAsync()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        await Dados.MontarOrcamentoAsync(f, e);
        var eng = await UsuarioAsync("Engenheira Ficticia", PerfilUsuario.Engenharia);
        var plan = Plan(eng);
        await plan.SugerirRedesAsync(e.ObraId);
        var v = (await plan.VersaoEmElaboracaoAsync(e.ObraId, true))!.Value;
        await plan.AlterarInicioProjetoAsync(v, DateOnly.FromDateTime(DateTime.Today).AddDays(-20), null);
        await plan.GerarPelasRedesAsync(v);
        return (e, v, eng);
    }

    [FatoComBanco]
    public async Task Gantt_por_local_e_editor_ligam_movem_e_recalculam()
    {
        var (e, v, eng) = await MontarAsync();
        var g = (await Acomp(eng).GanttLocalAsync(e.ObraId, v, podeEditar: true))!;
        Assert.True(g.Editavel);
        var rede = Assert.Single(g.Redes);
        Assert.Equal(12, rede.Locais.Count);                                          // apartamentos da torre
        Assert.Equal(2, g.Servicos.Count);                                            // dois serviços (pacotes) com cores
        Assert.Equal(24, g.Tarefas.Count);
        Assert.All(g.Tarefas, t => Assert.Contains(rede.Locais, l => l.Id == t.LinhaId));
        Assert.NotEmpty(g.Ligacoes);
        Assert.All(rede.Locais, l => Assert.NotNull(l.Grupo));                            // pavimento do apartamento (linhas por pavimento)

        // Liga o último serviço de um apartamento ao primeiro de outro (fim → início), muda a folga e exclui.
        var plan = Plan(eng);
        var a1 = g.Tarefas.Where(t => t.Servico == g.Servicos[1].Nome).OrderBy(t => t.Inicio).First();
        var b2 = g.Tarefas.Where(t => t.Servico == g.Servicos[0].Nome).OrderBy(t => t.Inicio).Last();
        var id = await plan.LigarAsync(v, a1.Id, b2.Id, TipoVinculo.TerminoInicio, 0);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => plan.LigarAsync(v, a1.Id, b2.Id, TipoVinculo.TerminoInicio, 0));   // repetida
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => plan.LigarAsync(v, b2.Id, a1.Id, TipoVinculo.TerminoInicio, 0));   // ciclo
        await plan.AlterarVinculoAsync(v, id, TipoVinculo.TerminoInicio, 3);
        var g2 = (await Acomp(eng).GanttLocalAsync(e.ObraId, v, true))!;
        var b2n = g2.Tarefas.Single(t => t.Id == b2.Id);
        Assert.True(b2n.Inicio > g2.Tarefas.Single(t => t.Id == a1.Id).Termino);
        Assert.Contains(g2.Ligacoes, l => l.Id == id && l.Lag == 3);
        await plan.ExcluirVinculoAsync(v, id);
        Assert.DoesNotContain((await Acomp(eng).GanttLocalAsync(e.ObraId, v, true))!.Ligacoes, l => l.Id == id);
    }

    [FatoComBanco]
    public async Task Medicao_semanal_sobrepoe_na_semana_atualiza_avanco_e_painel()
    {
        var (e, v, eng) = await MontarAsync();
        var mapa = await Acomp(eng).MapaMedicaoAsync(e.ObraId, null, null, DateOnly.FromDateTime(DateTime.Today));
        Assert.True(mapa.PodeMedir);
        var op = Assert.Single(mapa.Opcoes);
        Assert.Equal(12, mapa.Celulas.Count);
        var pac = op.Servicos[0].PacoteId;
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var locais = mapa.Celulas.Take(3).Select(c => c.LocalId).ToList();

        var r1 = await Acomp(eng).MedirAsync(e.ObraId, pac, locais, 50, hoje, "primeira");
        Assert.Equal((3, 0), (r1.Gravadas, r1.Sobrepostas));
        var r2 = await Acomp(eng).MedirAsync(e.ObraId, pac, locais.Take(2).ToList(), 100, hoje, null);
        Assert.Equal((2, 2), (r2.Gravadas, r2.Sobrepostas));                          // mesma semana: sobrepõe

        await using (var db = f.Fabrica.CriarLeitura())
        {
            var semana = MedicaoSemanal.SegundaDe(hoje);
            var med = await db.MedicoesSemanais.Where(m => m.ObraId == e.ObraId && m.Semana == semana).ToListAsync();
            Assert.Equal(3, med.Count);
            Assert.Equal(2, med.Count(m => m.Percentual == 100));
            var ats = await db.AtividadesCronograma.Where(a => a.VersaoId == v && a.PacoteId == pac && a.LocalId != null && locais.Contains(a.LocalId.Value)).ToListAsync();
            Assert.Equal(2, ats.Count(a => a.Avanco == 100));
            Assert.Single(ats, a => a.Avanco == 50);
        }
        var mapa2 = await Acomp(eng).MapaMedicaoAsync(e.ObraId, op.RedeId, pac, hoje);
        Assert.Equal(3, mapa2.Celulas.Count(c => c.MedidoEm == hoje));

        // Quem não é da Engenharia não mede; data futura recusada.
        var comprador = await UsuarioAsync("Comprador Ficticio", PerfilUsuario.Suprimentos);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Acomp(comprador).MedirAsync(e.ObraId, pac, locais, 10, hoje, null));
        await Assert.ThrowsAsync<ValidacaoException>(() => Acomp(eng).MedirAsync(e.ObraId, pac, locais, 10, hoje.AddDays(3), null));

        var painel = await Acomp(eng).PainelAsync(e.ObraId, null, hoje);
        Assert.NotNull(painel.Versao);
        Assert.True(painel.Real > 0);
        Assert.NotEmpty(painel.Macroetapas);
        Assert.NotEmpty(painel.Curva);
        Assert.Contains(painel.Curva, p => p.Real > 0);
        var porSpe = await Acomp(eng).PainelAsync(e.ObraId, e.SpeId, hoje);
        Assert.Equal(painel.Real, porSpe.Real);                                      // obra com uma SPE

        var prog = await Acomp(eng).ProgramacaoAsync(e.ObraId, null, hoje, hoje.AddDays(13));
        Assert.InRange(prog.Semanas.Count, 2, 3);
        Assert.Contains(prog.Semanas.SelectMany(s => s.Linhas), l => l.Dias.Any(d => d) && l.Ate >= l.De);
    }

    [FatoComBanco]
    public async Task Diario_de_obra_salva_assina_pelo_engenheiro_e_trava()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var eng = await UsuarioAsync("Engenheiro da Obra Ficticio", PerfilUsuario.Engenharia);
        var outro = await UsuarioAsync("Outro Engenheiro Ficticio", PerfilUsuario.Engenharia);
        await using (var db = f.Fabrica.Criar()) { (await db.Obras.SingleAsync(o => o.Id == e.ObraId)).EngenheiroId = eng.Id; await db.SaveChangesAsync(); }
        DiarioObraServico S(Usuario u) => new(f.Fabrica, L(u), f.Relogio);
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var d = await S(eng).AbrirAsync(e.ObraId, hoje);
        Assert.Equal(0, d.Numero);
        d.ClimaManha = ClimaDia.Nublado;
        d.Efetivo.Add(new DiarioEfetivo { Nome = "Pedreiro", Quantidade = 4 });
        d.Efetivo.Add(new DiarioEfetivo { Terceiro = true, Nome = "Empreiteira Exemplo", Quantidade = 6 });
        d.Atividades.Add(new DiarioAtividade { Descricao = "Alvenaria do 2º pavimento" });
        var id = await S(eng).SalvarAsync(d);
        var lido = await S(eng).AbrirAsync(e.ObraId, hoje);
        Assert.Equal((id, 10, 1), (lido.Id, lido.Efetivo.Sum(x => x.Quantidade), lido.Atividades.Count));
        Assert.True(lido.Numero > 0);

        // Mesma atividade (Id preservado) para as fotos; nova atividade entra.
        var ativ = lido.Atividades.Single().Id;
        lido.Atividades.Add(new DiarioAtividade { Descricao = "Contrapiso do 1º pavimento" });
        await S(eng).SalvarAsync(lido);
        Assert.Contains((await S(eng).AbrirAsync(e.ObraId, hoje)).Atividades, a => a.Id == ativ);

        Assert.False(await S(outro).PodeAssinarAsync(e.ObraId));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => S(outro).AssinarAsync(id));
        await S(eng).AssinarAsync(id);
        var assinado = await S(eng).AbrirAsync(e.ObraId, hoje);
        Assert.Equal(StatusDiario.Assinado, assinado.Status);
        Assert.Equal(eng.Nome, assinado.AssinadoPorNome);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => S(eng).SalvarAsync(assinado));
        Assert.Single(await S(eng).MesAsync(e.ObraId, hoje.Year, hoje.Month), x => x.Data == hoje && x.Status == StatusDiario.Assinado);

        // Validação: data futura e terceiro sem nome.
        var fut = await S(eng).AbrirAsync(e.ObraId, hoje.AddDays(2));
        await Assert.ThrowsAsync<ValidacaoException>(() => S(eng).SalvarAsync(fut));
        var semNome = await S(eng).AbrirAsync(e.ObraId, hoje.AddDays(-1));
        semNome.Efetivo.Add(new DiarioEfetivo { Terceiro = true, Nome = "", Quantidade = 3 });
        await Assert.ThrowsAsync<ValidacaoException>(() => S(eng).SalvarAsync(semNome));
    }
}
