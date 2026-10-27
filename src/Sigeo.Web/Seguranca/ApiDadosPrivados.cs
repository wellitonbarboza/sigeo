using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Sigeo.Web.Seguranca;

public sealed record ItemDado(string Nome, long Tamanho, DateTimeOffset Modificado);

/// <summary>Armazenamento do histórico do Planning Lab (obras, revisões, cenários, simulações, versões).</summary>
public interface IArmazemDados
{
    string Tipo { get; }
    Task<List<ItemDado>> ListarAsync(string prefixo, CancellationToken ct);
    Task<Stream?> AbrirAsync(string nome, CancellationToken ct);
    Task<bool> ExisteAsync(string nome, CancellationToken ct);
    Task GravarAsync(string nome, Stream conteudo, string tipo, bool sobrescrever, CancellationToken ct);
    Task<bool> ApagarAsync(string nome, CancellationToken ct);
}

public sealed class ArmazemBlob(BlobContainerClient conteiner, string prefixo) : IArmazemDados
{
    public string Tipo => "blob";

    public async Task<List<ItemDado>> ListarAsync(string p, CancellationToken ct)
    {
        var l = new List<ItemDado>();
        await foreach (var b in conteiner.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefixo + p, ct))
            l.Add(new ItemDado(b.Name[prefixo.Length..], b.Properties.ContentLength ?? 0, b.Properties.LastModified ?? DateTimeOffset.MinValue));
        return l;
    }

    public async Task<Stream?> AbrirAsync(string nome, CancellationToken ct)
    {
        try { return (await conteiner.GetBlobClient(prefixo + nome).DownloadStreamingAsync(cancellationToken: ct)).Value.Content; }
        catch (RequestFailedException e) when (e.Status == 404) { return null; }
    }

    public async Task<bool> ExisteAsync(string nome, CancellationToken ct) => (await conteiner.GetBlobClient(prefixo + nome).ExistsAsync(ct)).Value;

    public async Task GravarAsync(string nome, Stream conteudo, string tipo, bool sobrescrever, CancellationToken ct)
    {
        await conteiner.GetBlobClient(prefixo + nome).UploadAsync(conteudo, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = tipo },
            Conditions = sobrescrever ? null : new BlobRequestConditions { IfNoneMatch = ETag.All },
        }, ct);
    }

    public async Task<bool> ApagarAsync(string nome, CancellationToken ct) => (await conteiner.GetBlobClient(prefixo + nome).DeleteIfExistsAsync(cancellationToken: ct)).Value;
}

public sealed class ArmazemPasta(string pasta) : IArmazemDados
{
    private readonly string _raiz = Path.GetFullPath(pasta);
    public string Tipo => "pasta";

