using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Comum;
using Sigeo.Domain.Aprovacoes;

namespace Sigeo.Web.Servicos;

/// <summary>
/// Tradução de exceções em Problem Details (RFC 9457) para os endpoints HTTP.
/// Fica num só lugar para que o tratador global e o filtro do grupo /api/v1 respondam igual.
/// </summary>
public static class ProblemasApi
{
    // Só estes caminhos são "endpoint"; o resto são páginas Blazor, que seguem para /Error.
    // /configuracao-inicial/salvar e não /configuracao-inicial: a página do formulário também usa esse prefixo.
    private static readonly string[] Prefixos = ["/arquivos/", "/grd/", "/configuracao-inicial/salvar", "/api/"];

    public static bool EhEndpoint(PathString caminho) =>
        Prefixos.Any(p => caminho.StartsWithSegments(p.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));

    /// <summary>Monta o Problem Details da exceção. Erros não previstos viram 500 sem detalhe interno.</summary>
    public static ProblemDetails De(Exception ex) => ex switch
    {
        ValidacaoException v => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict, Title = "Dados inválidos", Detail = string.Join(" ", v.Erros),
            Extensions = { ["errors"] = v.Erros }
        },
        RegraDeNegocioException r => new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Operação não permitida", Detail = r.Message },
        DbUpdateConcurrencyException => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict, Title = "Registro alterado",
            Detail = "O registro foi alterado por outra pessoa. Recarregue e tente novamente."
        },
        _ => new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = "Erro interno", Detail = "Ocorreu um erro inesperado. Tente novamente; se persistir, avise o suporte." }
    };

    public static void Registrar(ILogger log, HttpContext http, Exception ex, int status)
    {
        // 409 é situação de negócio esperada; só o 500 merece pilha completa no log.
        if (status >= 500) log.LogError(ex, "Erro não tratado em {Metodo} {Caminho}.", http.Request.Method, http.Request.Path);
        else log.LogInformation("Requisição recusada ({Status}) em {Metodo} {Caminho}: {Mensagem}", status, http.Request.Method, http.Request.Path, ex.Message);
    }
}

/// <summary>Tratador global (UseExceptionHandler) restrito aos caminhos de endpoint.</summary>
public sealed class TratadorErrosApi(IProblemDetailsService problemas, ILogger<TratadorErrosApi> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        // Páginas continuam no /Error; resposta já iniciada (download em andamento) não tem como virar JSON.
        if (!ProblemasApi.EhEndpoint(http.Request.Path) || http.Response.HasStarted) return false;
        var pd = ProblemasApi.De(ex);
        ProblemasApi.Registrar(log, http, ex, pd.Status!.Value);
        http.Response.StatusCode = pd.Status.Value;
        return await problemas.TryWriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = pd, Exception = ex });
    }
}

/// <summary>Filtro de endpoint para os grupos de API: a exceção vira Problem Details já no endpoint.</summary>
public sealed class FiltroProblemDetails(ILogger<FiltroProblemDetails> log) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        try { return await next(ctx); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var pd = ProblemasApi.De(ex);
            ProblemasApi.Registrar(log, ctx.HttpContext, ex, pd.Status!.Value);
            return Results.Problem(pd);
        }
    }
}
