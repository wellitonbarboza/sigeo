using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Sigeo.Domain.Estrutura;
using Sigeo.Infrastructure.Servicos;

namespace Sigeo.Web.Seguranca;

/// <summary>
/// Login próprio do Sigeo: usuário e senha administrados em Sistema › Usuários (sem conta Microsoft).
/// Senha em hash PBKDF2, bloqueio após 5 tentativas, troca obrigatória da senha provisória,
/// limite de requisições por IP no login e cookie HttpOnly/Secure.
/// </summary>
public static class Autenticacao
{
    public const string ClaimTrocarSenha = "qh:trocar-senha";
    public const string PoliticaLogin = "login";
    /// <summary>Área do Fornecedor: cookie próprio, separado do login interno (o fornecedor nunca vê telas internas).</summary>
    public const string EsquemaFornecedor = "Fornecedor";
    public const string PoliticaFornecedor = "fornecedor";
    public const string LimiteFornecedor = "fornecedor-codigo";
    private const string EsquemaSeletor = "Sigeo";
    private const string EsquemaCircuito = "SigeoCircuito";

    public static void Configurar(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(o => { o.DefaultScheme = EsquemaSeletor; o.DefaultChallengeScheme = EsquemaSeletor; })
            // Escolhe o cookie pela área: /fornecedor/* usa a sessão do fornecedor, o resto o login interno.
            // O circuito do Blazor (/_blazor) não tem área (a conexão WebSocket nem sempre manda Referer): lê os dois cookies
            // e junta as identidades; cada tela confere a sua (interna exige empresa; a do fornecedor, o CNPJ verificado).
            .AddPolicyScheme(EsquemaSeletor, "Sigeo", o => o.ForwardDefaultSelector = ctx =>
                ctx.Request.Path.StartsWithSegments("/_blazor") ? EsquemaCircuito
                : ctx.Request.Path.StartsWithSegments("/fornecedor") ? EsquemaFornecedor
                : CookieAuthenticationDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, AutenticacaoCircuito>(EsquemaCircuito, null)
            .AddCookie(EsquemaFornecedor, o =>
            {
                o.LoginPath = "/fornecedor";
                o.LogoutPath = "/fornecedor/sair";
                o.AccessDeniedPath = "/fornecedor";
                o.ExpireTimeSpan = TimeSpan.FromHours(8);
                o.SlidingExpiration = false; // sessão do fornecedor não se renova sozinha: depois de 8 h, novo código
                o.Cookie.Name = "sigeo-fornecedor";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
            {
                o.LoginPath = "/login";
                o.LogoutPath = "/conta/sair";
                o.AccessDeniedPath = "/login";
                o.ExpireTimeSpan = TimeSpan.FromHours(10);
                o.SlidingExpiration = true;
                // API das telas no navegador: sessão vencida responde 401/403 (a tela leva ao login), em vez de redirecionar para o HTML do login.
                o.Events.OnRedirectToLogin = ctx => ApiOuRedireciona(ctx, StatusCodes.Status401Unauthorized);
                o.Events.OnRedirectToAccessDenied = ctx => ApiOuRedireciona(ctx, StatusCodes.Status403Forbidden);
                o.Cookie.Name = "sigeo";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            });
        builder.Services.AddAuthorization(o =>
        {
            // [Authorize] das telas internas: só usuário interno (tem empresa no cookie). A sessão do fornecedor não serve.
            o.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser().RequireClaim(ClaimsQh.EmpresaId).Build();
            o.AddPolicy(PoliticaFornecedor, p => p.AddAuthenticationSchemes(EsquemaFornecedor).RequireAuthenticatedUser()
                .RequireClaim(ClaimsFornecedor.Documento).RequireClaim(ClaimsFornecedor.Destino));
        });
        builder.Services.AddCascadingAuthenticationState();
        // Até 30 tentativas por minuto por IP nas telas de senha (escritório inteiro pode sair pelo mesmo IP),
        // além do bloqueio por usuário após 5 erros.
        builder.Services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = (ctx, _) =>
            {
                ctx.HttpContext.Response.Redirect(ctx.HttpContext.Request.Path.StartsWithSegments("/fornecedor")
                    ? "/fornecedor?erro=" + Uri.EscapeDataString("Muitas tentativas a partir desta rede. Aguarde alguns minutos.")
                    : "/login?erro=limite");
                return ValueTask.CompletedTask;
            };
            o.AddPolicy(PoliticaLogin, ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            // Área do Fornecedor (pública): consulta de contatos, envio e conferência de código — além dos limites por CNPJ no serviço.
            o.AddPolicy(LimiteFornecedor, ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));
        });
    }

