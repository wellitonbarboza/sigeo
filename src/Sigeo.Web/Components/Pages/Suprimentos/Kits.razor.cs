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

// Código da tela Kits.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Kits
{
    private sealed record LinhaNec(Guid ProdutoId, string Produto, string Unidade, decimal Necessario, decimal Saldo, decimal Central, decimal Falta, decimal Orcado);
    private const string EntregueTexto = "Entregue", PendenteTexto = "Pendente";
    private static readonly IReadOnlyList<(string, string)> _abas = [("kits", "Kits"), ("separar", "Separar e entregar"), ("consumo", "Consumo × orçado")];
    private string _aba = "kits";
    private Grade<KitMaterial>? _gKits;
    private Grade<EstoqueServico.ConsumoLocal>? _gCon;
    private IGrade? GradeAtiva => _aba == "consumo" ? _gCon : _gKits;
    private IGrade? _gradeVista;
    /// <summary>A barra de ferramentas acompanha a grade da aba visível (a referência só existe depois que a aba renderiza).</summary>
    protected override void OnAfterRender(bool firstRender) { if (!ReferenceEquals(_gradeVista, GradeAtiva)) { _gradeVista = GradeAtiva; StateHasChanged(); } }

    private List<Opcao> _obras = [], _aptos = [], _fornecedores = [];
    private Guid? _obraId;
    private List<KitMaterial> _kits = [];
    private List<Almoxarifado> _almoxs = [];
    private Almoxarifado? _central => _almoxs.FirstOrDefault(a => a.Tipo == TipoAlmoxarifado.Central && a.Ativo);
    private List<string> _etapas = [], _disciplinas = [];
    private List<ProdutoBusca> _produtos = [];
    private Dictionary<Guid, ProdutoBusca> _prodPorId = [];

    private KitMaterial? _kit;
    private bool _abertoKit;

    private Guid? _kitSep, _almoxSep, _kitCon;
    private string? _filtroApto;
    private readonly HashSet<Guid> _marcados = [];
    private HashSet<Guid> _entregues = [];
    private List<LinhaNec>? _nec;
    private DateOnly _limite;
    private List<EstoqueServico.ConsumoLocal>? _consumo;

    private Opcao? _aptoEnt;
    private bool _abertoEnt;
    private string? _entPara;
    private Guid? _entForn;

    private IEnumerable<Opcao> AptosFiltrados => string.IsNullOrWhiteSpace(_filtroApto) ? _aptos
        : _aptos.Where(a => SemAcento(a.Texto).Contains(SemAcento(_filtroApto), StringComparison.OrdinalIgnoreCase));

    private static string SimNao(bool b) => b ? "Sim" : "Não";
    private string NomeApto(EstoqueServico.ConsumoLocal c) => _aptos.FirstOrDefault(a => a.Id == c.LocalId)?.Texto ?? c.Local;
    private static string QtdItens(KitMaterial k) => k.Origem == OrigemKit.Manual ? k.Itens.Count.ToString() : "orçamento";
    private string NomeProduto(Guid id) => _prodPorId.TryGetValue(id, out var p) ? $"{p.Codigo} – {p.Nome}" : "(produto inativo)";
    private string UnProduto(Guid id) => _prodPorId.TryGetValue(id, out var p) ? p.Unidade : "";

    protected override async Task OnInitializedAsync()
    {
        _obras = await Cat.ObrasAsync();
        _obraId = _obras.FirstOrDefault()?.Id;
        _produtos = await Cat.ProdutosBuscaAsync(); _prodPorId = _produtos.ToDictionary(p => p.Id);
        _disciplinas = await Cat.ListaAsync(TipoLista.Disciplina);
        _fornecedores = await Cat.FornecedoresAsync();
        _almoxs = await EstoqueSrv.AlmoxarifadosAsync(Cancelamento);
        _limite = Hoje.AddDays(7);
        await TrocarObra();
    }

    private async Task TrocarObra()
    {
        _marcados.Clear(); LimparCalculo(); _consumo = null;
        _aptos = _obraId is { } o ? (await Cat.LocaisComCaminhoAsync(o)).Where(l => l.Extra is "Apartamento" or "Área comum" or "Unidade" or "Casa").ToList() : [];
        _almoxSep = _almoxs.FirstOrDefault(a => a.ObraId == _obraId)?.Id ?? _central?.Id;
        await Carregar();
    }

    private async Task Carregar()
    {
        await using var db = Fabrica.CriarLeitura();
        _kits = await db.KitsMaterial.AsNoTracking().Include(k => k.Itens).Where(k => k.ObraId == null || k.ObraId == _obraId).OrderBy(k => k.Codigo).ToListAsync(Cancelamento);
        _etapas = _obraId is null ? [] : await db.Composicoes.AsNoTracking().Where(c => c.Ativa && c.Grupo != null && c.Grupo != "").Select(c => c.Grupo!).Distinct().OrderBy(g => g).ToListAsync(Cancelamento);
    }

    private void LimparCalculo() { _nec = null; }

    // ───── Cadastro ─────
    private void NovoKit()
    {
        _kit = new KitMaterial { ObraId = _obraId, Origem = OrigemKit.Automatico };
        ErrosForm = null; _abertoKit = true;
    }

    private void AbrirKit(Guid id)
    {
        var k = _kits.First(x => x.Id == id);
        _kit = new KitMaterial { Id = k.Id, Codigo = k.Codigo, Nome = k.Nome, Origem = k.Origem, ObraId = k.ObraId ?? _obraId, Etapa = k.Etapa, Disciplina = k.Disciplina, Ativo = k.Ativo,
            Itens = k.Itens.Select(i => new KitMaterialItem { Id = i.Id, KitId = i.KitId, ProdutoId = i.ProdutoId, Quantidade = i.Quantidade }).ToList() };
        ErrosForm = null; _abertoKit = true;
    }

    private void IncluirItem(ProdutoBusca p)
    {
        if (_kit is null || _kit.Itens.Any(i => i.ProdutoId == p.Id)) return;
        _kit.Itens.Add(new KitMaterialItem { KitId = _kit.Id, ProdutoId = p.Id, Quantidade = 1 });
    }

    private async Task SalvarKit()
    {
        if (string.IsNullOrWhiteSpace(_kit!.Etapa)) _kit.Etapa = null;
        if (string.IsNullOrWhiteSpace(_kit.Disciplina)) _kit.Disciplina = null;
        if (await Executar(() => EstoqueSrv.SalvarKitAsync(_kit, Cancelamento), "Kit salvo.")) { _abertoKit = false; await Carregar(); }
    }

    // ───── Separação ─────
    private async Task SugerirDaSemana()
    {
        if (_obraId is null) return;
        await using var db = Fabrica.CriarLeitura();
        var seg = Hoje.AddDays(-(((int)Hoje.DayOfWeek + 6) % 7));
        var textos = await db.PlanosSemanais.AsNoTracking().Where(p => p.ObraId == _obraId && p.Semana >= seg.AddDays(-6) && p.Semana <= seg)
            .SelectMany(p => p.Compromissos).Select(c => c.Descricao + " " + c.Meta).ToListAsync(Cancelamento);
        var texto = SemAcento(string.Join(" | ", textos));
        var n = 0;
        foreach (var a in _aptos)
        {
            var nome = SemAcento(a.Texto);
            if (nome.Length >= 2 && System.Text.RegularExpressions.Regex.IsMatch(texto, $@"(?<![\w]){System.Text.RegularExpressions.Regex.Escape(nome)}(?![\w])", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            { _marcados.Add(a.Id); n++; }
        }
        LimparCalculo();
        if (textos.Count == 0) Notif.Erro("Sem programação semanal para esta semana.");
        else Notif.Sucesso(n == 0 ? "Nenhum apartamento citado nos compromissos da semana." : $"{n} apartamento(s) marcados pela programação da semana.");
    }

    private async Task Calcular()
    {
        await Executar(async () =>
        {
            var nec = await EstoqueSrv.NecessidadeAsync(_kitSep!.Value, _marcados.ToList(), Cancelamento);
            await using var db = Fabrica.CriarLeitura();
            var ids = nec.Select(n => n.ProdutoId).Distinct().ToList();
            var saldos = await db.SaldosEstoque.AsNoTracking().Where(s => s.AlmoxarifadoId == _almoxSep && ids.Contains(s.ProdutoId)).ToDictionaryAsync(s => s.ProdutoId, s => s.Disponivel, Cancelamento);
            var central = _central is { } c && c.Id != _almoxSep
                ? await db.SaldosEstoque.AsNoTracking().Where(s => s.AlmoxarifadoId == c.Id && ids.Contains(s.ProdutoId)).ToDictionaryAsync(s => s.ProdutoId, s => s.Disponivel, Cancelamento)
                : [];
            _nec = nec.GroupBy(n => n.ProdutoId).Select(g =>
            {
                var q = g.Sum(x => x.Quantidade); var s = saldos.GetValueOrDefault(g.Key);
                return new LinhaNec(g.Key, NomeProduto(g.Key), UnProduto(g.Key), q, s, central.GetValueOrDefault(g.Key), Math.Max(0, Math.Round(q - s, 3)), g.Sum(x => x.CustoOrcado));
            }).OrderBy(n => n.Produto).ToList();
            var marc = _marcados.ToList();
            _entregues = (await db.MovimentosEstoque.AsNoTracking().Where(m => m.KitId == _kitSep && m.LocalId != null && marc.Contains(m.LocalId.Value)).Select(m => m.LocalId!.Value).Distinct().ToListAsync(Cancelamento)).ToHashSet();
        });
    }

    private void PedirFalta()
    {
        var falta = _nec!.Where(n => n.Falta > 0).ToList();
        var kit = _kits.First(k => k.Id == _kitSep);
        Confirmar($"Pedir ao central {falta.Count} item(ns) em falta para o kit {kit.Codigo}? Cria a transferência (rascunho) e uma restrição de material no Last Planner até a conferência na obra.", async () =>
        {
            Guid t = default;
            if (await Executar(async () =>
            {
                t = await EstoqueSrv.SolicitarTransferenciaAsync(_central!.Id, _almoxSep!.Value, falta.Select(f => new EstoqueServico.ItemSolicitado(f.ProdutoId, f.Falta)).ToList(),
                    $"Kit {kit.Codigo} · {_marcados.Count} apto(s)", Cancelamento);
                await EstoqueSrv.CriarRestricaoAsync(_obraId!.Value, t, $"Kit {kit.Nome}", $"Material do kit {kit.Codigo} vindo do central ({falta.Count} itens)", _limite, Cancelamento);
            }, "Transferência solicitada e restrição criada."))
                Nav.NavigateTo($"/transferencias?id={t}");
        });
    }

    private void AbrirEntrega(Opcao a) { _aptoEnt = a; _entPara = null; _entForn = null; ErrosForm = null; _abertoEnt = true; }

    private async Task Entregar()
    {
        int n = 0;
        if (await Executar(async () => n = await EstoqueSrv.EntregarKitAsync(_kitSep!.Value, _almoxSep!.Value, _aptoEnt!.Id, _entPara ?? "", _entForn, null, Cancelamento)))
        {
            Notif.Sucesso($"Kit entregue — {n} lançamento(s) de saída."); _abertoEnt = false; await Calcular();
        }
    }

    // ───── Consumo ─────
    private async Task CalcularConsumo()
    {
        await Executar(async () =>
        {
            var locais = _marcados.ToList();
            if (locais.Count == 0)
            {
                await using var db = Fabrica.CriarLeitura();
                locais = await db.MovimentosEstoque.AsNoTracking().Where(m => m.KitId == _kitCon && m.LocalId != null).Select(m => m.LocalId!.Value).Distinct().ToListAsync(Cancelamento);
            }
            _consumo = locais.Count == 0 ? [] : (await EstoqueSrv.ConsumoAsync(_kitCon!.Value, locais, Cancelamento)).OrderBy(c => c.Local).ToList();
        });
    }
}
