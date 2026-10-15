using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace Groundkey.Web.Client.Servicos;

/// <summary>Recusa de regra de negócio (409/400 com Problem Details): o texto vem do servidor e vai para a tela como está.</summary>
public sealed class RecusaApi(string mensagem, IReadOnlyList<string> erros) : Exception(mensagem)
{
    public IReadOnlyList<string> Erros { get; } = erros;
}

/// <summary>
/// Acesso à API do Groundkey a partir das telas WebAssembly. Mesmo site, mesmo cookie: não há token a guardar no navegador.
/// Sessão expirada (401) volta para o login; recusa de negócio vira <see cref="RecusaApi"/> com a mensagem do servidor.
/// </summary>
public sealed class Api(HttpClient http, NavigationManager nav)
{
    public async Task<T> ObterAsync<T>(string url, CancellationToken ct = default)
    {
        using var r = await http.GetAsync(url, ct);
        await Verificar(r, ct);
        return (await r.Content.ReadFromJsonAsync<T>(ct))!;
    }

    public async Task<TResp?> EnviarAsync<TReq, TResp>(string url, TReq corpo, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(url, corpo, ct);
        await Verificar(r, ct);
        return r.StatusCode == HttpStatusCode.NoContent ? default : await r.Content.ReadFromJsonAsync<TResp>(ct);
    }

    public async Task EnviarAsync<TReq>(string url, TReq corpo, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(url, corpo, ct);
        await Verificar(r, ct);
    }

    private async Task Verificar(HttpResponseMessage r, CancellationToken ct)
    {
        if (r.IsSuccessStatusCode) return;
        if (r.StatusCode == HttpStatusCode.Unauthorized)
        {
            var volta = "/" + nav.ToBaseRelativePath(nav.Uri);
            nav.NavigateTo($"login?retorno={Uri.EscapeDataString(volta)}", forceLoad: true);
            throw new OperationCanceledException("Sessão expirada.");
        }
        if (r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            var p = await LerProblema(r, ct);
            throw new RecusaApi(p.Detail ?? p.Title ?? "Operação recusada.", p.Erros ?? []);
        }
        throw new HttpRequestException($"Falha no servidor ({(int)r.StatusCode}). Tente de novo em instantes.");
    }

    private static async Task<Problema> LerProblema(HttpResponseMessage r, CancellationToken ct)
    {
        try { return await r.Content.ReadFromJsonAsync<Problema>(ct) ?? new(); }
        catch { return new(); }
    }

    private sealed class Problema
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public List<string>? Erros { get; set; }
    }
}
