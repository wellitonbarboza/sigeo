using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Tests.Integracao;

/// <summary>Requisição: engenheiro responsável vem da SPE; solicitante volta para edição na 1ª aprovação e exclui; solicitante-aprovador aprova na alçada.</summary>
[Collection(ColecaoBanco.Nome)]
public sealed class AprovacaoRequisicaoTests(BancoFixture f)
{
    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Usuário logado fictício (mesma empresa do banco de teste).</summary>
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

    private RequisicoesServico Como(Usuario u)
    {
        var logado = new Logado(f.EmpresaId, u.Id, u.Nome);
        return new RequisicoesServico(f.Fabrica, new AprovacaoServico(logado, f.Relogio, f.Fabrica), logado, f.Relogio);
    }

    private AprovacaoServico AprovacoesComo(Usuario u) => new(new Logado(f.EmpresaId, u.Id, u.Nome), f.Relogio, f.Fabrica);

    private async Task<Usuario> CriarUsuarioAsync(string nome, PerfilUsuario perfil)
    {
        var u = new Usuario { Login = $"{nome.Replace(" ", "").ToLowerInvariant()}.{Guid.NewGuid():N}"[..30], Nome = nome, Email = $"{Guid.NewGuid():N}@exemplo.com", Perfil = perfil, AcessoTodasObras = true };
        await using var db = f.Fabrica.Criar();
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        return u;
    }

    private async Task DefinirEngenheiroSpeAsync(Guid speId, Guid? eng)
    {
        await using var db = f.Fabrica.Criar();
        var s = await db.Spes.SingleAsync(x => x.Id == speId);
        s.EngenheiroId = eng;
        await db.SaveChangesAsync();
    }

    private async Task<Requisicao> NovaAsync(RequisicoesServico srv, Empreendimento e)
    {
        var prod = await Dados.CriarProdutoAsync(f, $"Areia média {Guid.NewGuid():N}"[..20], 10);
        Guid? estab;
        await using (var db = f.Fabrica.CriarLeitura())
            estab = await db.Estabelecimentos.Where(x => x.Ativo).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
        var r = new Requisicao { ObraId = e.ObraId, SpeId = e.SpeId, EstabelecimentoFaturamentoId = estab, Tipo = TipoRequisicao.Material, Atividade = "Alvenaria", DataEntrega = Hoje.AddDays(20), SemOrcamento = true, JustificativaSemOrcamento = "Teste" };
        r.Itens.Add(new RequisicaoItem { RequisicaoId = r.Id, ProdutoId = prod.Id, Descricao = prod.Nome, Unidade = "M3", Quantidade = 5 });
        await srv.SalvarRequisicaoAsync(r);
        return r;
    }

    private async Task<(Requisicao R, SolicitacaoAprovacao? S)> LerAsync(Guid id)
    {
        await using var db = f.Fabrica.CriarLeitura();
        var r = await db.Requisicoes.IgnoreQueryFilters().AsNoTracking().SingleAsync(x => x.Id == id);
        var s = await db.SolicitacoesAprovacao.AsNoTracking().Include(x => x.Etapas).Where(x => x.EntidadeId == id).OrderByDescending(x => x.CriadoEm).FirstOrDefaultAsync();
        return (r, s);
    }

    [FatoComBanco]
    public async Task Engenheiro_vem_da_SPE_e_sem_ele_o_envio_e_barrado()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var sol = await CriarUsuarioAsync("Ana Solicitante", PerfilUsuario.Suprimentos);
        var eng = await CriarUsuarioAsync("Bruno Engenheiro", PerfilUsuario.Engenharia);
        var srv = Como(sol);
        var r = await NovaAsync(srv, e);

        await DefinirEngenheiroSpeAsync(e.SpeId, null);
        var ex = await Assert.ThrowsAsync<RegraDeNegocioException>(() => srv.EnviarRequisicaoAsync(r.Id));
        Assert.Contains("engenheiro responsável da SPE", ex.Message);

