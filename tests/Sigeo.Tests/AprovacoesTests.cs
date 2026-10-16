using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Tests;

/// <summary>Alçadas por login: limite do usuário (c), delegação temporária (a) e segregação de funções (b).</summary>
public class AprovacoesTests
{
    private static readonly DateTimeOffset Agora = new(2026, 9, 25, 10, 0, 0, TimeSpan.FromHours(-3));
    private static readonly Guid Empresa = Guid.CreateVersion7();
    private static readonly Guid Solicitante = Guid.CreateVersion7();

    private static Alcada Etapa(int ordem, PerfilUsuario perfil, decimal? min = null, Guid? usuario = null) =>
        new() { Entidade = EntidadeAprovavel.Pedido, Ordem = ordem, PerfilAprovador = perfil, ValorMinimo = min, UsuarioAprovadorId = usuario };

    private static SolicitacaoAprovacao Pedido(decimal valor, Guid? obra = null, params Alcada[] alcadas) =>
        SolicitacaoAprovacao.Criar(EntidadeAprovavel.Pedido, Guid.CreateVersion7(), Empresa, obra, "OC teste", valor,
            Solicitante, "Comprador", alcadas, Agora);

    private static Aprovador Usuario(PerfilUsuario perfil, string nome, params LimiteAprovacao[] limites)
    {
        var id = Guid.CreateVersion7();
        foreach (var l in limites) l.UsuarioId = id;
        return new Aprovador(id, nome, perfil, limites);
    }

    private static LimiteAprovacao Limite(decimal max, Guid? obra = null, EntidadeAprovavel e = EntidadeAprovavel.Pedido) =>
        new() { Entidade = e, ValorMaximo = max, ObraId = obra };

    // ---- (c) limite no cadastro do usuário ----

    [Fact]
    public void Limite_do_login_barra_aprovacao_final_acima_do_valor()
    {
        var s = Pedido(8000m, null, Etapa(1, PerfilUsuario.Engenharia));
        var eng = Usuario(PerfilUsuario.Engenharia, "Eng. Ana", Limite(5000m));

        var ex = Assert.Throws<RegraDeNegocioException>(() => s.Decidir(eng, DecisaoAprovacao.Aprovado, null, Agora));
        Assert.Contains("acima do limite", ex.Message);
        Assert.Equal(StatusAprovacao.EmAprovacao, s.Status);
    }

    [Fact]
    public void Limite_nao_pesa_em_etapa_intermediaria_e_reprovar_e_sempre_permitido()
    {
        var s = Pedido(8000m, null, Etapa(1, PerfilUsuario.Engenharia), Etapa(2, PerfilUsuario.Diretoria));
        var eng = Usuario(PerfilUsuario.Engenharia, "Eng. Ana", Limite(5000m));
        s.Decidir(eng, DecisaoAprovacao.Aprovado, null, Agora); // "de acordo" intermediário
        Assert.Equal(2, s.EtapaAtual);

        var dir = Usuario(PerfilUsuario.Diretoria, "Dir. Beto", Limite(1000m));
        s.Decidir(dir, DecisaoAprovacao.Reprovado, "Sem orçamento", Agora);
        Assert.Equal(StatusAprovacao.Reprovado, s.Status);
    }

    [Fact]
    public void Limite_especifico_da_obra_prevalece_sobre_o_geral()
    {
        var obra = Guid.CreateVersion7();
        var eng = Usuario(PerfilUsuario.Engenharia, "Eng. Ana", Limite(5000m), Limite(20000m, obra));

        Assert.Equal(20000m, eng.LimitePara(EntidadeAprovavel.Pedido, obra));
        Assert.Equal(5000m, eng.LimitePara(EntidadeAprovavel.Pedido, Guid.CreateVersion7()));
        Assert.Null(eng.LimitePara(EntidadeAprovavel.Medicao, obra));

        var s = Pedido(15000m, obra, Etapa(1, PerfilUsuario.Engenharia));
        s.Decidir(eng, DecisaoAprovacao.Aprovado, null, Agora);
        Assert.Equal(StatusAprovacao.Aprovado, s.Status);
    }

    [Fact]
    public void Sem_limite_cadastrado_valem_so_as_alcadas()
    {
        var s = Pedido(1_000_000m, null, Etapa(1, PerfilUsuario.Diretoria));
        s.Decidir(Usuario(PerfilUsuario.Diretoria, "Dir. Beto"), DecisaoAprovacao.Aprovado, null, Agora);
        Assert.Equal(StatusAprovacao.Aprovado, s.Status);
    }

    // ---- (b) segregação de funções ----