    private static Task ApiOuRedireciona(Microsoft.AspNetCore.Authentication.RedirectContext<CookieAuthenticationOptions> ctx, int status)
    {
        if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = status;
        else ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    }

    public static IEnumerable<Claim> Claims(Usuario u, string empresaNome, bool trocarSenha = false, Guid? conta = null, bool plataforma = false, int acessos = 1)
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
            new(ClaimsQh.TodasObras, u.VeTodasObras ? "1" : "0"),
            new(ClaimsQh.Obras, string.Join(',', u.Obras.Select(o => o.ObraId))),
            new(ClaimTypes.Role, u.Perfil.ToString()),
        };
        if (trocarSenha) c.Add(new(ClaimTrocarSenha, "1"));
        c.Add(new(ClaimsQh.Conta, (conta ?? u.ContaId ?? u.Id).ToString()));
        if (plataforma) c.Add(new(ClaimsQh.Plataforma, "1"));
        if (acessos > 1) c.Add(new(ClaimsQh.VariasConstrutoras, acessos.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        if (u.SuporteAte is { } ate) c.Add(new(ClaimsQh.Suporte, ate.ToString("O")));
        return c;
    }

    public static async Task EntrarAsync(HttpContext http, Usuario u, string empresaNome, bool trocarSenha = false, Guid? conta = null, bool plataforma = false, int acessos = 1)
    {
        var id = new ClaimsIdentity(Claims(u, empresaNome, trocarSenha, conta, plataforma, acessos), CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
    }

    /// <summary>Renova o cookie com os dados atuais do mesmo cadastro (perfil, plataforma, nº de construtoras).</summary>
    private static async Task<IResult> Renovar(HttpContext http, ContaServico conta, string destino)
    {
        if (!Guid.TryParse(http.User.FindFirst(ClaimsQh.Conta)?.Value, out var contaId) || !Guid.TryParse(http.User.FindFirst(ClaimsQh.UsuarioId)?.Value, out var uid))
            return Results.Redirect("/login");
        if (await conta.AcessoAsync(contaId, uid) is not { } a) return Results.Redirect("/conta/sair");
        await EntrarAsync(http, a.Usuario, a.Empresa, false, contaId, a.AdministradorPlataforma, a.Acessos);
        return Results.Redirect(destino);
    }

    /// <summary>Retorno da Área do Fornecedor: só endereços dentro de /fornecedor (sem redirecionamento aberto).</summary>
    public static string RetornoFornecedor(string? r) =>
        r is { Length: > 1 } && r.StartsWith("/fornecedor/") && !r.StartsWith("//") && !r.Contains('\\') ? r : "/fornecedor/area";

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
            // Construtoras da conta (principal + vínculos). Construtora suspensa não entra.
            var acessos = await conta.AcessosAsync(r.Usuario.Id);
            if (acessos.Count == 0) return Results.Redirect($"/login?erro=suspensa&login={Uri.EscapeDataString(f["login"].ToString())}");
            var alvo = acessos.FirstOrDefault(a => a.Principal) ?? acessos[0];
            var ac = (await conta.AcessoAsync(r.Usuario.Id, alvo.UsuarioId))!.Value;
            await EntrarAsync(http, ac.Usuario, ac.Empresa, r.TrocarSenha, r.Usuario.Id, ac.AdministradorPlataforma, ac.Acessos);
            if (r.TrocarSenha) return Results.Redirect("/conta/trocar-senha");
            return Results.Redirect(acessos.Count > 1 ? "/conta/construtora?retorno=" + Uri.EscapeDataString(Retorno(f["retorno"])) : Retorno(f["retorno"]));
        }).DisableAntiforgery().RequireRateLimiting(PoliticaLogin);

        app.MapPost("/conta/trocar-senha/salvar", async (HttpContext http, IAntiforgery af, ContaServico conta, UsuarioAtualWeb atual) =>
        {
            if (!await af.IsRequestValidAsync(http)) return Results.Redirect("/conta/trocar-senha?erro=sessao");
            var f = await http.Request.ReadFormAsync();
            // A senha é da conta (cadastro principal), mesmo entrando por uma construtora vinculada.
            if ((Guid.TryParse(http.User.FindFirst(ClaimsQh.Conta)?.Value, out var contaId) ? contaId : atual.UsuarioId) is not { } id) return Results.Redirect("/login");
            try { await conta.AlterarSenhaAsync(id, f["atual"], f["nova"], f["confirmacao"]); }
            catch (ValidacaoException v) { return Results.Redirect("/conta/trocar-senha?erro=" + Uri.EscapeDataString(string.Join(" ", v.Erros))); }
            catch (Domain.Aprovacoes.RegraDeNegocioException e) { return Results.Redirect("/conta/trocar-senha?erro=" + Uri.EscapeDataString(e.Message)); }
            // Renova o cookie sem a marca de troca obrigatória.
            var ident = (ClaimsIdentity)http.User.Identity!;
            var novas = http.User.Claims.Where(c => c.Type != ClaimTrocarSenha).Select(c => new Claim(c.Type, c.Value));
            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(novas, ident.AuthenticationType)));
            return Results.Redirect("/?senha=alterada");
        }).DisableAntiforgery().RequireAuthorization().RequireRateLimiting(PoliticaLogin);

        // Troca de construtora (conta com acesso a mais de uma): renova o cookie com o cadastro da construtora escolhida.
        app.MapPost("/conta/construtora/trocar", async (HttpContext http, IAntiforgery af, ContaServico conta) =>
        {
            if (!await af.IsRequestValidAsync(http)) return Results.Redirect("/conta/construtora?erro=sessao");
            var f = await http.Request.ReadFormAsync();
            if (!Guid.TryParse(http.User.FindFirst(ClaimsQh.Conta)?.Value, out var contaId) || !Guid.TryParse(f["usuario"], out var usuarioId))
                return Results.Redirect("/login");
            var ac = await conta.AcessoAsync(contaId, usuarioId);
            if (ac is not { } a) return Results.Redirect("/conta/construtora?erro=acesso");
            await EntrarAsync(http, a.Usuario, a.Empresa, false, contaId, a.AdministradorPlataforma, a.Acessos);
            return Results.Redirect(Retorno(f["retorno"]));
        }).DisableAntiforgery().RequireAuthorization();

        // Plataforma: ativa a conta logada como administradora (exige o código da plataforma do Key Vault).
        app.MapPost("/plataforma/ativar/salvar", async (HttpContext http, IAntiforgery af, IConfiguration cfg, IWebHostEnvironment env, PlataformaServico plat, ContaServico conta) =>
        {
            if (!await af.IsRequestValidAsync(http)) return Results.Redirect("/plataforma/ativar?erro=sessao");
            var f = await http.Request.ReadFormAsync();
            var codigo = cfg["CodigoPlataforma"] ?? cfg["CodigoConfiguracaoInicial"];
            if (string.IsNullOrWhiteSpace(codigo) && !env.IsDevelopment()) return Results.Redirect("/plataforma/ativar?erro=sem-codigo");
            if (!string.IsNullOrWhiteSpace(codigo) && !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(f["codigo"].ToString()), System.Text.Encoding.UTF8.GetBytes(codigo)))
                return Results.Redirect("/plataforma/ativar?erro=codigo");
            await plat.AtivarAdministradorAsync();
            return await Renovar(http, conta, "/plataforma/construtoras");
        }).DisableAntiforgery().RequireAuthorization().RequireRateLimiting(PoliticaLogin);

        // Suporte: vínculo temporário na construtora (motivo na auditoria) e troca para ela.
        app.MapPost("/plataforma/suporte", async (HttpContext http, IAntiforgery af, PlataformaServico plat, ContaServico conta) =>
        {
            if (!await af.IsRequestValidAsync(http)) return Results.Redirect("/plataforma/construtoras?erro=sessao");
            var f = await http.Request.ReadFormAsync();
            if (!Guid.TryParse(f["empresa"], out var empresa) || !Guid.TryParse(http.User.FindFirst(ClaimsQh.Conta)?.Value, out var contaId)) return Results.Redirect("/plataforma/construtoras");
            Guid id;
            try { id = await plat.SuporteAsync(empresa, f["motivo"].ToString()); }
            catch (ValidacaoException v) { return Results.Redirect("/plataforma/construtoras?erro=" + Uri.EscapeDataString(string.Join(" ", v.Erros))); }
            catch (Domain.Aprovacoes.RegraDeNegocioException e) { return Results.Redirect("/plataforma/construtoras?erro=" + Uri.EscapeDataString(e.Message)); }
            if (await conta.AcessoAsync(contaId, id) is not { } a) return Results.Redirect("/plataforma/construtoras?erro=" + Uri.EscapeDataString("Acesso de suporte indisponível."));
            await EntrarAsync(http, a.Usuario, a.Empresa, false, contaId, a.AdministradorPlataforma, a.Acessos);
            return Results.Redirect("/");
        }).DisableAntiforgery().RequireAuthorization();

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

        // ───── Área do Fornecedor: código de acesso ─────
        app.MapPost("/fornecedor/codigo", async (HttpContext http, IAntiforgery af, AcessoFornecedorServico acesso) =>
        {
            if (!await af.IsRequestValidAsync(http)) return Results.Redirect("/fornecedor?erro=" + Uri.EscapeDataString("A página ficou aberta por muito tempo. Tente de novo."));
            var f = await http.Request.ReadFormAsync();
            var doc = f["doc"].ToString(); var volta = RetornoFornecedor(f["volta"]);
            // Opção escolhida = "<contato>|<canal>" (um rádio por contato e canal; funciona sem JavaScript).
            var sel = f["sel"].ToString().Split('|');
            var contato = Guid.TryParse(sel[0], out var cid) ? cid : Guid.Empty;
            var canal = sel.Length > 1 && sel[1] == "whatsapp" ? Domain.Cadastros.CanalCodigo.WhatsApp : Domain.Cadastros.CanalCodigo.Email;
            try
            {
                var destino = await acesso.EnviarCodigoAsync(doc, contato, canal, http.Connection.RemoteIpAddress?.ToString(), http.RequestAborted);
                return Results.Redirect($"/fornecedor?doc={Uri.EscapeDataString(doc)}&enviado={Uri.EscapeDataString(destino)}&volta={Uri.EscapeDataString(volta)}");
            }
            catch (Domain.Aprovacoes.RegraDeNegocioException e)
            { return Results.Redirect($"/fornecedor?doc={Uri.EscapeDataString(doc)}&volta={Uri.EscapeDataString(volta)}&erro={Uri.EscapeDataString(e.Message)}"); }
        }).DisableAntiforgery().RequireRateLimiting(LimiteFornecedor);

        app.MapPost("/fornecedor/entrar", async (HttpContext http, IAntiforgery af, AcessoFornecedorServico acesso) =>
        {
            if (!await af.IsRequestValidAsync(http)) return Results.Redirect("/fornecedor?erro=" + Uri.EscapeDataString("A página ficou aberta por muito tempo. Tente de novo."));
            var f = await http.Request.ReadFormAsync();
            var doc = f["doc"].ToString(); var volta = RetornoFornecedor(f["volta"]);
            try
            {
                var id = await acesso.EntrarAsync(doc, f["codigo"], http.RequestAborted);
                var ident = new ClaimsIdentity([new(ClaimsFornecedor.Documento, id.Documento), new(ClaimsFornecedor.Destino, id.DestinoVerificado),
                                                new(ClaimTypes.Name, id.DestinoVerificado)], EsquemaFornecedor);
                await http.SignInAsync(EsquemaFornecedor, new ClaimsPrincipal(ident));
                return Results.Redirect(volta);
            }
            catch (Domain.Aprovacoes.RegraDeNegocioException e)
            {
                return Results.Redirect($"/fornecedor?doc={Uri.EscapeDataString(doc)}&enviado={Uri.EscapeDataString(f["enviado"].ToString())}&volta={Uri.EscapeDataString(volta)}&erro={Uri.EscapeDataString(e.Message)}");
            }
        }).DisableAntiforgery().RequireRateLimiting(LimiteFornecedor);

        app.MapGet("/fornecedor/sair", async (HttpContext http) =>
        {
            await http.SignOutAsync(EsquemaFornecedor);
            return Results.Redirect("/fornecedor?saiu=1");
        });

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

/// <summary>
/// Autenticação do circuito do Blazor: junta a identidade do login interno e a da Área do Fornecedor (se houver).
/// Só autentica; desafio/negação ficam com os cookies de cada área.
/// </summary>
public sealed class AutenticacaoCircuito(IOptionsMonitor<AuthenticationSchemeOptions> o, ILoggerFactory l, System.Text.Encodings.Web.UrlEncoder e)
    : AuthenticationHandler<AuthenticationSchemeOptions>(o, l, e)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var interno = await Context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var fornecedor = await Context.AuthenticateAsync(Autenticacao.EsquemaFornecedor);
        var ids = new[] { interno, fornecedor }.Where(r => r.Succeeded).SelectMany(r => r.Principal!.Identities).ToList();
        if (ids.Count == 0) return AuthenticateResult.NoResult();
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(ids), Scheme.Name));
    }
}
