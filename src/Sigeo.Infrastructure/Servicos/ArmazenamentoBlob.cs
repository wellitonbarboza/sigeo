using System.Security.Cryptography;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Sigeo.Infrastructure.Servicos;

/// <summary>
/// Armazenamento em produção: Azure Blob Storage com Managed Identity (sem chave na aplicação).
/// A pasta lógica escolhe o contêiner criado pelo Bicep:
/// sgq → "sgq" | fotos → "evidencias" | sst-restrito (ASO) → "sensivel" | importacoes → "importacoes" | demais → "documentos".
/// O identificador salvo no banco é o mesmo formato do modo local (pasta/aaaa/mm/guid.ext).
/// </summary>
public sealed class ArmazenamentoBlob : IArmazenamento
{
    private readonly BlobServiceClient _cliente;

    public ArmazenamentoBlob(Uri blobUri) : this(new BlobServiceClient(blobUri, new DefaultAzureCredential())) { }
    public ArmazenamentoBlob(BlobServiceClient cliente) => _cliente = cliente;

    public static string Conteiner(string blob)
    {
        var pasta = blob.Split('/', 2)[0].ToLowerInvariant();
        return pasta switch
        {
            "sgq" => "sgq",
            "fotos" => "evidencias",
            "sst-restrito" => "sensivel",
            "importacoes" => "importacoes",
            _ => "documentos",
        };
    }

    public async Task<ArquivoSalvo> SalvarAsync(Stream conteudo, string nomeOriginal, string pasta, string? contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(nomeOriginal);
        var blob = $"{pasta.Trim('/')}/{DateTime.UtcNow:yyyy/MM}/{Guid.CreateVersion7():N}{ext}";
        var cliente = _cliente.GetBlobContainerClient(Conteiner(blob)).GetBlobClient(blob);

        // Copia para memória calculando o hash (arquivos de obra são pequenos: fotos, PDFs, certidões).
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using var buffer = new MemoryStream();
        var bloco = new byte[81920];
        int lidos;
        while ((lidos = await conteudo.ReadAsync(bloco, ct)) > 0)
        {
            sha.AppendData(bloco, 0, lidos);
            await buffer.WriteAsync(bloco.AsMemory(0, lidos), ct);
        }
        var hash = Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
        buffer.Position = 0;
        await cliente.UploadAsync(buffer, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType ?? "application/octet-stream" },
            Metadata = new Dictionary<string, string> { ["sha256"] = hash },
            Conditions = new BlobRequestConditions { IfNoneMatch = Azure.ETag.All }, // nunca sobrescreve
        }, ct);
        return new ArquivoSalvo(blob, hash, buffer.Length, nomeOriginal, contentType);
    }

    public async Task<Stream> AbrirAsync(string blob, CancellationToken ct = default) =>
        await _cliente.GetBlobContainerClient(Conteiner(blob)).GetBlobClient(blob).OpenReadAsync(cancellationToken: ct);
}
