using Sigeo.Application.Planejamento;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Planejamento;

namespace Sigeo.Tests.Integracao;

/// <summary>
/// Planejamento no banco real: redes sugeridas pelo orçamento, cronograma gerado com o custo do orçamento, edição por EAP,
/// arrastar = folga no vínculo, aprovação engenheiro → diretor técnico (vigente, base congelada, desembolso), reprogramação devolvida.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public sealed class PlanejamentoTests(BancoFixture f)
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

    private async Task<Usuario> CriarUsuarioAsync(string nome, PerfilUsuario perfil)
    {
        var u = new Usuario { Login = $"{nome.Replace(" ", "").ToLowerInvariant()}.{Guid.NewGuid():N}"[..30], Nome = nome, Email = $"{Guid.NewGuid():N}@exemplo.com", Perfil = perfil, AcessoTodasObras = true };
        await using var db = f.Fabrica.Criar();
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        return u;
    }

    private (PlanejamentoServico Plan, AprovacaoServico Aprov) Como(Usuario u)
    {
        var l = new Logado(f.EmpresaId, u.Id, u.Nome);
        var a = new AprovacaoServico(l, f.Relogio, f.Fabrica);
        return (new PlanejamentoServico(f.Fabrica, l, a, f.Relogio), a);
    }

    private async Task<VersaoCronograma> LerAsync(Guid id)
    {
        await using var db = f.Fabrica.CriarLeitura();
        return await db.VersoesCronograma.AsNoTracking().Include(v => v.Atividades).Include(v => v.Vinculos).Include(v => v.Desembolso).AsSplitQuery().SingleAsync(v => v.Id == id);
    }

    private async Task DecidirAsync(Usuario u, Guid versaoId, DecisaoAprovacao d, string? just = null)
    {
        Guid sid;
        await using (var db = f.Fabrica.CriarLeitura())
            sid = await db.SolicitacoesAprovacao.Where(s => s.EntidadeId == versaoId && s.Status == StatusAprovacao.EmAprovacao).Select(s => s.Id).SingleAsync();
        await Como(u).Aprov.DecidirESalvarAsync(sid, d, just);
    }

    [FatoComBanco]
    public async Task Redes_do_orcamento_geram_cronograma_e_aprovacao_congela_base_e_desembolso()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var (_, _, _, orc) = await Dados.MontarOrcamentoAsync(f, e);
        var planejador = await CriarUsuarioAsync("Planejador Ficticio", PerfilUsuario.Engenharia);
        var engenheiro = await CriarUsuarioAsync("Engenheiro Ficticio", PerfilUsuario.Engenharia);
        var diretor = await CriarUsuarioAsync("Diretor Ficticio", PerfilUsuario.Diretoria);
        var (plan, _) = Como(planejador);

        // Redes sugeridas: uma por torre com os 12 apartamentos e os 2 serviços orçados neles.
        Assert.Equal(1, await plan.SugerirRedesAsync(e.ObraId));
        var lb0 = (await plan.VersaoEmElaboracaoAsync(e.ObraId, criarSeNaoHouver: true))!.Value;
        Assert.Equal(lb0, await plan.VersaoEmElaboracaoAsync(e.ObraId, criarSeNaoHouver: true));
        var n = await plan.GerarPelasRedesAsync(lb0);
        Assert.Equal(24, n);

        var v = await LerAsync(lb0);
        decimal custoOrcado;
        await using (var db = f.Fabrica.CriarLeitura())
            custoOrcado = await db.ServicosLocais.Where(s => s.ObraId == e.ObraId).SumAsync(s => s.Custo);
        Assert.Equal(custoOrcado, v.CustoTotal);                       // todo o custo do orçamento ligado ao cronograma
        Assert.NotNull(v.TerminoPrevisto);
        Assert.Contains(v.Atividades, a => a.Resumo && a.Eap == "1");
        var leaves = v.Atividades.Where(a => !a.Resumo).ToList();
        Assert.All(leaves, a => Assert.False(string.IsNullOrEmpty(a.Eap)));

        // Edição por EAP: predecessora em texto e "arrastar" (vira folga no vínculo).
        var segundo = leaves.Where(a => a.Eap.StartsWith("1.2.")).OrderBy(a => a.Ordem).First();
        var primeiro = leaves.First(a => a.Eap == "1.1.1");
        await plan.SalvarAtividadeAsync(lb0, new PlanejamentoServico.AtividadeEdicao(segundo.Id, segundo.PaiId, segundo.Nome, 3, 0, "1.1.1TI+1", null));
        v = await LerAsync(lb0);
        var vin = v.Vinculos.Single(l => l.SucessoraId == segundo.Id);
        Assert.Equal((primeiro.Id, TipoVinculo.TerminoInicio, 1), (vin.PredecessoraId, vin.Tipo, vin.Lag));
        var cal = new Calendario();
        var s2 = v.Atividades.Single(a => a.Id == segundo.Id);
        await plan.MoverAsync(lb0, segundo.Id, cal.Data(cal.Indice(s2.Inicio) + 2));
        v = await LerAsync(lb0);
        Assert.Equal(3, v.Vinculos.Single(l => l.SucessoraId == segundo.Id).Lag);
        Assert.Null(v.Atividades.Single(a => a.Id == segundo.Id).InicioMinimo);

        // Ciclo é barrado.
        var ex = await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            plan.SalvarAtividadeAsync(lb0, new PlanejamentoServico.AtividadeEdicao(primeiro.Id, primeiro.PaiId, primeiro.Nome, primeiro.Duracao, 0, $"{segundo.Eap}TI", null)));
        Assert.Contains("ciclo", ex.Message);

        // Aprovação: engenheiro responsável → diretor técnico. O solicitante não aprova a própria versão.
        await plan.EnviarParaAprovacaoAsync(lb0, "Planejamento inicial", "LB0 gerada pelas redes");
        Assert.Equal(StatusVersao.EmAprovacao, (await LerAsync(lb0)).Status);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => plan.SalvarAtividadeAsync(lb0, new PlanejamentoServico.AtividadeEdicao(primeiro.Id, primeiro.PaiId, "x", 1, 0, null, null)));
        await Assert.ThrowsAnyAsync<Exception>(() => DecidirAsync(planejador, lb0, DecisaoAprovacao.Aprovado));
        await DecidirAsync(engenheiro, lb0, DecisaoAprovacao.Aprovado);
        Assert.Equal(StatusVersao.EmAprovacao, (await LerAsync(lb0)).Status);
        await DecidirAsync(diretor, lb0, DecisaoAprovacao.Aprovado);
        v = await LerAsync(lb0);
        Assert.Equal(StatusVersao.Vigente, v.Status);
        Assert.Equal("Diretor Ficticio", v.AprovadoPor);
        Assert.All(v.Atividades, a => Assert.Equal(a.Inicio, a.InicioBase));  // linha de base congelada
        Assert.Equal(v.TerminoPrevisto, v.TerminoBase);
        Assert.Equal(custoOrcado, v.Desembolso.Sum(d => d.Valor), 0);       // desembolso mensal gravado
        Assert.Equal(v.Desembolso.Sum(d => d.Valor), v.Desembolso.Sum(d => d.ValorBase), 0);

        // Reprogramação: copia a vigente; alongar uma atividade empurra o término; devolvida volta para elaboração.
        var r1 = await plan.NovaReprogramacaoAsync(e.ObraId);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => plan.NovaReprogramacaoAsync(e.ObraId));
        var rv = await LerAsync(r1);
        Assert.Equal("R1", rv.Codigo);
        Assert.Equal(v.Atividades.Count, rv.Atividades.Count);
        var ultima = rv.Atividades.Where(a => !a.Resumo && a.Critica).OrderByDescending(a => a.Termino).First();
        await plan.SalvarAtividadeAsync(r1, new PlanejamentoServico.AtividadeEdicao(ultima.Id, ultima.PaiId, ultima.Nome, ultima.Duracao + 5, 50, null, null));
        rv = await LerAsync(r1);
        Assert.Equal(5, cal.DiasUteisEntre(rv.TerminoBase!.Value, rv.TerminoPrevisto!.Value));
        await plan.EnviarParaAprovacaoAsync(r1, "Chuvas acima da média", null);
        await DecidirAsync(engenheiro, r1, DecisaoAprovacao.DevolvidoParaAjuste, "Incluir ação de recuperação");
        rv = await LerAsync(r1);
        Assert.Equal(StatusVersao.EmElaboracao, rv.Status);
        Assert.Equal("Incluir ação de recuperação", rv.MotivoDevolucao);
        Assert.Equal(StatusVersao.Vigente, (await LerAsync(lb0)).Status);

        // Regerar pelas redes mantém o avanço lançado.
        await plan.GerarPelasRedesAsync(r1);
        rv = await LerAsync(r1);
        Assert.Contains(rv.Atividades, a => a.Avanco == 50);

        // MS Project: exporta e importa de volta na versão em elaboração.
        var xml = MsProjectXml.Escrever("Obra ficticia", rv.InicioProjeto, rv.Atividades.ToList(), rv.Vinculos.ToList());
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        Assert.Equal(24, await plan.ImportarMsProjectAsync(r1, ms));
        var imp = await LerAsync(r1);
        Assert.Equal(rv.TerminoPrevisto, imp.TerminoPrevisto);
        Assert.Equal(rv.Vinculos.Count, imp.Vinculos.Count);
    }

    [FatoComBanco]
    public async Task Rede_de_area_comum_com_o_que_esta_incluso_e_vinculo_apos_o_fim_da_torre()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var (rev, cp, _, _) = await Dados.MontarOrcamentoAsync(f, e);
        var eng = await CriarUsuarioAsync("Engenheira Ficticia", PerfilUsuario.Engenharia);
        var (plan, _) = Como(eng);
        var locais = await Dados.LocaisAsync(f, e.ObraId);
        var aptos = locais.Where(l => l.Tipo == GeradorEmpreendimento.TipoApartamento).OrderBy(l => l.Codigo).Select(l => l.Id).ToList();
        var comuns = locais.Where(l => l.Tipo == GeradorEmpreendimento.TipoAreaComum).OrderBy(l => l.Codigo).Take(3).Select(l => l.Id).ToList();

        var torre = await plan.SalvarRedeAsync(new PlanejamentoServico.RedeEdicao(null, e.ObraId, "Torre 1 — aptos", TipoRede.PorApartamento, "#4f52d6", 1, aptos,
            [new(null, "Contrapiso", cp.Id, 2, 2), new(null, "Revestimento", rev.Id, 3, 2)]));
        // área comum: pacote sem custo; o 2º local fica de fora (ajuste manual)
        var comum = await plan.SalvarRedeAsync(new PlanejamentoServico.RedeEdicao(null, e.ObraId, "Área comum", TipoRede.PorLocal, "#0f8a6b", 0, comuns,
            [new(null, "Pintura de hall e escada", null, 4, 1, new Dictionary<Guid, bool> { [comuns[1]] = false })]));
        Guid pacRev, pacPint;
        await using (var db = f.Fabrica.CriarLeitura())
        {
            pacRev = await db.RedePacotes.Where(p => p.RedeId == torre && p.Ordem == 2).Select(p => p.Id).SingleAsync();
            pacPint = await db.RedePacotes.Where(p => p.RedeId == comum).Select(p => p.Id).SingleAsync();
        }
        await plan.SalvarVinculoRedeAsync(new VinculoRede { ObraId = e.ObraId, PacoteOrigemId = pacRev, PacoteDestinoId = pacPint, Modo = ModoVinculoRede.AposFimDaRede, Lag = 2 });

        var v = (await plan.VersaoEmElaboracaoAsync(e.ObraId, true))!.Value;
        Assert.Equal(12 + 12 + 2, await plan.GerarPelasRedesAsync(v));
        var lida = await LerAsync(v);
        var pint = lida.Atividades.Where(a => a.PacoteId == pacPint && !a.Resumo).OrderBy(a => a.Ordem).ToList();
        Assert.Equal(2, pint.Count);
        Assert.DoesNotContain(pint, a => a.LocalId == comuns[1]);
        var ultimaRev = lida.Atividades.Where(a => a.PacoteId == pacRev && !a.Resumo).MaxBy(a => a.Termino)!;
        var cal = new Calendario();
        Assert.Equal(cal.Data(cal.Indice(ultimaRev.Termino) + 1 + 2), pint[0].Inicio);   // após o último local da torre + 2
        // 2 equipes: o 3º apto do contrapiso espera o 1º
        var contra = lida.Atividades.Where(a => a.PacoteId != null && a.Nome.StartsWith("Contrapiso ·")).OrderBy(a => a.Ordem).ToList();
        Assert.Equal(contra[0].Inicio, contra[1].Inicio);
        Assert.True(contra[2].Inicio > contra[0].Termino);

        // Excluir pacote com vínculo é barrado; excluir a rede leva os vínculos junto.
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => plan.SalvarRedeAsync(new PlanejamentoServico.RedeEdicao(comum, e.ObraId, "Área comum", TipoRede.PorLocal, "#0f8a6b", 0, comuns, [])));
        await plan.ExcluirRedeAsync(comum);
        await using (var db = f.Fabrica.CriarLeitura())
            Assert.False(await db.VinculosRede.AnyAsync(x => x.PacoteDestinoId == pacPint));
    }
}
