using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using QualyHub.Application.Abstracoes;

namespace QualyHub.Web.Seguranca;

/// <summary>Nomes das claims do QualyHub (preenchidas no login — cookie local ou Entra ID).</summary>
public static class ClaimsQh
{
    public const string UsuarioId = "qh:uid";
    public const string EmpresaId = "qh:empresa";
    public const string EmpresaNome = "qh:empresa_nome";
    public const string Perfil = "qh:perfil";
    public const string Funcao = "qh:funcao";
    public const string TodasObras = "qh:todas_obras";
    public const string Obras = "qh:obras";
    /// <summary>Conta (cadastro principal com login e senha) — igual ao usuário, ou a dona do vínculo em outra construtora.</summary>
    public const string Conta = "qh:conta";
    public const string Plataforma = "qh:plataforma";
    public const string VariasConstrutoras = "qh:construtoras";
    public const string Suporte = "qh:suporte";
}

/// <summary>Claims da sessão da Área do Fornecedor.</summary>
public static class ClaimsFornecedor
{
    public const string Documento = "qhf:doc";
    public const string Destino = "qhf:destino";

    public static IdentidadeFornecedor? Identidade(ClaimsPrincipal u) =>
        u.FindFirst(Documento)?.Value is { Length: > 0 } d && u.FindFirst(Destino)?.Value is { Length: > 0 } t ? new IdentidadeFornecedor(d, t) : null;
}

/// <summary>
/// Usuário da requisição/circuito. Em requisição HTTP lê o HttpContext; no circuito Blazor lê o AuthenticationState
/// (que no Blazor Server já vem resolvido do cookie na abertura do circuito).
/// </summary>
public sealed class UsuarioAtualWeb(IHttpContextAccessor http, AuthenticationStateProvider? auth = null) : IUsuarioAtual
{
    private ClaimsPrincipal? _principal;

    private ClaimsPrincipal Principal
    {
        get
        {
            if (_principal is not null) return _principal;
            var u = http.HttpContext?.User;
            if (u?.Identity?.IsAuthenticated == true) return _principal = u;
            if (auth is not null)
            {
                try
                {
                    var t = auth.GetAuthenticationStateAsync();
                    if (t.IsCompleted) return _principal = t.Result.User;
                }
                catch (InvalidOperationException) { /* fora de circuito */ }
            }
            return u ?? new ClaimsPrincipal(new ClaimsIdentity());
        }
    }

    private string? Claim(string tipo) => Principal.FindFirst(tipo)?.Value;

    public bool Autenticado => Principal.Identity?.IsAuthenticated == true;
    public Guid? ContaId => Guid.TryParse(Claim(ClaimsQh.Conta), out var g) ? g : UsuarioId;
    public bool AdministradorPlataforma => Claim(ClaimsQh.Plataforma) == "1";
    public Guid EmpresaId => Guid.TryParse(Claim(ClaimsQh.EmpresaId), out var g) ? g : Guid.Empty;
    public Guid? UsuarioId => Guid.TryParse(Claim(ClaimsQh.UsuarioId), out var g) ? g : null;
    public string Nome => Claim(ClaimTypes.Name) ?? Claim("name") ?? "anônimo";
    public string EmpresaNome => Claim(ClaimsQh.EmpresaNome) ?? "";
    public string Perfil => Claim(ClaimsQh.Perfil) ?? "";
    public string? Funcao => Claim(ClaimsQh.Funcao);
    public bool AcessoTodasObras => Claim(ClaimsQh.TodasObras) == "1";
    public IReadOnlyCollection<Guid> ObrasPermitidas =>
        (Claim(ClaimsQh.Obras) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList();
    public string Origem => "Web";
    public string? CorrelationId => http.HttpContext?.TraceIdentifier;
}
