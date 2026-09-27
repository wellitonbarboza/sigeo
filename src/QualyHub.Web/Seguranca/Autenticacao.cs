using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using QualyHub.Domain.Estrutura;
using QualyHub.Infrastructure.Dados;
using QualyHub.Infrastructure.Servicos;

namespace QualyHub.Web.Seguranca;

/// <summary>
/// Dois modos:
/// • EntraId (produção): login Microsoft; o ObjectId (oid) é casado com Usuario.EntraObjectId e as claims do QualyHub são adicionadas.
/// • Local (desenvolvimento/testes): escolhe um usuário cadastrado, sem senha — só habilitado fora de produção.
/// </summary>
public static class Autenticacao
{
    public static bool ModoEntra(IConfiguration cfg) => !string.IsNullOrWhiteSpace(cfg["AzureAd:ClientId"]);

    public static void Configurar(WebApplicationBuilder builder)
    {
        var cfg = builder.Configuration;
        if (ModoEntra(cfg))
        {
            builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(cfg.GetSection("AzureAd"));
            builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, o =>
            {
                var anterior = o.Events.OnTokenValidated;
                o.Events.OnTokenValidated = async ctx =>
                {
                    if (anterior is not null) await anterior(ctx);
                    var oid = ctx.Principal?.GetObjectId();
                    var sistema = ctx.HttpContext.RequestServices.GetRequiredService<FabricaSistema>();
                    await using var db = sistema.Criar(Guid.Empty, "Login");
                    var u = Guid.TryParse(oid, out var g)
                        ? await db.Usuarios.IgnoreQueryFilters().Include(x => x.Obras).FirstOrDefaultAsync(x => x.EntraObjectId == g && x.Ativo && !x.Excluido)
                        : null;
                    if (u is null) { ctx.Fail("Usuário sem cadastro ativo no QualyHub. Peça acesso ao administrador."); return; }
                    var emp = await db.Empresas.Where(e => e.Id == u.EmpresaId).Select(e => e.NomeFantasia).FirstAsync();
                    ((ClaimsIdentity)ctx.Principal!.Identity!).AddClaims(Claims(u, emp));
                };
            });
        }
        else
        {
            // Login local (sem senha) é só para desenvolvimento/testes. Em produção exige Entra ID,
            // a menos que alguém ligue explicitamente Autenticacao:PermitirLoginLocal (nunca em dados reais).
            if (builder.Environment.IsProduction() && !cfg.GetValue<bool>("Autenticacao:PermitirLoginLocal"))
                throw new InvalidOperationException("Produção sem Entra ID: configure AzureAd:TenantId e AzureAd:ClientId.");
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(o =>
                {
                    o.LoginPath = "/login";
                    o.LogoutPath = "/conta/sair";
                    o.ExpireTimeSpan = TimeSpan.FromHours(10);
                    o.SlidingExpiration = true;
                    o.Cookie.Name = "qualyhub";
                });
        }
        builder.Services.AddAuthorization();
        builder.Services.AddCascadingAuthenticationState();
    }

    public static IEnumerable<Claim> Claims(Usuario u, string empresaNome) =>
    [
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
    ];

    public static async Task EntrarLocalAsync(HttpContext http, Usuario u, string empresaNome)
    {
        var id = new ClaimsIdentity(Claims(u, empresaNome), CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
    }

    public static void MapearEndpoints(WebApplication app)
    {
        app.MapPost("/conta/entrar", async (HttpContext http, FabricaSistema sistema, IWebHostEnvironment env, IConfiguration cfg) =>
        {
            if (ModoEntra(cfg) || (env.IsProduction() && !cfg.GetValue<bool>("Autenticacao:PermitirLoginLocal"))) return Results.Forbid();
            var form = await http.Request.ReadFormAsync();
            if (!Guid.TryParse(form["usuario"], out var uid)) return Results.Redirect("/login?erro=1");
            await using var db = sistema.Criar(Guid.Empty, "Login");
            var u = await db.Usuarios.IgnoreQueryFilters().Include(x => x.Obras).FirstOrDefaultAsync(x => x.Id == uid && x.Ativo && !x.Excluido);
            if (u is null) return Results.Redirect("/login?erro=1");
            var emp = await db.Empresas.Where(e => e.Id == u.EmpresaId).Select(e => e.NomeFantasia).FirstAsync();
            await EntrarLocalAsync(http, u, emp);
            return Results.Redirect(form["retorno"].FirstOrDefault() is { Length: > 0 } r && r.StartsWith('/') ? r : "/");
        }).DisableAntiforgery();

        app.MapGet("/conta/entrar-microsoft", (HttpContext http) =>
            Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [OpenIdConnectDefaults.AuthenticationScheme]));

        app.MapGet("/conta/sair", async (HttpContext http, IConfiguration cfg) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (ModoEntra(cfg)) await http.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        });
    }
}
