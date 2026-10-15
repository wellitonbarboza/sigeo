using Microsoft.EntityFrameworkCore;
using Groundkey.Domain.Cadastros;

namespace Groundkey.Application.Servicos;

/// <summary>Contato do fornecedor visto pela equipe interna (sem máscara), com os canais possíveis.</summary>
public sealed record ContatoConvite(Guid Id, string Nome, string? Email, string? Celular, bool Principal);

/// <summary>
/// Convites enviados pela equipe (Suprimentos) ao fornecedor: acesso à Área do Fornecedor ou a uma cotação.
/// O convite leva só o endereço da área — o acesso continua exigindo CNPJ + código.
/// </summary>
public sealed class ConvitesFornecedorServico(IFabricaDb fabrica, IEnvioMensagens envio)
{
    public bool EmailDisponivel => envio.EmailDisponivel;
    public bool WhatsAppDisponivel => envio.WhatsAppDisponivel;

    public async Task<IReadOnlyList<ContatoConvite>> ContatosAsync(Guid fornecedorId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        if (!await db.Fornecedores.AnyAsync(f => f.Id == fornecedorId, ct)) throw new RegraDeNegocioException("Fornecedor não encontrado.");
        var cs = await db.FornecedorContatos.Where(c => c.FornecedorId == fornecedorId).OrderByDescending(c => c.Principal).ThenBy(c => c.Nome).ToListAsync(ct);
        return cs.Select(c => new ContatoConvite(c.Id, c.Nome, Destinos.Email(c.Email), Destinos.Celular(c.Celular) ?? Destinos.Celular(c.Telefone), c.Principal))
                 .Where(c => c.Email is not null || c.Celular is not null).ToList();
    }

    /// <summary>Envia o convite. <paramref name="endereco"/> é o endereço absoluto da área (ou da cotação dentro da área).</summary>
    public async Task<string> EnviarAsync(Guid fornecedorId, Guid contatoId, CanalCodigo canal, string assunto, string endereco, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(endereco, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps && !uri.IsLoopback)
            throw new RegraDeNegocioException("Endereço da Área do Fornecedor inválido.");
        await using var db = fabrica.CriarLeitura();
        var c = await db.FornecedorContatos.Where(x => x.Id == contatoId && x.FornecedorId == fornecedorId)
                        .Select(x => new { x.Nome, x.Email, x.Celular, x.Telefone }).FirstOrDefaultAsync(ct)
                ?? throw new RegraDeNegocioException("Escolha um contato do fornecedor.");
        var f = await db.Fornecedores.Where(x => x.Id == fornecedorId).Select(x => new { x.RazaoSocial, x.CpfCnpj }).FirstAsync(ct);
        var construtora = await db.Empresas.Where(e => e.Id == db.EmpresaAtualId).Select(e => e.NomeFantasia != "" ? e.NomeFantasia : e.RazaoSocial).FirstOrDefaultAsync(ct) ?? "Construtora";
        if (canal == CanalCodigo.Email)
        {
            var para = Destinos.Email(c.Email) ?? throw new RegraDeNegocioException("Este contato não tem e-mail cadastrado.");
            if (!envio.EmailDisponivel) throw new RegraDeNegocioException("Envio por e-mail não configurado.");
            var html = $"<p>Olá, {System.Net.WebUtility.HtmlEncode(c.Nome)}.</p><p><b>{System.Net.WebUtility.HtmlEncode(construtora)}</b> convida <b>{System.Net.WebUtility.HtmlEncode(f.RazaoSocial)}</b>: {System.Net.WebUtility.HtmlEncode(assunto)}.</p>" +
                       $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(endereco)}\">Acessar a Área do Fornecedor</a></p>" +
                       $"<p>Para entrar, informe o CNPJ {System.Net.WebUtility.HtmlEncode(f.CpfCnpj)} e o código que enviaremos a um dos contatos cadastrados.</p>";
            await envio.EnviarEmailAsync(para, $"{construtora}: {assunto}", $"{construtora} convida {f.RazaoSocial}: {assunto}. Acesse {endereco} e entre com o CNPJ {f.CpfCnpj} e o código enviado ao contato.", html, ct);
            return Destinos.MascararEmail(para);
        }
        var cel = Destinos.Celular(c.Celular) ?? Destinos.Celular(c.Telefone) ?? throw new RegraDeNegocioException("Este contato não tem celular cadastrado.");
        if (!envio.WhatsAppDisponivel) throw new RegraDeNegocioException("Envio por WhatsApp não configurado.");
        await envio.EnviarConviteWhatsAppAsync(cel, construtora, assunto, endereco, ct);
        return Destinos.MascararCelular(cel);
    }
}
