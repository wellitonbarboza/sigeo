using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Documentos;
using QualyHub.Domain.Estrutura;
using Xunit;

namespace QualyHub.Tests;

public class SistemaServicoTests
{
    [Fact]
    public void Parametro_nao_aceita_negativo_e_peso_da_avaliacao_vai_ate_1()
    {
        Assert.NotEmpty(SistemaServico.ValidarParametro("PRAZO.LEITURA", -1));
        Assert.Empty(SistemaServico.ValidarParametro("PRAZO.LEITURA", 30));
        Assert.NotEmpty(SistemaServico.ValidarParametro("AVAL.PESO.PRAZO", 1.5m));
        Assert.Empty(SistemaServico.ValidarParametro("AVAL.PESO.PRAZO", 0.25m));
    }

    [Fact]
    public void Permissao_exige_alvo_e_perfil_ou_usuario_mas_nao_ambos()
    {
        Assert.Equal(2, SistemaServico.ValidarPermissao(new PermissaoDocumento()).Count);
        var ambos = new PermissaoDocumento { AlvoId = Guid.NewGuid(), Perfil = PerfilUsuario.Campo, UsuarioId = Guid.NewGuid() };
        Assert.Contains("Escolha perfil OU usuário.", SistemaServico.ValidarPermissao(ambos));
        Assert.Empty(SistemaServico.ValidarPermissao(new PermissaoDocumento { AlvoId = Guid.NewGuid(), Perfil = PerfilUsuario.Campo }));
    }

    [Fact]
    public void Usuario_valido_passa_e_administrador_nao_tira_o_proprio_perfil()
    {
        var u = new Usuario { Nome = "Pessoa Teste", Email = "pessoa@exemplo.com", Login = "pessoa.teste", Perfil = PerfilUsuario.Administrador };
        Assert.Empty(SistemaServico.ValidarUsuario(u, u.Id));
        u.Perfil = PerfilUsuario.Campo;
        Assert.Contains("Você não pode remover o próprio perfil de Administrador.", SistemaServico.ValidarUsuario(u, u.Id));
        Assert.Empty(SistemaServico.ValidarUsuario(u, Guid.NewGuid()));
    }

    [Fact]
    public void Usuario_exige_nome_email_login_e_limite_nao_negativo()
    {
        var u = new Usuario { Nome = " ", Email = "sem-arroba", Login = "x" };
        u.LimitesAprovacao.Add(new LimiteAprovacao { ValorMaximo = -1 });
        var e = SistemaServico.ValidarUsuario(u, null);
        Assert.Contains("Informe o nome.", e);
        Assert.Contains("Informe um e-mail válido.", e);
        Assert.Contains("Limite não pode ser negativo.", e);
        Assert.Contains(e, m => m.StartsWith("Login"));
    }

    [Fact]
    public void Alcada_exige_ordem_e_faixa_de_valor_coerente()
    {
        Assert.NotEmpty(SistemaServico.ValidarAlcada(new Alcada { Ordem = 0 }));
        Assert.NotEmpty(SistemaServico.ValidarAlcada(new Alcada { Ordem = 1, ValorMinimo = 1000, ValorMaximo = 500 }));
        Assert.Empty(SistemaServico.ValidarAlcada(new Alcada { Ordem = 1, ValorMinimo = 0, ValorMaximo = 500 }));
    }

    [Fact]
    public void Delegacao_exige_pessoas_diferentes_periodo_e_motivo()
    {
        var p = Guid.NewGuid();
        var inicio = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var mesma = new DelegacaoAprovacao { TitularUsuarioId = p, SubstitutoUsuarioId = p, Inicio = inicio, Fim = inicio.AddDays(1), Motivo = "Férias" };
        Assert.Contains("Titular e substituto devem ser diferentes.", SistemaServico.ValidarDelegacao(mesma));
        var invertida = new DelegacaoAprovacao { TitularUsuarioId = p, SubstitutoUsuarioId = Guid.NewGuid(), Inicio = inicio, Fim = inicio, Motivo = "" };
        Assert.Equal(2, SistemaServico.ValidarDelegacao(invertida).Count);
        var ok = new DelegacaoAprovacao { TitularUsuarioId = p, SubstitutoUsuarioId = Guid.NewGuid(), Inicio = inicio, Fim = inicio.AddDays(15), Motivo = "Férias" };
        Assert.Empty(SistemaServico.ValidarDelegacao(ok));
    }

    [Fact]
    public void Anexo_exige_registro_salvo_e_respeita_limite_de_tamanho()
    {
        Assert.NotEmpty(AnexosServico.ValidarDestino("Tarefa", Guid.Empty));
        Assert.NotEmpty(AnexosServico.ValidarDestino(" ", Guid.NewGuid()));
        Assert.Empty(AnexosServico.ValidarDestino("Tarefa", Guid.NewGuid()));
        Assert.Equal("foto.jpg: limite de 15 MB.", AnexosServico.ValidarTamanho("foto.jpg", AnexosServico.LimiteFotoBytes + 1, AnexosServico.LimiteFotoBytes));
        Assert.Null(AnexosServico.ValidarTamanho("foto.jpg", AnexosServico.LimiteFotoBytes, AnexosServico.LimiteFotoBytes));
    }
}
