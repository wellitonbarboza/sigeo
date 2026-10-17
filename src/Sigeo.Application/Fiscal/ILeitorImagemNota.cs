using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Abstracoes;
using Sigeo.Application.Comum;
using Sigeo.Application.IA;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Fiscal;

namespace Sigeo.Application.Fiscal;

/// <summary>Leitura de imagem sem custo de IA: códigos de barras/QR e redução para envio (implementação com ZXing + SkiaSharp).</summary>
public interface ILeitorImagemNota
{
    /// <summary>Textos dos códigos de barras (Code 128 do DANFE) e QR Codes encontrados na foto; vazio se nenhum.</summary>
    IReadOnlyList<string> LerCodigos(byte[] imagem);
    /// <summary>JPEG com o maior lado limitado (o Document Intelligence gratuito aceita até 4 MB por arquivo).</summary>
    byte[] Reduzir(byte[] imagem, int maiorLado = 2000, int qualidade = 85);
}
