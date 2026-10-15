namespace Groundkey.Application.IA;

/// <summary>Resposta do modelo com o consumo (para o controle de custo por empresa).</summary>
public sealed record RespostaIa(string Texto, int TokensEntrada, int TokensSaida);

/// <summary>
/// Modelo de linguagem (Azure OpenAI na assinatura da construtora). Só é chamado por ação da pessoa (botão) ou pela rotina
/// de notas; nunca decide sozinho o que grava — sugere, e a tela mostra para conferir.
/// </summary>
public interface IAssistenteIa
{
    bool Configurado { get; }
    /// <summary>Pergunta com instruções fixas + conteúdo; <paramref name="json"/> pede resposta em objeto JSON.</summary>
    Task<RespostaIa> PerguntarAsync(string instrucoes, string conteudo, bool json, int maxTokensSaida, CancellationToken ct = default);
}

/// <summary>Leitura de PDF/imagem (laudo, certificado) em texto — Azure AI Document Intelligence (modelo de layout).</summary>
public interface ILeitorDocumentos
{
    bool Configurado { get; }
    Task<(string Texto, int Paginas)> ExtrairTextoAsync(Stream arquivo, string contentType, CancellationToken ct = default);
}
