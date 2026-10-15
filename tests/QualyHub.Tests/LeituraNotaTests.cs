using QualyHub.Domain.Fiscal;
using QualyHub.Infrastructure.Servicos;
using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace QualyHub.Tests;

public class LeituraNotaTests
{
    /// <summary>Chave fictícia (CNPJ de exemplo, PR, out/2026, modelo 55, série 1, nº 4521) com o DV calculado.</summary>
    public static string ChaveFicticia(int numero = 4521)
    {
        var c43 = "41" + "2610" + "11222333000181" + "55" + "001" + numero.ToString("D9") + "1" + "12345678";
        return c43 + ChaveNfe.DigitoVerificador(c43);
    }

    [Fact]
    public void Chave_valida_e_partes()
    {
        var c = ChaveFicticia();
        Assert.Equal(44, c.Length);
        Assert.True(ChaveNfe.Valida(c));
        var p = ChaveNfe.Partes(c)!;
        Assert.Equal("41", p.Uf);
        Assert.Equal(2026, p.Ano);
        Assert.Equal(10, p.Mes);
        Assert.Equal("11222333000181", p.CnpjEmitente);
        Assert.Equal("55", p.Modelo);
        Assert.Equal("1", p.Serie);
        Assert.Equal(4521, p.Numero);
    }

    [Fact]
    public void Chave_com_dv_errado_ou_tamanho_errado_e_recusada()
    {
        var c = ChaveFicticia();
        var errada = c[..43] + (char)('0' + (c[43] - '0' + 1) % 10);
        Assert.False(ChaveNfe.Valida(errada));
        Assert.False(ChaveNfe.Valida(c[..43]));
        Assert.Null(ChaveNfe.Partes(errada));
    }

    [Fact]
    public void Encontra_chave_impressa_em_grupos_no_texto_do_danfe()
    {
        var c = ChaveFicticia();
        var grupos = string.Join(" ", Enumerable.Range(0, 11).Select(i => c.Substring(i * 4, 4)));
        var texto = $"DANFE\nNº 000.004.521 SÉRIE 001\nCHAVE DE ACESSO\n{grupos}\nConsulta de autenticidade no portal nacional";
        Assert.Equal(c, ChaveNfe.Encontrar(texto));
        Assert.Equal(c, ChaveNfe.Encontrar($"https://www.fazenda.pr.gov.br/nfce/qrcode?p={c}|2|1|1|ABC"));
        Assert.Null(ChaveNfe.Encontrar("NF 4521 valor 1.234,56 CNPJ 11.222.333/0001-81"));
    }

    private static byte[] FotoComCodigo(string conteudo, int girar = 0)
    {
        var escritor = new ZXing.SkiaSharp.BarcodeWriter
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions { Width = 900, Height = 120, Margin = 10, PureBarcode = true }
        };
        using var codigo = escritor.Write(conteudo);
        // "Folha" maior, com o código no meio, como numa foto do DANFE.
        using var folha = new SKBitmap(1700, 1200);
        using (var c = new SKCanvas(folha))
        {
            c.Clear(SKColors.White);
            if (girar != 0) { c.Translate(850, 600); c.RotateDegrees(girar); c.Translate(-850, -600); }
            c.DrawBitmap(codigo, 400, 540);
        }
        using var img = SKImage.FromBitmap(folha);
        using var d = img.Encode(SKEncodedImageFormat.Jpeg, 90);
        return d.ToArray();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    public void Le_o_codigo_de_barras_da_foto(int girar)
    {
        var c = ChaveFicticia();
        var lidos = new LeitorImagemNota().LerCodigos(FotoComCodigo(c, girar));
        Assert.Contains(c, lidos.Select(ChaveNfe.Encontrar));
    }

    [Fact]
    public void Foto_sem_codigo_volta_vazia_e_reducao_gera_jpeg_menor()
    {
        using var folha = new SKBitmap(3000, 4000);
        using (var cv = new SKCanvas(folha)) cv.Clear(SKColors.White);
        using var img = SKImage.FromBitmap(folha);
        var jpg = img.Encode(SKEncodedImageFormat.Jpeg, 95).ToArray();
        var leitor = new LeitorImagemNota();
        Assert.Empty(leitor.LerCodigos(jpg));
        var menor = leitor.Reduzir(jpg, 2000);
        using var b = SKBitmap.Decode(menor);
        Assert.Equal(2000, Math.Max(b.Width, b.Height));
        Assert.Empty(leitor.LerCodigos([1, 2, 3]));
    }
}
