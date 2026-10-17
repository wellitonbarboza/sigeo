using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Tests.Integracao;

/// <summary>Área do Fornecedor: código por contato cadastrado, acesso por CNPJ/contato e cotações com situação.</summary>
[Collection(ColecaoBanco.Nome)]
public sealed class AreaFornecedorTests(BancoFixture f)
{
    /// <summary>Não envia nada: guarda o último código para o teste "digitar".</summary>
    private sealed class EnvioFalso : IEnvioMensagens
    {
        public string? UltimoCodigo { get; private set; }
        public string? UltimoDestino { get; private set; }
        public bool EmailDisponivel => true;
        public bool WhatsAppDisponivel => true;
        public Task EnviarEmailAsync(string para, string assunto, string texto, string html, CancellationToken ct = default)
        { UltimoDestino = para; UltimoCodigo = new string(assunto.Where(char.IsDigit).ToArray()); return Task.CompletedTask; }
        public Task EnviarCodigoWhatsAppAsync(string celularE164, string codigo, CancellationToken ct = default)
        { UltimoDestino = celularE164; UltimoCodigo = codigo; return Task.CompletedTask; }
        public Task EnviarConviteWhatsAppAsync(string celularE164, string construtora, string assunto, string endereco, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static string Sufixo() => Guid.NewGuid().ToString("N")[..6];

    private async Task<(Fornecedor F, FornecedorContato C)> FornecedorComContatoAsync(string? email = null, string? celular = null)
    {
        var x = new Fornecedor { CpfCnpj = Dados.Cnpj(), RazaoSocial = $"Fornecedor Ficticio {Sufixo()} Ltda", TipoFornecimento = TipoFornecimento.Material, Emails = "x@exemplo.com" };
        var c = new FornecedorContato { FornecedorId = x.Id, Nome = "Maria Exemplo Teste", Email = email ?? $"contato.{Sufixo()}@exemplo.com", Celular = celular, Principal = true };
        x.Contatos.Add(c);
        await using var db = f.Fabrica.Criar();
        db.Fornecedores.Add(x);
        await db.SaveChangesAsync();
        return (x, c);
    }

    private async Task<Cotacao> CotacaoAsync(Guid fornecedorId, StatusCotacao status, StatusConvite convite)
    {
        var obra = await Dados.CriarObraAsync(f);
        await using var db = f.Fabrica.Criar();
        var c = new Cotacao { ObraId = obra, Numero = Random.Shared.Next(100_000, 999_999), Nome = $"Cotação fictícia {Sufixo()}", Status = status, Tipo = TipoRequisicao.Material };
        c.Fornecedores.Add(new CotacaoFornecedor { CotacaoId = c.Id, FornecedorId = fornecedorId, Status = convite });
        db.Cotacoes.Add(c);
        await db.SaveChangesAsync();
        return c;
    }

    [FatoComBanco]
    public async Task Codigo_enviado_ao_contato_escolhido_entra_uma_vez_so()
    {
        var (forn, contato) = await FornecedorComContatoAsync(celular: "(41) 99876-5432");
        var envio = new EnvioFalso();
        var s = f.AcessoFornecedor(envio);

        var contatos = await s.ContatosAsync(forn.CpfCnpj);
        var lista = Assert.Single(contatos);
        Assert.Equal("Maria T.", lista.Nome);
        Assert.Contains("•••", lista.Email);
        Assert.EndsWith("5432", lista.Celular);

        var mascarado = await s.EnviarCodigoAsync(forn.CpfCnpj, contato.Id, CanalCodigo.WhatsApp, "10.0.0.1");
        Assert.Equal("+5541998765432", envio.UltimoDestino);
        Assert.EndsWith("5432", mascarado);

        var id = await s.EntrarAsync(forn.CpfCnpj, envio.UltimoCodigo);
        Assert.Equal("+5541998765432", id.DestinoVerificado);
        // Uso único: o mesmo código não entra de novo.
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => s.EntrarAsync(forn.CpfCnpj, envio.UltimoCodigo));
    }

