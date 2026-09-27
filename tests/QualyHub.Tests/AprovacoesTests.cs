using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;

namespace QualyHub.Tests;

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
