using Sigeo.Domain.Estrutura;
using Sigeo.Infrastructure.Servicos;
using Xunit;

namespace Sigeo.Tests;

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
    [InlineData("12345", false)]
    [InlineData("123456", true)]
    [InlineData("abcdef", true)]
    [InlineData("joao.obra", true)]
    public void Politica_so_exige_tamanho_minimo(string senha, bool valida) =>
        Assert.Equal(valida, PoliticaSenha.Validar(senha, "joao.obra", "Joao Obra").Count == 0);

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
