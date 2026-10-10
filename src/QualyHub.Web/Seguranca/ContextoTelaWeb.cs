using Microsoft.AspNetCore.Components;
using QualyHub.Application.Servicos;

namespace QualyHub.Web.Seguranca;

/// <summary>
/// Tela em que o usuário está: no circuito Blazor, o endereço atual; numa chamada da API das telas do navegador,
/// a tela que o endpoint declara (<see cref="Chave"/>). Fora disso (login, rotinas, portal), nenhuma.
/// </summary>
public sealed class ContextoTelaWeb(IHttpContextAccessor http, IServiceProvider sp) : IContextoTela
{
    public const string Chave = "qh:tela";

    public string? Rota
    {
        get
        {
            if (http.HttpContext?.Items[Chave] is string t) return t;
            try
            {
                var nav = sp.GetService<NavigationManager>();
                return nav is null ? null : nav.ToBaseRelativePath(nav.Uri);
            }
            catch (InvalidOperationException) { return null; } // fora de componente: navegação não inicializada
        }
    }
}
