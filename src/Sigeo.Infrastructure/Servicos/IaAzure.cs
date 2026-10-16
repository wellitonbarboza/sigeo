using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Sigeo.Application.IA;

namespace Sigeo.Infrastructure.Servicos;

/// <summary>
/// Autenticação nos serviços de IA do Azure: identidade gerenciada do App Service (sem chave guardada);
/// em desenvolvimento aceita a chave em IA:Chave / IA:DocumentosChave.
/// </summary>
internal static class CredencialIa
{
    private static readonly TokenCredential Credencial = new DefaultAzureCredential();
    private static readonly string[] Escopo = ["https://cognitiveservices.azure.com/.default"];

    public static async Task AutenticarAsync(HttpRequestMessage req, string? chave, string cabecalhoChave, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(chave)) { req.Headers.Add(cabecalhoChave, chave); return; }
        var t = await Credencial.GetTokenAsync(new TokenRequestContext(Escopo), ct);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", t.Token);
    }
}

/// <summary>
/// Azure OpenAI (chat completions) pela API REST — modelo implantado na assinatura da construtora (região Brazil South ou a mais próxima
/// com o modelo), dados não usados para treino. Configuração: IA:Endpoint, IA:Deployment, IA:ApiVersion.
/// </summary>
public sealed class AssistenteAzureOpenAi(IHttpClientFactory http, IConfiguration cfg) : IAssistenteIa
{
    private string? Endpoint => cfg["IA:Endpoint"]?.TrimEnd('/');
    private string? Deployment => cfg["IA:Deployment"];
    /// <summary>Modelos de raciocínio (gpt-5*, o*): sem temperature e com max_completion_tokens (o raciocínio consome tokens de saída).</summary>
    private bool Raciocinio => cfg.GetValue<bool?>("IA:Raciocinio")
        ?? (Deployment ?? "").StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase) || System.Text.RegularExpressions.Regex.IsMatch(Deployment ?? "", "^o[0-9]");
    private string ApiVersion => cfg["IA:ApiVersion"] ?? (Raciocinio ? "2025-04-01-preview" : "2024-10-21");

    public bool Configurado => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(Deployment);

    public async Task<RespostaIa> PerguntarAsync(string instrucoes, string conteudo, bool json, int maxTokensSaida, CancellationToken ct = default)
    {
        if (!Configurado) throw new InvalidOperationException("Azure OpenAI não configurado.");
        var corpo = new Dictionary<string, object>
        {
            ["messages"] = new object[] { new { role = "system", content = instrucoes }, new { role = "user", content = conteudo } }
        };
        if (Raciocinio) { corpo["max_completion_tokens"] = Math.Max(4000, maxTokensSaida * 3); corpo["reasoning_effort"] = "low"; }
        else { corpo["max_tokens"] = maxTokensSaida; corpo["temperature"] = 0.1; }
        if (json) corpo["response_format"] = new { type = "json_object" };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}/openai/deployments/{Deployment}/chat/completions?api-version={ApiVersion}") { Content = CorpoJson.De(corpo) };
        await CredencialIa.AutenticarAsync(req, cfg["IA:Chave"], "api-key", ct);
        using var resp = await http.CreateClient("ia").SendAsync(req, ct);
        var txt = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException($"Azure OpenAI respondeu {(int)resp.StatusCode}: {(txt.Length > 300 ? txt[..300] : txt)}");
        using var d = JsonDocument.Parse(txt);
        var texto = d.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        var uso = d.RootElement.TryGetProperty("usage", out var u) ? u : default;
        return new RespostaIa(texto,
            uso.ValueKind == JsonValueKind.Object ? uso.GetProperty("prompt_tokens").GetInt32() : 0,
            uso.ValueKind == JsonValueKind.Object ? uso.GetProperty("completion_tokens").GetInt32() : 0);
    }
}

/// <summary>
/// Azure AI Document Intelligence, modelo "prebuilt-layout" (texto + tabelas em markdown) para laudos e certificados.
/// Assíncrono no serviço: envia, acompanha o Operation-Location até concluir (máx. ~2 min). Configuração: IA:DocumentosEndpoint.
/// </summary>
public sealed class LeitorDocumentIntelligence(IHttpClientFactory http, IConfiguration cfg) : ILeitorDocumentos
{
    private string? Endpoint => cfg["IA:DocumentosEndpoint"]?.TrimEnd('/');
    private string ApiVersion => cfg["IA:DocumentosApiVersion"] ?? "2024-11-30";

    public bool Configurado => !string.IsNullOrWhiteSpace(Endpoint);

    public async Task<(string Texto, int Paginas)> ExtrairTextoAsync(Stream arquivo, string contentType, CancellationToken ct = default)
    {
        if (!Configurado) throw new InvalidOperationException("Document Intelligence não configurado.");
        var cli = http.CreateClient("ia");
        using var conteudo = new StreamContent(arquivo);
        conteudo.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/pdf" : contentType);
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}/documentintelligence/documentModels/prebuilt-layout:analyze?api-version={ApiVersion}&outputContentFormat=markdown") { Content = conteudo };
        await CredencialIa.AutenticarAsync(req, cfg["IA:DocumentosChave"], "Ocp-Apim-Subscription-Key", ct);
        using var resp = await cli.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException($"Document Intelligence respondeu {(int)resp.StatusCode}.");
        var operacao = resp.Headers.TryGetValues("Operation-Location", out var v) ? v.First() : throw new HttpRequestException("Sem Operation-Location.");
        for (var i = 0; i < 60; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(i < 5 ? 1 : 2), ct);
            using var g = new HttpRequestMessage(HttpMethod.Get, operacao);
            await CredencialIa.AutenticarAsync(g, cfg["IA:DocumentosChave"], "Ocp-Apim-Subscription-Key", ct);
            using var r = await cli.SendAsync(g, ct);
            using var d = JsonDocument.Parse(await r.Content.ReadAsStringAsync(ct));
            var status = d.RootElement.GetProperty("status").GetString();
            if (status == "succeeded")
            {
                var res = d.RootElement.GetProperty("analyzeResult");
                var paginas = res.TryGetProperty("pages", out var p) ? p.GetArrayLength() : 1;
                return (res.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "", paginas);
            }
            if (status == "failed") throw new HttpRequestException("Document Intelligence não conseguiu ler o documento.");
        }
        throw new TimeoutException("Leitura do documento demorou demais.");
    }
}
