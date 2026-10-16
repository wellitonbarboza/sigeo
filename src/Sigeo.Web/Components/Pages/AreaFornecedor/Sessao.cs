using Microsoft.AspNetCore.Components;

namespace Sigeo.Web.Components.Pages.AreaFornecedor;

/// <summary>Sem sessão do fornecedor: volta à entrada (CNPJ + código) e, depois de entrar, para a página pedida.</summary>
internal static class Sessao
{
    public static string Entrada(NavigationManager nav) =>
        $"fornecedor?volta={Uri.EscapeDataString("/" + nav.ToBaseRelativePath(nav.Uri))}";
}
