using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Infrastructure.Contexto;
using QualyHub.Infrastructure.Dados;
using QualyHub.Infrastructure.Servicos;
using QualyHub.Web.Seguranca;

namespace QualyHub.Web.Servicos;

public static class Endpoints
{
    public static void Mapear(WebApplication app)
    {
        // Configuração inicial (banco vazio): cria a empresa, o administrador e aplica a carga padrão SiAC.
        app.MapPost("/configuracao-inicial/salvar", async (HttpContext http, FabricaSistema sistema, Microsoft.AspNetCore.Antiforgery.IAntiforgery af) =>
        {
            if (!await af.IsRequestValidAsync(http)) return Results.Redirect("/configuracao-inicial?erro=1");
            var f = await http.Request.ReadFormAsync();
            await using (var check = sistema.Criar(Guid.Empty, "Setup"))
                if (await check.Empresas.AnyAsync()) return Results.Redirect("/");
            string V(string k) => f[k].ToString().Trim();
            // Proteção da primeira execução: fora de desenvolvimento exige o código guardado no Key Vault
            // (segredo CodigoConfiguracaoInicial) — quem só conhece a URL não consegue "tomar" o sistema vazio.
            var codigo = app.Configuration["CodigoConfiguracaoInicial"];
            if (!app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(codigo)) return Results.Forbid();
            if (!string.IsNullOrWhiteSpace(codigo) && !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(V("codigoSetup")), Encoding.UTF8.GetBytes(codigo)))
                return Results.Redirect("/configuracao-inicial?erro=codigo");
            if (V("razao") == "" || V("nome") == "" || V("email") == "") return Results.Redirect("/configuracao-inicial?erro=1");
            var login = PoliticaSenha.NormalizarLogin(V("login"));
            var senha = f["senha"].ToString();
            var errosSenha = PoliticaSenha.ValidarLogin(login).Concat(PoliticaSenha.Validar(senha, login, V("nome"))).ToList();
            if (senha != f["confirmacao"].ToString()) errosSenha.Add("A confirmação não confere com a senha.");
            if (errosSenha.Count > 0) return Results.Redirect("/configuracao-inicial?erro=senha&msg=" + Uri.EscapeDataString(string.Join(" ", errosSenha)));

            var empresa = new Empresa { RazaoSocial = V("razao"), NomeFantasia = V("fantasia") is { Length: > 0 } nf ? nf : V("razao"), Cnpj = V("cnpj"), Codigo = V("codigo"), NivelSiac = V("nivel") is { Length: > 0 } n ? n : "B" };
            await using var db = sistema.Criar(empresa.Id, "Setup");
            db.Empresas.Add(empresa);
            var admin = new Usuario
            {
                EmpresaId = empresa.Id, Nome = V("nome"), Email = V("email"), Perfil = PerfilUsuario.Administrador, Funcao = "Coordenador da Qualidade",
                AcessoTodasObras = true, Login = login
            };
            db.Usuarios.Add(admin);
            ContaServico.DefinirSenha(db, admin, senha, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
            await CargaPadrao.AplicarAsync(db, empresa.Id);
            await Autenticacao.EntrarAsync(http, admin, empresa.NomeFantasia);
            return Results.Redirect("/");
        }).DisableAntiforgery().RequireRateLimiting(Autenticacao.PoliticaLogin);

        // Download de anexo (sempre autenticado; log de auditoria no acesso a documento do SGQ).
        app.MapGet("/arquivos/anexo/{id:guid}", async (Guid id, FabricaDb fabrica, IArmazenamento arm) =>
        {
            await using var db = fabrica.Criar();
            var a = await db.Anexos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return a is null ? Results.NotFound() : Results.File(await arm.AbrirAsync(a.ArquivoBlob), a.ContentType ?? "application/octet-stream", a.NomeOriginal);
        }).RequireAuthorization();

        app.MapGet("/arquivos/fornecedor/{id:guid}", async (Guid id, FabricaDb fabrica, IArmazenamento arm) =>
        {
            await using var db = fabrica.Criar();
            var d = await db.FornecedorDocumentos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return d?.ArquivoBlob is null ? Results.NotFound() : Results.File(await arm.AbrirAsync(d.ArquivoBlob), "application/octet-stream", d.ArquivoNomeOriginal ?? "documento");
        }).RequireAuthorization();

        app.MapGet("/arquivos/sgq/{revisaoId:guid}/{tipo}", async (Guid revisaoId, string tipo, FabricaDb fabrica, IArmazenamento arm, DocumentosServico docs) =>
        {
            await using var db = fabrica.Criar();
            var r = await db.Revisoes.Include(x => x.Documento).FirstOrDefaultAsync(x => x.Id == revisaoId);
            if (r is null) return Results.NotFound();
            var perm = await docs.PermissaoAsync(db, r.Documento);
            var editavel = tipo == "editavel";
            if (editavel ? !perm.Editar : !perm.Baixar) return Results.Forbid();
            if (r.Status == StatusRevisao.Obsoleta && !perm.VerObsoletos) return Results.Forbid();
            var blob = editavel ? r.ArquivoEditavelBlob : r.ArquivoPublicadoBlob;
            if (blob is null) return Results.NotFound();
            docs.Log(db, EventoDocumentoTipo.Download, r.DocumentoId, r.Id, detalhe: $"{(editavel ? "Editável" : "Publicado")} – cópia não controlada");
            await db.SaveChangesAsync();
            return Results.File(await arm.AbrirAsync(blob), editavel ? "application/octet-stream" : "application/pdf",
                (editavel ? r.ArquivoEditavelNome : r.ArquivoPublicadoNome) ?? "documento");
        }).RequireAuthorization();

        // Confirmação de recebimento da GRD (link externo, sem login).
        app.MapGet("/grd/confirmar/{token:guid}", async (Guid token, FabricaSistema sistema) =>
        {
            await using var db = sistema.Criar(Guid.Empty, "GRD");
            var ok = await DocumentosServico.ConfirmarGrdAsync(db, token, DateTimeOffset.UtcNow);
            return Results.Content(ok ? "<h2 style='font-family:sans-serif'>Recebimento confirmado. Obrigado.</h2>" : "<h2>Link inválido.</h2>", "text/html; charset=utf-8");
        });
    }
}
