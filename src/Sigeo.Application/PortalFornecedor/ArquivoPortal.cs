using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Comum;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.PortalFornecedor;

/// <summary>Conferência do arquivo enviado pelo portal: extensão permitida e conteúdo compatível (assinatura do arquivo), até 20 MB.</summary>
public static class ArquivoPortal
{
    public const long TamanhoMaximo = 20 * 1024 * 1024;

    /// <summary>Confere e devolve um stream posicionável (copia para memória quando o original não permite voltar ao início).</summary>
    public static async Task<Stream> ConferirAsync(Stream arquivo, string nomeArquivo, CancellationToken ct)
    {
        var ext = Path.GetExtension(nomeArquivo).ToLowerInvariant();
        if (ext is not (".pdf" or ".jpg" or ".jpeg" or ".png")) throw new RegraDeNegocioException("Envie PDF, JPG ou PNG.");
        if (!arquivo.CanSeek)
        {
            var ms = new MemoryStream();
            await arquivo.CopyToAsync(ms, ct);
            ms.Position = 0;
            arquivo = ms;
        }
        if (arquivo.Length > TamanhoMaximo) throw new RegraDeNegocioException("Arquivo maior que 20 MB.");
        var cab = new byte[8];
        var lidos = await arquivo.ReadAtLeastAsync(cab, cab.Length, throwOnEndOfStream: false, ct);
        arquivo.Position = 0;
        var ok = ext switch
        {
            ".pdf" => lidos >= 4 && cab[0] == 0x25 && cab[1] == 0x50 && cab[2] == 0x44 && cab[3] == 0x46,             // %PDF
            ".png" => lidos >= 8 && cab[0] == 0x89 && cab[1] == 0x50 && cab[2] == 0x4E && cab[3] == 0x47,             // .PNG
            _ => lidos >= 3 && cab[0] == 0xFF && cab[1] == 0xD8 && cab[2] == 0xFF                                     // JPEG
        };
        if (!ok) throw new RegraDeNegocioException("O conteúdo do arquivo não corresponde à extensão (envie um PDF, JPG ou PNG válido).");
        return arquivo;
    }
}
