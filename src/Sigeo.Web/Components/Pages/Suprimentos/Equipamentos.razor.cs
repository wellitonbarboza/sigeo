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

// Código da tela Equipamentos.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Equipamentos
{
    [Inject] private EquipamentosObraConsultas Consultas { get; set; } = default!;
    private sealed record LinhaPlano(PlanoManutencao Plano, string Equipamento, string Situacao);
    private sealed record LinhaOs(OrdemServico Os, string Equipamento);
    private sealed record Hist(DateOnly Data, string Evento, string? Detalhe);
    private const string Vencida = "Vencida", PertoTexto = "Perto de vencer", EmDia = "Em dia";
    private const string PerguntaCancelarOs = "Cancelar esta OS?";
    private static readonly IReadOnlyList<(string, string)> _abas = [("equip", "Equipamentos"), ("prev", "Preventivas e agenda"), ("os", "Ordens de serviço"), ("familias", "Famílias")];
    private string _aba = "equip";
    private Grade<EquipamentosServico.Situacao>? _gEq;
    private Grade<LinhaPlano>? _gPl;
    private Grade<LinhaOs>? _gOs;
    private Grade<FamiliaEquipamento>? _gFam;
    private IGrade? GradeAtiva => _aba switch { "prev" => _gPl, "os" => _gOs, "familias" => _gFam, _ => _gEq };
    private IGrade? _gradeVista;
    /// <summary>A barra de ferramentas acompanha a grade da aba visível (a referência só existe depois que a aba renderiza).</summary>
    protected override void OnAfterRender(bool firstRender) { if (!ReferenceEquals(_gradeVista, GradeAtiva)) { _gradeVista = GradeAtiva; StateHasChanged(); } }

    private List<EquipamentosServico.Situacao> _sits = [];
    private List<FamiliaEquipamento> _familias = [];
    private List<Almoxarifado> _almoxs = [];
    private List<Opcao> _fornecedores = [], _contratos = [], _itensContrato = [];
    private List<string> _modosFalha = [];
    private List<LinhaPlano>? _planos;
    private List<LinhaOs>? _ordens;
    private bool _mostrarInativos, _soAbertas = true;
    private readonly HashSet<Guid> _selec = [];

    private IEnumerable<EquipamentosServico.Situacao> EquipFiltrados => _mostrarInativos ? _sits : _sits.Where(s => s.Equipamento.Situacao is not (SituacaoEquipamento.Baixado or SituacaoEquipamento.Devolvido));
    private IEnumerable<LinhaOs>? OrdensFiltradas => _soAbertas ? _ordens?.Where(o => o.Os.EmAberto || o.Os.Status == StatusOrdemServico.Concluida) : _ordens;

    private Equipamento? _e;
    private bool _aberto, _novo;
    private Dictionary<string, string> _extras = [];
    private List<Hist> _historico = [];
    private bool _abertoMov, _abertoLeitura, _abertoCorr;
    private TipoMovimentoEquipamento _mvTipo = TipoMovimentoEquipamento.Transferencia;
    private DateOnly _mvData;
    private Guid? _mvPara;
    private string? _mvCheck, _mvObs, _corrDesc;
    private bool _corrParado = true;
    private decimal _leitura;
    private PlanoManutencao? _pl;
    private bool _abertoPlano;
    private OrdemServico? _os;
    private bool _abertoOs;
    private Dictionary<string, bool> _osChecklist = [];
    private FamiliaEquipamento? _fam;
    private bool _abertoFam;

    private static string Juntar(IEnumerable<string> l) => string.Join("; ", l);
    private static string SimNao(bool b) => b ? "Sim" : "Não";
    private string NomeFamilia(Guid id) => _familias.FirstOrDefault(f => f.Id == id)?.Nome ?? "";
    private string NomeAlmox(Guid? id) => id is { } g && _almoxs.FirstOrDefault(a => a.Id == g) is { } a ? (a.Tipo == TipoAlmoxarifado.Central ? "Central · " : "") + a.Nome : "—";
    private string NomeEquip(Guid id) => _sits.FirstOrDefault(s => s.Equipamento.Id == id)?.Equipamento is { } e ? $"{e.Patrimonio} – {e.Nome}" : "";
    private string Leitura(Equipamento e) => e.Hodometro > 0 ? $"{e.Hodometro:N0} km" : e.Horimetro > 0 ? $"{e.Horimetro:N1} h" : "";
    private static string Intervalo(PlanoManutencao p) => p.Gatilho switch { GatilhoManutencao.Tempo => $"{p.IntervaloDias} dias", GatilhoManutencao.Uso => $"{p.IntervaloUso:N0} h/km", _ => "a cada mobilização" };
    private static string UltimaTexto(PlanoManutencao p) => p.Gatilho == GatilhoManutencao.Uso ? (p.UltimaLeitura?.ToString("N0") ?? "—") : Data(p.UltimaData);
    private static string ProximaTexto(PlanoManutencao p) => p.Gatilho switch { GatilhoManutencao.Tempo => Data(p.ProximaData), GatilhoManutencao.Uso => p.ProximaLeitura?.ToString("N0") ?? "—", _ => "na transferência" };
    private static FamiliaEquipamento Copia(FamiliaEquipamento f) => new() { Id = f.Id, Nome = f.Nome, UsaHorimetro = f.UsaHorimetro, UsaHodometro = f.UsaHodometro, CamposExtras = f.CamposExtras, CriticidadePadrao = f.CriticidadePadrao, Ativa = f.Ativa };
    private static string UrlEtiquetas(IEnumerable<Guid> ids) => "/equipamentos/etiquetas?ids=" + string.Join(",", ids);
    private void Marcar(Guid id, bool v) { if (v) _selec.Add(id); else _selec.Remove(id); }
    private void ImprimirEtiquetas() => Nav.NavigateTo(UrlEtiquetas(_selec), forceLoad: true);

    protected override async Task OnInitializedAsync()
    {
        _almoxs = await EstoqueSrv.AlmoxarifadosAsync(Cancelamento);
        _fornecedores = await Cat.FornecedoresAsync();
        _contratos = await Cat.ContratosAtivosAsync();
        _modosFalha = await Cat.ListaAsync(TipoLista.ModoFalha);
        _mvData = Hoje;
        await Carregar();
    }

    private async Task Carregar()
    {
        var r = await Consultas.ListaAsync(Hoje, LimiteAtual, Cancelamento);
        _familias = r.Familias;
        _sits = r.Situacoes;
        var eqs = _sits.ToDictionary(s => s.Equipamento.Id, s => s.Equipamento);
        var planos = r.Planos;
        _planos = planos.Where(p => eqs.ContainsKey(p.EquipamentoId)).Select(p =>
        {
            var e = eqs[p.EquipamentoId]; var leit = e.Horimetro + e.Hodometro;
            var sit = !p.Ativo ? "Inativo" : p.Vencido(Hoje, leit) ? Vencida : p.PertoDeVencer(Hoje, leit) ? PertoTexto : EmDia;
            return new LinhaPlano(p, $"{e.Patrimonio} – {e.Nome}", sit);
        }).OrderBy(l => l.Situacao == Vencida ? 0 : l.Situacao == PertoTexto ? 1 : 2).ThenBy(l => l.Equipamento).ToList();
        var oss = r.Ordens;
        _ordens = oss.Select(o => new LinhaOs(o, eqs.TryGetValue(o.EquipamentoId, out var e) ? $"{e.Patrimonio} – {e.Nome}" : "")).ToList();
    }

    // ───── Equipamento ─────
    private void Novo()
    {
        if (_aba == "prev") { NovoPlano(); return; }
        if (_aba == "familias") { _fam = new FamiliaEquipamento(); ErrosForm = null; _abertoFam = true; return; }
        _e = new Equipamento(); _novo = true; _extras = []; _itensContrato = []; _historico = []; ErrosForm = null; _aberto = true;
    }

    private async Task Abrir(Guid id)
    {
        var a = await Consultas.AbrirAsync(id, Cancelamento);
        _e = a.Equipamento;
        _novo = false;
        _extras = (_e.CamposExtras ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Split('=', 2)).Where(p => p.Length == 2)
            .GroupBy(p => p[0].Trim()).ToDictionary(g => g.Key, g => g.First()[1].Trim());
        await CarregarItensContrato();
        var movs = a.Movimentos;
        var oss = a.Ordens;
        _historico = movs.Select(m => new Hist(m.Data, De(m.Tipo), string.Join(" · ", new[] { m.ParaAlmoxarifadoId is { } p ? "para " + NomeAlmox(p) : null, m.Observacao }.Where(x => !string.IsNullOrEmpty(x)))))
            .Concat(oss.Select(o => new Hist(DateOnly.FromDateTime(o.AbertaEm.LocalDateTime), $"OS {o.Numero} {De(o.Tipo).ToLowerInvariant()}", $"{De(o.Status)} · {o.Descricao}")))
            .OrderByDescending(h => h.Data).ToList();
        ErrosForm = null; _aberto = true;
    }

    private async Task CarregarItensContrato()
    {
        _itensContrato = [];
        if (_e?.ContratoId is not { } cid) return;
        _itensContrato = await Consultas.ItensContratoAsync(cid, Cancelamento);
    }

    private async Task TrocarContrato(string? v)
    {
        _e!.ContratoId = Guid.TryParse(v, out var g) ? g : null; _e.ContratoItemId = null;
        await CarregarItensContrato();
    }

    private void TrocarFamilia(string? v)
    {
        _e!.FamiliaId = Guid.TryParse(v, out var g) ? g : Guid.Empty;
        if (_novo && _familias.FirstOrDefault(f => f.Id == g) is { } f) _e.Criticidade = f.CriticidadePadrao;
    }

    private async Task Salvar()
    {
        var campos = _familias.FirstOrDefault(f => f.Id == _e!.FamiliaId)?.Campos ?? [];
        _e!.CamposExtras = string.Join(";", campos.Where(c => !string.IsNullOrWhiteSpace(_extras.GetValueOrDefault(c))).Select(c => $"{c}={_extras[c].Replace(";", ",").Replace("=", "-").Trim()}"));
        if (string.IsNullOrEmpty(_e.CamposExtras)) _e.CamposExtras = null;
        if (_e.Propriedade == PropriedadeEquipamento.Propria) { _e.FornecedorId = null; _e.ContratoId = null; _e.ContratoItemId = null; }
        Guid id = default;
        if (await Executar(async () => id = await EquipSrv.SalvarAsync(_e, Cancelamento), "Equipamento salvo.")) { await Carregar(); await Abrir(id); }
    }

    private void AbrirMovimento() { _mvTipo = TipoMovimentoEquipamento.Transferencia; _mvData = Hoje; _mvPara = null; _mvCheck = null; _mvObs = null; ErrosForm = null; _abertoMov = true; }

    private async Task Movimentar()
    {
        if (await Executar(() => EquipSrv.MovimentarAsync(_e!.Id, _mvTipo, _mvPara, _mvData, _mvCheck, _mvObs, Cancelamento), "Movimento registrado."))
        { _abertoMov = false; await Carregar(); await Abrir(_e!.Id); }
    }

    private async Task RegistrarLeitura()
    {
        var f = _familias.First(x => x.Id == _e!.FamiliaId);
        if (await Executar(() => EquipSrv.RegistrarLeituraAsync(_e!.Id, f.UsaHodometro ? null : _leitura, f.UsaHodometro ? _leitura : null, Cancelamento), "Leitura registrada."))
        { _abertoLeitura = false; await Carregar(); await Abrir(_e!.Id); }
    }

    private async Task AbrirCorretiva()
    {
        Guid id = default;
        if (await Executar(async () => id = await EquipSrv.AbrirCorretivaAsync(_e!.Id, _corrDesc ?? "", _corrParado, Cancelamento), "OS corretiva aberta."))
        { _abertoCorr = false; _aberto = false; await Carregar(); _aba = "os"; await AbrirOs(id); }
    }

    // ───── Planos ─────
    private void NovoPlano() { _pl = new PlanoManutencao(); ErrosForm = null; _abertoPlano = true; }

    private void AbrirPlano(PlanoManutencao p)
    {
        _pl = new PlanoManutencao { Id = p.Id, EquipamentoId = p.EquipamentoId, Nome = p.Nome, Gatilho = p.Gatilho, IntervaloDias = p.IntervaloDias, IntervaloUso = p.IntervaloUso,
            UltimaData = p.UltimaData, UltimaLeitura = p.UltimaLeitura, Executante = p.Executante, ContratoId = p.ContratoId, Checklist = p.Checklist, DiasAntecedencia = p.DiasAntecedencia, Ativo = p.Ativo };
        ErrosForm = null; _abertoPlano = true;
    }

    private async Task SalvarPlano()
    {
        if (await Executar(() => EquipSrv.SalvarPlanoAsync(_pl!, Cancelamento), "Plano salvo.")) { _abertoPlano = false; await Carregar(); }
    }

    private async Task GerarOsDoPlano()
    {
        Guid id = default;
        if (await Executar(async () => id = await EquipSrv.GerarOsAsync(_pl!.Id, Cancelamento), "OS preventiva gerada.")) { _abertoPlano = false; await Carregar(); _aba = "os"; await AbrirOs(id); }
    }

    private async Task GerarPreventivas()
    {
        var n = 0;
        if (await Executar(async () => n = await EquipSrv.GerarPreventivasAsync(Cancelamento)))
        { Notif.Sucesso(n == 0 ? "Nenhum plano vencido sem OS." : $"{n} OS preventiva(s) gerada(s)."); await Carregar(); }
    }

    // ───── OS ─────
    private async Task AbrirOs(Guid id)
    {
        var r = await Consultas.OrdemServicoAsync(id, Cancelamento);
        _os = r.Os;
        var feitos = (_os.ChecklistResultado ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();
        var itens = r.ItensChecklist;
        _osChecklist = itens.Distinct().ToDictionary(i => i, i => feitos.Contains("[x] " + i));
        ErrosForm = null; _abertoOs = true;
    }

    private void AplicarChecklist()
    {
        if (_osChecklist.Count > 0) _os!.ChecklistResultado = string.Join("\n", _osChecklist.Select(kv => (kv.Value ? "[x] " : "[ ] ") + kv.Key));
    }

    private async Task SalvarOs()
    {
        AplicarChecklist();
        if (await Executar(() => EquipSrv.SalvarOsAsync(_os!, Cancelamento), "OS salva.")) { await Carregar(); await AbrirOs(_os!.Id); }
    }

    private async Task Avancar(StatusOrdemServico novo)
    {
        AplicarChecklist();
        if (await Executar(async () =>
        {
            if (_os!.Status is not (StatusOrdemServico.Verificada or StatusOrdemServico.Cancelada)) await EquipSrv.SalvarOsAsync(_os, Cancelamento);
            await EquipSrv.AvancarOsAsync(_os.Id, novo, Cancelamento);
        }, $"OS: {De(novo).ToLowerInvariant()}."))
        { await Carregar(); await AbrirOs(_os!.Id); }
    }

    // ───── Famílias ─────
    private async Task SalvarFamilia()
    {
        if (await Executar(() => EquipSrv.SalvarFamiliaAsync(_fam!, Cancelamento), "Família salva.")) { _abertoFam = false; await Carregar(); }
    }
}