    private string Caminho(string nome)
    {
        var c = Path.GetFullPath(Path.Combine(_raiz, nome));
        if (!c.StartsWith(_raiz + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new UnauthorizedAccessException();
        return c;
    }

    public Task<List<ItemDado>> ListarAsync(string p, CancellationToken ct)
    {
        var l = new List<ItemDado>();
        if (Directory.Exists(_raiz))
            foreach (var f in Directory.EnumerateFiles(_raiz, "*", SearchOption.AllDirectories))
            {
                var nome = Path.GetRelativePath(_raiz, f).Replace('\\', '/');
                if (!nome.StartsWith(p, StringComparison.Ordinal)) continue;
                var i = new FileInfo(f);
                l.Add(new ItemDado(nome, i.Length, i.LastWriteTimeUtc));
            }
        return Task.FromResult(l.OrderBy(x => x.Nome, StringComparer.Ordinal).ToList());
    }

    public Task<Stream?> AbrirAsync(string nome, CancellationToken ct)
    {
        var c = Caminho(nome);
        return Task.FromResult<Stream?>(File.Exists(c) ? File.OpenRead(c) : null);
    }

    public Task<bool> ExisteAsync(string nome, CancellationToken ct) => Task.FromResult(File.Exists(Caminho(nome)));

    public async Task GravarAsync(string nome, Stream conteudo, string tipo, bool sobrescrever, CancellationToken ct)
    {
        var c = Caminho(nome);
        if (!sobrescrever && File.Exists(c)) throw new RequestFailedException(409, "existe");
        Directory.CreateDirectory(Path.GetDirectoryName(c)!);
        var tmp = c + ".tmp";
        await using (var f = File.Create(tmp)) await conteudo.CopyToAsync(f, ct);
        File.Move(tmp, c, overwrite: true);
    }

    public Task<bool> ApagarAsync(string nome, CancellationToken ct)
    {
        var c = Caminho(nome);
        if (!File.Exists(c)) return Task.FromResult(false);
        File.Delete(c);
        return Task.FromResult(true);
    }
}

/// <summary>
/// API do histórico, sob o mesmo caminho escondido e o mesmo bloqueio (logado + e-mail liberado):
/// GET {caminho}/api/estado · GET {caminho}/api/dados?prefixo= · GET|PUT|DELETE {caminho}/api/dados/{nome}.
/// Escrita exige o cabeçalho X-PlanningLab (proteção contra envio de outro site). Pastas "versoes" são imutáveis.
/// </summary>
public static class ApiDados
{
    private const long Limite = 40L * 1024 * 1024;

    public static async Task TratarAsync(HttpContext ctx, PathString resto, IArmazemDados dados, string email)
    {
        var ct = ctx.RequestAborted;
        var r = ctx.Response;
        r.Headers.CacheControl = "no-store";
        r.Headers["X-Robots-Tag"] = "noindex, nofollow";
        var metodo = ctx.Request.Method;
        bool escrita = HttpMethods.IsPut(metodo) || HttpMethods.IsDelete(metodo);
        if (escrita && ctx.Request.Headers["X-PlanningLab"] != "1") { r.StatusCode = StatusCodes.Status403Forbidden; return; }

        if (resto.Equals("/estado") && HttpMethods.IsGet(metodo))
        {
            await r.WriteAsJsonAsync(new { ok = true, email, armazenamento = dados.Tipo }, ct);
            return;
        }
        if (resto.Equals("/dados") && HttpMethods.IsGet(metodo))
        {
            var prefixo = AcessoPrivado.PrefixoDado(ctx.Request.Query["prefixo"]);
            if (prefixo is null) { r.StatusCode = StatusCodes.Status400BadRequest; return; }
            await r.WriteAsJsonAsync(await dados.ListarAsync(prefixo, ct), new JsonSerializerOptions(JsonSerializerDefaults.Web), ct);
            return;
        }
        if (!resto.StartsWithSegments("/dados", out var nomeResto) || AcessoPrivado.NomeDado(nomeResto.Value) is not { } nome)
        {
            r.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (HttpMethods.IsGet(metodo))
        {
            var s = await dados.AbrirAsync(nome, ct);
            if (s is null) { r.StatusCode = StatusCodes.Status404NotFound; return; }
            r.ContentType = Tipo(nome);
            await using (s) await s.CopyToAsync(r.Body, ct);
            return;
        }
        if (HttpMethods.IsPut(metodo))
        {
            if (ctx.Request.ContentLength is > Limite) { r.StatusCode = StatusCodes.Status413PayloadTooLarge; return; }
            var imutavel = AcessoPrivado.Imutavel(nome);
            if (imutavel && await dados.ExisteAsync(nome, ct)) { r.StatusCode = StatusCodes.Status409Conflict; return; }
            using var buffer = new MemoryStream();
            await ctx.Request.Body.CopyToAsync(buffer, ct);
            if (buffer.Length > Limite) { r.StatusCode = StatusCodes.Status413PayloadTooLarge; return; }
            buffer.Position = 0;
            try { await dados.GravarAsync(nome, buffer, Tipo(nome), sobrescrever: !imutavel, ct); }
            catch (RequestFailedException e) when (e.Status is 409 or 412) { r.StatusCode = StatusCodes.Status409Conflict; return; }
            r.StatusCode = StatusCodes.Status204NoContent;
            return;
        }
        if (HttpMethods.IsDelete(metodo))
        {
            if (AcessoPrivado.Imutavel(nome)) { r.StatusCode = StatusCodes.Status403Forbidden; return; }
            r.StatusCode = await dados.ApagarAsync(nome, ct) ? StatusCodes.Status204NoContent : StatusCodes.Status404NotFound;
            return;
        }
        r.StatusCode = StatusCodes.Status405MethodNotAllowed;
    }

    private static string Tipo(string nome) => Path.GetExtension(nome).ToLowerInvariant() switch
    {
        ".json" => "application/json",
        ".xml" => "application/xml",
        ".html" => "text/html; charset=utf-8",
        ".csv" => "text/csv; charset=utf-8",
        _ => "application/octet-stream",
    };
}
