using Microsoft.AspNetCore.Components;
using Groundkey.Web.Client.Servicos;

namespace Groundkey.Web.Client.Componentes;

/// <summary>
/// Base das telas que rodam no navegador: acesso à API, cancelamento ao sair da tela e tratamento de erro padrão
/// (recusa de negócio mostra o texto do servidor; falha técnica mostra aviso com "tentar de novo").
/// </summary>
public abstract class TelaNavegador : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    protected CancellationToken Cancelamento => _cts.Token;

    [Inject] protected Api Api { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;

    /// <summary>Primeira carga ainda não terminou (esqueleto só dentro da região de dados).</summary>
    protected bool Carregando { get; private set; } = true;
    /// <summary>Mensagem de falha da última carga (null = ok).</summary>
    protected string? Falha { get; private set; }
    protected bool FalhaTecnica { get; private set; }

    protected static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Executa a carga mantendo na tela o que já havia até o novo dado chegar.</summary>
    protected async Task Buscar(Func<Task> carga)
    {
        try
        {
            Falha = null;
            await carga();
        }
        catch (OperationCanceledException) { }
        catch (RecusaApi r) { Falha = r.Message; FalhaTecnica = false; }
        catch (Exception e) { Falha = e is HttpRequestException ? e.Message : "Não foi possível carregar os dados."; FalhaTecnica = true; }
        finally { Carregando = false; }
    }

    public virtual void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
