using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Estrutura;
using Xunit;

namespace QualyHub.Tests;

/// <summary>Regras puras dos cadastros (produto, controlados) e da obra — sem banco.</summary>
public class CadastrosServicoTests
{
    private static Produto Valido() => new() { Nome = "Tubo PVC 25 mm", Unidade = "M", Grupo = "Hidráulica", SubGrupo = "Tubos" };

    [Fact]
    public void Produto_valido_passa() => Assert.Empty(CadastrosServico.PrepararProduto(Valido()));

    [Fact]
    public void Produto_sem_nome_e_grupo_sem_subgrupo_falham()
    {
        var p = Valido(); p.Nome = " "; p.SubGrupo = null;
        var e = CadastrosServico.PrepararProduto(p);
        Assert.Contains("Informe a descrição genérica.", e);
        Assert.Contains(e, x => x.Contains("subgrupo"));
    }

    [Fact]
    public void Produto_a_classificar_dispensa_subgrupo()
    {
        var p = Valido(); p.Grupo = "A Cadastrar"; p.SubGrupo = null;
        Assert.Empty(CadastrosServico.PrepararProduto(p));
    }

    [Theory]
    [InlineData("3917.23.00", "39172300", true)]
    [InlineData("1234", null, false)]
    [InlineData("0000.00.00", null, true)]
    [InlineData("  ", null, true)]
    public void Ncm_normaliza_e_valida(string ncm, string? esperado, bool ok)
    {
        var p = Valido(); p.Ncm = ncm;
        var e = CadastrosServico.PrepararProduto(p);
        Assert.Equal(ok, e.Count == 0);
        if (ok) Assert.Equal(esperado, p.Ncm);
    }

    [Theory]
    [InlineData("7891234567895", true)]
    [InlineData("789-1234-5678-95", true)]
    [InlineData("12345", false)]
    public void Ean_aceita_8_12_13_14_digitos(string ean, bool ok)
    {
        var p = Valido(); p.Ean = ean;
        Assert.Equal(ok, CadastrosServico.PrepararProduto(p).Count == 0);
    }

    [Fact]
    public void Numeros_negativos_ou_embalagem_zero_falham()
    {
        var p = Valido(); p.LeadTimeDias = -1; p.PrecoReferencia = -1; p.QuantidadePorEmbalagem = 0;
        Assert.Equal(3, CadastrosServico.PrepararProduto(p).Count);
    }

    [Fact]
    public void Controlado_exige_codigo_e_nome()
    {
        Assert.Equal(2, CadastrosServico.ValidarBasico("", null).Count);
        Assert.Empty(CadastrosServico.ValidarBasico("ITS 03.02", "Alvenaria"));
    }

    [Fact]
    public void Obra_exige_dados_minimos_e_normaliza_uf()
    {
        var o = new Obra { Codigo = "OB1", Nome = "Obra Exemplo", EnderecoEntrega = "Rua Fictícia, 100", EngenheiroId = Guid.NewGuid(), Uf = " pr " };
        Assert.Empty(EngenhariaServico.PrepararObra(o));
        Assert.Equal("PR", o.Uf);
        Assert.Equal(4, EngenhariaServico.PrepararObra(new Obra()).Count);
    }
}
