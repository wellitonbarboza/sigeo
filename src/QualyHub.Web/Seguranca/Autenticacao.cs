using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using QualyHub.Domain.Estrutura;
using QualyHub.Infrastructure.Servicos;

namespace QualyHub.Web.Seguranca;

/// <summary>
/// Login próprio do QualyHub: usuário e senha administrados em Sistema › Usuários (sem conta Microsoft).
/// Senha em hash PBKDF2, bloqueio após 5 tentativas, troca obrigatória da senha provisória,
/// limite de requisições por IP no login e cookie HttpOnly/Secure.
/// </summary>
public static class Autenticacao
{
    public const string ClaimTrocarSenha = "qh:trocar-senha";
    public const string PoliticaLogin = "login";

    public static void Configurar(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.LoginPath = "/login";
                o.LogoutPath = "/conta/sair";
                o.AccessDeniedPath = "/login";
                o.ExpireTimeSpan = TimeSpan.FromHours(10);
                o.SlidingExpiration = true;
                o.Cookie.Name = "qualyhub";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            });
        builder.Services.AddAuthorization();
        builder.Services.AddCascadingAuthenticationState();
        // Até 30 tentativas por minuto por IP nas telas de senha (escritório inteiro pode sair pelo mesmo IP),
        // além do bloqueio por usuário após 5 erros.
        builder.Services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = (ctx, _) => { ctx.HttpContext.Response.Redirect("/login?erro=limite"); return ValueTask.CompletedTask; };
            o.AddPolicy(PoliticaLogin, ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
    }

    public static IEnumerable<Claim> Claims(Usuario u, string empresaNome, bool trocarSenha = false)
    {
        var c = new List<Claim>
        {
            new(ClaimTypes.Name, u.Nome),
            new(ClaimTypes.Email, u.Email),
            new(ClaimsQh.UsuarioId, u.Id.ToString()),
            new(ClaimsQh.EmpresaId, u.EmpresaId.ToString()),
            new(ClaimsQh.EmpresaNome, empresaNome),
            new(ClaimsQh.Perfil, u.Perfil.ToString()),
            new(ClaimsQh.Funcao, u.Funcao ?? ""),
            new(ClaimsQh.TodasObras, u.AcessoTodasObras || u.Perfil is Domain.Common.PerfilUsuario.Administrador or Domain.Common.PerfilUsuario.Diretoria or Domain.Common.PerfilUsuario.Qualidade ? "1" : "0"),
            new(ClaimsQh.Obras, string.Join(',', u.Obras.Select(o => o.ObraId))),
            new(ClaimTypes.Role, u.Perfil.ToString()),
        };
        if (trocarSenha) c.Add(new(ClaimTrocarSenha, "1"));
        return c;
    }

    public static async Task EntrarAsync(HttpContext http, Usuario u, string empresaNome, bool trocarSenha = false)
    {
        var id = new ClaimsIdentity(Claims(u, empresaNome, trocarSenha), CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
    }

    private static string Retorno(string? r) => r is { Length: > 1 } && r.StartsWith('/') && !r.StartsWith("//") && !r.StartsWith("/\\") ? r : "/";

    public static void MapearEndpoints(WebApplication app)
    {
        app.MapPost("/conta/entrar", async (HttpContext http, IAntiforgery af, ContaServico conta) =>
        {
            if (!await af.IsRequestValidAsync(http)) return Results.Redirect("/login?erro=sessao");
            var f = await http.Request.ReadFormAsync();
            var r = await conta.EntrarAsync(f["login"], f["senha"]);
            if (r.Usuario is null)
                return Results.Redirect($"/login?erro={(r.Erro == ContaServico.ErroGenerico ? "credencial" : "bloqueado")}&login={Uri.EscapeDataString(f["login"].ToString())}");
            await EntrarAsync(http, r.Usuario, r.EmpresaNome!, r.TrocarSenha);
            return Results.Redirect(r.TrocarSenha ? "/conta/trocar-senha" : Retorno(f["retorno"]));
        }).DisableAntiforgery().RequireRateLimiting(PoliticaLogin);

        app.MapPost("/conta/trocar-senha/salvar", async (HttpContext http, IAntiforgery af, ContaServico conta, UsuarioAtualWeb atual) =>
        {
            if (!await af.IsRequestValidAsync(http)) return Results.Redirect("/conta/trocar-senha?erro=sessao");
            var f = await http.Request.ReadFormAsync();
            if (atual.UsuarioId is not { } id) return Results.Redirect("/login");
            try { await conta.AlterarSenhaAsync(id, f["atual"], f["nova"], f["confirmacao"]); }
            catch (ValidacaoException v) { return Results.Redirect("/conta/trocar-senha?erro=" + Uri.EscapeDataString(string.Join(" ", v.Erros))); }
            catch (Domain.Aprovacoes.RegraDeNegocioException e) { return Results.Redirect("/conta/trocar-senha?erro=" + Uri.EscapeDataString(e.Message)); }
            // Renova o cookie sem a marca de troca obrigatória.
            var ident = (ClaimsIdentity)http.User.Identity!;
            var novas = http.User.Claims.Where(c => c.Type != ClaimTrocarSenha).Select(c => new Claim(c.Type, c.Value));
            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(novas, ident.AuthenticationType)));
            return Results.Redirect("/?senha=alterada");
        }).DisableAntiforgery().RequireAuthorization().RequireRateLimiting(PoliticaLogin);

        app.MapPost("/conta/primeiro-acesso/salvar", async (HttpContext http, IAntiforgery af, ContaServico conta, IConfiguration cfg) =>
        {
            if (!await af.IsRequestValidAsync(http)) return Results.Redirect("/conta/primeiro-acesso?erro=sessao");
            var f = await http.Request.ReadFormAsync();
            var codigo = cfg["CodigoConfiguracaoInicial"];
            if (string.IsNullOrWhiteSpace(codigo) || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(f["codigoSetup"].ToString()), System.Text.Encoding.UTF8.GetBytes(codigo)))
                return Results.Redirect("/conta/primeiro-acesso?erro=" + Uri.EscapeDataString("Código de configuração inválido."));
            try { await conta.DefinirSenhaAdministradorAsync(f["login"], f["senha"], f["confirmacao"]); }
            catch (ValidacaoException v) { return Results.Redirect("/conta/primeiro-acesso?erro=" + Uri.EscapeDataString(string.Join(" ", v.Erros))); }
            catch (Domain.Aprovacoes.RegraDeNegocioException e) { return Results.Redirect("/conta/primeiro-acesso?erro=" + Uri.EscapeDataString(e.Message)); }
            return Results.Redirect("/login?ok=senha");
        }).DisableAntiforgery().RequireRateLimiting(PoliticaLogin);

        app.MapGet("/conta/sair", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        });
    }

    /// <summary>Com senha provisória, o usuário só acessa a troca de senha até definir a própria.</summary>
    public static void UsarTrocaObrigatoria(WebApplication app) => app.Use(async (ctx, next) =>
    {
        var p = ctx.Request.Path.Value ?? "";
        if (ctx.User.HasClaim(ClaimTrocarSenha, "1") && !p.StartsWith("/conta/") && !p.StartsWith("/_framework") && !p.Contains('.'))
        {
            ctx.Response.Redirect("/conta/trocar-senha");
            return;
        }
        await next();
    });
}
