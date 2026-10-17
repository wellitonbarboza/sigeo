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

namespace Sigeo.Web.Components.Pages.Cadastros;

// Código da tela Fornecedores.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Fornecedores
{
    private bool _abertoImp;
    private byte[]? _arquivoImp;
    private Sigeo.Infrastructure.Importacao.PreviaImportacaoFornecedores? _previa;
    private string _filtroImp = "Todos";

    private async Task Prever(InputFileChangeEventArgs e)
    {
        await Executar(async () =>
        {
            if (e.File.Size > 20 * 1024 * 1024) throw new RegraDeNegocioException("Planilha acima de 20 MB.");
            using var ms = new MemoryStream();
            await e.File.OpenReadStream(20 * 1024 * 1024).CopyToAsync(ms, Cancelamento);
            _arquivoImp = ms.ToArray();
            _previa = await ImpExpert.PreverAsync(new MemoryStream(_arquivoImp), ct: Cancelamento);
            _filtroImp = "Todos"; _abertoImp = true;
        });
    }

    private async Task ConfirmarImportacao()
    {
        (int Inseridos, int Atualizados) r = default;
        if (await Executar(async () => r = await ImpExpert.ImportarAsync(new MemoryStream(_arquivoImp!), ct: Cancelamento)))
        {
            Notif.Sucesso($"{r.Inseridos} fornecedor(es) incluído(s) e {r.Atualizados} atualizado(s).");
            _abertoImp = false; _previa = null; _arquivoImp = null;
            await Carregar();
        }
    }

    private Grade<Fornecedor>? _grade;
    private List<Fornecedor>? _itens;
    private Fornecedor? _f;
    private bool _aberto, _novo;
    private bool _abertoConvite;
    private string _assuntoConvite = "";
    private FornecedorContato _contatoPre = new();
    private const string AssuntoCadastro = "complete o cadastro e envie os documentos na Área do Fornecedor";
    private string EnderecoArea => $"{Nav.BaseUri}fornecedor?doc={new string((_f?.CpfCnpj ?? "").Where(char.IsDigit).ToArray())}";
    private void AbrirConvite(string assunto) { _assuntoConvite = assunto; _abertoConvite = true; }

    protected override Task OnInitializedAsync() => Carregar();

    private async Task Carregar()
    {
        _itens = await Consultas.ListarAsync(Cancelamento);
    }

    private bool _abertoPre;
    private string _abaF = "dados";
    private static readonly List<(string, string)> _abasF = [("dados", "Dados e contatos"), ("grupos", "Grupos e serviços (ITS)"), ("docs", "Documentos exigidos"), ("painel", "Painel")];
    private List<CategoriaProduto> _cats = [];
    private List<Opcao> _servicosIts = [];
    private List<TipoDocumentoExigido> _tiposDoc = [];
    private PainelFornecedor? _painel;

    private async Task CarregarApoio()
    {
        var apoio = await Consultas.ApoioAsync(Cancelamento);
        _cats = apoio.Categorias;
        _servicosIts = await Cat.ServicosAsync();
        _tiposDoc = apoio.TiposDocumento;
    }

    private async Task Novo() { await CarregarApoio(); _f = new Fornecedor(); _novo = true; _painel = null; _abaF = "dados"; ErrosForm = null; _aberto = true; }
    private async Task NovoPre()
    {
        await CarregarApoio();
        _f = new Fornecedor { PreCadastro = true, TipoFornecimento = TipoFornecimento.Material };
        _contatoPre = new FornecedorContato { FornecedorId = _f.Id, Principal = true };
        _daMatriz.Clear();
        await AplicarMatriz();
        ErrosForm = null; _abertoPre = true;
    }
    private async Task Editar(Fornecedor f)
    {
        await CarregarApoio();
        _f = await Consultas.ObterAsync(f.Id, Cancelamento);
        _painel = await Forn.PainelAsync(f.Id, ct: Cancelamento);
        _novo = false; ErrosForm = null; _aberto = true;
    }

    private async Task MarcarPre(TipoFornecimento t, bool on)
    {
        _f!.TipoFornecimento = on ? _f.TipoFornecimento | t : _f.TipoFornecimento & ~t;
        await AplicarMatriz();
    }

    /// <summary>Documentos que entraram sozinhos pela matriz (os incluídos/ajustados à mão nunca são tirados).</summary>
    private readonly HashSet<Guid> _daMatriz = [];

    /// <summary>Mescla a matriz: mantém o que já está (com os ajustes manuais), acrescenta o que falta
    /// e só retira o que a própria matriz tinha incluído e deixou de valer para o tipo de fornecimento.</summary>
    private async Task AplicarMatriz()
    {
        var f = _f!;
        var m = await Forn.MatrizAsync(f.TipoFornecimento, f.IncluirDocsFuncionarios, ct: Cancelamento);
        var novos = m.Select(t => t.Id).ToHashSet();
        foreach (var x in f.Exigencias.Where(x => _daMatriz.Contains(x.TipoDocumentoExigidoId) && !novos.Contains(x.TipoDocumentoExigidoId)).ToList())
        { f.Exigencias.Remove(x); _daMatriz.Remove(x.TipoDocumentoExigidoId); }
        var existentes = f.Exigencias.Select(x => x.TipoDocumentoExigidoId).ToHashSet();
        foreach (var t in m.Where(t => !existentes.Contains(t.Id)))
        {
            f.Exigencias.Add(new FornecedorExigencia { FornecedorId = f.Id, TipoDocumentoExigidoId = t.Id, Obrigatorio = t.Obrigatorio });
            _daMatriz.Add(t.Id);
        }
    }

    private void TrocarGrupo(FornecedorGrupo g, string? v)
    {
        var novo = Guid.TryParse(v, out var id) ? id : Guid.Empty;
        if (novo == g.CategoriaProdutoId) return;
        g.CategoriaProdutoId = novo;
        g.Subgrupos = null; // subgrupos eram do grupo anterior
    }

    private void MarcarSub(FornecedorGrupo g, Guid sub, bool on)
    {
        var l = (g.Subgrupos ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (on) l.Add(sub.ToString()); else l.Remove(sub.ToString());
        g.Subgrupos = l.Count == 0 ? null : string.Join(',', l.Distinct());
    }

    private void MarcarIts(Guid id, bool on)
    {
        if (on && _f!.Servicos.All(x => x.ServicoControladoId != id)) _f.Servicos.Add(new FornecedorServico { FornecedorId = _f.Id, ServicoControladoId = id });
        else if (!on) foreach (var x in _f!.Servicos.Where(x => x.ServicoControladoId == id).ToList()) _f.Servicos.Remove(x);
    }

    private async Task SalvarPre()
    {
        var ok = await Executar(async () =>
        {
            // O contato do pré-cadastro é quem recebe o código de acesso à Área do Fornecedor.
            if (string.IsNullOrWhiteSpace(_contatoPre.Nome)) throw new RegraDeNegocioException("Informe o nome do contato.");
            _contatoPre.Email = _f!.Emails?.Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!_f.Contatos.Contains(_contatoPre)) _f.Contatos.Add(_contatoPre);
            await Forn.SalvarAsync(_f, ct: Cancelamento);
        }, "Pré-cadastro salvo.");
        if (!ok) return;
        await Carregar();
        _abertoPre = false;
        AbrirConvite("faça o pré-cadastro: complete os dados e envie os documentos na Área do Fornecedor");
    }
    private void Marcar(TipoFornecimento t, bool on) => _f!.TipoFornecimento = on ? _f.TipoFornecimento | t : _f.TipoFornecimento & ~t;

    private async Task Salvar()
    {
        _f!.Grupos = _f.Grupos.Where(g => g.CategoriaProdutoId != Guid.Empty).ToList();
        var ok = await Executar(() => Forn.SalvarAsync(_f, ct: Cancelamento), "Fornecedor salvo.");
        if (ok) { _aberto = false; await Carregar(); }
    }
}
