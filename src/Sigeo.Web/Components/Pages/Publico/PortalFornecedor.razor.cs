using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Sigeo.Contratos;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Common;
using Sigeo.Domain.Documentos;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Qualidade;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Sst;
using Sigeo.Domain.Suprimentos;
using Sigeo.Infrastructure.Servicos;
using Sigeo.Web.Components.Base;
using Sigeo.Web.Components.Layout;
using Sigeo.Web.Components.Shared;
using Sigeo.Web.Components;
using Sigeo.Web.Seguranca;
using Sigeo.Web.Servicos;
using Sigeo.Web;
using static Microsoft.AspNetCore.Components.Web.RenderMode;
using static Sigeo.Web.Servicos.Rotulos;

namespace Sigeo.Web.Components.Pages.Publico;

// Código da tela PortalFornecedor.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class PortalFornecedor
{
    [Parameter] public string Token { get; set; } = "";
    /// <summary>Dentro da Área do Fornecedor (sessão por código): sem a moldura e os dados do link.</summary>
    [Parameter] public bool NaArea { get; set; }
    /// <summary>Área do Fornecedor: mostra só uma aba (empresa, func, dados) sem o cabeçalho e as abas internas.</summary>
    [Parameter] public string? Aba { get; set; }
    private ColaboradorForm? _form;
    private IReadOnlyList<VersaoDocumentoPortal>? _versoes;
    private string? _versoesNome;
    private HashSet<Guid> _avisos = [];

    protected override void OnParametersSet() { if (Aba is not null) _aba = Aba; }

    private async Task Versoes(DocumentoSolicitado d) { _tipo = null; _versoesNome = d.Nome; _versoes = await Portal.VersoesDocumentoAsync(Token, d.TipoId); }

    private void NovoColab(ColaboradorPortal? c)
    {
        _msg = null; _colab = null;
        _form = c is null
            ? new ColaboradorForm { Admissao = DateOnly.FromDateTime(DateTime.Today), ContratoId = _s!.Contratos.Count == 1 ? _s.Contratos[0].Id : null }
            : new ColaboradorForm { Id = c.Id, Nome = c.Nome, FuncaoId = c.FuncaoId, Admissao = c.Admissao, Desligamento = c.Desligamento, ContratoId = c.ContratoId };
    }

    private async Task SalvarColab()
    {
        _enviando = true;
        try
        {
            await Portal.SalvarColaboradorAsync(Token, _form!);
            _ok = true; _msg = $"{_form!.Nome} salvo. Aguarde a validação da construtora e envie os documentos (ASO, NR, ficha de EPI)."; _form = null;
            await Abrir();
        }
        catch (ValidacaoException v) { _ok = false; _msg = string.Join(" ", v.Erros); }
        catch (RegraDeNegocioException e) { _ok = false; _msg = e.Message; }
        finally { _enviando = false; }
    }

    private async Task SalvarAvisos()
    {
        _enviando = true;
        try { await Portal.DefinirAvisosAsync(Token, _avisos.ToList()); _ok = true; _msg = "Contatos dos avisos salvos."; await Abrir(); }
        catch (RegraDeNegocioException e) { _ok = false; _msg = e.Message; }
        finally { _enviando = false; }
    }
    private SessaoPortal? _s;
    private bool _carregando = true, _enviando, _ok;
    private DocumentoSolicitado? _tipo;
    private string? _numero, _responsavel, _msg;
    private DateOnly? _emissao, _validade;
    private IBrowserFile? _arquivo;
    private string? _ip;
    private string _aba = "empresa";
    private ColaboradorPortal? _colab;
    private Guid? _exig;
    private DateOnly _emissaoC = DateOnly.FromDateTime(DateTime.Today);

    private async Task EnviarColab()
    {
        if (_arquivo is null || _exig is null) { _ok = false; _msg = "Escolha o documento e o arquivo."; return; }
        _enviando = true;
        try
        {
            await using var s = _arquivo.OpenReadStream(15 * 1024 * 1024);
            await Portal.ReceberDocColaboradorAsync(Token, _colab!.Id, _exig.Value, _emissaoC, _validade, _responsavel ?? "", s, _arquivo.Name, _arquivo.ContentType);
            _ok = true; _msg = $"Documento de {_colab.Nome} enviado. Aguarde a validação do SST."; _colab = null;
            await Abrir();
        }
        catch (RegraDeNegocioException e) { _ok = false; _msg = e.Message; }
        catch (IOException) { _ok = false; _msg = "Arquivo acima de 15 MB."; }
        finally { _enviando = false; }
    }

    protected override async Task OnInitializedAsync()
    {
        _ip = Http.HttpContext?.Connection.RemoteIpAddress?.ToString();
        await Abrir();
    }

    private async Task Abrir()
    {
        _s = await Portal.AbrirAsync(Token, _ip); _carregando = false;
        if (_s is null) return;
        var f = _s.Fornecedor;
        var c = f.Contatos.FirstOrDefault(x => x.Principal) ?? f.Contatos.FirstOrDefault();
        _d = new DadosPortal
        {
            NomeFantasia = f.NomeFantasia, InscricaoEstadual = f.InscricaoEstadual, InscricaoMunicipal = f.InscricaoMunicipal, Cep = f.Cep, Endereco = f.Endereco, Numero = f.Numero,
            Complemento = f.Complemento, Bairro = f.Bairro, Cidade = f.Cidade, Uf = f.Uf, Telefones = f.Telefones, Emails = f.Emails, Site = f.Site,
            ContatoNome = c?.Nome, ContatoEmail = c?.Email, ContatoTelefone = c?.Telefone
        };
        _avisos = f.Contatos.Where(x => x.RecebeAvisos).Select(x => x.Id).ToHashSet();
        if (Aba is null && f.PreCadastro && _aba == "empresa" && string.IsNullOrWhiteSpace(f.Endereco)) _aba = "dados";
    }

    private void Escolher(DocumentoSolicitado d) { _tipo = d; _numero = null; _emissao = null; _validade = null; _arquivo = null; _msg = null; _comp = d.Competencia; }

    private DateOnly? _comp;
    private DadosPortal _d = new();

    private void DefinirComp(ChangeEventArgs e) => _comp = LerMes(e.Value);

    private async Task SalvarDados()
    {
        _enviando = true;
        try
        {
            await Portal.AtualizarDadosAsync(Token, _d);
            _ok = true; _msg = "Dados salvos. Agora envie os documentos na aba Documentos da empresa."; if (Aba is null) _aba = "empresa";
            await Abrir();
        }
        catch (ValidacaoException v) { _ok = false; _msg = string.Join(" ", v.Erros); }
        catch (RegraDeNegocioException e) { _ok = false; _msg = e.Message; }
        finally { _enviando = false; }
    }

    private async Task Enviar()
    {
        if (_arquivo is null) { _ok = false; _msg = "Selecione o arquivo."; return; }
        _enviando = true;
        try
        {
            await using var s = _arquivo.OpenReadStream(15 * 1024 * 1024);
            await Portal.ReceberAsync(Token, _tipo!.TipoId, _numero, _emissao, _validade, _responsavel ?? "", s, _arquivo.Name, _arquivo.ContentType, default, _comp);
            _ok = true; _msg = $"'{_tipo.Nome}' enviado. Aguarde a validação."; _tipo = null;
            await Abrir();
        }
        catch (RegraDeNegocioException e) { _ok = false; _msg = e.Message; }
        catch (IOException) { _ok = false; _msg = "Arquivo acima de 15 MB."; }
        finally { _enviando = false; }
    }
}
