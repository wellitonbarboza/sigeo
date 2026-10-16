using System.Collections.Concurrent;

namespace Sigeo.Web.Servicos;

/// <summary>
/// Cache do processo, por empresa, para catálogos grandes e iguais para todos os usuários da empresa (ex.: produtos).
/// Antes cada usuário conectado carregava o catálogo inteiro na própria memória; agora é uma cópia por empresa.
/// Os valores são só leitura (as telas não alteram as listas recebidas).
/// </summary>
public sealed class CacheCompartilhado(TimeProvider relogio)
{
    private readonly ConcurrentDictionary<(Guid Empresa, string Chave), (DateTimeOffset Em, object Valor)> _itens = new();

    public async Task<T> ObterAsync<T>(Guid empresa, string chave, TimeSpan validade, Func<Task<T>> carregar)
    {
        var agora = relogio.GetUtcNow();
        if (_itens.TryGetValue((empresa, chave), out var c) && agora - c.Em < validade) return (T)c.Valor;
        var v = await carregar();
        _itens[(empresa, chave)] = (agora, v!);
        return v;
    }

    public void Invalidar(Guid empresa, string chave) => _itens.TryRemove((empresa, chave), out _);
}