    [Fact]
    public void Solicitante_nao_aprova_a_propria_solicitacao()
    {
        var s = Pedido(1000m, null, Etapa(1, PerfilUsuario.Engenharia));
        var mesmo = new Aprovador(Solicitante, "Comprador", PerfilUsuario.Engenharia);

        var ex = Assert.Throws<RegraDeNegocioException>(() => s.Decidir(mesmo, DecisaoAprovacao.Aprovado, null, Agora));
        Assert.Contains("Segregação", ex.Message);
    }

    [Fact]
    public void Mesma_pessoa_nao_decide_duas_etapas()
    {
        var s = Pedido(1000m, null, Etapa(1, PerfilUsuario.Engenharia), Etapa(2, PerfilUsuario.Engenharia));
        var eng = Usuario(PerfilUsuario.Engenharia, "Eng. Ana");
        s.Decidir(eng, DecisaoAprovacao.Aprovado, null, Agora);

        Assert.Throws<RegraDeNegocioException>(() => s.Decidir(eng, DecisaoAprovacao.Aprovado, null, Agora));
        s.Decidir(Usuario(PerfilUsuario.Engenharia, "Eng. Caio"), DecisaoAprovacao.Aprovado, null, Agora);
        Assert.Equal(StatusAprovacao.Aprovado, s.Status);
    }

    // ---- requisição: solicitante que também é aprovador ----

    private static SolicitacaoAprovacao Requisicao(params Alcada[] alcadas) =>
        SolicitacaoAprovacao.Criar(EntidadeAprovavel.Requisicao, Guid.CreateVersion7(), Empresa, null, "Requisição teste", 0m,
            Solicitante, "Eng. Solicitante", alcadas, Agora);

    private static Alcada EtapaReq(int ordem, PerfilUsuario perfil, Guid? usuario = null) =>
        new() { Entidade = EntidadeAprovavel.Requisicao, Ordem = ordem, PerfilAprovador = perfil, UsuarioAprovadorId = usuario };

    [Fact]
    public void Solicitante_na_alcada_aprova_a_propria_requisicao_mas_nao_duas_etapas()
    {
        var s = Requisicao(EtapaReq(1, PerfilUsuario.Engenharia, Solicitante), EtapaReq(2, PerfilUsuario.Engenharia));
        var mesmo = new Aprovador(Solicitante, "Eng. Solicitante", PerfilUsuario.Engenharia);

        s.Decidir(mesmo, DecisaoAprovacao.Aprovado, null, Agora);
        Assert.Equal(2, s.EtapaAtual);
        Assert.Throws<RegraDeNegocioException>(() => s.Decidir(mesmo, DecisaoAprovacao.Aprovado, null, Agora));
    }

    [Fact]
    public void Solicitante_fora_da_alcada_nao_aprova_a_requisicao()
    {
        var outro = Guid.CreateVersion7();
        var s = Requisicao(EtapaReq(1, PerfilUsuario.Engenharia, outro));
        var mesmo = new Aprovador(Solicitante, "Eng. Solicitante", PerfilUsuario.Engenharia);

        Assert.Throws<RegraDeNegocioException>(() => s.Decidir(mesmo, DecisaoAprovacao.Aprovado, null, Agora));
        Assert.Equal(StatusAprovacao.EmAprovacao, s.Status);
    }

    [Fact]
    public void Solicitante_da_requisicao_nao_aprova_como_substituto()
    {
        var dir = Usuario(PerfilUsuario.Diretoria, "Dir. Beto");
        var s = Requisicao(EtapaReq(1, PerfilUsuario.Diretoria, dir.UsuarioId));
        var d = DelegacaoAprovacao.Criar(Empresa, dir.UsuarioId, Solicitante, Agora.AddDays(-1), Agora.AddDays(1), "Férias");
        var mesmo = new Aprovador(Solicitante, "Eng. Solicitante", PerfilUsuario.Engenharia);

        var ex = Assert.Throws<RegraDeNegocioException>(() => s.Decidir(mesmo, DecisaoAprovacao.Aprovado, null, Agora, [new DelegacaoRecebida(d, dir)]));
        Assert.Contains("Segregação", ex.Message);
    }

