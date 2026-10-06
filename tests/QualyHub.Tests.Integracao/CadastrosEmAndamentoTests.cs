using QualyHub.Application.Comum;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Tests.Integracao;

/// <summary>
/// Cadastro alterado vale para os processos em andamento (engenheiro da SPE, alçadas, produto, qualificação do fornecedor);
/// cotação aceita incluir/retirar fornecedor; quebra por local situada na árvore da obra.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public sealed class CadastrosEmAndamentoTests(BancoFixture f)
{
    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Today);

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

    private AprovacaoServico Aprov(Usuario u) => new(new Logado(f.EmpresaId, u.Id, u.Nome), f.Relogio, f.Fabrica);
    private RequisicoesServico Req(Usuario u) => new(f.Fabrica, Aprov(u), new Logado(f.EmpresaId, u.Id, u.Nome), f.Relogio);
    private CotacoesServico Cot(Usuario u) => new(f.Fabrica, Aprov(u), f.Relogio);

    private async Task<Usuario> UsuarioAsync(string nome, PerfilUsuario perfil)
    {
        var u = new Usuario { Login = $"u{Guid.NewGuid():N}"[..20], Nome = nome, Email = $"{Guid.NewGuid():N}@exemplo.com", Perfil = perfil, AcessoTodasObras = true };
        await using var db = f.Fabrica.Criar();
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        return u;
    }

    private async Task EngenheiroSpeAsync(Guid speId, Guid? eng)
    {
        Spe s;
        await using (var db = f.Fabrica.CriarLeitura()) s = await db.Spes.AsNoTracking().SingleAsync(x => x.Id == speId);
        s.EngenheiroId = eng;
        await f.Engenharia.SalvarSpeAsync(s); // pelo serviço real: dispara a atualização das aprovações em andamento
    }

    private async Task<(Requisicao R, Produto P)> RequisicaoEnviadaAsync(Empreendimento e, Usuario sol, int locais = 0)
    {
        var prod = await Dados.CriarProdutoAsync(f, $"Cimento teste {Guid.NewGuid():N}"[..22], 30);
        Guid? estab;
        List<Local> salas;
        await using (var db = f.Fabrica.CriarLeitura())
        {
            estab = await db.Estabelecimentos.Where(x => x.Ativo).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
            salas = (await Dados.LocaisAsync(f, e.ObraId)).Where(l => l.AmbienteModeloId == e.SalaAp2d).OrderBy(l => l.Codigo).Take(locais).ToList();
        }
        var r = new Requisicao { ObraId = e.ObraId, SpeId = e.SpeId, EstabelecimentoFaturamentoId = estab, Tipo = TipoRequisicao.Material, Atividade = "Alvenaria", DataEntrega = Hoje.AddDays(20), SemOrcamento = true, JustificativaSemOrcamento = "Teste" };
        var i = new RequisicaoItem { RequisicaoId = r.Id, ProdutoId = prod.Id, Descricao = prod.Nome, Unidade = "KG", Quantidade = 10 * Math.Max(1, locais) };
        foreach (var s in salas) i.Locais.Add(new RequisicaoItemLocal { RequisicaoItemId = i.Id, LocalId = s.Id, Quantidade = 10 });
        r.Itens.Add(i);
        var srv = Req(sol);
        await srv.SalvarRequisicaoAsync(r);
        await srv.EnviarRequisicaoAsync(r.Id);
        return (r, prod);
    }

    private async Task<SolicitacaoAprovacao> SolicitacaoAsync(Guid entidadeId)
    {
        await using var db = f.Fabrica.CriarLeitura();
        return await db.SolicitacoesAprovacao.AsNoTracking().Include(x => x.Etapas).Where(x => x.EntidadeId == entidadeId && x.Status == StatusAprovacao.EmAprovacao).SingleAsync();
    }

    [FatoComBanco]
    public async Task Troca_do_engenheiro_da_SPE_passa_para_a_etapa_ainda_nao_decidida()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var sol = await UsuarioAsync("Ana Solicitante", PerfilUsuario.Suprimentos);
        var eng1 = await UsuarioAsync("Bruno Engenheiro", PerfilUsuario.Engenharia);
        var eng2 = await UsuarioAsync("Caio Engenheiro", PerfilUsuario.Engenharia);
        await EngenheiroSpeAsync(e.SpeId, eng1.Id);
        var (r, _) = await RequisicaoEnviadaAsync(e, sol);
        Assert.Equal(eng1.Id, (await SolicitacaoAsync(r.Id)).Etapas.Single(x => x.Ordem == 1).UsuarioAprovadorId);

        await EngenheiroSpeAsync(e.SpeId, eng2.Id);

        var s = await SolicitacaoAsync(r.Id);
        Assert.Equal(eng2.Id, s.Etapas.Single(x => x.Ordem == 1).UsuarioAprovadorId);
        Assert.Equal(2, s.Etapas.Count);
        await Aprov(eng2).DecidirESalvarAsync(s.Id, DecisaoAprovacao.Aprovado, null); // o novo engenheiro aprova
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Aprov(eng1).DecidirESalvarAsync(s.Id, DecisaoAprovacao.Aprovado, null));
    }

    [FatoComBanco]
    public async Task Mudanca_de_alcada_refaz_so_as_etapas_pendentes()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var sol = await UsuarioAsync("Dora Solicitante", PerfilUsuario.Suprimentos);
        var eng = await UsuarioAsync("Edu Engenheiro", PerfilUsuario.Engenharia);
        var dir = await UsuarioAsync("Fabi Diretora", PerfilUsuario.Diretoria);
        await EngenheiroSpeAsync(e.SpeId, eng.Id);
        var (r, _) = await RequisicaoEnviadaAsync(e, sol);
        var s0 = await SolicitacaoAsync(r.Id);
        await Aprov(eng).DecidirESalvarAsync(s0.Id, DecisaoAprovacao.Aprovado, null);

        Guid alcadaId;
        await using (var db = f.Fabrica.Criar())
        {
            var a = await db.Alcadas.SingleAsync(x => x.Entidade == EntidadeAprovavel.Requisicao && x.Ordem == 2 && !x.Excluido);
            alcadaId = a.Id; a.UsuarioAprovadorId = dir.Id; // diretor nominal na 2ª etapa
            await db.SaveChangesAsync();
            Assert.True(await AprovacaoServico.RecalcularPendentesAsync(db, x => x.Entidade == EntidadeAprovavel.Requisicao) >= 1);
            await db.SaveChangesAsync();
        }
        try
        {
            var s = await SolicitacaoAsync(r.Id);
            var primeira = s.Etapas.Single(x => x.Ordem == 1);
            Assert.Equal(DecisaoAprovacao.Aprovado, primeira.Decisao); // decidida fica
            Assert.Equal(dir.Id, s.Etapas.Single(x => x.Ordem == 2).UsuarioAprovadorId);
        }
        finally
        {
            await using var db = f.Fabrica.Criar();
            (await db.Alcadas.SingleAsync(x => x.Id == alcadaId)).UsuarioAprovadorId = null;
            await db.SaveChangesAsync();
        }
    }

    [FatoComBanco]
    public async Task Cotacao_inclui_e_retira_fornecedor_e_produto_alterado_chega_nos_itens_abertos()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var sol = await UsuarioAsync("Gabi Solicitante", PerfilUsuario.Suprimentos);
        var eng = await UsuarioAsync("Hugo Engenheiro", PerfilUsuario.Engenharia);
        var dir = await UsuarioAsync("Iris Diretora", PerfilUsuario.Diretoria);
        await EngenheiroSpeAsync(e.SpeId, eng.Id);
        var (r, prod) = await RequisicaoEnviadaAsync(e, sol, locais: 3);
        var s = await SolicitacaoAsync(r.Id);
        await Aprov(eng).DecidirESalvarAsync(s.Id, DecisaoAprovacao.Aprovado, null);
        await Aprov(dir).DecidirESalvarAsync(s.Id, DecisaoAprovacao.Aprovado, null);

        var fa = await Dados.CriarFornecedorAsync(f);
        var fb = await Dados.CriarFornecedorAsync(f);
        var cot = Cot(sol);
        var cid = await cot.GerarCotacaoAsync(r.Id, [fa]);

        Assert.Equal(1, await cot.AdicionarFornecedoresAsync(cid, [fb, fa])); // fa já estava
        await cot.RemoverFornecedorAsync(cid, fa);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => cot.RemoverFornecedorAsync(cid, fb)); // não fica sem fornecedor

        // Produto renomeado no cadastro → requisição e cotação abertas mostram o nome novo.
        Produto p;
        await using (var db = f.Fabrica.CriarLeitura()) p = await db.Produtos.AsNoTracking().SingleAsync(x => x.Id == prod.Id);
        p.Nome = "Cimento CP II 50 kg (renomeado)"; p.SubGrupo = "Colantes";
        await new CadastrosServico(f.Fabrica).SalvarProdutoAsync(p, default);

        await using (var db = f.Fabrica.CriarLeitura())
        {
            var c = await db.Cotacoes.AsNoTracking().Include(x => x.Fornecedores).Include(x => x.Itens).SingleAsync(x => x.Id == cid);
            Assert.Equal(fb, Assert.Single(c.Fornecedores).FornecedorId);
            Assert.All(c.Itens, i => Assert.Equal(fb, i.FornecedorId));
            Assert.All(c.Itens, i => Assert.Equal(p.Nome, i.Descricao));
            Assert.Equal(p.Nome, (await db.RequisicaoItens.AsNoTracking().SingleAsync(i => i.RequisicaoId == r.Id)).Descricao);
            Assert.Equal(2, await db.Notas.CountAsync(n => n.Entidade == nameof(Cotacao) && n.EntidadeId == cid));

            // Quebra por local situada na árvore: torre, pavimento, apartamento e cômodo.
            var rid = c.Itens.First().RequisicaoItemId!.Value;
            var q = (await QuadroLocais.CarregarAsync(db, [rid]))[rid];
            Assert.Equal(3, q.Count);
            Assert.All(q, l => { Assert.NotNull(l.Torre); Assert.NotNull(l.Pavimento); Assert.NotNull(l.Apartamento); Assert.Equal("Sala de estar", l.Comodo); });
            Assert.EndsWith("total 30 KG", QuadroLocais.Resumo(q, "KG"));
        }
    }

    [FatoComBanco]
    public async Task Qualificacao_nova_do_fornecedor_vale_na_cotacao_aberta()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var sol = await UsuarioAsync("Joana Solicitante", PerfilUsuario.Suprimentos);
        var eng = await UsuarioAsync("Kleber Engenheiro", PerfilUsuario.Engenharia);
        var dir = await UsuarioAsync("Lia Diretora", PerfilUsuario.Diretoria);
        await EngenheiroSpeAsync(e.SpeId, eng.Id);
        var (r, _) = await RequisicaoEnviadaAsync(e, sol);
        var s = await SolicitacaoAsync(r.Id);
        await Aprov(eng).DecidirESalvarAsync(s.Id, DecisaoAprovacao.Aprovado, null);
        await Aprov(dir).DecidirESalvarAsync(s.Id, DecisaoAprovacao.Aprovado, null);
        var fa = await Dados.CriarFornecedorAsync(f);
        var cid = await Cot(sol).GerarCotacaoAsync(r.Id, [fa]);

        await using (var db = f.Fabrica.Criar())
        {
            db.Qualificacoes.Add(new FornecedorQualificacao
            {
                FornecedorId = fa, TipoFornecimento = TipoFornecimento.Material, CriterioQualificacao = "Teste",
                DataQualificacao = Hoje, DataValidade = Hoje.AddYears(1), Resultado = ResultadoQualificacao.Qualificado
            });
            await db.SaveChangesAsync();
        }
        await new AvaliacaoFornecedoresServico(f.Fabrica, f.Usuario, f.Relogio).RecalcularFornecedoresAsync([fa]);

        await using var db2 = f.Fabrica.CriarLeitura();
        Assert.Equal(StatusQualificacao.Qualificado, (await db2.CotacaoFornecedores.AsNoTracking().SingleAsync(x => x.CotacaoId == cid)).QualificacaoNaData);
    }
}
