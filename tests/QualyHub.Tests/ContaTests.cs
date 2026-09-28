using QualyHub.Domain.Estrutura;
using QualyHub.Infrastructure.Servicos;
using Xunit;

namespace QualyHub.Tests;

public class ContaTests
{
    [Fact]
    public void Hash_confere_e_nao_guarda_a_senha()
    {
        var h = Senhas.Gerar("Obra2026segura");
        Assert.StartsWith("pbkdf2-sha512$", h);
        Assert.DoesNotContain("Obra2026segura", h);
        Assert.True(Senhas.Verificar("Obra2026segura", h));
        Assert.False(Senhas.Verificar("obra2026segura", h));
        Assert.NotEqual(h, Senhas.Gerar("Obra2026segura")); // sal aleatório
    }

    [Fact]
    public void Verificar_sem_hash_ou_hash_invalido_falha()
    {
        Assert.False(Senhas.Verificar("qualquer1234", null));
        Assert.False(Senhas.Verificar("qualquer1234", "texto-qualquer"));
    }

    [Fact]
    public void Senha_provisoria_atende_a_politica()
    {
        for (var i = 0; i < 50; i++)
        {
            var s = Senhas.GerarProvisoria();
            Assert.Equal(12, s.Length);
            Assert.Empty(PoliticaSenha.Validar(s.PadRight(12), "maria.silva", "Maria Silva"));
        }
    }

    [Theory]
    [InlineData("curta1", "pelo menos")]
    [InlineData("somenteletrasaqui", "letras e números")]
    [InlineData("1111111111a", "repetitiva")]
    [InlineData("joao.obra2026", "login")]
    [InlineData("Welliton2026x", "nome")]
    public void Politica_recusa(string senha, string trecho)
    {
        var e = PoliticaSenha.Validar(senha, "joao.obra", "Welliton Teste");
        Assert.Contains(e, x => x.Contains(trecho, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(" Maria.Silva ", "maria.silva", true)]
    [InlineData("ab", "ab", false)]
    [InlineData("joão", "joão", false)]
    [InlineData("eng@obra", "eng@obra", true)]
    public void Login_normalizado_e_validado(string entrada, string esperado, bool valido)
    {
        Assert.Equal(esperado, PoliticaSenha.NormalizarLogin(entrada));
        Assert.Equal(valido, PoliticaSenha.ValidarLogin(entrada).Count == 0);
    }
}
