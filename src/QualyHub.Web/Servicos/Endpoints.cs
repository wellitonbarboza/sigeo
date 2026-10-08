using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Application.Abstracoes;
using QualyHub.Infrastructure.Dados;
using QualyHub.Infrastructure.Servicos;
using QualyHub.Web.Seguranca;

namespace QualyHub.Web.Servicos;

public static class Endpoints
{
    public static void Mapear(WebApplication app)
    {
        // Configuração inicial (banco vazio): cria a empresa, o administrador e aplica a carga padrão SiAC.
        app.MapPost("/configuracao-inicial/salvar", (HttpContext http, IFabricaSistema sistema, Microsoft.AspNetCore.Antiforgery.IAntiforgery af) =>
            CriarEmpresaAsync(app, http, sistema, af, novaConstrutora: false)).DisableAntiforgery().RequireRateLimiting(Autenticacao.PoliticaLogin);

        // Nova construtora (plataforma com várias empresas no mesmo banco): mesmo cadastro, protegido pelo código da plataforma.
        app.MapPost("/plataforma/nova-construtora/salvar", (HttpContext http, IFabricaSistema sistema, Microsoft.AspNetCore.Antiforgery.IAntiforgery af) =>
            CriarEmpresaAsync(app, http, sistema, af, novaConstrutora: true)).DisableAntiforgery().RequireRateLimiting(Autenticacao.PoliticaLogin);

        // Download de anexo (sempre autenticado; log de auditoria no acesso a documento do SGQ).
        app.MapGet("/arquivos/anexo/{id:guid}", async (Guid id, IFabricaDb fabrica, IArmazenamento arm) =>
        {
            await using var db = fabrica.Criar();
            var a = await db.Anexos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return a is null ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Anexo não encontrado") : Results.File(await arm.AbrirAsync(a.ArquivoBlob), a.ContentType ?? "application/octet-stream", a.NomeOriginal);
        }).RequireAuthorization();

        app.MapGet("/arquivos/nota/{id:guid}", async (Guid id, IFabricaDb fabrica, IArmazenamento arm) =>
        {
            await using var db = fabrica.CriarLeitura();
            var n = await db.NotasFiscais.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.Chave, x.ArquivoXml }).FirstOrDefaultAsync();
            return n?.ArquivoXml is null ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "XML não encontrado")
                : Results.File(await arm.AbrirAsync(n.ArquivoXml), "application/xml", $"NFe{n.Chave}.xml");
        }).RequireAuthorization();

        app.MapGet("/arquivos/fornecedor/{id:guid}", async (Guid id, IFabricaDb fabrica, IArmazenamento arm) =>
        {
            await using var db = fabrica.Criar();
            var d = await db.FornecedorDocumentos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return d?.ArquivoBlob is null ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Documento do fornecedor não encontrado") : Results.File(await arm.AbrirAsync(d.ArquivoBlob), "application/octet-stream", d.ArquivoNomeOriginal ?? "documento");
        }).RequireAuthorization();

        app.MapGet("/arquivos/sgq/{revisaoId:guid}/{tipo}", async (Guid revisaoId, string tipo, IFabricaDb fabrica, IArmazenamento arm, DocumentosServico docs) =>
        {
            await using var db = fabrica.Criar();
            var r = await db.Revisoes.Include(x => x.Documento).FirstOrDefaultAsync(x => x.Id == revisaoId);
            if (r is null) return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Revisão do documento não encontrada");
            var perm = await docs.PermissaoAsync(db, r.Documento);
            var editavel = tipo == "editavel";
            if (editavel ? !perm.Editar : !perm.Baixar) return Results.Forbid();
            if (r.Status == StatusRevisao.Obsoleta && !perm.VerObsoletos) return Results.Forbid();
            var blob = editavel ? r.ArquivoEditavelBlob : r.ArquivoPublicadoBlob;
            if (blob is null) return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Arquivo da revisão não encontrado");
            docs.Log(db, EventoDocumentoTipo.Download, r.DocumentoId, r.Id, detalhe: $"{(editavel ? "Editável" : "Publicado")} – cópia não controlada");
            await db.SaveChangesAsync();
            return Results.File(await arm.AbrirAsync(blob), editavel ? "application/octet-stream" : "application/pdf",
                (editavel ? r.ArquivoEditavelNome : r.ArquivoPublicadoNome) ?? "documento");
        }).RequireAuthorization();

        // Confirmação de recebimento da GRD (link externo, sem login).
        app.MapGet("/grd/confirmar/{token:guid}", async (Guid token, IFabricaSistema sistema) =>
        {
            await using var db = sistema.Criar(Guid.Empty, "GRD");
            var ok = await DocumentosServico.ConfirmarGrdAsync(db, token, DateTimeOffset.UtcNow);
            return Results.Content(ok ? "<h2 style='font-family:sans-serif'>Recebimento confirmado. Obrigado.</h2>" : "<h2>Link inválido.</h2>", "text/html; charset=utf-8");
        });

        // API do app de campo (versionada). Fica vazia por enquanto: os endpoints entram neste grupo
        // (var api = ...; api.MapGet/MapPost(...)) e herdam autenticação e Problem Details. Versão nova = novo grupo /api/v2,
        // mantendo o v1 enquanto houver aparelho com a versão antiga do app.
        app.MapGroup("/api/v1").RequireAuthorization().AddEndpointFilter<FiltroProblemDetails>();
    }

    /// <summary>
    /// Cria empresa + administrador + carga padrão. Primeira execução: só com o banco vazio (código CodigoConfiguracaoInicial).
    /// Nova construtora: com o banco já em uso, exige o código da plataforma (CodigoPlataforma; sem ele, o mesmo CodigoConfiguracaoInicial).
    /// Cada construtora fica isolada pelo EmpresaId (filtros globais); o login é único entre todas.
    /// </summary>
    private static async Task<IResult> CriarEmpresaAsync(WebApplication app, HttpContext http, IFabricaSistema sistema,
        Microsoft.AspNetCore.Antiforgery.IAntiforgery af, bool novaConstrutora)
    {
        var rota = novaConstrutora ? "/plataforma/nova-construtora" : "/configuracao-inicial";
        if (!await af.IsRequestValidAsync(http)) return Results.Redirect($"{rota}?erro=1");
        var f = await http.Request.ReadFormAsync();
        await using (var check = sistema.Criar(Guid.Empty, "Setup"))
        {
            var existe = await check.Empresas.AnyAsync();
            if (!novaConstrutora && existe) return Results.Redirect("/");
            if (novaConstrutora && !existe) return Results.Redirect("/configuracao-inicial");
        }
        string V(string k) => f[k].ToString().Trim();
        // Proteção: fora de desenvolvimento exige o código guardado no Key Vault — quem só conhece a URL não cria empresa.
        var codigo = novaConstrutora ? app.Configuration["CodigoPlataforma"] ?? app.Configuration["CodigoConfiguracaoInicial"] : app.Configuration["CodigoConfiguracaoInicial"];
        if (!app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(codigo)) return Results.Forbid();
        if (!string.IsNullOrWhiteSpace(codigo) && !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(V("codigoSetup")), Encoding.UTF8.GetBytes(codigo)))
            return Results.Redirect($"{rota}?erro=codigo");
        if (V("razao") == "" || V("nome") == "" || V("email") == "") return Results.Redirect($"{rota}?erro=1");
        var login = PoliticaSenha.NormalizarLogin(V("login"));
        var senha = f["senha"].ToString();
        var errosSenha = PoliticaSenha.ValidarLogin(login).Concat(PoliticaSenha.Validar(senha, login, V("nome"))).ToList();
        if (senha != f["confirmacao"].ToString()) errosSenha.Add("A confirmação não confere com a senha.");
        if (errosSenha.Count > 0) return Results.Redirect($"{rota}?erro=senha&msg=" + Uri.EscapeDataString(string.Join(" ", errosSenha)));

        var empresa = new Empresa { RazaoSocial = V("razao"), NomeFantasia = V("fantasia") is { Length: > 0 } nf ? nf : V("razao"), Cnpj = V("cnpj"), Codigo = V("codigo"), NivelSiac = V("nivel") is { Length: > 0 } n ? n : "B" };
        await using var db = sistema.Criar(empresa.Id, "Setup");
        if (novaConstrutora)
        {
            var dup = new List<string>();
            if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Login == login && !u.Excluido)) dup.Add("Login já está em uso em outra construtora.");
            var doc = new string(empresa.Cnpj.Where(char.IsDigit).ToArray());
            if (doc.Length > 0 && (await db.Empresas.IgnoreQueryFilters().Select(e => e.Cnpj).ToListAsync()).Any(c => new string((c ?? "").Where(char.IsDigit).ToArray()) == doc))
                dup.Add("Já existe construtora com este CNPJ.");
            if (dup.Count > 0) return Results.Redirect($"{rota}?erro=senha&msg=" + Uri.EscapeDataString(string.Join(" ", dup)));
        }
        db.Empresas.Add(empresa);
        var admin = new Usuario
        {
            EmpresaId = empresa.Id, Nome = V("nome"), Email = V("email"), Perfil = PerfilUsuario.Administrador, Funcao = "Coordenador da Qualidade",
            AcessoTodasObras = true, Login = login
        };
        db.Usuarios.Add(admin);
        ContaServico.DefinirSenha(db, admin, senha, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
        await CargaPadrao.AplicarSeNovaVersaoAsync(db, empresa.Id);
        if (novaConstrutora) return Results.Redirect("/plataforma/nova-construtora?ok=" + Uri.EscapeDataString(empresa.NomeFantasia));
        await Autenticacao.EntrarAsync(http, admin, empresa.NomeFantasia);
        return Results.Redirect("/");
    }
}