    [Fact]
    public void Substituto_do_solicitante_aprova_a_requisicao_em_nome_dele()
    {
        var titular = new Aprovador(Solicitante, "Eng. Solicitante", PerfilUsuario.Engenharia);
        var sub = Usuario(PerfilUsuario.Engenharia, "Eng. Substituto");
        var s = Requisicao(EtapaReq(1, PerfilUsuario.Engenharia, Solicitante));
        var d = DelegacaoAprovacao.Criar(Empresa, Solicitante, sub.UsuarioId, Agora.AddDays(-1), Agora.AddDays(5), "Férias");

        s.Decidir(sub, DecisaoAprovacao.Aprovado, null, Agora, [new DelegacaoRecebida(d, titular)]);

        var etapa = s.Etapas.Single();
        Assert.Equal(StatusAprovacao.Aprovado, s.Status);
        Assert.Equal(sub.UsuarioId, etapa.DecididoPorUsuarioId);
        Assert.Equal(Solicitante, etapa.EmNomeDeUsuarioId);
    }

    [Fact]
    public void Substituto_do_solicitante_nao_aprova_pedido_dele()
    {
        var titular = new Aprovador(Solicitante, "Comprador", PerfilUsuario.Diretoria);
        var sub = Usuario(PerfilUsuario.Diretoria, "Dir. Substituto");
        var s = Pedido(1000m, null, Etapa(1, PerfilUsuario.Diretoria, usuario: Solicitante));
        var d = DelegacaoAprovacao.Criar(Empresa, Solicitante, sub.UsuarioId, Agora.AddDays(-1), Agora.AddDays(5), "Férias");

        var ex = Assert.Throws<RegraDeNegocioException>(() => s.Decidir(sub, DecisaoAprovacao.Aprovado, null, Agora, [new DelegacaoRecebida(d, titular)]));
        Assert.Contains("Segregação", ex.Message);
    }

    [Fact]
    public void Solicitante_retira_da_aprovacao_so_antes_de_qualquer_decisao()
    {
        var s = Requisicao(EtapaReq(1, PerfilUsuario.Engenharia), EtapaReq(2, PerfilUsuario.Diretoria));
        Assert.Throws<RegraDeNegocioException>(() => s.RetirarPeloSolicitante(Guid.CreateVersion7(), Agora)); // outra pessoa

        var t = Requisicao(EtapaReq(1, PerfilUsuario.Engenharia), EtapaReq(2, PerfilUsuario.Diretoria));
        t.Decidir(Usuario(PerfilUsuario.Engenharia, "Eng. Ana"), DecisaoAprovacao.Aprovado, null, Agora);
        Assert.Throws<RegraDeNegocioException>(() => t.RetirarPeloSolicitante(Solicitante, Agora)); // já passou da 1ª

        s.RetirarPeloSolicitante(Solicitante, Agora);
        Assert.Equal(StatusAprovacao.Cancelado, s.Status);
    }

    // ---- cadastro alterado com a aprovação em andamento ----

    [Fact]
    public void Troca_de_etapas_pendentes_mantem_as_decididas()
    {
        var s = Requisicao(EtapaReq(1, PerfilUsuario.Engenharia), EtapaReq(2, PerfilUsuario.Diretoria));
        s.Decidir(Usuario(PerfilUsuario.Engenharia, "Eng. Ana"), DecisaoAprovacao.Aprovado, null, Agora);
        var dir = Guid.CreateVersion7();

        var (removidas, novas) = s.TrocarEtapasPendentes([(PerfilUsuario.Engenharia, null), (PerfilUsuario.Diretoria, dir)]);

        Assert.Single(removidas);
        var nova = Assert.Single(novas);
        Assert.Equal(2, nova.Ordem);
        Assert.Equal(dir, nova.UsuarioAprovadorId);
        Assert.Equal(DecisaoAprovacao.Aprovado, s.Etapas.Single(e => e.Ordem == 1).Decisao);
        Assert.Equal(2, s.EtapaAtual);
    }

    [Fact]
    public void Troca_de_etapas_sem_mudanca_ou_sem_etapa_restante_nao_mexe()
    {
        var s = Requisicao(EtapaReq(1, PerfilUsuario.Engenharia), EtapaReq(2, PerfilUsuario.Diretoria));
        Assert.Empty(s.TrocarEtapasPendentes([(PerfilUsuario.Engenharia, null), (PerfilUsuario.Diretoria, null)]).Novas);
        s.Decidir(Usuario(PerfilUsuario.Engenharia, "Eng. Ana"), DecisaoAprovacao.Aprovado, null, Agora);
        Assert.Empty(s.TrocarEtapasPendentes([(PerfilUsuario.Engenharia, null)]).Novas); // nunca aprova sozinho por falta de etapa
        Assert.Equal(StatusAprovacao.EmAprovacao, s.Status);
    }

    // ---- (a) delegação temporária ----

