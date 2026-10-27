using System.Security.Claims;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.StaticFiles;

namespace Sigeo.Web.Seguranca;

/// <summary>
/// Planning Lab (ferramenta independente do SIGEO) servido num caminho escondido do mesmo endereço.
/// <list type="bullet">
/// <item>Não é módulo: sem menu, sem banco, sem perfil de acesso. Só existe se <c>PlanningLab:Caminho</c> estiver configurado.</item>
/// <item>Só abre para quem está logado no SIGEO com e-mail listado em <c>PlanningLab:Emails</c>. Os demais recebem a
///       mesma página "não encontrada" de qualquer endereço inexistente.</item>
/// <item>Os arquivos ficam fora do pacote do SIGEO: contêiner privado <c>ferramentas</c> do Storage (prefixo
///       <c>planning-lab/</c>), lido com a Managed Identity. Atualizar o Planning Lab não republica o SIGEO.</item>
/// <item>Desenvolvimento (sem <c>Sigeo:BlobUri</c>): lê da pasta <c>PlanningLab:PastaLocal</c> (padrão App_Data/planning-lab).</item>
/// </list>
/// </summary>
public static class FerramentaPrivada
{
    private static readonly FileExtensionContentTypeProvider Tipos = CriarTipos();

    public static void UsarPlanningLab(WebApplication app)
    {
        var cfg = app.Configuration.GetSection("PlanningLab");
        var caminho = AcessoPrivado.NormalizarCaminho(cfg["Caminho"]);
        if (caminho is null) return; // desligado: o endereço simplesmente não existe

        var liberados = AcessoPrivado.Emails(cfg["Emails"]);
        IFonteArquivos fonte = app.Configuration["Sigeo:BlobUri"] is { Length: > 0 } blobUri
            ? new FonteBlob(new BlobServiceClient(new Uri(blobUri), new DefaultAzureCredential())
                .GetBlobContainerClient(cfg["Conteiner"] is { Length: > 0 } c ? c : "ferramentas"),
                cfg["Prefixo"] is { Length: > 0 } p ? p.Trim('/') + "/" : "planning-lab/")
            : new FontePasta(cfg["PastaLocal"] is { Length: > 0 } pasta ? pasta
                : Path.Combine(app.Environment.ContentRootPath, "App_Data", "planning-lab"));
        // Histórico (obras, revisões, cenários, simulações, versões): mesmo contêiner privado, prefixo próprio
        IArmazemDados dados = app.Configuration["Sigeo:BlobUri"] is { Length: > 0 } blobUri2
            ? new ArmazemBlob(new BlobServiceClient(new Uri(blobUri2), new DefaultAzureCredential())
                .GetBlobContainerClient(cfg["Conteiner"] is { Length: > 0 } c2 ? c2 : "ferramentas"),
                cfg["PrefixoDados"] is { Length: > 0 } pd ? pd.Trim('/') + "/" : "planning-lab-dados/")
            : new ArmazemPasta(cfg["PastaDados"] is { Length: > 0 } pastaD ? pastaD
                : Path.Combine(app.Environment.ContentRootPath, "App_Data", "planning-lab-dados"));

        app.Use(async (ctx, next) =>
        {
            if (!ctx.Request.Path.StartsWithSegments(caminho, StringComparison.OrdinalIgnoreCase, out var resto))
            {
                await next();
                return;
            }
            var ehApi = resto.StartsWithSegments("/api", out var restoApi);
            if (ctx.User.Identity?.IsAuthenticated != true)
            {
                if (ehApi) { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return; }
                if (!HttpMethods.IsGet(ctx.Request.Method)) { ctx.Response.StatusCode = StatusCodes.Status404NotFound; return; }
                ctx.Response.Redirect("/login?retorno=" + Uri.EscapeDataString(caminho + "/"));
                return;
            }
            if (!AcessoPrivado.Liberado(ctx.User.FindFirstValue(ClaimTypes.Email), liberados))
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound; // não revela que existe
                return;
            }
            if (ehApi)
            {
                await ApiDados.TratarAsync(ctx, restoApi, dados, ctx.User.FindFirstValue(ClaimTypes.Email)!);
                return;
            }
            if (!HttpMethods.IsGet(ctx.Request.Method) && !HttpMethods.IsHead(ctx.Request.Method))
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            if (!resto.HasValue)
            {
                // base href="./" do Planning Lab exige a barra final
                ctx.Response.Redirect(caminho + "/" + ctx.Request.QueryString);
                return;
            }
            var arquivo = AcessoPrivado.Arquivo(resto.Value);
            if (arquivo is null)
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            ctx.Response.Headers.CacheControl = AcessoPrivado.SempreRevalidar(arquivo) ? "private, no-cache" : "private, max-age=3600";
            ctx.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
            ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
            if (arquivo == "index.html") ctx.Response.Headers["X-Frame-Options"] = "DENY";
            if (arquivo == "service-worker.js") ctx.Response.Headers["Service-Worker-Allowed"] = caminho + "/";
            ctx.Response.ContentType = Tipos.TryGetContentType(arquivo, out var tipo) ? tipo : "application/octet-stream";
            await fonte.EnviarAsync(ctx, arquivo);
        });
    }

    private static FileExtensionContentTypeProvider CriarTipos()
    {
        var t = new FileExtensionContentTypeProvider();
        t.Mappings[".wasm"] = "application/wasm";
        t.Mappings[".dll"] = "application/octet-stream";
        t.Mappings[".pdb"] = "application/octet-stream";
        t.Mappings[".dat"] = "application/octet-stream";
        t.Mappings[".blat"] = "application/octet-stream";
        t.Mappings[".webcil"] = "application/octet-stream";
        t.Mappings[".webmanifest"] = "application/manifest+json";
        t.Mappings[".json"] = "application/json";
        t.Mappings[".js"] = "text/javascript";
        t.Mappings[".mjs"] = "text/javascript";
        return t;
    }

    private interface IFonteArquivos
    {
        Task EnviarAsync(HttpContext ctx, string arquivo);
    }

    private sealed class FonteBlob(BlobContainerClient conteiner, string prefixo) : IFonteArquivos
    {
        public async Task EnviarAsync(HttpContext ctx, string arquivo)
        {
            var blob = conteiner.GetBlobClient(prefixo + arquivo);
            var condicoes = ctx.Request.Headers.IfNoneMatch.ToString() is { Length: > 0 } etag
                ? new BlobRequestConditions { IfNoneMatch = new ETag(etag.Trim('"')) }
                : null;
            try
            {
                var r = await blob.DownloadStreamingAsync(new BlobDownloadOptions { Conditions = condicoes }, ctx.RequestAborted);
                ctx.Response.Headers.ETag = "\"" + r.Value.Details.ETag.ToString().Trim('"') + "\"";
                ctx.Response.ContentLength = r.Value.Details.ContentLength;
                if (HttpMethods.IsHead(ctx.Request.Method)) return;
                await using var s = r.Value.Content;
                await s.CopyToAsync(ctx.Response.Body, ctx.RequestAborted);
            }
            catch (RequestFailedException e) when (e.Status == StatusCodes.Status304NotModified)
            {
                ctx.Response.StatusCode = StatusCodes.Status304NotModified;
            }
            catch (RequestFailedException e) when (e.Status == StatusCodes.Status404NotFound)
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            }
        }
    }

    private sealed class FontePasta(string pasta) : IFonteArquivos
    {
        private readonly string _raiz = Path.GetFullPath(pasta);

        public async Task EnviarAsync(HttpContext ctx, string arquivo)
        {
            var completo = Path.GetFullPath(Path.Combine(_raiz, arquivo));
            if (!completo.StartsWith(_raiz + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !File.Exists(completo))
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            var info = new FileInfo(completo);
            var etag = "\"" + info.LastWriteTimeUtc.Ticks.ToString("x") + "-" + info.Length.ToString("x") + "\"";
            if (ctx.Request.Headers.IfNoneMatch.ToString() == etag)
            {
                ctx.Response.StatusCode = StatusCodes.Status304NotModified;
                return;
            }
            ctx.Response.Headers.ETag = etag;
            ctx.Response.ContentLength = info.Length;
            if (!HttpMethods.IsHead(ctx.Request.Method)) await ctx.Response.SendFileAsync(completo, ctx.RequestAborted);
        }
    }
}
