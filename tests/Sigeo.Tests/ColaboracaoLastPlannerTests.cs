using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Planejamento;

namespace Sigeo.Tests;

public class ColaboracaoLastPlannerTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Ana = Guid.CreateVersion7(), Bia = Guid.CreateVersion7(), Caio = Guid.CreateVersion7();

    [Fact]
    public void Conversa_abre_com_participantes_unicos_e_primeira_mensagem()
    {
        var c = Conversa.Abrir(" Entrega do cimento ", "Chega amanhã às 7h", Ana, "Ana", [(Bia, "Bia"), (Ana, "Ana"), (Bia, "Bia")], [(Caio, "Caio"), (Bia, "Bia")], Agora);
        Assert.Equal("Entrega do cimento", c.Assunto);
        Assert.Equal(3, c.Participantes.Count);
        Assert.Equal(PapelParticipante.Remetente, c.Participantes.Single(p => p.UsuarioId == Ana).Papel);
        Assert.Equal(PapelParticipante.Copia, c.Participantes.Single(p => p.UsuarioId == Caio).Papel);
        Assert.Equal(Agora, c.Participantes.Single(p => p.UsuarioId == Ana).LidaAte);
        Assert.Single(c.Mensagens);
    }

    [Theory]
    [InlineData("", "texto")]
    [InlineData("assunto", " ")]
    public void Conversa_sem_assunto_ou_texto_e_recusada(string assunto, string texto) =>
        Assert.Throws<RegraDeNegocioException>(() => Conversa.Abrir(assunto, texto, Ana, "Ana", [(Bia, "Bia")], [], Agora));

    [Fact]
    public void Conversa_so_comigo_e_recusada() =>
        Assert.Throws<RegraDeNegocioException>(() => Conversa.Abrir("a", "b", Ana, "Ana", [(Ana, "Ana")], [], Agora));

    [Fact]
    public void Resposta_desarquiva_para_os_outros_e_so_participante_responde()
    {
        var c = Conversa.Abrir("a", "b", Ana, "Ana", [(Bia, "Bia")], [], Agora);
        c.Participantes.Single(p => p.UsuarioId == Bia).Arquivada = true;
        c.Participantes.Single(p => p.UsuarioId == Ana).Arquivada = true;
        var m = c.Responder(Ana, "Ana", "nova", Agora.AddMinutes(5));
        Assert.False(c.Participantes.Single(p => p.UsuarioId == Bia).Arquivada);
        Assert.True(c.Participantes.Single(p => p.UsuarioId == Ana).Arquivada);
        Assert.Equal(Agora.AddMinutes(5), c.UltimaEm);
        Assert.Equal(m.EnviadaEm, c.Participantes.Single(p => p.UsuarioId == Ana).LidaAte);
        Assert.Throws<RegraDeNegocioException>(() => c.Responder(Caio, "Caio", "x", Agora));
        Assert.Null(c.Incluir(Bia, "Bia", PapelParticipante.Copia));
        Assert.NotNull(c.Incluir(Caio, "Caio", PapelParticipante.Copia));
    }

    [Fact]
    public void Texto_formatado_codifica_html_e_aplica_negrito_italico_e_lista()
    {
        var h = TextoFormatado.ParaHtml("Oi **time**, veja _isto_:\n- item <b>1</b>\n- item 2\n\nfim <script>alert(1)</script>");
        Assert.Contains("<b>time</b>", h);
        Assert.Contains("<i>isto</i>", h);
        Assert.Contains("<ul><li>item &lt;b&gt;1&lt;/b&gt;</li><li>item 2</li></ul>", h);
        Assert.Contains("&lt;script&gt;", h);
        Assert.DoesNotContain("<script>", h);
        Assert.Equal("Oi time · item 2", TextoFormatado.Resumo("Oi **time**\n- item 2"));
        Assert.Equal(10, TextoFormatado.Resumo(new string('a', 50), 10).Length);
    }

    [Fact]
    public void Aviso_preenche_campos_e_espera_mais_a_cada_falha()
    {
        Assert.Equal("RNC 12 venceu em 01/10. {link}", AvisoEmail.Preencher("{RNC} venceu em {data}. {link}", new Dictionary<string, string?> { ["rnc"] = "RNC 12", ["data"] = "01/10" }));
        var a = new AvisoEmail();
        a.MarcarFalha("x", Agora);
        Assert.Equal(Agora.AddMinutes(1), a.ProximaTentativaEm);
        a.MarcarFalha("x", Agora);
        Assert.Equal(Agora.AddMinutes(4), a.ProximaTentativaEm);
        for (var i = 0; i < 3; i++) a.MarcarFalha("y", Agora);
        Assert.Equal(SituacaoAviso.Falhou, a.Situacao);
        Assert.Null(a.ProximaTentativaEm);
        a.MarcarEnviado(Agora);
        Assert.Equal(SituacaoAviso.Enviado, a.Situacao);
    }

    [Theory]
    [InlineData("2026-10-05", "2026-10-05")]
    [InlineData("2026-10-08", "2026-10-05")]
    [InlineData("2026-10-11", "2026-10-05")]
    [InlineData("2026-10-12", "2026-10-12")]
    public void Semana_comeca_na_segunda(string dia, string segunda) =>
        Assert.Equal(DateOnly.Parse(segunda), PlanoSemanal.SegundaDe(DateOnly.Parse(dia)));

    private static CompromissoSemanal C(SituacaoCompromisso s, bool fora = false, CausaNaoCumprimento? causa = null) =>
        new() { Descricao = "x", Situacao = s, ForaDaMeta = fora, Causa = causa };

    [Fact]
    public void Ppc_conta_so_o_planejado_e_fora_da_meta_fica_a_parte()
    {
        var (pl, ok, fora, ppc) = PlanoSemanal.Apurar([C(SituacaoCompromisso.Concluido), C(SituacaoCompromisso.Concluido), C(SituacaoCompromisso.NaoConcluido, causa: CausaNaoCumprimento.Clima),
            C(SituacaoCompromisso.Concluido, fora: true)]);
        Assert.Equal((3, 2, 1, 66.7m), (pl, ok, fora, ppc));
        Assert.Null(PlanoSemanal.Apurar([C(SituacaoCompromisso.Concluido, fora: true)]).Ppc);
    }

    [Fact]
    public void Fechar_exige_apontamento_e_causa_e_trava_a_semana()
    {
        var p = new PlanoSemanal { Semana = new DateOnly(2026, 10, 5) };
        Assert.Throws<RegraDeNegocioException>(() => p.Fechar("eng", Agora));                         // sem compromissos
        p.Compromissos.Add(C(SituacaoCompromisso.Pendente));
        Assert.Throws<RegraDeNegocioException>(() => p.Fechar("eng", Agora));
        p.Compromissos.Clear();
        p.Compromissos.Add(C(SituacaoCompromisso.NaoConcluido));
        Assert.Throws<RegraDeNegocioException>(() => p.Fechar("eng", Agora));
        p.Compromissos.Clear();
        p.Compromissos.Add(C(SituacaoCompromisso.Concluido));
        p.Compromissos.Add(C(SituacaoCompromisso.NaoConcluido, causa: CausaNaoCumprimento.Material));
        p.Fechar("eng", Agora);
        Assert.Equal(50m, p.Ppc);
        Assert.False(p.Editavel);
        Assert.Throws<RegraDeNegocioException>(() => p.Fechar("eng", Agora));
    }

    [Fact]
    public void Apontamento_nao_concluido_exige_causa_e_concluido_limpa_a_causa()
    {
        var c = new CompromissoSemanal { Descricao = "x" };
        Assert.Throws<RegraDeNegocioException>(() => c.Apontar(SituacaoCompromisso.NaoConcluido, null, null));
        c.Apontar(SituacaoCompromisso.NaoConcluido, CausaNaoCumprimento.Projeto, " falta detalhe ");
        Assert.Equal("falta detalhe", c.ObservacaoCausa);
        c.Apontar(SituacaoCompromisso.Concluido, CausaNaoCumprimento.Projeto, "x");
        Assert.Null(c.Causa);
        Assert.Null(c.ObservacaoCausa);
    }

    [Fact]
    public void Restricao_valida_vence_remove_e_nao_reabre()
    {
        var r = new Restricao();
        Assert.Equal(4, r.Validar().Count);
        r = new Restricao { Descricao = "Projeto sem revisão", AtividadeChave = "R:x", ResponsavelNome = "Eva", DataLimite = new DateOnly(2026, 10, 7) };
        Assert.Empty(r.Validar());
        Assert.True(r.Vencida(new DateOnly(2026, 10, 8)));
        Assert.Throws<RegraDeNegocioException>(() => r.Remover(" ", "eva", Agora));
        r.Remover("Revisão R02 emitida", "eva", Agora);
        Assert.Equal(StatusRestricao.Removida, r.Status);
        Assert.False(r.Vencida(new DateOnly(2026, 10, 8)));
        Assert.Throws<RegraDeNegocioException>(() => r.Cancelar("x", "eva", Agora));
    }

    [Theory]
    [InlineData("2026-10-08T07:00:00-03:00", true)]
    [InlineData("2026-10-08T05:59:00-03:00", false)]
    [InlineData("2026-10-08T21:00:00-03:00", false)]
    [InlineData("2026-10-11T10:00:00-03:00", false)]
    public void Expediente_das_rotinas(string quando, bool dentro) =>
        Assert.Equal(dentro, Sigeo.Application.Comum.Expediente.Dentro(DateTimeOffset.Parse(quando)));

    [Fact]
    public void Rotas_dos_registros()
    {
        var id = Guid.CreateVersion7();
        Assert.Equal($"obra/rnc?id={id}", Sigeo.Application.Comum.RotasRegistro.Link("Rnc", id));
        Assert.Equal($"mensagens?id={id}", Sigeo.Application.Comum.RotasRegistro.Link("Conversa", id));
        Assert.Null(Sigeo.Application.Comum.RotasRegistro.Link("Desconhecida", id));
        Assert.Equal("RNC", Sigeo.Application.Comum.RotasRegistro.Rotulo("rnc"));
    }
}
