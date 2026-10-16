namespace Sigeo.Web.Servicos;

/// <summary>Endereço antigo (qualyhub.azurewebsites.net) → novo, mantendo caminho e parâmetros (links de e-mail e favoritos continuam valendo).</summary>
public static class RedirecionamentoEndereco
{
    public static string Destino(string baseNova, PathString caminho, QueryString consulta) =>
        baseNova.TrimEnd('/') + (caminho.HasValue ? caminho.Value : "/") + consulta.Value;
}
