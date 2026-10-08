using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QualyHub.Application.Fiscal;

namespace QualyHub.Infrastructure.Servicos;

/// <summary>
/// Conector da API do SIEG (cofre de XML): baixa as NF-e emitidas contra os CNPJs da construtora.
/// API nova (a antiga parou em 31/07/2026): token JWT gerado com Client ID + Secret Key (vale 24 h) enviado em
/// "Authorization: Bearer" junto com a API Key em "X-Api-Key"; POST baixar-xmls devolve um ZIP com os XMLs;
/// até 2 meses por chamada e cerca de 2 chamadas por minuto por chave.
///
/// Credenciais por construtora (cada uma tem o seu contrato SIEG), no Key Vault:
///   Sieg--Empresas--{empresaId}--ApiKey / ClientId / SecretKey
/// Endereços e nomes de campo ficam em configuração (Sieg:UrlBase, Sieg:CaminhoToken, Sieg:CaminhoBaixar, Sieg:TipoXmlNfe)
/// para ajustar ao portal de integrações do SIEG sem publicar o sistema de novo.
/// </summary>
public sealed class FonteSieg(IHttpClientFactory http, IConfiguration cfg, ILogger<FonteSieg> log) : IFonteNotas
{
    private static readonly ConcurrentDictionary<Guid, (string Token, DateTimeOffset Vence)> Tokens = new();
    private static readonly SemaphoreSlim Fila = new(1, 1);
    private static DateTimeOffset _ultimaChamada = DateTimeOffset.MinValue;

    private string UrlBase => (cfg["Sieg:UrlBase"] ?? "https://api.sieg.com").TrimEnd('/');
    private string CaminhoToken => cfg["Sieg:CaminhoToken"] ?? "/api/v1/auth/token";
    private string CaminhoBaixar => cfg["Sieg:CaminhoBaixar"] ?? "/api/v1/baixar-xmls";
    private int TipoXmlNfe => int.TryParse(cfg["Sieg:TipoXmlNfe"], out var t) ? t : 1;
    private int PorPagina => int.TryParse(cfg["Sieg:PorPagina"], out var t) ? Math.Clamp(t, 1, 500) : 50;
    /// <summary>Intervalo entre chamadas (limite de ~2 por minuto por chave).</summary>
    private TimeSpan Intervalo => TimeSpan.FromSeconds(int.TryParse(cfg["Sieg:IntervaloSegundos"], out var s) ? s : 31);

    private string? Credencial(Guid empresa, string nome) => cfg[$"Sieg:Empresas:{empresa}:{nome}"] is { Length: > 0 } v ? v : null;

    public bool Configurada(Guid empresaId) =>
        Credencial(empresaId, "ApiKey") is not null && Credencial(empresaId, "ClientId") is not null && Credencial(empresaId, "SecretKey") is not null;

    public async Task<IReadOnlyList<(string Nome, string Xml)>> BaixarAsync(Guid empresaId, string cnpjDestinatario, DateOnly de, DateOnly ate, CancellationToken ct = default)
    {
        if (!Configurada(empresaId)) throw new InvalidOperationException("Integração SIEG sem credenciais para esta empresa.");
        var r = new List<(string, string)>();
        for (var skip = 0; ; skip += PorPagina)
        {
            var corpo = new Dictionary<string, object?>
            {
                ["TipoXml"] = TipoXmlNfe, ["Take"] = PorPagina, ["Skip"] = skip,
                ["DataEmissaoInicio"] = de.ToString("yyyy-MM-dd"), ["DataEmissaoFim"] = ate.ToString("yyyy-MM-dd"),
                ["CnpjDest"] = cnpjDestinatario, ["Downloadevent"] = true
            };
            var pagina = await ChamarAsync(empresaId, corpo, ct);
            r.AddRange(pagina);
            if (pagina.Count < PorPagina || skip > 20_000) break;
        }
        log.LogInformation("SIEG: {Qtd} XML(s) para {Cnpj} de {De} a {Ate}.", r.Count, cnpjDestinatario, de, ate);
        return r;
    }

