using Azure;
using Azure.Communication.Email;
using Azure.Communication.Messages;
using Azure.Communication.Messages.Models.Channels;
using Azure.Core;
using Microsoft.Extensions.Logging;

namespace Groundkey.Infrastructure.Comunicacao;

/// <summary>Configuração do Azure Communication Services (App Settings: Groundkey__Comunicacao__*).</summary>
public sealed class OpcoesComunicacao
{
    /// <summary>https://&lt;recurso&gt;.communication.azure.com — autenticação pela identidade gerenciada do App Service.</summary>
    public string? Endpoint { get; set; }
    /// <summary>Remetente verificado no domínio de e-mail ligado ao recurso (ex.: DoNotReply@xxxx.azurecomm.net).</summary>
    public string? EmailRemetente { get; set; }
    /// <summary>Id do canal WhatsApp registrado no recurso (Advanced Messaging).</summary>
    public string? WhatsAppCanalId { get; set; }
    /// <summary>Modelo de autenticação aprovado pela Meta (corpo com {{1}} = código e botão "copiar código").</summary>
    public string? WhatsAppModeloCodigo { get; set; }
    /// <summary>Modelo de convite aprovado pela Meta ({{1}} construtora, {{2}} assunto, {{3}} endereço).</summary>
    public string? WhatsAppModeloConvite { get; set; }
    public string WhatsAppIdioma { get; set; } = "pt_BR";
}

/// <summary>Envio real pelo Azure Communication Services (e-mail e WhatsApp), sem chave guardada: identidade gerenciada.</summary>
public sealed class EnvioAcs : IEnvioMensagens
{
    private readonly OpcoesComunicacao _o;
    private readonly EmailClient? _email;
    private readonly NotificationMessagesClient? _whats;
    private readonly Guid _canal;
    private readonly ILogger<EnvioAcs> _log;

    public EnvioAcs(OpcoesComunicacao o, TokenCredential credencial, ILogger<EnvioAcs> log)
    {
        _o = o; _log = log;
        var endpoint = new Uri(o.Endpoint!);
        if (!string.IsNullOrWhiteSpace(o.EmailRemetente)) _email = new EmailClient(endpoint, credencial);
        if (Guid.TryParse(o.WhatsAppCanalId, out _canal) && !string.IsNullOrWhiteSpace(o.WhatsAppModeloCodigo))
            _whats = new NotificationMessagesClient(endpoint, credencial);
    }

    public bool EmailDisponivel => _email is not null;
    public bool WhatsAppDisponivel => _whats is not null;

    public async Task EnviarEmailAsync(string para, string assunto, string texto, string html, CancellationToken ct = default)
    {
        if (_email is null) throw new InvalidOperationException("E-mail não configurado.");
        var msg = new EmailMessage(_o.EmailRemetente!, para, new EmailContent(assunto) { PlainText = texto, Html = html });
        // Started: o ACS aceita e entrega em segundo plano; falha de entrega aparece nos logs do recurso.
        var op = await _email.SendAsync(WaitUntil.Started, msg, ct);
        _log.LogInformation("E-mail {Operacao} enviado para {Destino}.", op.Id, Mascara(para));
    }

    public async Task EnviarCodigoWhatsAppAsync(string celularE164, string codigo, CancellationToken ct = default)
    {
        if (_whats is null) throw new InvalidOperationException("WhatsApp não configurado.");
        // Modelo de autenticação da Meta: o código vai no corpo e no botão "copiar código" (parâmetro de URL).
        var modelo = new MessageTemplate(_o.WhatsAppModeloCodigo!, _o.WhatsAppIdioma);
        modelo.Values.Add(new MessageTemplateText("codigo", codigo));
        var vinculos = new WhatsAppMessageTemplateBindings();
        vinculos.Body.Add(new WhatsAppMessageTemplateBindingsComponent("codigo"));
        vinculos.Buttons.Add(new WhatsAppMessageTemplateBindingsButton(WhatsAppMessageButtonSubType.Url.ToString(), "codigo"));
        modelo.Bindings = vinculos;
        var r = await _whats.SendAsync(new TemplateNotificationContent(_canal, [celularE164], modelo), ct);
        _log.LogInformation("WhatsApp {Mensagem} (código) enviado para {Destino}.", r.Value.Receipts.FirstOrDefault()?.MessageId, Mascara(celularE164));
    }

