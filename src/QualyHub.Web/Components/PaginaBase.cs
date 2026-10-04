using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Infrastructure.Servicos;
using QualyHub.Web.Components.Shared;
using QualyHub.Web.Seguranca;
using QualyHub.Web.Servicos;

namespace QualyHub.Web.Components;

/// <summary>Base das telas: acesso ao banco, usuário, mensagens e execução com tratamento de regra de negócio.</summary>
public abstract class PaginaBase : ComponentBase
{
    [Inject] protected FabricaDb Fabrica { get; set; } = default!;
    [Inject] protected UsuarioAtualWeb Usuario { get; set; } = default!;
    [Inject] protected Notificacoes Notif { get; set; } = default!;
    [Inject] protected Catalogos Cat { get; set; } = default!;
    [Inject] protected TimeProvider Relogio { get; set; } = default!;
    [Inject] private ILoggerFactory Logs { get; set; } = default!;
    private ILogger? _log;
    private ILogger Log => _log ??= Logs.CreateLogger<PaginaBase>();

    protected IReadOnlyList<string>? ErrosForm { get; set; }
    protected bool Ocupado { get; set; }
    protected DateOnly Hoje => DateOnly.FromDateTime(Relogio.GetLocalNow().Date);

    protected bool Perfil(params QualyHub.Domain.Common.PerfilUsuario[] perfis) =>
        Enum.TryParse<QualyHub.Domain.Common.PerfilUsuario>(Usuario.Perfil, out var p)
        && (p == QualyHub.Domain.Common.PerfilUsuario.Administrador || perfis.Contains(p));

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
        catch (QualyHub.Domain.Qualidade.ValidacaoFalha v) { ErrosForm = v.Erros; }
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