    private async Task<List<(string, string)>> ChamarAsync(Guid empresa, object corpo, CancellationToken ct)
    {
        await Fila.WaitAsync(ct);
        try
        {
            var espera = _ultimaChamada + Intervalo - DateTimeOffset.UtcNow;
            if (espera > TimeSpan.Zero) await Task.Delay(espera, ct);
            for (var tentativa = 0; ; tentativa++)
            {
                var token = await TokenAsync(empresa, renovar: tentativa > 0, ct);
                using var req = new HttpRequestMessage(HttpMethod.Post, UrlBase + CaminhoBaixar) { Content = CorpoJson.De(corpo) };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                req.Headers.Add("X-Api-Key", Credencial(empresa, "ApiKey"));
                using var resp = await http.CreateClient("sieg").SendAsync(req, ct);
                _ultimaChamada = DateTimeOffset.UtcNow;
                if (resp.StatusCode == HttpStatusCode.Unauthorized && tentativa == 0) continue; // token vencido: gera outro uma vez
                if (resp.StatusCode == HttpStatusCode.NotFound || resp.StatusCode == HttpStatusCode.NoContent) return [];
                if (!resp.IsSuccessStatusCode)
                {
                    var txt = await resp.Content.ReadAsStringAsync(ct);
                    // "nenhum xml encontrado" vem como erro em algumas versões da API
                    if (txt.Contains("nenhum", StringComparison.OrdinalIgnoreCase) && txt.Contains("encontrad", StringComparison.OrdinalIgnoreCase)) return [];
                    throw new HttpRequestException($"SIEG respondeu {(int)resp.StatusCode}: {Resumo(txt)}");
                }
                var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
                return LerResposta(bytes);
            }
        }
        finally { Fila.Release(); }
    }

    /// <summary>Resposta em ZIP (API nova) ou JSON com XMLs em Base64 (formato antigo) — aceita os dois.</summary>
    public static List<(string, string)> LerResposta(byte[] bytes)
    {
        var r = new List<(string, string)>();
        if (bytes.Length >= 2 && bytes[0] == 'P' && bytes[1] == 'K')
        {
            using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
            foreach (var e in zip.Entries.Where(e => e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) && e.Length <= LeitorNfe.TamanhoMaximoXml).Take(LeitorNfe.MaximoArquivosZip))
            {
                using var s = new StreamReader(e.Open(), Encoding.UTF8);
                r.Add((e.FullName, s.ReadToEnd()));
            }
            return r;
        }
        using var doc = JsonDocument.Parse(bytes);
        IEnumerable<JsonElement> itens = doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.EnumerateArray()
            : doc.RootElement.TryGetProperty("xmls", out var x) || doc.RootElement.TryGetProperty("Xmls", out x) ? x.EnumerateArray() : [];
        var n = 0;
        foreach (var e in itens)
            if (e.ValueKind == JsonValueKind.String && e.GetString() is { } b64)
            {
                try { r.Add(($"sieg-{++n}.xml", Encoding.UTF8.GetString(Convert.FromBase64String(b64)))); }
                catch (FormatException) { r.Add(($"sieg-{++n}.xml", b64)); }
            }
        return r;
    }

    private async Task<string> TokenAsync(Guid empresa, bool renovar, CancellationToken ct)
    {
        if (!renovar && Tokens.TryGetValue(empresa, out var t) && t.Vence > DateTimeOffset.UtcNow) return t.Token;
        var corpo = new { clientId = Credencial(empresa, "ClientId"), secretKey = Credencial(empresa, "SecretKey") };
        using var req = new HttpRequestMessage(HttpMethod.Post, UrlBase + CaminhoToken) { Content = CorpoJson.De(corpo) };
        req.Headers.Add("X-Api-Key", Credencial(empresa, "ApiKey"));
        using var resp = await http.CreateClient("sieg").SendAsync(req, ct);
        var txt = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException($"SIEG recusou o token ({(int)resp.StatusCode}): {Resumo(txt)} — confira Client ID e Secret Key.");
        string? token = null;
        try
        {
            using var d = JsonDocument.Parse(txt);
            foreach (var nome in new[] { "token", "accessToken", "access_token", "Token", "jwt" })
                if (d.RootElement.ValueKind == JsonValueKind.Object && d.RootElement.TryGetProperty(nome, out var v) && v.GetString() is { Length: > 0 } s) { token = s; break; }
        }
        catch (JsonException) { token = txt.Trim('"', ' ', '\n'); }
        if (string.IsNullOrWhiteSpace(token)) throw new HttpRequestException("SIEG não devolveu o token.");
        Tokens[empresa] = (token, DateTimeOffset.UtcNow.AddHours(23));
        return token;
    }

    // Nunca devolve credencial: só o começo da mensagem do SIEG.
    private static string Resumo(string s) => s.Length <= 200 ? s : s[..200];
}

internal static class CorpoJson
{
    /// <summary>JSON com Content-Length (sem "chunked"): algumas APIs externas recusam corpo em partes.</summary>
    public static StringContent De(object corpo) =>
        new(System.Text.Json.JsonSerializer.Serialize(corpo), System.Text.Encoding.UTF8, "application/json");
}
