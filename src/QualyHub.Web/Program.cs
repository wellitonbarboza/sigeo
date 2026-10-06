using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Infrastructure;
using QualyHub.Application.Abstracoes;
using QualyHub.Infrastructure.Dados;
using QualyHub.Infrastructure.Servicos;
using QualyHub.Web.Components;
using QualyHub.Web.Seguranca;
using QualyHub.Web.Servicos;

var builder = WebApplication.CreateBuilder(args);

// Produção: segredos (CpfHmacChave, etc.) vêm do Key Vault via Managed Identity.
if (builder.Configuration["QualyHub:KeyVaultUri"] is { Length: > 0 } kvUri)
    builder.Configuration.AddAzureKeyVault(new Uri(kvUri), new Azure.Identity.DefaultAzureCredential());

var cs = builder.Configuration.GetConnectionString("QualyHub")
         ?? throw new InvalidOperationException("Configure ConnectionStrings:QualyHub (produção: Managed Identity — Authentication=Active Directory Default).");

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpContextAccessor();
Autenticacao.Configurar(builder);

builder.Services.AddScoped<UsuarioAtualWeb>();
builder.Services.AddScoped<IUsuarioAtual>(sp => sp.GetRequiredService<UsuarioAtualWeb>());
// Chave HMAC do CPF (LGPD): obrigatória fora de desenvolvimento — sem ela os hashes mudariam a cada reinício.
var chaveCpfTexto = builder.Configuration["CpfHmacChave"] ?? builder.Configuration["QualyHub:CpfHmacChave"];
byte[]? chaveCpf = string.IsNullOrWhiteSpace(chaveCpfTexto) ? null : Convert.FromBase64String(chaveCpfTexto);
if (chaveCpf is null && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Configure o segredo CpfHmacChave (Key Vault) antes de iniciar fora de desenvolvimento.");
builder.Services.AddQualyHubInfra(cs, chaveCpf);

// Arquivos: Azure Blob (Managed Identity) quando QualyHub:BlobUri existe; senão pasta local (desenvolvimento).
if (builder.Configuration["QualyHub:BlobUri"] is { Length: > 0 } blobUri)
    builder.Services.AddSingleton<IArmazenamento>(new ArmazenamentoBlob(new Uri(blobUri)));
else
    builder.Services.AddSingleton<IArmazenamento>(new ArmazenamentoLocal(
        builder.Configuration["Armazenamento:PastaLocal"] is { Length: > 0 } pasta ? pasta : Path.Combine(builder.Environment.ContentRootPath, "App_Data", "arquivos")));
builder.Services.AddQualyHubComunicacao(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddScoped<Notificacoes>();
builder.Services.AddScoped<Catalogos>();
builder.Services.AddSingleton<RotinaCadastros>();
builder.Services.AddScoped<EstadoGrades>();
// Endpoints HTTP (downloads, GRD, configuração inicial, /api) respondem erro em Problem Details (RFC 9457).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorErrosApi>();

var app = builder.Build();

// Banco: aplica migrations automaticamente só em desenvolvimento (produção: pipeline/deploy.sh).
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Banco:MigrarAoIniciar"))
{
    await using var db = app.Services.GetRequiredService<IFabricaSistema>().Criar(Guid.Empty, "Migracao");
    await db.Database.MigrateAsync();
    // Carga padrão é idempotente: empresas já existentes recebem as listas e cadastros novos de cada versão.
    // Uma vez por versão da carga; trava de aplicação no SQL evita duas instâncias aplicando ao mesmo tempo;
    // falha numa empresa é registrada e não impede o app de subir.
    var fs = app.Services.GetRequiredService<IFabricaSistema>();
    var log = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("CargaPadrao");
    await db.Database.OpenConnectionAsync();
    await db.Database.ExecuteSqlRawAsync("EXEC sp_getapplock @Resource = N'QualyHub:CargaPadrao', @LockMode = N'Exclusive', @LockOwner = N'Session', @LockTimeout = 60000");
    try
    {
        foreach (var id in await db.Empresas.AsNoTracking().Select(e => e.Id).ToListAsync())
        {
            try
            {
                await using var dbe = fs.Criar(id, "CargaPadrao");
                if (await CargaPadrao.AplicarSeNovaVersaoAsync(dbe, id))
                    log.LogInformation("Carga padrão v{Versao} aplicada na empresa {Empresa}.", CargaPadrao.Versao, id);
            }
            catch (Exception ex) { log.LogError(ex, "Falha ao aplicar a carga padrão na empresa {Empresa}.", id); }
        }
    }
    finally
    {
        await db.Database.ExecuteSqlRawAsync("EXEC sp_releaseapplock @Resource = N'QualyHub:CargaPadrao', @LockOwner = N'Session'");
        await db.Database.CloseConnectionAsync();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
// Interno ao /Error: erros dos endpoints viram Problem Details antes de chegar ao tratador das páginas.
app.UseWhen(ctx => ProblemasApi.EhEndpoint(ctx.Request.Path), ramo => ramo.UseExceptionHandler());
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
Autenticacao.UsarTrocaObrigatoria(app);
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();

// Primeira execução: sem empresa cadastrada → configuração inicial.
app.Use(async (ctx, next) =>
{
    var p = ctx.Request.Path.Value ?? "";
    if (p == "/saude") { await next(); return; }
    if (!p.StartsWith("/configuracao-inicial") && !p.StartsWith("/_") && !p.Contains('.') && !p.StartsWith("/portal") && !p.StartsWith("/cotacao/") && !p.StartsWith("/qr"))
    {
        await using var db = ctx.RequestServices.GetRequiredService<IFabricaSistema>().Criar(Guid.Empty, "Web");
        if (!await db.Empresas.AnyAsync()) { ctx.Response.Redirect("/configuracao-inicial"); return; }
    }
    await next();
});

app.MapStaticAssets();
app.MapGet("/saude", () => Results.Ok("ok")).AllowAnonymous(); // health check do App Service
Autenticacao.MapearEndpoints(app);
Endpoints.Mapear(app);
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
