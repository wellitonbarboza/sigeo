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
var cs = builder.Configuration.GetConnectionString("QualyHub")
         ?? throw new InvalidOperationException("Configure ConnectionStrings:QualyHub (produção: Managed Identity — Authentication=Active Directory Default).");

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpContextAccessor();
Autenticacao.Configurar(builder);

builder.Services.AddScoped<UsuarioAtualWeb>();
builder.Services.AddScoped<IUsuarioAtual>(sp => sp.GetRequiredService<UsuarioAtualWeb>());
builder.Services.AddQualyHubInfra(cs);
builder.Services.AddSingleton<IArmazenamento>(new ArmazenamentoLocal(
    builder.Configuration["Armazenamento:PastaLocal"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "arquivos")));
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
app.UseAuthorization();
app.UseAntiforgery();

// Primeira execução: sem empresa cadastrada → configuração inicial.
app.Use(async (ctx, next) =>
{
    var p = ctx.Request.Path.Value ?? "";
    if (!p.StartsWith("/configuracao-inicial") && !p.StartsWith("/_") && !p.Contains('.') && !p.StartsWith("/portal") && !p.StartsWith("/qr"))
    {
        await using var db = ctx.RequestServices.GetRequiredService<FabricaSistema>().Criar(Guid.Empty, "Web");
        if (!await db.Empresas.AnyAsync()) { ctx.Response.Redirect("/configuracao-inicial"); return; }
    }
    await next();
});

app.MapStaticAssets();
Autenticacao.MapearEndpoints(app);
Endpoints.Mapear(app);
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
