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
using Sigeo.Application.Comum;
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

namespace Sigeo.Web.Components.Pages.Suprimentos;

// Código da tela Cotacoes.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Cotacoes
{
    private sealed record Linha(Guid Id, int Numero, string Nome, string Obra, TipoRequisicao Tipo, int Recebidas, int Exigidas, decimal Valor, string? Vencedor, StatusCotacao Status, string CriadoPor, DateTimeOffset CriadoEm);
    private Grade<Linha>? _grade;
    private List<Linha>? _itens;
    private Cotacao? _c;
    private bool _aberto;
    private int _versao;
    private ParametrosVigentes? _p;
    private Dictionary<Guid, string> _nomes = [];
    private List<string> _condicoes = [];

    private bool Editavel => _c is { Status: StatusCotacao.Aberto or StatusCotacao.AguardandoPropostas or StatusCotacao.EmAnalise or StatusCotacao.ReprovadoVoltaCotacao };
    private bool EmAndamento => _c is not null && SincronizacaoCadastros.CotacoesEmAndamento.Contains(_c.Status);
    private string Nome(Guid id) => _nomes.GetValueOrDefault(id, "?");
    private decimal Total(Guid f) => _c!.Itens.Where(i => i.FornecedorId == f).Sum(i => i.ValorTotal) + (_c.Fornecedores.First(x => x.FornecedorId == f).ValorFrete ?? 0);
    private void Recalcular() => _c!.Recalcular(_p!);

    /// <summary>Itens do vencedor (ou, sem vencedor, o menor preço de cada item) — base das 3 economias.</summary>
    private EconomiaCotacao Econ => EconomiaCotacao.Calcular(_c!.FornecedorVencedorId is { } v
        ? _c.Itens.Where(i => i.FornecedorId == v)
        : _c.Itens.Where(i => i.ValorUnitarioFinal > 0).GroupBy(i => i.RequisicaoItemId).Select(g => g.OrderBy(i => i.ValorTotal).First()));

    // Convite pela Área do Fornecedor: o endereço não leva chave; o fornecedor entra com CNPJ + código.
    // Links antigos (/cotacao/{chave}) continuam valendo até a cotação fechar.
    private CotacaoFornecedor? _convidado;
    private bool _abertoConvite;
    private string EnderecoConvite => _convidado is null ? "" : $"{Nav.BaseUri}fornecedor/cotacao/{_c!.Id}?f={_convidado.FornecedorId}";
    private void Convidar(CotacaoFornecedor f) { _convidado = f; _abertoConvite = true; }
    private void Convidou()
    {
        if (_convidado is { Status: StatusConvite.Convidado } && _c!.Status == StatusCotacao.Aberto) _c.Status = StatusCotacao.AguardandoPropostas;
    }

    protected override async Task OnInitializedAsync()
    {
        await using (var db = Fabrica.CriarLeitura()) _p = await ParametrosServico.CarregarAsync(db, Cancelamento);
        _condicoes = await Cat.ListaAsync(TipoLista.CondicaoPagamento);
        await Carregar();
        if (Nav.Uri.Contains("?id=") && Guid.TryParse(Nav.Uri.Split("?id=")[1], out var id)) await Abrir(id);
    }

    private async Task Carregar()
    {
        await using var db = Fabrica.CriarLeitura();
        _itens = await db.Cotacoes.AsNoTracking().OrderByDescending(c => c.Numero).Take(LimiteAtual)
            .Select(c => new Linha(c.Id, c.Numero, c.Nome, db.Obras.Where(o => o.Id == c.ObraId).Select(o => o.Nome).FirstOrDefault() ?? "", c.Tipo,
                c.PropostasRecebidas, c.PropostasExigidas, c.ValorEstimado, c.FornecedorVencedor != null ? c.FornecedorVencedor.RazaoSocial : null, c.Status, c.CriadoPor, c.CriadoEm)).ToListAsync(Cancelamento);
    }

    private Dictionary<Guid, List<LocalQuantidade>> _locaisItem = [];
    private readonly HashSet<Guid> _abertos = [];
    private void AlternarLocais(Guid id) { if (!_abertos.Remove(id)) _abertos.Add(id); }

    // Incluir / retirar fornecedor com a cotação em andamento (grava o que está na tela antes).
    private bool _abertoAdd;
    private string? _filtroAdd;
    private List<Opcao> _paraAdicionar = [];
    private HashSet<Guid> _sugeridosAdd = [];
    private readonly HashSet<Guid> _selAdd = [];

    private async Task AbrirAdicionar()
    {
        await using var db = Fabrica.CriarLeitura();
        var itens = _c!.RequisicaoId is { } rq ? await db.RequisicaoItens.AsNoTracking().Where(i => i.RequisicaoId == rq).ToListAsync(Cancelamento) : [];
        var (lista, sug) = await SugestaoFornecedores.ParaAsync(db, Forn, _c.Tipo, itens, Cancelamento);
        var ja = _c.Fornecedores.Select(f => f.FornecedorId).ToHashSet();
        _paraAdicionar = lista.Where(f => !ja.Contains(f.Id)).ToList();
        _sugeridosAdd = sug; _selAdd.Clear(); _filtroAdd = null; _abertoAdd = true;
    }

    private async Task Adicionar()
    {
        if (await Executar(async () => { await CotacoesSrv.SalvarCotacaoAsync(_c!, Cancelamento); await CotacoesSrv.AdicionarFornecedoresAsync(_c!.Id, _selAdd, Cancelamento); },
                $"{_selAdd.Count} fornecedor(es) incluído(s). Use Convidar para mandar o acesso."))
        { _abertoAdd = false; await Recarregar(); }
    }

    private void Retirar(CotacaoFornecedor f) =>
        Confirmar($"Retirar {Nome(f.FornecedorId)} da cotação?{(f.Status != StatusConvite.Convidado ? " A proposta dele será descartada." : "")} Fica registrado nas notas.", async () =>
        {
            if (await Executar(async () => { await CotacoesSrv.SalvarCotacaoAsync(_c!, Cancelamento); await CotacoesSrv.RemoverFornecedorAsync(_c!.Id, f.FornecedorId, Cancelamento); }, "Fornecedor retirado."))
                await Recarregar();
        });

    private async Task Abrir(Guid id)
    {
        // Qualificação de hoje (o cadastro do fornecedor pode ter mudado desde o convite) — grava antes de ler.
        await CotacoesSrv.AtualizarQualificacoesAsync(id, Cancelamento);
        await using var db = Fabrica.CriarLeitura();
        _c = await db.Cotacoes.AsNoTracking().Include(c => c.Fornecedores).Include(c => c.Itens).FirstAsync(c => c.Id == id, Cancelamento);
        var ids = _c.Fornecedores.Select(f => f.FornecedorId).ToList();
        _nomes = await db.Fornecedores.AsNoTracking().Where(f => ids.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.NomeFantasia ?? f.RazaoSocial, Cancelamento);
        var rids = _c.Itens.Where(i => i.RequisicaoItemId != null).Select(i => i.RequisicaoItemId!.Value).Distinct().ToList();
        _locaisItem = await QuadroLocais.CarregarAsync(db, rids, Cancelamento);
        _c.Recalcular(_p!);
        await CotacoesServico.PrecosReferenciaAsync(db, _c, Cancelamento);
        ErrosForm = null; _versao++; _aberto = true;
    }

    private async Task Recarregar() { if (_c is not null) await Abrir(_c.Id); await Carregar(); }

    private async Task Salvar()
    {
        if (_c!.Status == StatusCotacao.AguardandoPropostas && _c.Fornecedores.Any(f => f.Status != StatusConvite.Convidado)) _c.Status = StatusCotacao.EmAnalise;
        if (await Executar(() => CotacoesSrv.SalvarCotacaoAsync(_c!, Cancelamento), "Cotação salva.")) await Recarregar();
    }

    /// <summary>A IA lê o mapa gravado: em edição, salva antes (sem recarregar; a análise recarrega no fim).</summary>
    private Task<bool> SalvarAntesDaIa() => Editavel ? Executar(() => CotacoesSrv.SalvarCotacaoAsync(_c!, Cancelamento)) : Task.FromResult(true);

    private async Task Enviar()
    {
        if (await Executar(async () => { await CotacoesSrv.SalvarCotacaoAsync(_c!, Cancelamento); await CotacoesSrv.EnviarCotacaoAprovacaoAsync(_c!.Id, Cancelamento); }, "Cotação enviada para aprovação.")) await Recarregar();
    }

    /// <summary>Prazo de resposta na hora local do navegador da construtora.</summary>
    private DateTime? PrazoLocal
    {
        get => _c?.PrazoResposta?.ToLocalTime().DateTime;
        set { if (_c is not null) _c.PrazoResposta = value is { } v ? new DateTimeOffset(v, Relogio.GetLocalNow().Offset) : null; }
    }

    private async Task GerarPedido()
    {
        IReadOnlyList<Guid> ids = [];
        if (await Executar(async () => ids = await CotacoesSrv.GerarPedidosAsync(_c!.Id, Cancelamento), "Pedido(s) gerado(s)."))
        { _aberto = false; Nav.NavigateTo(ids.Count == 1 ? $"pedidos?id={ids[0]}" : "pedidos"); }
    }

    private async Task GerarContrato()
    {
        IReadOnlyList<Guid> ids = [];
        if (await Executar(async () => ids = await CotacoesSrv.GerarContratosAsync(_c!.Id, Cancelamento), "Minuta(s) de contrato gerada(s)."))
        { _aberto = false; Nav.NavigateTo(ids.Count == 1 ? $"contratos?id={ids[0]}" : "contratos"); }
    }
}