    [Fact]
    public void Substituto_aprova_em_nome_do_titular_durante_a_delegacao()
    {
        var dir = Usuario(PerfilUsuario.Diretoria, "Dir. Beto", Limite(50000m));
        var sub = Usuario(PerfilUsuario.Engenharia, "Eng. Ana");
        var s = Pedido(30000m, null, Etapa(1, PerfilUsuario.Diretoria, usuario: dir.UsuarioId));
        var d = DelegacaoAprovacao.Criar(Empresa, dir.UsuarioId, sub.UsuarioId, Agora.AddDays(-1), Agora.AddDays(10), "Férias");

        s.Decidir(sub, DecisaoAprovacao.Aprovado, null, Agora, [new DelegacaoRecebida(d, dir)]);

        var etapa = s.Etapas.Single();
        Assert.Equal(StatusAprovacao.Aprovado, s.Status);
        Assert.Equal(sub.UsuarioId, etapa.DecididoPorUsuarioId);
        Assert.Equal(dir.UsuarioId, etapa.EmNomeDeUsuarioId);
        Assert.Equal(d.Id, etapa.DelegacaoAprovacaoId);
    }

    [Fact]
    public void Delegacao_vencida_revogada_ou_de_outro_tipo_nao_autoriza()
    {
        var dir = Usuario(PerfilUsuario.Diretoria, "Dir. Beto");
        var sub = Usuario(PerfilUsuario.Engenharia, "Eng. Ana");
        var vencida = DelegacaoAprovacao.Criar(Empresa, dir.UsuarioId, sub.UsuarioId, Agora.AddDays(-10), Agora.AddDays(-1), "Férias");
        var revogada = DelegacaoAprovacao.Criar(Empresa, dir.UsuarioId, sub.UsuarioId, Agora.AddDays(-1), Agora.AddDays(5), "Férias");
        revogada.Revogar(Agora.AddHours(-1));
        var soMedicao = DelegacaoAprovacao.Criar(Empresa, dir.UsuarioId, sub.UsuarioId, Agora.AddDays(-1), Agora.AddDays(5), "Viagem", EntidadeAprovavel.Medicao);

        foreach (var d in new[] { vencida, revogada, soMedicao })
        {
            var s = Pedido(1000m, null, Etapa(1, PerfilUsuario.Diretoria));
            Assert.Throws<RegraDeNegocioException>(() => s.Decidir(sub, DecisaoAprovacao.Aprovado, null, Agora, [new DelegacaoRecebida(d, dir)]));
        }
    }

    [Fact]
    public void Substituto_herda_o_limite_do_titular()
    {
        var dir = Usuario(PerfilUsuario.Diretoria, "Dir. Beto", Limite(10000m));
        var sub = Usuario(PerfilUsuario.Diretoria, "Dir. Carla", Limite(1_000_000m));
        var s = Pedido(30000m, null, Etapa(1, PerfilUsuario.Diretoria, usuario: dir.UsuarioId));
        var d = DelegacaoAprovacao.Criar(Empresa, dir.UsuarioId, sub.UsuarioId, Agora.AddDays(-1), Agora.AddDays(1), "Férias");

        Assert.Throws<RegraDeNegocioException>(() => s.Decidir(sub, DecisaoAprovacao.Aprovado, null, Agora, [new DelegacaoRecebida(d, dir)]));
    }

    [Fact]
    public void Solicitante_nao_aprova_nem_como_substituto_do_titular()
    {
        var dir = Usuario(PerfilUsuario.Diretoria, "Dir. Beto");
        var solicitante = new Aprovador(Solicitante, "Comprador", PerfilUsuario.Suprimentos);
        var s = Pedido(1000m, null, Etapa(1, PerfilUsuario.Diretoria));
        var d = DelegacaoAprovacao.Criar(Empresa, dir.UsuarioId, Solicitante, Agora.AddDays(-1), Agora.AddDays(1), "Férias");

        var ex = Assert.Throws<RegraDeNegocioException>(() => s.Decidir(solicitante, DecisaoAprovacao.Aprovado, null, Agora, [new DelegacaoRecebida(d, dir)]));
        Assert.Contains("Segregação", ex.Message);
    }

    [Fact]
    public void Delegacao_exige_pessoas_diferentes_periodo_valido_e_motivo()
    {
        var u = Guid.CreateVersion7();
        Assert.Throws<RegraDeNegocioException>(() => DelegacaoAprovacao.Criar(Empresa, u, u, Agora, Agora.AddDays(1), "Férias"));
        Assert.Throws<RegraDeNegocioException>(() => DelegacaoAprovacao.Criar(Empresa, u, Guid.CreateVersion7(), Agora, Agora, "Férias"));
        Assert.Throws<RegraDeNegocioException>(() => DelegacaoAprovacao.Criar(Empresa, u, Guid.CreateVersion7(), Agora, Agora.AddDays(1), " "));
    }
}