        await DefinirEngenheiroSpeAsync(e.SpeId, eng.Id);
        await srv.EnviarRequisicaoAsync(r.Id);
        var (lido, s) = await LerAsync(r.Id);
        Assert.Equal(StatusRequisicao.AguardandoEngenheiro, lido.Status);
        Assert.Equal(eng.Id, s!.Etapas.OrderBy(x => x.Ordem).First().UsuarioAprovadorId);
    }

    [FatoComBanco]
    public async Task Solicitante_volta_para_edicao_na_primeira_aprovacao_e_exclui()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var sol = await CriarUsuarioAsync("Carla Solicitante", PerfilUsuario.Suprimentos);
        var eng = await CriarUsuarioAsync("Davi Engenheiro", PerfilUsuario.Engenharia);
        await DefinirEngenheiroSpeAsync(e.SpeId, eng.Id);
        var srv = Como(sol);
        var r = await NovaAsync(srv, e);
        await srv.EnviarRequisicaoAsync(r.Id);

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => srv.ExcluirRequisicaoAsync(r.Id, false)); // em aprovação: precisa voltar antes
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Como(eng).VoltarParaEdicaoAsync(r.Id));    // só o solicitante

        await srv.VoltarParaEdicaoAsync(r.Id);
        var (lido, s) = await LerAsync(r.Id);
        Assert.Equal(StatusRequisicao.Rascunho, lido.Status);
        Assert.Equal(StatusAprovacao.Cancelado, s!.Status);

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Como(eng).ExcluirRequisicaoAsync(r.Id, false)); // não é o solicitante
        await srv.ExcluirRequisicaoAsync(r.Id, false);
        (lido, _) = await LerAsync(r.Id);
        Assert.True(lido.Excluido);
    }

    [FatoComBanco]
    public async Task Depois_da_primeira_aprovacao_nao_volta_para_edicao()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var sol = await CriarUsuarioAsync("Elisa Solicitante", PerfilUsuario.Suprimentos);
        var eng = await CriarUsuarioAsync("Fabio Engenheiro", PerfilUsuario.Engenharia);
        await DefinirEngenheiroSpeAsync(e.SpeId, eng.Id);
        var srv = Como(sol);
        var r = await NovaAsync(srv, e);
        await srv.EnviarRequisicaoAsync(r.Id);
        var (_, s) = await LerAsync(r.Id);

        await AprovacoesComo(eng).DecidirESalvarAsync(s!.Id, DecisaoAprovacao.Aprovado, null);
        Assert.Equal(StatusRequisicao.AguardandoDiretorTecnico, (await LerAsync(r.Id)).R.Status);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => srv.VoltarParaEdicaoAsync(r.Id));
    }

    [FatoComBanco]
    public async Task Engenheiro_solicitante_aprova_a_propria_requisicao_na_alcada()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var eng = await CriarUsuarioAsync("Gil Engenheiro", PerfilUsuario.Engenharia);
        await DefinirEngenheiroSpeAsync(e.SpeId, eng.Id);
        var srv = Como(eng);
        var r = await NovaAsync(srv, e);
        await srv.EnviarRequisicaoAsync(r.Id);
        var (_, s) = await LerAsync(r.Id);

        await AprovacoesComo(eng).DecidirESalvarAsync(s!.Id, DecisaoAprovacao.Aprovado, null);
        Assert.Equal(StatusRequisicao.AguardandoDiretorTecnico, (await LerAsync(r.Id)).R.Status);
    }

    [FatoComBanco]
    public async Task Substituto_delegado_pelo_engenheiro_solicitante_aprova_a_requisicao()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var eng = await CriarUsuarioAsync("Heitor Engenheiro", PerfilUsuario.Engenharia);
        var sub = await CriarUsuarioAsync("Iara Substituta", PerfilUsuario.Engenharia);
        await DefinirEngenheiroSpeAsync(e.SpeId, eng.Id);
        await using (var db = f.Fabrica.Criar())
        {
            var agora = f.Relogio.GetUtcNow();
            db.DelegacoesAprovacao.Add(DelegacaoAprovacao.Criar(f.EmpresaId, eng.Id, sub.Id, agora.AddHours(-1), agora.AddDays(7), "Férias"));
            await db.SaveChangesAsync();
        }
        var srv = Como(eng);
        var r = await NovaAsync(srv, e);
        await srv.EnviarRequisicaoAsync(r.Id);
        var (_, s) = await LerAsync(r.Id);

        await AprovacoesComo(sub).DecidirESalvarAsync(s!.Id, DecisaoAprovacao.Aprovado, null);
        var (lido, s2) = await LerAsync(r.Id);
        Assert.Equal(StatusRequisicao.AguardandoDiretorTecnico, lido.Status);
        var etapa = s2!.Etapas.OrderBy(x => x.Ordem).First();
        Assert.Equal(sub.Id, etapa.DecididoPorUsuarioId);
        Assert.Equal(eng.Id, etapa.EmNomeDeUsuarioId);
    }
}
