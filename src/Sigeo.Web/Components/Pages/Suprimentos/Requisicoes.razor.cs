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

// Código da tela Requisicoes.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Requisicoes
{
    [Inject] private RequisicoesConsultas Consultas { get; set; } = default!;
    private Grade<RequisicaoLinha>? _grade;
    private List<RequisicaoLinha>? _itens;
    private Requisicao? _r;
    private bool _aberto, _cotar;
    private int _versao;
    private List<Opcao> _spes = [];
    private IEnumerable<Opcao> SpesDaObra => _r is null ? [] : _spes.Where(s => s.Extra == _r.ObraId.ToString());
    private List<Opcao> _obras = [], _ccs = [], _servicos = [], _fornecedores = [], _estabs = [], _grupos = [];
    private List<ServicoOrcado> _compsOrc = [];
    private List<ProdutoBusca> _produtos = [];
    private string? _grupoFiltro;
    private bool _abertoOrc;
    private int _compsObra;
    private readonly HashSet<Guid> _selComp = [];
    private HashSet<Guid> _selLoc = [];
    private List<LinhaDemanda>? _demanda;
    private HashSet<Guid> _sugeridos = [];
    private bool TemLocais => _r?.Itens.Any(i => i.Locais.Count > 0) == true;
    private List<Local> _locaisObraLista = [];
    private Dictionary<Guid, string> _nomeLocal = [];
    /// <summary>Locais da obra da requisição; ao trocar a lista, refaz o dicionário de nomes.</summary>
    private List<Local> _locaisObra { get => _locaisObraLista; set { _locaisObraLista = value; _locaisSel = value.Select(LocalOrcamento.De).ToList(); _nomeLocal = value.ToDictionary(l => l.Id, l => l.Codigo); _arvore = new ArvoreLocais(value); } }
    private List<LocalOrcamento> _locaisSel = [];
    private ArvoreLocais _arvore = ArvoreLocais.Vazia;
    private readonly HashSet<Guid> _abertos = [];
    private void AlternarLocais(Guid id) { if (!_abertos.Remove(id)) _abertos.Add(id); }
    private int _versaoObra;
    private List<string> _atividades = [], _categorias = [];
    private readonly HashSet<Guid> _convidados = [];

    private bool Editavel => _r is { Status: StatusRequisicao.Rascunho or StatusRequisicao.DevolvidaAjuste };
    /// <summary>Autorizada e ainda com saldo para comprar (várias cotações/pedidos até atender).</summary>
    private bool EmCompra => _r is { Status: StatusRequisicao.Autorizada or StatusRequisicao.EmCotacao or StatusRequisicao.PedidoEmitido or StatusRequisicao.ParcialmenteAtendida };
    private bool EmAtendimento => _r is not null && (EmCompra || _r.Status is StatusRequisicao.Atendida or StatusRequisicao.EncerradaComSaldo or StatusRequisicao.Concluida);
    private List<LoteCompra> _lotes = [];
    private Guid? _loteCotacao;
    private DateTime? _prazoResposta;
    private bool Solicitante => _r is not null && _r.SolicitanteUsuarioId == Usuario.UsuarioId;
    /// <summary>Solicitante pode retirar da aprovação enquanto ninguém decidiu (1ª aprovação).</summary>
    private bool PodeVoltar => _r is { Status: StatusRequisicao.AguardandoEngenheiro } && Solicitante;
    private bool PodeExcluir => Editavel && _r!.Numero > 0 && (Solicitante || Perfil());

    protected override async Task OnInitializedAsync()
    {
        _obras = await Cat.ObrasAsync(); _spes = await Cat.SpesAsync(); _ccs = await Cat.CentrosCustoAsync(); _produtos = await Cat.ProdutosBuscaAsync(); _servicos = await Cat.ServicosAsync();
        _estabs = await Cat.EstabelecimentosAsync(); _grupos = await Cat.GruposProdutoAsync();
        _marcas = await Cat.MarcasAsync();
        _prodMarca = await Consultas.MarcasDosProdutosAsync(Cancelamento);
        _atividades = await Cat.ListaAsync(TipoLista.Atividade); _categorias = await Cat.ListaAsync(TipoLista.CategoriaServico);
        await Carregar();
        if (Nav.Uri.Contains("?id=") && Guid.TryParse(Nav.Uri.Split("?id=")[1], out var id)) await Abrir(id);
    }

    private async Task Carregar() => _itens = await Consultas.ListaAsync(LimiteAtual, Cancelamento);

    private async Task Novo()
    {
        _r = new Requisicao { DataEntrega = Hoje.AddDays(15), ObraId = _obras.Count == 1 ? _obras[0].Id : Guid.Empty };
        await CarregarEstoque();
        if (_r.ObraId != Guid.Empty) await EnderecoDaObra();
        _locaisObra = [];
        if (SpesDaObra.Count() == 1) _r.SpeId = SpesDaObra.First().Id;
        ErrosForm = null; _aberto = true;
    }

    private Dictionary<Guid, decimal> _saldoObra = [], _saldoCentral = [];
    private bool _obraPropria;
    private List<string> _marcas = [];
    private Dictionary<Guid, string?> _prodMarca = [];
    private Almoxarifado? _central, _almoxObra;

    /// <summary>Saldo em estoque (obra e central) para mostrar ao lado de cada item: o comprador vê se dá para atender por transferência.</summary>
    private async Task CarregarEstoque()
    {
        _saldoObra = []; _saldoCentral = []; _almoxObra = null;
        if (_r is null || _r.ObraId == Guid.Empty) { _obraPropria = false; return; }
        var e = await Consultas.EstoqueAsync(_r.ObraId, Cancelamento);
        _obraPropria = e.ObraPropria; _central = e.Central; _almoxObra = e.AlmoxObra;
        _saldoObra = e.SaldoObra; _saldoCentral = e.SaldoCentral;
    }

    private void AtenderDoCentral()
    {
        var itens = _r!.Itens.Where(i => i.ProdutoId is { } p && _saldoCentral.GetValueOrDefault(p) > 0 && i.Quantidade > 0)
            .GroupBy(i => i.ProdutoId!.Value).Select(g => new EstoqueServico.ItemSolicitado(g.Key, Math.Min(g.Sum(i => i.Quantidade), _saldoCentral[g.Key]))).ToList();
        Confirmar($"Criar transferência do central para a obra com {itens.Count} item(ns) em estoque? Depois ajuste a requisição para comprar só o que faltar.", async () =>
        {
            Guid t = default;
            if (await Executar(async () => t = await EstoqueSrv.SolicitarTransferenciaAsync(_central!.Id, _almoxObra!.Id, itens, $"Requisição {_r!.Numero} atendida pelo estoque", Cancelamento), "Transferência criada."))
                Nav.NavigateTo($"/transferencias?id={t}");
        });
    }

    private async Task Abrir(Guid id)
    {
        _r = await Consultas.RequisicaoAsync(id, Cancelamento);
        _locaisObra = await Consultas.LocaisDaObraAsync(_r.ObraId, Cancelamento);
        await CarregarEstoque();
        ErrosForm = null; _versao++; _aberto = true;
    }

    private async Task Recarregar() { if (_r is not null) await Abrir(_r.Id); await Carregar(); }

    private List<Etapas.Passo> Passos()
    {
        var r = _r!;
        string Est(bool feito, bool atual) => feito ? "feita" : atual ? "atual" : "pendente";
        var reprov = r.Status == StatusRequisicao.NaoAutorizada;
        return
        [
            new("Solicitação", r.Numero == 0 ? Usuario.Nome : r.SolicitanteNome, r.Numero == 0 ? "atual" : "feita"),
            new("Aprov. Engenheiro", r.AprovadoEngenheiroEm is { } e ? Rotulos.DataHora(e) : r.Status == StatusRequisicao.AguardandoEngenheiro ? "pendente" : "—",
                reprov && r.AprovadoEngenheiroEm is null ? "erro" : Est(r.AprovadoEngenheiroEm is not null, r.Status == StatusRequisicao.AguardandoEngenheiro)),
            new("Aprov. Diretor Técnico", r.AprovadoDiretorEm is { } d ? Rotulos.DataHora(d) : r.Status == StatusRequisicao.AguardandoDiretorTecnico ? "pendente" : "—",
                reprov && r.AprovadoEngenheiroEm is not null ? "erro" : Est(r.AprovadoDiretorEm is not null, r.Status == StatusRequisicao.AguardandoDiretorTecnico)),
            new("Compra", "Comprador", Est(r.Status is StatusRequisicao.EmCotacao or StatusRequisicao.PedidoEmitido or StatusRequisicao.Concluida or StatusRequisicao.ParcialmenteAtendida or StatusRequisicao.Atendida or StatusRequisicao.EncerradaComSaldo, r.Status == StatusRequisicao.Autorizada)),
            new("Atendimento", "Recebimentos", Est(r.Status is StatusRequisicao.Atendida or StatusRequisicao.EncerradaComSaldo or StatusRequisicao.Concluida, r.Status is StatusRequisicao.ParcialmenteAtendida or StatusRequisicao.EmCotacao or StatusRequisicao.PedidoEmitido)),
        ];
    }

    private async Task EnderecoDaObra()
    {
        var spes = SpesDaObra.ToList();
        if (_r!.SpeId is { } sp && !spes.Any(s => s.Id == sp)) _r.SpeId = null;
        if (spes.Count == 1) _r.SpeId = spes[0].Id;
        var obra = await Consultas.ObraEntregaAsync(_r!.ObraId, Cancelamento);
        _obraPropria = obra?.DonoCusto == DonoCustoObra.Construtora;
        if (_obraPropria && _r!.EstabelecimentoFaturamentoId is null) _r.EstabelecimentoFaturamentoId = obra!.EstabelecimentoDonoId;
        if (string.IsNullOrWhiteSpace(_r!.EnderecoEntrega)) _r.EnderecoEntrega = obra?.EnderecoEntrega;
    }

    private async Task AbrirOrcamento()
    {
        var o = await Consultas.OrcamentoAsync(_r!.ObraId, Cancelamento);
        _compsObra = o.ComposicoesObra;
        _compsOrc = o.Servicos;
        _locaisObra = o.Locais;
        _selComp.Clear(); _selLoc = []; _demanda = null; ErrosForm = null; _abertoOrc = true;
    }

    private async Task PreviaOrc() =>
        await Executar(async () => _demanda = DescontarItensDaTela(await Orc.DemandaAsync(_r!.ObraId, _selComp, _selLoc, _r.Tipo, _r.CriadoEm == default ? null : _r.Id, Cancelamento)));

    /// <summary>
    /// A demanda do banco não enxerga os itens desta requisição (nova, ou excluída da conta quando já salva):
    /// desconta o que já está nos itens da tela (mesmo produto/serviço × local) para não puxar em dobro.
    /// </summary>
    private List<LinhaDemanda> DescontarItensDaTela(List<LinhaDemanda> demanda)
    {
        var ja = new Dictionary<(Guid? Produto, Guid? Comp, Guid Local), decimal>();
        foreach (var i in _r!.Itens)
            foreach (var x in i.Locais)
            {
                var k = (i.ProdutoId, x.ComposicaoId ?? i.ComposicaoId, x.LocalId);
                ja[k] = ja.GetValueOrDefault(k) + x.Quantidade;
            }
        if (ja.Count == 0) return demanda;
        return demanda.Select(d =>
        {
            decimal extra = 0;
            var locais = d.Locais.Select(x =>
            {
                if (!ja.TryGetValue((d.ProdutoId, x.ComposicaoId, x.LocalId), out var q)) return x;
                extra += q;
                return x with { Requisitado = x.Requisitado + q };
            }).ToList();
            return extra == 0 ? d : d with { Locais = locais, Requisitado = d.Requisitado + extra };
        }).ToList();
    }

    private async Task TrocarObra(ChangeEventArgs e)
    {
        var nova = Guid.TryParse(e.Value?.ToString(), out var g) ? g : Guid.Empty;
        if (nova == _r!.ObraId) return;
        if (TemLocais)
            Confirmar("Os itens têm quebra por local da obra atual. Trocar de obra remove essa quebra (as quantidades dos itens ficam). Continuar?",
                () => AplicarObra(nova), () => { _versaoObra++; return Task.CompletedTask; });
        else await AplicarObra(nova);
    }

    private async Task AplicarObra(Guid obra)
    {
        _r!.ObraId = obra;
        foreach (var i in _r.Itens) i.Locais.Clear();
        _selComp.Clear(); _selLoc = []; _demanda = null;
        _locaisObra = obra == Guid.Empty ? [] : await Consultas.LocaisDaObraAsync(obra, Cancelamento);
        await EnderecoDaObra();
        await CarregarEstoque();
    }

    private async Task IncluirOrc()
    {
        if (!await Executar(async () =>
        {
            var spe = await Orc.SpeDosLocaisAsync(_r!.ObraId, _selLoc, Cancelamento);
            if (TemLocais && spe is not null && _r.SpeId is { } atual && atual != spe) throw new RegraDeNegocioException("Os locais são de outra SPE: faça outra requisição.");
            var ids = _demanda!.Where(d => d.ProdutoId != null).Select(d => d.ProdutoId!.Value).ToList();
            var prods = await Consultas.ProdutosAsync(ids, Cancelamento);
            foreach (var i in OrcamentoServico.Itens(_r.Id, _demanda!, prods)) _r.Itens.Add(i);
            if (spe is not null) _r.SpeId = spe;
            _r.RecalcularPrazo();
        })) return;
        _abertoOrc = false;
        Notif.Sucesso("Itens incluídos com a quebra por local. Salve a requisição.");
    }

    private void AddItem() => _r!.Itens.Add(new RequisicaoItem { RequisicaoId = _r.Id, Quantidade = 1 });

    private async Task EscolherProduto(RequisicaoItem i, string? valor)
    {
        if (!Guid.TryParse(valor, out var pid)) { i.ProdutoId = null; return; }
        var p = await Consultas.ProdutoAsync(pid, Cancelamento);
        i.EspecificacaoTecnica = null; i.DocumentosExigidos = null;
        i.HerdarDoProduto(p);
        _r!.RecalcularPrazo();
    }

    private async Task Salvar()
    {
        if (await Executar(async () => { var id = await RequisicoesSrv.SalvarRequisicaoAsync(_r!, Cancelamento); await AbrirSilencioso(id); }, "Requisição salva."))
            await Carregar();
    }

    private async Task AbrirSilencioso(Guid id)
    {
        _r = await Consultas.RequisicaoAsync(id, Cancelamento);
        _versao++;
    }

    private async Task Enviar()
    {
        if (await Executar(async () => { await RequisicoesSrv.SalvarRequisicaoAsync(_r!, Cancelamento); await RequisicoesSrv.EnviarRequisicaoAsync(_r!.Id, Cancelamento); }, "Enviada para aprovação do engenheiro responsável da SPE."))
            await Recarregar();
    }

    private void VoltarEdicao() =>
        Confirmar($"Retirar a requisição {_r!.Numero} da aprovação e voltar para edição?", async () =>
        {
            if (await Executar(() => RequisicoesSrv.VoltarParaEdicaoAsync(_r!.Id, Cancelamento), "Requisição voltou para edição.")) await Recarregar();
        });

    private void Excluir() =>
        Confirmar($"Excluir a requisição {_r!.Numero}? Ela sai da lista (fica no histórico de auditoria).", async () =>
        {
            if (await Executar(() => RequisicoesSrv.ExcluirRequisicaoAsync(_r!.Id, Perfil(), Cancelamento), "Requisição excluída.")) { _aberto = false; _r = null; await Carregar(); }
        });

    private async Task AbrirCotacao()
    {
        var c = await Consultas.ConvitesAsync(_r!.Id, _r.Tipo, _r.Itens, Cancelamento);
        (_fornecedores, _sugeridos) = (c.Fornecedores, c.Sugeridos);
        _convidados.Clear();
        foreach (var f in _sugeridos.Where(id => _fornecedores.Any(x => x.Id == id))) _convidados.Add(f);
        _lotes = c.Lotes;
        // Comprador abre a cotação do próprio lote por padrão.
        _loteCotacao = _lotes.Count > 1 ? _lotes.FirstOrDefault(l => l.CompradorUsuarioId == Usuario.UsuarioId)?.Id : null;
        _prazoResposta = null; ErrosForm = null;
        _cotar = true;
    }

    private async Task GerarCotacao()
    {
        Guid id = default;
        DateTimeOffset? prazo = _prazoResposta is { } p ? new DateTimeOffset(p, Relogio.GetLocalNow().Offset) : null;
        if (await Executar(async () => id = await CotacoesSrv.GerarCotacaoAsync(_r!.Id, _convidados, _loteCotacao, null, prazo, Cancelamento), "Cotação criada."))
        { _cotar = false; _aberto = false; Nav.NavigateTo($"cotacoes?id={id}"); }
    }
}
