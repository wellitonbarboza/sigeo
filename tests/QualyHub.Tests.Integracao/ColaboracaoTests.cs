using QualyHub.Application.Comum;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Colaboracao;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Planejamento;

namespace QualyHub.Tests.Integracao;

/// <summary>
/// Mensagens internas (grupo por perfil, cópia, ligação ao registro, leitura, arquivar/responder, só participante abre),
/// fila de avisos por e-mail (regra, sem repetir, envio e nova tentativa) e Last Planner (restrição barra o plano,
/// fora da meta depois da segunda, fechamento com PPC e causas) no banco real.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public sealed class ColaboracaoTests(BancoFixture f)
{
    private sealed class Logado(Guid empresa, Usuario u) : IUsuarioAtual
    {
        public Guid EmpresaId => empresa;
        public Guid? UsuarioId => u.Id;
        public string Nome => u.Nome;
        public bool AcessoTodasObras => u.AcessoTodasObras;
        public IReadOnlyCollection<Guid> ObrasPermitidas => u.Obras.Select(o => o.ObraId).ToList();
        public string Origem => "Teste";
        public string? CorrelationId => null;
    }

    private sealed class EnvioFalso : IEnvioMensagens
    {
        public List<(string Para, string Assunto, string Texto)> Enviados { get; } = [];
        public bool Falhar { get; set; }
        public bool EmailDisponivel => true;
        public bool WhatsAppDisponivel => false;
        public Task EnviarEmailAsync(string para, string assunto, string texto, string html, CancellationToken ct = default)
        {
            if (Falhar) throw new InvalidOperationException("caixa cheia");
            Enviados.Add((para, assunto, texto));
            return Task.CompletedTask;
        }
        public Task EnviarCodigoWhatsAppAsync(string celularE164, string codigo, CancellationToken ct = default) => Task.CompletedTask;
        public Task EnviarConviteWhatsAppAsync(string celularE164, string construtora, string assunto, string endereco, CancellationToken ct = default) => Task.CompletedTask;
    }

    private readonly CanalAvisos _canal = new();

    private async Task<Usuario> UsuarioAsync(string nome, PerfilUsuario perfil, bool todas = true)
    {
        var u = new Usuario { Login = $"{nome.Replace(" ", "").ToLowerInvariant()}{Guid.NewGuid():N}"[..30], Nome = nome, Email = $"{Guid.NewGuid():N}@exemplo.com", Perfil = perfil, AcessoTodasObras = todas };
        await using var db = f.Fabrica.Criar();
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        return u;
    }

    private MensagensServico Msg(Usuario u) => new(f.Fabrica, new Logado(f.EmpresaId, u), f.Relogio, _canal);
    private CurtoPrazoServico Lp(Usuario u) => new(f.Fabrica, new Logado(f.EmpresaId, u), f.Relogio);

    [FatoComBanco]
    public async Task Mensagem_para_grupo_ligada_ao_registro_com_leitura_aviso_e_envio()
    {
        var sufixo = Guid.NewGuid().ToString("N")[..5];
        var ana = await UsuarioAsync($"Ana Remetente {sufixo}", PerfilUsuario.Qualidade);
        var bruno = await UsuarioAsync($"Bruno Engenheiro {sufixo}", PerfilUsuario.Engenharia);
        var dora = await UsuarioAsync($"Dora Diretora {sufixo}", PerfilUsuario.Diretoria);
        var caio = await UsuarioAsync($"Caio Fora {sufixo}", PerfilUsuario.Suprimentos, todas: false);
        var tarefa = new Tarefa { Numero = 9000 + Random.Shared.Next(999), Titulo = "Conferir contrapiso", ResponsavelId = bruno.Id, ResponsavelNome = bruno.Nome, Prazo = DateOnly.FromDateTime(DateTime.Today.AddDays(5)) };
        tarefa.PrepararNova();
        await using (var db = f.Fabrica.Criar()) { db.Tarefas.Add(tarefa); await db.SaveChangesAsync(); }

        // Grupo por perfil + cópia nominal, ligada à tarefa.
        var id = await Msg(ana).EnviarAsync(new MensagensServico.NovaMensagem("Fissuras no apto 202", "Encontrei **fissuras**.\n- remover 6 m²\n- refazer com tela\n<script>x</script>",
            ["p:Engenharia"], [$"u:{dora.Id}"], null, "Tarefa", tarefa.Id));

        Assert.Equal(1, await Msg(bruno).NaoLidasAsync());
        Assert.Equal(0, await Msg(ana).NaoLidasAsync());
        Assert.Equal(0, await Msg(caio).NaoLidasAsync());
        Assert.Contains(await Msg(bruno).CaixaAsync(MensagensServico.Pasta.Entrada), c => c.Id == id && c.NaoLidas == 1 && c.RegistroDescricao == $"Tarefa {tarefa.Numero}");
        Assert.Contains(await Msg(ana).CaixaAsync(MensagensServico.Pasta.Enviadas), c => c.Id == id);
        Assert.DoesNotContain(await Msg(ana).CaixaAsync(MensagensServico.Pasta.Entrada), c => c.Id == id);   // só a minha mensagem: não está na minha entrada
        Assert.Contains(await Msg(dora).CaixaAsync(MensagensServico.Pasta.Entrada, "fissuras"), c => c.Id == id);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Msg(caio).AbrirAsync(id));                  // só participante abre
        Assert.Single(await Msg(ana).DoRegistroAsync("Tarefa", tarefa.Id));
        Assert.Empty(await Msg(caio).DoRegistroAsync("Tarefa", tarefa.Id));

        // Aviso por e-mail na fila para quem recebeu (regra "Nova mensagem interna" com e-mail), não para quem enviou.
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var avisos = await db.AvisosEmail.Where(a => a.Link == $"mensagens?id={id}").ToListAsync();
            Assert.Contains(avisos, a => a.UsuarioId == bruno.Id);
            Assert.Contains(avisos, a => a.UsuarioId == dora.Id);
            Assert.DoesNotContain(avisos, a => a.UsuarioId == ana.Id);
        }

        // Leitura registrada; texto sai codificado (nada vira HTML ativo).
        var vista = await Msg(bruno).AbrirAsync(id);
        Assert.Contains("<b>fissuras</b>", vista.Mensagens[0].Html);
        Assert.Contains("<li>remover 6 m²</li>", vista.Mensagens[0].Html);
        Assert.DoesNotContain("<script>", vista.Mensagens[0].Html);
        Assert.Equal(0, await Msg(bruno).NaoLidasAsync());
        var deAna = await Msg(ana).AbrirAsync(id);
        Assert.Contains(deAna.Mensagens[0].Lidos, l => l.Nome == bruno.Nome);
        Assert.Contains(dora.Nome, deAna.Mensagens[0].NaoLeram);

        // Arquivar tira da entrada; resposta de outro participante traz de volta e inclui pessoa nova.
        await Msg(bruno).ArquivarAsync(id, true);
        Assert.DoesNotContain(await Msg(bruno).CaixaAsync(MensagensServico.Pasta.Entrada), c => c.Id == id);
        Assert.Contains(await Msg(bruno).CaixaAsync(MensagensServico.Pasta.Arquivadas), c => c.Id == id);
        await Msg(dora).ResponderAsync(id, "Ok, aprovo a remoção.", [$"u:{caio.Id}"]);
        Assert.Contains(await Msg(bruno).CaixaAsync(MensagensServico.Pasta.Entrada), c => c.Id == id && c.NaoLidas == 1);
        Assert.Equal(2, (await Msg(caio).AbrirAsync(id)).Mensagens.Count);                                 // incluído vê o histórico
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Msg(caio).EnviarAsync(new MensagensServico.NovaMensagem("x", "y", [$"u:{caio.Id}"])));

        // Envio da fila: sai uma vez; falha conta tentativa e espera.
        var envio = new EnvioFalso();
        await using (var db = f.Fabrica.Criar())
        {
            var (ok, falhas) = await AvisosServico.EnviarPendentesAsync(db, envio, "https://qualyhub.exemplo/", "Ficticia", DateTimeOffset.UtcNow, lote: 500);
            Assert.True(ok >= 2); Assert.Equal(0, falhas);
        }
        Assert.Contains(envio.Enviados, e => e.Assunto.Contains("Fissuras no apto 202") && e.Texto.Contains($"https://qualyhub.exemplo/mensagens?id={id}"));
        await using (var db = f.Fabrica.Criar())
            Assert.Equal((0, 0), await AvisosServico.EnviarPendentesAsync(db, envio, "https://qualyhub.exemplo/", "Ficticia", DateTimeOffset.UtcNow, lote: 500));

        await Msg(ana).ResponderAsync(id, "Obrigada!");
        envio.Falhar = true;
        await using (var db = f.Fabrica.Criar())
        {
            var (_, falhas) = await AvisosServico.EnviarPendentesAsync(db, envio, "https://qualyhub.exemplo/", "Ficticia", DateTimeOffset.UtcNow, lote: 500);
            Assert.True(falhas >= 1);
        }
        await using (var db = f.Fabrica.CriarLeitura())
            Assert.Contains(await db.AvisosEmail.Where(a => a.Link == $"mensagens?id={id}" && a.Situacao == SituacaoAviso.Pendente).ToListAsync(),
                a => a.Tentativas == 1 && a.ProximaTentativaEm > DateTimeOffset.UtcNow && a.Detalhe == "caixa cheia");
    }

    [FatoComBanco]
    public async Task Lembretes_seguem_a_regra_e_nao_repetem_no_mesmo_dia()
    {
        var resp = await UsuarioAsync("Rita Responsavel", PerfilUsuario.Engenharia);
        var t = new Tarefa { Numero = 8000 + Random.Shared.Next(999), Titulo = "Tarefa nova de teste", ResponsavelId = resp.Id, ResponsavelNome = resp.Nome, Prazo = DateOnly.FromDateTime(DateTime.Today.AddDays(3)) };
        t.PrepararNova();
        await using (var db = f.Fabrica.Criar()) { db.Tarefas.Add(t); await db.SaveChangesAsync(); }
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        await using (var db = f.Fabrica.Criar())
        {
            Assert.True(await AvisosServico.GerarAsync(db, DateTimeOffset.UtcNow, hoje) >= 1);
            await db.SaveChangesAsync();
        }
        await using (var db = f.Fabrica.Criar())
        {
            await AvisosServico.GerarAsync(db, DateTimeOffset.UtcNow, hoje);
            await db.SaveChangesAsync();
        }
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var avisos = await db.AvisosEmail.Where(a => a.UsuarioId == resp.Id && a.Evento == EventosAviso.TarefaAtribuida).ToListAsync();
            Assert.Single(avisos);                                                    // a segunda varredura não repete
            Assert.Contains(t.Titulo, avisos[0].Texto);
            Assert.Equal($"tarefas?id={t.Id}", avisos[0].Link);
        }

        // Regra sem o canal e-mail: nada entra na fila.
        await using (var db = f.Fabrica.Criar())
        {
            var r = await db.RegrasNotificacao.SingleAsync(x => x.Evento == EventosAviso.TarefaAtribuida);
            r.Canal = CanalNotificacao.Pendencias;
            await db.SaveChangesAsync();
        }
        try
        {
            var t2 = new Tarefa { Numero = 7000 + Random.Shared.Next(999), Titulo = "Outra tarefa", ResponsavelId = resp.Id, ResponsavelNome = resp.Nome, Prazo = hoje.AddDays(3) };
            t2.PrepararNova();
            await using (var db = f.Fabrica.Criar()) { db.Tarefas.Add(t2); await db.SaveChangesAsync(); }
            await using (var db = f.Fabrica.Criar()) { await AvisosServico.GerarAsync(db, DateTimeOffset.UtcNow, hoje); await db.SaveChangesAsync(); }
            await using (var db = f.Fabrica.CriarLeitura())
                Assert.False(await db.AvisosEmail.AnyAsync(a => a.Link == $"tarefas?id={t2.Id}"));
        }
        finally
        {
            await using var db = f.Fabrica.Criar();
            (await db.RegrasNotificacao.SingleAsync(x => x.Evento == EventosAviso.TarefaAtribuida)).Canal = CanalNotificacao.Pendencias | CanalNotificacao.Email;
            await db.SaveChangesAsync();
        }
    }

    [FatoComBanco]
    public async Task Restricao_barra_o_plano_semanal_e_fechamento_apura_ppc()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        await Dados.MontarOrcamentoAsync(f, e);
        var eng = await UsuarioAsync("Eva Planejadora", PerfilUsuario.Engenharia);
        var supr = await UsuarioAsync("Sergio Suprimentos", PerfilUsuario.Suprimentos);
        var l = new Logado(f.EmpresaId, eng);
        var plan = new PlanejamentoServico(f.Fabrica, l, new AprovacaoServico(l, f.Relogio, f.Fabrica), f.Relogio);
        await plan.SugerirRedesAsync(e.ObraId);
        var v = (await plan.VersaoEmElaboracaoAsync(e.ObraId, true))!.Value;
        await plan.GerarPelasRedesAsync(v);
        var lp = Lp(eng);

        // Médio prazo: atividades do horizonte; restrição numa delas tira a atividade do plano.
        var h = await lp.MedioPrazoAsync(e.ObraId, 16);
        Assert.False(h.Vigente);
        Assert.NotEmpty(h.Atividades);
        Assert.All(h.Atividades, a => Assert.True(a.Livre));
        var semana = PlanoSemanal.SegundaDe(h.Atividades.Min(a => a.Inicio));
        var naSemana = (await lp.SemanaAsync(e.ObraId, semana)).Candidatas;
        for (var i = 0; i < 3 && naSemana.Count < 2; i++)
            naSemana = (await lp.SemanaAsync(e.ObraId, semana = semana.AddDays(7))).Candidatas;
        Assert.True(naSemana.Count >= 2);
        var alvo = naSemana[0];
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var rid = await lp.SalvarRestricaoAsync(new Restricao { ObraId = e.ObraId, Tipo = TipoRestricao.Material, Descricao = "Cimento sem pedido aprovado",
            AtividadeChave = alvo.Chave, AtividadeNome = alvo.Nome, ResponsavelId = supr.Id, DataLimite = hoje.AddDays(1) });
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Lp(supr).SalvarRestricaoAsync(new Restricao { ObraId = e.ObraId, Descricao = "x", AtividadeChave = alvo.Chave, ResponsavelId = supr.Id, DataLimite = hoje }));
        Assert.False((await lp.MedioPrazoAsync(e.ObraId, 16)).Atividades.Single(a => a.Chave == alvo.Chave).Livre);

        var planoId = await lp.CriarPlanoAsync(e.ObraId, semana);
        Assert.Equal(planoId, await lp.CriarPlanoAsync(e.ObraId, semana));
        var ex = await Assert.ThrowsAsync<RegraDeNegocioException>(() => lp.AdicionarAtividadesAsync(planoId, [alvo.Chave]));
        Assert.Contains("restrição aberta", ex.Message);
        await lp.RemoverRestricaoAsync(rid, "Pedido aprovado e entrega confirmada");
        Assert.Equal(2, await lp.AdicionarAtividadesAsync(planoId, [alvo.Chave, naSemana[1].Chave]));
        await lp.AdicionarAvulsoAsync(planoId, "Limpeza do pavimento", "pav. 1", "Equipe própria");

        var s = await lp.SemanaAsync(e.ObraId, semana);
        var fora = hoje > semana;                                                   // semana já começou: entra fora da meta
        Assert.All(s.Plano!.Compromissos, c => Assert.Equal(fora, c.ForaDaMeta));
        var cs = s.Plano.Compromissos.ToList();
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => lp.ApontarAsync(cs[0].Id, SituacaoCompromisso.NaoConcluido, null, null));
        await lp.ApontarAsync(cs[0].Id, SituacaoCompromisso.Concluido, null, null);
        await lp.ApontarAsync(cs[1].Id, SituacaoCompromisso.NaoConcluido, CausaNaoCumprimento.Material, "atrasou");
        if (hoje >= semana.AddDays(4))
        {
            await Assert.ThrowsAsync<RegraDeNegocioException>(() => lp.FecharSemanaAsync(planoId, null));   // avulso ainda pendente
            await lp.ApontarAsync(cs[2].Id, SituacaoCompromisso.Concluido, null, null);
            await lp.FecharSemanaAsync(planoId, "semana de chuva");
            var ind = await lp.IndicadoresAsync(e.ObraId, 52);
            var p = ind.Serie.Single(x => x.Semana == semana);
            Assert.True(p.Fechada);
            if (!fora) Assert.Equal(66.7m, p.Ppc);
            Assert.Contains(ind.Causas, c => c.Causa == CausaNaoCumprimento.Material);
            await Assert.ThrowsAsync<RegraDeNegocioException>(() => lp.ApontarAsync(cs[0].Id, SituacaoCompromisso.NaoConcluido, CausaNaoCumprimento.Clima, null));
        }
        else
        {
            await Assert.ThrowsAsync<RegraDeNegocioException>(() => lp.FecharSemanaAsync(planoId, null));   // antes da sexta não fecha
            var ind = await lp.IndicadoresAsync(e.ObraId, 52);
            Assert.Contains(ind.Causas, c => c.Causa == CausaNaoCumprimento.Material);
            Assert.Equal(1, ind.RestricoesRemovidas);
            Assert.Equal(100m, ind.RemovidasNoPrazo);
        }
    }
}
