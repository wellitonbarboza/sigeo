namespace Sigeo.Application.Abstracoes;

/// <summary>
/// Envio de mensagens para fora (e-mail e WhatsApp). Produção: Azure Communication Services.
/// Desenvolvimento sem configuração: registra no log (o código aparece no console).
/// </summary>
public interface IEnvioMensagens
{
    bool EmailDisponivel { get; }
    bool WhatsAppDisponivel { get; }

    Task EnviarEmailAsync(string para, string assunto, string texto, string html, CancellationToken ct = default);

    /// <summary>Código de acesso pelo modelo de autenticação aprovado no WhatsApp Business (parâmetro = código).</summary>
    Task EnviarCodigoWhatsAppAsync(string celularE164, string codigo, CancellationToken ct = default);

    /// <summary>Convite pelo modelo de convite do WhatsApp Business (parâmetros: construtora, assunto, endereço).</summary>
    Task EnviarConviteWhatsAppAsync(string celularE164, string construtora, string assunto, string endereco, CancellationToken ct = default);
}
