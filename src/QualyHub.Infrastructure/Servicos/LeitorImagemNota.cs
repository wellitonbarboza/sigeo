using QualyHub.Application.Fiscal;
using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace QualyHub.Infrastructure.Servicos;

/// <summary>
/// Códigos de barras e QR Codes da foto do DANFE com ZXing (SkiaSharp para abrir JPEG/PNG/HEIF/WebP), no servidor:
/// funciona igual em Android e iPhone, sem biblioteca no navegador. Tenta em tamanhos crescentes (foto reduzida é mais rápida
/// e muitas vezes lê melhor; a original fica para código pequeno ou distante) e gira a imagem quando a foto saiu deitada.
/// </summary>
public sealed class LeitorImagemNota : ILeitorImagemNota
{
    private static readonly int[] Lados = [1600, 2400, 0];

    public IReadOnlyList<string> LerCodigos(byte[] imagem)
    {
        using var original = Abrir(imagem);
        if (original is null) return [];
        var leitor = new ZXing.SkiaSharp.BarcodeReader
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                TryInverted = true,
                PossibleFormats = [BarcodeFormat.CODE_128, BarcodeFormat.QR_CODE, BarcodeFormat.ITF, BarcodeFormat.PDF_417]
            }
        };
        var maior = Math.Max(original.Width, original.Height);
        foreach (var lado in Lados)
        {
            if (lado > 0 && maior <= lado) continue;
            using var reduzida = lado > 0 ? Redimensionar(original, lado) : null;
            var r = leitor.DecodeMultiple(reduzida ?? original);
            if (r is { Length: > 0 }) return r.Select(x => x.Text).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
        }
        return [];
    }

    public byte[] Reduzir(byte[] imagem, int maiorLado = 2000, int qualidade = 85)
    {
        using var original = Abrir(imagem) ?? throw new InvalidDataException("Imagem em formato não reconhecido.");
        using var reduzida = Math.Max(original.Width, original.Height) > maiorLado ? Redimensionar(original, maiorLado) : null;
        using var img = SKImage.FromBitmap(reduzida ?? original);
        using var dados = img.Encode(SKEncodedImageFormat.Jpeg, qualidade);
        return dados.ToArray();
    }

    /// <summary>Abre a imagem já na orientação da câmera (EXIF), para a foto de celular não chegar deitada.</summary>
    private static SKBitmap? Abrir(byte[] imagem)
    {
        using var codec = SKCodec.Create(new SKMemoryStream(imagem));
        if (codec is null) return null;
        var bmp = SKBitmap.Decode(codec);
        if (bmp is null) return null;
        var o = codec.EncodedOrigin;
        if (o is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default) return bmp;
        var girar = o switch { SKEncodedOrigin.RightTop => 90, SKEncodedOrigin.BottomRight => 180, SKEncodedOrigin.LeftBottom => 270, _ => 0 };
        if (girar == 0) return bmp;
        var (w, h) = girar == 180 ? (bmp.Width, bmp.Height) : (bmp.Height, bmp.Width);
        var destino = new SKBitmap(w, h, bmp.ColorType, bmp.AlphaType);
        using (var c = new SKCanvas(destino))
        {
            c.Translate(w / 2f, h / 2f);
            c.RotateDegrees(girar);
            c.Translate(-bmp.Width / 2f, -bmp.Height / 2f);
            c.DrawBitmap(bmp, 0, 0);
        }
        bmp.Dispose();
        return destino;
    }

    private static SKBitmap Redimensionar(SKBitmap b, int maiorLado)
    {
        var f = (float)maiorLado / Math.Max(b.Width, b.Height);
        var info = new SKImageInfo(Math.Max(1, (int)(b.Width * f)), Math.Max(1, (int)(b.Height * f)), b.ColorType, b.AlphaType);
        return b.Resize(info, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            ?? throw new InvalidOperationException("Falha ao reduzir a imagem.");
    }
}
