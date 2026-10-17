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

namespace Sigeo.Web.Components.Pages.Suprimentos;

// Código da tela Estoque.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Estoque
{
    private sealed record LinhaSaldo(Guid ProdutoId, string Codigo, string Produto, string Unidade, decimal Disponivel, decimal Bloqueado, decimal CustoMedio, decimal Valor,
        decimal Minimo, decimal PontoPedido, decimal Maximo, decimal NoCentral, string Situacao, decimal Reposicao);
    private sealed record LinhaMov(Guid Id, bool Estornavel, DateTimeOffset Data, TipoMovimentoEstoque Tipo, string Produto, decimal Quantidade, decimal Custo, decimal Valor, decimal SaldoApos,
        string Documento, string? Aplicacao, string? Para, string? Por);
    private sealed record LinhaInv(Guid Id, int Numero, DateOnly Data, TipoInventario Tipo, int Itens, int ComDiferenca, string? AprovadoPor, StatusInventario Status);
    private sealed class Lanc
    {
        public TipoMovimentoEstoque Tipo { get; set; } = TipoMovimentoEstoque.Saida;
        public Guid? ProdutoId { get; set; }
        public decimal Quantidade { get; set; }
        public Guid? LocalId { get; set; }
        public Guid? ComposicaoId { get; set; }
        public Guid? ColaboradorId { get; set; }
        public Guid? FornecedorId { get; set; }
        public string? EntreguePara { get; set; }
        public string? Observacao { get; set; }
    }

    private static readonly IReadOnlyList<(string, string)> _abas = [("saldos", "Saldos e mínimos"), ("movimentos", "Movimentos (kardex)"), ("inventario", "Inventário"), ("almox", "Almoxarifados")];
    private string _aba = "saldos";
    private Grade<LinhaSaldo>? _gSaldos;
    private Grade<LinhaMov>? _gMovs;
    private Grade<LinhaInv>? _gInv;
    private Grade<Almoxarifado>? _gAlm;
    private IGrade? GradeAtiva => _aba switch { "movimentos" => _gMovs, "inventario" => _gInv, "almox" => _gAlm, _ => _gSaldos };
    private IGrade? _gradeVista;
    /// <summary>A barra de ferramentas acompanha a grade da aba visível (a referência só existe depois que a aba renderiza).</summary>
    protected override void OnAfterRender(bool firstRender) { if (!ReferenceEquals(_gradeVista, GradeAtiva)) { _gradeVista = GradeAtiva; StateHasChanged(); } }

    private List<Almoxarifado> _almoxs = [];
    private Guid? _almoxId;
    private Almoxarifado? _almox => _almoxs.FirstOrDefault(a => a.Id == _almoxId);
    private Almoxarifado? _central => _almoxs.FirstOrDefault(a => a.Tipo == TipoAlmoxarifado.Central && a.Ativo);
    private List<LinhaSaldo>? _saldos;
    private List<LinhaMov>? _movs;
    private List<LinhaInv>? _invs;
    private bool _soAlerta;
    private IEnumerable<LinhaSaldo>? SaldosFiltrados => _soAlerta ? _saldos?.Where(s => s.Situacao is "Abaixo do mínimo" or "Ponto de pedido") : _saldos;

    private List<ProdutoBusca> _produtos = [];
    private Dictionary<Guid, ProdutoBusca> _prodPorId = [];
    private List<Opcao> _locais = [], _composicoes = [], _colabs = [], _fornecedores = [];

    private LinhaSaldo? _saldo;
    private bool _abertoSaldo;
    private decimal _min, _pp, _max, _pedir;
    private Lanc _lc = new();
    private bool _abertoLanc;
    private InventarioEstoque? _inv;
    private bool _abertoInv;
    private TipoInventario _tipoInv = TipoInventario.Ciclico;
    private Almoxarifado? _alm;
    private bool _abertoAlm;

    protected override async Task OnInitializedAsync()
    {
        var obras = (await Cat.ObrasAsync()).Select(o => o.Id).ToHashSet();
        _almoxs = (await EstoqueSrv.AlmoxarifadosAsync(Cancelamento)).Where(a => a.ObraId is null || obras.Contains(a.ObraId.Value)).ToList();
        _produtos = await Cat.ProdutosBuscaAsync();
        _prodPorId = _produtos.ToDictionary(p => p.Id);
        _composicoes = await Cat.ComposicoesAsync(); _colabs = await Cat.ColaboradoresAsync(); _fornecedores = await Cat.FornecedoresAsync();
        var q = Nav.ToAbsoluteUri(Nav.Uri).Query;
        if (q.Contains("almox=") && Guid.TryParse(q.Split("almox=")[1].Split('&')[0], out var a) && _almoxs.Any(x => x.Id == a)) _almoxId = a;
        if (q.Contains("aba=")) _aba = q.Split("aba=")[1].Split('&')[0];
        _almoxId ??= _almoxs.FirstOrDefault(x => x.Tipo == TipoAlmoxarifado.Central && x.Ativo)?.Id ?? _almoxs.FirstOrDefault(x => x.Ativo)?.Id;
        await Recarregar();
    }

    private static string SimNao(bool b) => b ? "Sim" : "Não";
    private void IrRequisicao() => Nav.NavigateTo("/requisicoes");
    private string NomeProduto(Guid id) => _prodPorId.TryGetValue(id, out var p) ? $"{p.Codigo} – {p.Nome}" : "(produto inativo)";
    private string UnProduto(Guid id) => _prodPorId.TryGetValue(id, out var p) ? p.Unidade : "";
    private string? SaldoDoProduto(Guid? id) => id is { } g && _saldos?.FirstOrDefault(s => s.ProdutoId == g) is { } s ? $"Disponível: {s.Disponivel:N3} {s.Unidade}" : null;

    private async Task Recarregar()
    {
        _locais = _almox?.ObraId is { } o ? await Cat.LocaisComCaminhoAsync(o) : [];
        await CarregarSaldos(); await CarregarMovimentos(); await CarregarInventarios();
    }

    private async Task CarregarSaldos()
    {
        if (_almoxId is not { } id) { _saldos = []; return; }
        await using var db = Fabrica.CriarLeitura();
        var lista = await db.SaldosEstoque.AsNoTracking().Where(s => s.AlmoxarifadoId == id).ToListAsync(Cancelamento);
        var central = _central is { } c && c.Id != id
            ? await db.SaldosEstoque.AsNoTracking().Where(s => s.AlmoxarifadoId == c.Id).ToDictionaryAsync(s => s.ProdutoId, s => s.Disponivel, Cancelamento)
            : [];
        _saldos = lista.Select(s =>
        {
            var p = _prodPorId.GetValueOrDefault(s.ProdutoId);
            var sit = s.AbaixoDoMinimo ? "Abaixo do mínimo" : s.NoPontoDePedido ? "Ponto de pedido" : s.Minimo + s.PontoPedido + s.Maximo > 0 ? "OK" : "Sem parâmetro";
            var repor = s.NoPontoDePedido ? RegrasEstoque.Reposicao(s) : 0;
            return new LinhaSaldo(s.ProdutoId, p?.Codigo ?? "", p?.Nome ?? "(produto inativo)", p?.Unidade ?? "", s.Disponivel, s.Bloqueado, s.CustoMedio, s.Valor,
                s.Minimo, s.PontoPedido, s.Maximo, central.GetValueOrDefault(s.ProdutoId), sit, repor);
        }).OrderBy(s => s.Produto).ToList();
    }

    private async Task CarregarMovimentos()
    {
        if (_almoxId is not { } id) { _movs = []; return; }
        await using var db = Fabrica.CriarLeitura();
        var lista = await db.MovimentosEstoque.AsNoTracking().Where(m => m.AlmoxarifadoId == id).OrderByDescending(m => m.Data).ThenByDescending(m => m.CriadoEm).Take(LimiteAtual).ToListAsync(Cancelamento);
        var locIds = lista.Where(m => m.LocalId != null).Select(m => m.LocalId!.Value).Distinct().ToList();
        var locs = _locais.Count > 0 ? _locais.Where(l => locIds.Contains(l.Id)).ToDictionary(l => l.Id, l => l.Texto) : await db.Locais.AsNoTracking().Where(l => locIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.Nome, Cancelamento);
        var comps = _composicoes.ToDictionary(c => c.Id, c => c.Texto);
        var colabs = _colabs.ToDictionary(c => c.Id, c => c.Texto);
        var forns = _fornecedores.ToDictionary(c => c.Id, c => c.Texto);
        var estornados = lista.Where(m => m.OrigemTipo == "Estorno" && m.OrigemId != null).Select(m => m.OrigemId!.Value).ToHashSet();
        _movs = lista.Select(m => new LinhaMov(m.Id, m.Tipo is TipoMovimentoEstoque.Saida or TipoMovimentoEstoque.Devolucao or TipoMovimentoEstoque.Perda or TipoMovimentoEstoque.EntregaEpi
                && m.OrigemTipo != "Estorno" && !estornados.Contains(m.Id), m.Data, m.Tipo, NomeProduto(m.ProdutoId), m.Quantidade, m.CustoUnitario, m.Valor, m.SaldoApos, m.Documento,
            string.Join(" · ", new[] { m.LocalId is { } l ? locs.GetValueOrDefault(l) : null, m.ComposicaoId is { } c ? comps.GetValueOrDefault(c) : null }.Where(x => !string.IsNullOrEmpty(x))),
            m.EntreguePara ?? (m.ColaboradorId is { } co ? colabs.GetValueOrDefault(co) : null) ?? (m.FornecedorId is { } f ? forns.GetValueOrDefault(f) : null),
            m.CriadoPor)).ToList();
    }

    private async Task CarregarInventarios()
    {
        if (_almoxId is not { } id) { _invs = []; return; }
        await using var db = Fabrica.CriarLeitura();
        var lista = await db.Inventarios.AsNoTracking().Include(i => i.Itens).Where(i => i.AlmoxarifadoId == id).OrderByDescending(i => i.Numero).ToListAsync(Cancelamento);
        _invs = lista.Select(i => new LinhaInv(i.Id, i.Numero, i.Data, i.Tipo, i.Itens.Count, i.Itens.Count(x => x.Diferenca != 0), i.AprovadoPor, i.Status)).ToList();
    }

    // ───── Saldos ─────
    private void AbrirSaldo(LinhaSaldo s)
    {
        _saldo = s; _min = s.Minimo; _pp = s.PontoPedido; _max = s.Maximo; _pedir = s.Reposicao; ErrosForm = null; _abertoSaldo = true;
    }

    private async Task SalvarMinimos()
    {
        if (await Executar(() => EstoqueSrv.SalvarMinimosAsync(_almoxId!.Value, _saldo!.ProdutoId, _min, _pp, _max, Cancelamento), "Parâmetros salvos."))
        { _abertoSaldo = false; await CarregarSaldos(); }
    }

    private async Task PedirAoCentral()
    {
        Guid t = default;
        if (await Executar(async () => t = await EstoqueSrv.SolicitarTransferenciaAsync(_central!.Id, _almoxId!.Value, [new(_saldo!.ProdutoId, _pedir)], "Reposição pelo ponto de pedido", Cancelamento)))
            Nav.NavigateTo($"/transferencias?id={t}");
    }

    private void ReporDoCentral()
    {
        var itens = _saldos!.Where(s => s.Reposicao > 0).ToList();
        Confirmar($"Pedir ao central a reposição de {itens.Count} item(ns) no ponto de pedido? Cria uma transferência em rascunho.", async () =>
        {
            Guid t = default;
            if (await Executar(async () => t = await EstoqueSrv.SolicitarTransferenciaAsync(_central!.Id, _almoxId!.Value,
                    itens.Select(i => new EstoqueServico.ItemSolicitado(i.ProdutoId, i.Reposicao)).ToList(), "Reposição pelo ponto de pedido", Cancelamento)))
                Nav.NavigateTo($"/transferencias?id={t}");
        });
    }

    private async Task SugerirMinimos()
    {
        var n = 0;
        if (await Executar(async () => n = await EstoqueSrv.SugerirMinimosAsync(_almoxId!.Value, Cancelamento)))
        { Notif.Sucesso(n == 0 ? "Nenhum item sem parâmetro com consumo nos últimos 60 dias." : $"{n} item(ns) com mínimo sugerido — revise na grade."); await CarregarSaldos(); }
    }

    // ───── Lançamento ─────
    private void AbrirLancamento() { _lc = new(); ErrosForm = null; _abertoLanc = true; }

    private async Task Lancar()
    {
        if (_lc.ProdutoId is null) { ErrosForm = ["Escolha o produto."]; return; }
        var info = new EstoqueServico.SaidaInfo(_almoxId!.Value, _lc.ProdutoId.Value, _lc.Quantidade, _lc.Tipo, _lc.LocalId, _lc.ComposicaoId, _lc.ColaboradorId, _lc.FornecedorId, _lc.EntreguePara, _lc.Observacao);
        if (await Executar(() => EstoqueSrv.LancarAsync(info, Cancelamento), "Lançado no estoque."))
        { _abertoLanc = false; await CarregarSaldos(); await CarregarMovimentos(); }
    }

    // ───── Estorno ─────
    private LinhaMov? _est;
    private bool _abertoEst;
    private string? _motivoEst;
    private void AbrirEstorno(LinhaMov l) { _est = l; _motivoEst = null; ErrosForm = null; _abertoEst = true; }
    private async Task Estornar()
    {
        if (await Executar(() => EstoqueSrv.EstornarAsync(_est!.Id, _motivoEst ?? "", Cancelamento), "Lançamento estornado."))
        { _abertoEst = false; await CarregarSaldos(); await CarregarMovimentos(); }
    }

    private async Task TirarDoInventario(InventarioItem i)
    {
        if (await Executar(() => EstoqueSrv.RemoverItemInventarioAsync(_inv!.Id, i.ProdutoId, Cancelamento))) _inv!.Itens.Remove(i);
    }

    // ───── Inventário ─────
    private async Task AbrirInventario()
    {
        Guid id = default;
        if (await Executar(async () => id = await EstoqueSrv.AbrirInventarioAsync(_almoxId!.Value, _tipoInv, null, Cancelamento), "Inventário aberto."))
        { await CarregarInventarios(); await AbrirInv(id); }
    }

    private async Task AbrirInv(Guid id)
    {
        await using var db = Fabrica.CriarLeitura();
        _inv = await db.Inventarios.AsNoTracking().Include(i => i.Itens).FirstAsync(i => i.Id == id, Cancelamento);
        ErrosForm = null; _abertoInv = true;
    }

    private async Task IncluirNoInventario(ProdutoBusca p)
    {
        if (_inv is null || _inv.Itens.Any(i => i.ProdutoId == p.Id)) return;
        var sist = _saldos?.FirstOrDefault(s => s.ProdutoId == p.Id)?.Disponivel ?? 0;
        _inv.Itens.Add(new InventarioItem { InventarioId = _inv.Id, ProdutoId = p.Id, SaldoSistema = sist, CustoUnitario = _inv.Tipo == TipoInventario.Implantacao ? p.Preco : null });
        await Task.CompletedTask;
    }

    private async Task SalvarContagem(bool enviar)
    {
        var itens = _inv!.Itens.Select(i => new EstoqueServico.Contagem(i.ProdutoId, i.Contado, i.CustoUnitario, i.Observacao)).ToList();
        if (await Executar(() => EstoqueSrv.SalvarContagemAsync(_inv.Id, itens, enviar, Cancelamento), enviar ? "Enviado para aprovação." : "Contagem salva."))
        { await CarregarInventarios(); await AbrirInv(_inv.Id); }
    }

    private void AprovarInventario() => Confirmar("Aprovar o inventário? As diferenças viram movimento de ajuste no estoque.", async () =>
    {
        if (await Executar(() => EstoqueSrv.AprovarInventarioAsync(_inv!.Id, Cancelamento), "Inventário aprovado — estoque ajustado."))
        { _abertoInv = false; await Recarregar(); }
    });

    private void CancelarInventario() => Confirmar("Cancelar este inventário? A contagem é descartada.", async () =>
    {
        if (await Executar(() => EstoqueSrv.CancelarInventarioAsync(_inv!.Id, Cancelamento), "Inventário cancelado."))
        { _abertoInv = false; await CarregarInventarios(); }
    });

    private async Task SalvarAlmox()
    {
        if (await Executar(() => EstoqueSrv.SalvarAlmoxarifadoAsync(_alm!, Cancelamento), "Almoxarifado salvo."))
        {
            _abertoAlm = false;
            var obras = (await Cat.ObrasAsync()).Select(o => o.Id).ToHashSet();
            _almoxs = (await EstoqueSrv.AlmoxarifadosAsync(Cancelamento)).Where(a => a.ObraId is null || obras.Contains(a.ObraId.Value)).ToList();
        }
    }
}
