namespace Sigeo.Application.Abstracoes;

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
