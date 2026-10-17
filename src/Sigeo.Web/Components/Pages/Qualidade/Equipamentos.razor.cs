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

namespace Sigeo.Web.Components.Pages.Qualidade;

// Código da tela Equipamentos.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Equipamentos
{
    private static readonly IReadOnlyList<(string, string)> _abas = [("ferr", "Ferramentas"), ("tipos", "Tipos e periodicidade")];
    private string _aba = "ferr";
    private Grade<TipoEquipamentoMedicao>? _gTipos;
    private IGrade? GradeAtiva => _aba == "tipos" ? _gTipos : _grade;
    private IGrade? _gradeVista;
    /// <summary>A barra de ferramentas acompanha a grade da aba visível (a referência só existe depois que a aba renderiza).</summary>
    protected override void OnAfterRender(bool firstRender) { if (!ReferenceEquals(_gradeVista, GradeAtiva)) { _gradeVista = GradeAtiva; StateHasChanged(); } }
    private List<TipoEquipamentoMedicao> _tipos = [];
    private List<Opcao> _fornecedores = [], _colabs = [];
    private readonly HashSet<Guid> _selec = [];
    private List<CautelaFerramenta> _cautelas = [];
    private string? _para;
    private Guid? _paraColab, _paraForn;
    private TipoEquipamentoMedicao? _t;
    private bool _abertoTipo;
    [Inject] private EquipamentosMedicaoConsultas Consultas { get; set; } = default!;

    private string NomeTipo(Guid? id) => id is { } g ? _tipos.FirstOrDefault(t => t.Id == g)?.Nome ?? "" : "";
    private string Dono(EquipamentoMedicao e) => e.Dono == DonoFerramenta.Propria ? "Própria" : _fornecedores.FirstOrDefault(f => f.Id == e.FornecedorId)?.Texto ?? "Terceiro";
    private void Etiquetas() => Nav.NavigateTo("/obra/equipamentos/etiquetas?ids=" + string.Join(",", _selec), true);

    private void EscolherTipo(string? v)
    {
        _e!.TipoMedicaoId = Guid.TryParse(v, out var g) ? g : null;
        if (_tipos.FirstOrDefault(t => t.Id == g) is { } t)
        {
            _e.PeriodicidadeMeses = t.PeriodicidadeMeses; _e.Criterio = t.Criterio; _e.TipoControle = t.TipoControle;
            if (_e.CriadoEm == default && !string.IsNullOrEmpty(t.Prefixo) && (string.IsNullOrEmpty(_e.Codigo) || _e.Codigo.Contains('-'))) _e.Codigo = "";
            if (string.IsNullOrWhiteSpace(_e.Descricao)) _e.Descricao = t.Nome;
        }
    }

    private async Task Entregar()
    {
        if (await Executar(() => Qob.EntregarFerramentaAsync(_e!.Id, _para ?? "", _paraColab, _paraForn, Hoje, null, Cancelamento), "Ferramenta entregue."))
        { await Carregar(); await Abrir(_e!); }
    }

    private async Task Devolver()
    {
        if (await Executar(() => Qob.DevolverFerramentaAsync(_e!.Id, Hoje, Cancelamento), "Devolvida ao almoxarifado.")) { await Carregar(); await Abrir(_e!); }
    }

    private void NovoTipo() { _t = new TipoEquipamentoMedicao(); ErrosForm = null; _abertoTipo = true; }
    private void AbrirTipo(TipoEquipamentoMedicao t)
    {
        _t = new TipoEquipamentoMedicao { Id = t.Id, Nome = t.Nome, Prefixo = t.Prefixo, PeriodicidadeMeses = t.PeriodicidadeMeses, Criterio = t.Criterio, Metodo = t.Metodo, TipoControle = t.TipoControle, Ativo = t.Ativo };
        ErrosForm = null; _abertoTipo = true;
    }
    private async Task SalvarTipo() { if (await Executar(() => Qob.SalvarTipoMedicaoAsync(_t!, Cancelamento), "Tipo salvo.")) { _abertoTipo = false; await Carregar(); } }

    private Grade<EquipamentoMedicao>? _grade;
    private List<EquipamentoMedicao>? _itens;
    private EquipamentoMedicao? _e;
    private List<Calibracao> _cals = [];
    private Calibracao _nova = new();
    private IReadOnlyList<string> _afetadas = [];
    private bool _aberto;
    private int _alerta = 30;
    private ParametrosVigentes? _p;
    private List<Opcao> _obras = [];
    private StatusEquipamento St(EquipamentoMedicao e) => e.Status(Hoje, _alerta);

    protected override async Task OnInitializedAsync()
    {
        _obras = await Cat.ObrasAsync();
        _fornecedores = await Cat.FornecedoresAsync(); _colabs = await Cat.ColaboradoresAsync();
        _p = await Consultas.ParametrosAsync(Cancelamento); _alerta = _p.Int(Parametros.AlertaCalibracaoDias);
        await Carregar();
    }

    private async Task Carregar()
    {
        var r = await Consultas.ListarAsync(Cancelamento);
        _itens = r.Itens; _tipos = r.Tipos;
    }

    private void Novo() { if (_aba == "tipos") { NovoTipo(); return; } NovoFerr(); }
    private void NovoFerr() { _e = new EquipamentoMedicao { PeriodicidadeMeses = _p!.Int(Parametros.PeriodicidadeVerificacaoMeses), ObraId = _obras.Count == 1 ? _obras[0].Id : null }; _cals = []; ErrosForm = null; _aberto = true; }

    private async Task Abrir(EquipamentoMedicao e)
    {
        var r = await Consultas.AbrirAsync(e.Id, Cancelamento);
        _e = r.Equipamento; _cals = r.Calibracoes;
        _nova = new Calibracao { Data = Hoje, Tipo = _e.TipoControle == TipoControleEquipamento.VerificacaoInterna ? TipoControleEquipamento.VerificacaoInterna : TipoControleEquipamento.CalibracaoExterna };
        _cautelas = r.Cautelas;
        _para = null; _paraColab = null; _paraForn = null;
        _afetadas = []; ErrosForm = null; _aberto = true;
    }

    private async Task Salvar()
    {
        Guid id = default;
        if (await Executar(async () => id = await Qob.SalvarEquipamentoAsync(_e!, Cancelamento), "Equipamento salvo.")) { await Carregar(); await Abrir(_itens!.First(x => x.Id == id)); }
    }

    private async Task Registrar()
    {
        IReadOnlyList<string> af = [];
        if (await Executar(async () => af = await Qob.RegistrarCalibracaoAsync(_e!.Id, _nova, Cancelamento), "Registro incluído."))
        { await Carregar(); await Abrir(_e!); _afetadas = af; }
    }
}
