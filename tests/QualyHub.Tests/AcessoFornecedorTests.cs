using QualyHub.Domain.Cadastros;
using Xunit;

namespace QualyHub.Tests;

/// <summary>Código de acesso da Área do Fornecedor e normalização/máscara dos destinos.</summary>
public class AcessoFornecedorTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private static (CodigoAcessoFornecedor Reg, string Codigo) Novo()
    {
        var codigo = CodigoAcessoFornecedor.NovoCodigo();
        var r = new CodigoAcessoFornecedor { CriadoEm = Agora, ExpiraEm = Agora.AddMinutes(CodigoAcessoFornecedor.ValidadeMinutos) };
        r.CodigoHash = CodigoAcessoFornecedor.Hash(r.Id, codigo);
        return (r, codigo);
    }

    [Fact]
    public void Codigo_tem_seis_digitos()
    {
        for (var i = 0; i < 50; i++) Assert.Matches("^[0-9]{6}$", CodigoAcessoFornecedor.NovoCodigo());
    }

    [Fact]
    public void Codigo_certo_entra_uma_vez()
    {
        var (r, c) = Novo();
        Assert.True(r.Conferir(c, Agora.AddMinutes(1)));
        Assert.NotNull(r.UsadoEm);
        Assert.False(r.Conferir(c, Agora.AddMinutes(2)));
    }

    [Fact]
    public void Codigo_vencido_nao_entra()
    {
        var (r, c) = Novo();
        Assert.False(r.Conferir(c, Agora.AddMinutes(CodigoAcessoFornecedor.ValidadeMinutos)));
    }

    [Fact]
    public void Tentativas_esgotadas_bloqueiam_ate_o_codigo_certo()
    {
        var (r, c) = Novo();
        var errado = c == "000000" ? "111111" : "000000";
        for (var i = 0; i < CodigoAcessoFornecedor.MaxTentativas; i++) Assert.False(r.Conferir(errado, Agora));
        Assert.False(r.Conferir(c, Agora));
        Assert.Null(r.UsadoEm);
    }

    [Fact]
    public void Hash_depende_do_registro()
    {
        Assert.NotEqual(CodigoAcessoFornecedor.Hash(Guid.NewGuid(), "123456"), CodigoAcessoFornecedor.Hash(Guid.NewGuid(), "123456"));
    }

    [Theory]
    [InlineData("(41) 99876-5432", "+5541998765432")]
    [InlineData("+55 41 99876 5432", "+5541998765432")]
    [InlineData("041998765432", "+5541998765432")]
    [InlineData("(41) 3333-4444", null)]   // fixo: sem WhatsApp
    [InlineData("", null)]
    public void Celular_normaliza_para_e164(string entrada, string? esperado) => Assert.Equal(esperado, Destinos.Celular(entrada));

    [Theory]
    [InlineData(" Compras@Exemplo.COM ", "compras@exemplo.com")]
    [InlineData("sem-arroba", null)]
    [InlineData("@exemplo.com", null)]
    public void Email_normaliza(string entrada, string? esperado) => Assert.Equal(esperado, Destinos.Email(entrada));

    [Fact]
    public void Mascaras_nao_expoem_o_destino()
    {
        Assert.Equal("co•••@e•••.com", Destinos.MascararEmail("compras@exemplo.com"));
        Assert.Equal("(••) •••••-5432", Destinos.MascararCelular("+5541998765432"));
        Assert.Equal("Maria T.", Destinos.MascararNome("Maria Exemplo Teste"));
    }
}
