using Groundkey.Domain.Cadastros;
using Xunit;

namespace Groundkey.Tests;

public class CodigoProdutoTests
{
    [Theory]
    [InlineData("Hidráulica", "HID")]
    [InlineData("Aço", "ACO")]
    [InlineData("EPI", "EPI")]
    [InlineData("Material Elétrico", "ELE")]
    [InlineData("Material de Escritório", "ESC")]
    [InlineData("Cabos e Fios", "CAB")]
    public void Sugere_sigla_legivel(string nome, string esperado) => Assert.Equal(esperado, CodigoProduto.SugerirSigla(nome, []));

    [Fact]
    public void Evita_colisao_no_mesmo_nivel()
    {
        var fer = CodigoProduto.SugerirSigla("Ferramentas", []);
        var outra = CodigoProduto.SugerirSigla("Ferragens", [fer]);
        Assert.Equal("FER", fer);
        Assert.NotEqual(fer, outra);
        Assert.True(CodigoProduto.EhSiglaValida(outra));
    }

    [Fact]
    public void Monta_e_le_codigo()
    {
        Assert.Equal("HID-CNX-0042", CodigoProduto.Montar("HID", "CNX", 42));
        Assert.Equal(42, CodigoProduto.Sequencia("HID-CNX-0042"));
        Assert.True(CodigoProduto.EhPadrao("HID-CNX-0042"));
        Assert.False(CodigoProduto.EhPadrao("P000001"));
        Assert.True(CodigoProduto.EhProvisorio(CodigoProduto.Provisorio(7)));
        Assert.Equal("TMP-000007", CodigoProduto.Provisorio(7));
    }
}
