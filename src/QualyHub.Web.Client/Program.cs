using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using QualyHub.Web.Client.Servicos;
using QualyHub.Web.Servicos;

// Telas pesadas que rodam no navegador. O servidor manda as claims do usuário junto com a página
// (AddAuthenticationStateSerialization) e os dados vêm da API do mesmo site, com o mesmo cookie de login.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<Api>();
builder.Services.AddScoped<Notificacoes>();
builder.Services.AddScoped<IContadorMensagens, ContadorMensagensApi>();
await builder.Build().RunAsync();
