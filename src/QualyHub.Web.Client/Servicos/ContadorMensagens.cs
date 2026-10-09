namespace QualyHub.Web.Client.Servicos;

/// <summary>
/// Contador de conversas não lidas do menu. O layout serve às telas do servidor e às do navegador, então o contador
/// é uma abstração: no servidor lê direto (e acende na hora com mensagem nova); no navegador pergunta à API.
/// </summary>
public interface IContadorMensagens
{
    Task<int> NaoLidasAsync(CancellationToken ct = default);
    /// <summary>Disparado quando chega novidade para o usuário (só no servidor; no navegador o layout consulta de tempos em tempos).</summary>
    event Action? Mudou;
}

public sealed class ContadorMensagensApi(Api api) : IContadorMensagens
{
    public async Task<int> NaoLidasAsync(CancellationToken ct = default)
    {
        try { return await api.ObterAsync<int>("api/telas/mensagens/nao-lidas", ct); }
        catch (Exception e) when (e is HttpRequestException or RecusaApi) { return 0; }
    }

    public event Action? Mudou { add { } remove { } }
}
