using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Infrastructure.Servicos;
using Sigeo.Web.Components.Shared;
using Sigeo.Web.Seguranca;
using Sigeo.Web.Servicos;

namespace Sigeo.Web.Components;

/// <summary>Base das telas: acesso ao banco, usuário, mensagens e execução com tratamento de regra de negócio.</summary>
public abstract class PaginaBase : ComponentBase, IDisposable
{
    // Cancelado quando o usuário sai da tela ou fecha a aba: consulta em andamento não fica ocupando conexão do pool.
    private readonly CancellationTokenSource _cts = new();
    protected CancellationToken Cancelamento => _cts.Token;

    public virtual void Dispose()
    {
        // Só cancela, não descarta a fonte: uma carga que ainda está num await vai ler Cancelamento depois do Dispose
        // e precisa receber "cancelado" (o Blazor ignora), não ObjectDisposedException (que derrubaria o circuito).
        if (!_cts.IsCancellationRequested) _cts.Cancel();
        GC.SuppressFinalize(this);
    }

    [Inject] protected IFabricaDb Fabrica { get; set; } = default!;
    [Inject] protected UsuarioAtualWeb Usuario { get; set; } = default!;
    [Inject] protected Notificacoes Notif { get; set; } = default!;
    [Inject] protected Catalogos Cat { get; set; } = default!;
    [Inject] protected TimeProvider Relogio { get; set; } = default!;
    [Inject] private ILoggerFactory Logs { get; set; } = default!;
    private ILogger? _log;
    private ILogger Log => _log ??= Logs.CreateLogger<PaginaBase>();

    protected IReadOnlyList<string>? ErrosForm { get; set; }
    protected bool Ocupado { get; set; }
    /// <summary>
    /// Listas grandes buscam no banco só os registros mais recentes (menos leitura no banco e menos memória por usuário);
    /// a grade avisa quando o limite é atingido e "Carregar todos" liga <see cref="ListaCompleta"/>.
    /// </summary>
    protected const int LimiteLista = 1000;
    protected bool ListaCompleta { get; set; }
    protected int LimiteAtual => ListaCompleta ? int.MaxValue : LimiteLista;
    protected int? LimiteGrade => ListaCompleta ? null : LimiteLista;

    protected DateOnly Hoje => DateOnly.FromDateTime(Relogio.GetLocalNow().Date);

    protected bool Perfil(params Sigeo.Domain.Common.PerfilUsuario[] perfis) =>
        Enum.TryParse<Sigeo.Domain.Common.PerfilUsuario>(Usuario.Perfil, out var p)
        && (p == Sigeo.Domain.Common.PerfilUsuario.Administrador || perfis.Contains(p));

    /// <summary>Executa uma ação da tela: validação/regra viram mensagem legível; sucesso mostra aviso.</summary>
    protected async Task<bool> Executar(Func<Task> acao, string? sucesso = null)
    {
        ErrosForm = null;
        Ocupado = true;
        try
        {
            await acao();
            if (sucesso is not null) Notif.Sucesso(sucesso);
            Cat.Invalidar();
            return true;
        }
        catch (ValidacaoException v) { ErrosForm = v.Erros; }
        catch (Sigeo.Domain.Qualidade.ValidacaoFalha v) { ErrosForm = v.Erros; }
        catch (RegraDeNegocioException r) { ErrosForm = [r.Message]; }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException) { ErrosForm = ["Registro alterado por outra pessoa. Feche e abra novamente."]; }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException e) when (e.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
                                                                        || e.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true)
        { ErrosForm = ["Já existe um registro com esse código/identificação."]; }
        catch (OperationCanceledException) { return false; } // circuito encerrado / navegação: nada a mostrar
        catch (Exception ex)
        {
            Log.LogError(ex, "Erro inesperado na tela {Tela} (usuário {Usuario})", GetType().Name, Usuario.Nome);
            ErrosForm = ["Erro inesperado — a equipe técnica foi registrada. Tente novamente."];
        }
        finally { Ocupado = false; }
        if (ErrosForm is { Count: > 0 }) Notif.Erro(ErrosForm.Count == 1 ? ErrosForm[0] : $"{ErrosForm.Count} pendências — veja o formulário.");
        return false;
    }

    // ───── Confirmação de ações destrutivas (usada com <Confirmacao Pergunta=... />) ─────
    protected string? PerguntaConfirmacao { get; private set; }
    private Func<Task>? _acaoConfirmada;
    private Func<Task>? _acaoRecusada;

    /// <summary>Abre a confirmação; a ação só roda se o usuário confirmar.</summary>
    protected void Confirmar(string pergunta, Func<Task> acao, Func<Task>? aoRecusar = null)
    { PerguntaConfirmacao = pergunta; _acaoConfirmada = acao; _acaoRecusada = aoRecusar; }

    protected async Task ConfirmacaoSim()
    {
        var a = _acaoConfirmada; PerguntaConfirmacao = null; _acaoConfirmada = null; _acaoRecusada = null;
        if (a is not null) await a();
    }

    protected async Task ConfirmacaoNao()
    {
        var a = _acaoRecusada; PerguntaConfirmacao = null; _acaoConfirmada = null; _acaoRecusada = null;
        if (a is not null) await a();
    }

    /// <summary>Garante que a barra de ferramentas enxergue a grade (a referência só existe após a 1ª renderização).</summary>
    protected override void OnAfterRender(bool firstRender) { if (firstRender) StateHasChanged(); }
}