    public async Task EnviarConviteWhatsAppAsync(string celularE164, string construtora, string assunto, string endereco, CancellationToken ct = default)
    {
        if (_whats is null || string.IsNullOrWhiteSpace(_o.WhatsAppModeloConvite)) throw new InvalidOperationException("Modelo de convite do WhatsApp não configurado.");
        var modelo = new MessageTemplate(_o.WhatsAppModeloConvite!, _o.WhatsAppIdioma);
        modelo.Values.Add(new MessageTemplateText("construtora", construtora));
        modelo.Values.Add(new MessageTemplateText("assunto", assunto));
        modelo.Values.Add(new MessageTemplateText("endereco", endereco));
        var vinculos = new WhatsAppMessageTemplateBindings();
        vinculos.Body.Add(new WhatsAppMessageTemplateBindingsComponent("construtora"));
        vinculos.Body.Add(new WhatsAppMessageTemplateBindingsComponent("assunto"));
        vinculos.Body.Add(new WhatsAppMessageTemplateBindingsComponent("endereco"));
        modelo.Bindings = vinculos;
        await _whats.SendAsync(new TemplateNotificationContent(_canal, [celularE164], modelo), ct);
    }

    private static string Mascara(string d) => d.Length <= 4 ? "••••" : "••••" + d[^4..];
}

/// <summary>
/// Desenvolvimento sem Azure: não envia nada, só registra no log (o código aparece no console de quem roda localmente).
/// Nunca é usado em produção (lá, sem configuração, os canais ficam indisponíveis).
/// </summary>
public sealed class EnvioLocalLog(ILogger<EnvioLocalLog> log) : IEnvioMensagens
{
    public bool EmailDisponivel => true;
    public bool WhatsAppDisponivel => true;

    public Task EnviarEmailAsync(string para, string assunto, string texto, string html, CancellationToken ct = default)
    { log.LogWarning("[DESENVOLVIMENTO] E-mail para {Para}: {Assunto} — {Texto}", para, assunto, texto); return Task.CompletedTask; }

    public Task EnviarCodigoWhatsAppAsync(string celularE164, string codigo, CancellationToken ct = default)
    { log.LogWarning("[DESENVOLVIMENTO] WhatsApp para {Para}: código {Codigo}", celularE164, codigo); return Task.CompletedTask; }

    public Task EnviarConviteWhatsAppAsync(string celularE164, string construtora, string assunto, string endereco, CancellationToken ct = default)
    { log.LogWarning("[DESENVOLVIMENTO] WhatsApp para {Para}: {Construtora} — {Assunto} — {Endereco}", celularE164, construtora, assunto, endereco); return Task.CompletedTask; }
}

/// <summary>Produção sem Azure Communication Services configurado: os canais aparecem como indisponíveis.</summary>
public sealed class EnvioIndisponivel : IEnvioMensagens
{
    public bool EmailDisponivel => false;
    public bool WhatsAppDisponivel => false;
    public Task EnviarEmailAsync(string para, string assunto, string texto, string html, CancellationToken ct = default) => throw new InvalidOperationException("E-mail não configurado.");
    public Task EnviarCodigoWhatsAppAsync(string celularE164, string codigo, CancellationToken ct = default) => throw new InvalidOperationException("WhatsApp não configurado.");
    public Task EnviarConviteWhatsAppAsync(string celularE164, string construtora, string assunto, string endereco, CancellationToken ct = default) => throw new InvalidOperationException("WhatsApp não configurado.");
}
