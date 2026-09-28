using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Infrastructure;
using QualyHub.Infrastructure.Contexto;
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
builder.Services.AddScoped<Notificacoes>();
builder.Services.AddScoped<Catalogos>();

var app = builder.Build();

// Banco: aplica migrations automaticamente só em desenvolvimento (produção: pipeline/deploy.sh).
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Banco:MigrarAoIniciar"))
{
    await using var db = app.Services.GetRequiredService<FabricaSistema>().Criar(Guid.Empty, "Migracao");
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
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
        await using var db = ctx.RequestServices.GetRequiredService<FabricaSistema>().Criar(Guid.Empty, "Web");
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
