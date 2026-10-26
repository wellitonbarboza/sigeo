using Sigeo.Application.Comum;

namespace Sigeo.Tests;

public class AcessoPrivadoTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("/x", null)]                 // curto demais
    [InlineData("/x/../admin", null)]
    [InlineData("x/pl-7f3a9c2e/", "/x/pl-7f3a9c2e")]
    [InlineData("/x/pl-7f3a9c2e", "/x/pl-7f3a9c2e")]
    public void Normaliza_caminho(string? entrada, string? esperado) =>
        Assert.Equal(esperado, AcessoPrivado.NormalizarCaminho(entrada));

    [Fact]
    public void So_libera_email_da_lista_sem_diferenciar_maiusculas()
    {
        var lista = AcessoPrivado.Emails("welliton@x.com.br; outro@y.com");
        Assert.True(AcessoPrivado.Liberado("Welliton@X.com.br", lista));
        Assert.False(AcessoPrivado.Liberado("intruso@x.com.br", lista));
        Assert.False(AcessoPrivado.Liberado(null, lista));
    }

    [Fact]
    public void Lista_vazia_nao_libera_ninguem() =>
        Assert.False(AcessoPrivado.Liberado("welliton@x.com.br", AcessoPrivado.Emails("")));

    [Theory]
    [InlineData("", "index.html")]
    [InlineData("/", "index.html")]
    [InlineData("/_framework/dotnet.native.wasm", "_framework/dotnet.native.wasm")]
    [InlineData("/css/app.css", "css/app.css")]
    [InlineData("/../appsettings.json", null)]
    [InlineData("/_framework/../../segredo", null)]
    [InlineData("/.env", null)]
    [InlineData("/a//b", null)]
    [InlineData("/c:/windows", null)]
    [InlineData("/a\\b", null)]
    [InlineData("/x%2e%2e", null)]
    public void Arquivo_seguro(string resto, string? esperado) =>
        Assert.Equal(esperado, AcessoPrivado.Arquivo(resto));
}