    [FatoComBanco]
    public async Task Codigo_errado_conta_tentativas_e_bloqueia()
    {
        var (forn, contato) = await FornecedorComContatoAsync();
        var envio = new EnvioFalso();
        var s = f.AcessoFornecedor(envio);
        await s.EnviarCodigoAsync(forn.CpfCnpj, contato.Id, CanalCodigo.Email, null);
        var errado = envio.UltimoCodigo == "000000" ? "111111" : "000000";
        for (var i = 1; i <= CodigoAcessoFornecedor.MaxTentativas; i++)
            await Assert.ThrowsAsync<RegraDeNegocioException>(() => s.EntrarAsync(forn.CpfCnpj, errado));
        // Bloqueado: nem o código certo entra mais.
        var e = await Assert.ThrowsAsync<RegraDeNegocioException>(() => s.EntrarAsync(forn.CpfCnpj, envio.UltimoCodigo));
        Assert.Contains("bloqueado", e.Message);
    }

    [FatoComBanco]
    public async Task Contato_de_outro_fornecedor_nao_recebe_codigo_e_limite_por_hora()
    {
        var (a, _) = await FornecedorComContatoAsync();
        var (_, contatoB) = await FornecedorComContatoAsync();
        var envio = new EnvioFalso();
        var s = f.AcessoFornecedor(envio);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => s.EnviarCodigoAsync(a.CpfCnpj, contatoB.Id, CanalCodigo.Email, null));

        var ca = (await s.ContatosAsync(a.CpfCnpj)).Single().ContatoId;
        for (var i = 0; i < AcessoFornecedorServico.MaxCodigosPorDocumentoHora; i++) await s.EnviarCodigoAsync(a.CpfCnpj, ca, CanalCodigo.Email, null);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => s.EnviarCodigoAsync(a.CpfCnpj, ca, CanalCodigo.Email, null));
    }

    [FatoComBanco]
    public async Task Acesso_inclui_outro_cnpj_do_mesmo_contato_e_lista_situacao_das_cotacoes()
    {
        var email = $"grupo.{Sufixo()}@exemplo.com";
        var (matriz, contato) = await FornecedorComContatoAsync(email);
        var (filial, _) = await FornecedorComContatoAsync(email.ToUpperInvariant());
        var (outro, _) = await FornecedorComContatoAsync();

        var envio = new EnvioFalso();
        var s = f.AcessoFornecedor(envio);
        await s.EnviarCodigoAsync(matriz.CpfCnpj, contato.Id, CanalCodigo.Email, null);
        var id = await s.EntrarAsync(matriz.CpfCnpj, envio.UltimoCodigo);

        var acessos = await s.AcessosAsync(id);
        Assert.Equal(new[] { matriz.Id, filial.Id }.Order(), acessos.Select(x => x.FornecedorId).Order());

        var naoEnviada = await CotacaoAsync(filial.Id, StatusCotacao.AguardandoPropostas, StatusConvite.Convidado);
        var enviada = await CotacaoAsync(filial.Id, StatusCotacao.EmAnalise, StatusConvite.Respondido);
        var encerrada = await CotacaoAsync(filial.Id, StatusCotacao.Aprovado, StatusConvite.Respondido);
        var cots = (await s.CotacoesAsync(id, filial.Id)).ToDictionary(c => c.CotacaoId);
        Assert.Equal(("Convite recebido", false, true), (cots[naoEnviada.Id].Situacao, cots[naoEnviada.Id].Enviada, cots[naoEnviada.Id].PodeEditar));
        Assert.Equal(("Proposta enviada", true, true), (cots[enviada.Id].Situacao, cots[enviada.Id].Enviada, cots[enviada.Id].PodeEditar));
        Assert.False(cots[encerrada.Id].PodeEditar);
        Assert.Equal(SituacaoProposta.NaoVencedora, cots[encerrada.Id].Codigo); // sem item vencido

        // Convite: só do fornecedor convidado e só para quem tem acesso a ele.
        Assert.NotEqual(Guid.Empty, await s.ConviteAsync(id, filial.Id, naoEnviada.Id));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => s.ConviteAsync(id, matriz.Id, naoEnviada.Id));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => s.CotacoesAsync(id, outro.Id));
        Assert.False(string.IsNullOrEmpty(await s.SessaoPortalAsync(id, filial.Id)));
    }
}
