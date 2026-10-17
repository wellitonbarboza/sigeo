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

namespace Sigeo.Web.Components.Pages.Engenharia;

// Código da tela MaoObra.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class MaoObra
{
    [Inject] private MaoObraConsultas Consultas { get; set; } = default!;
    [SupplyParameterFromQuery(Name = "obra")] public Guid? ObraQuery { get; set; }
    private List<Opcao> _funcoes = [], _colabs = [], _comps = [];
    private Guid? _obraId, _aEquipe, _aColab, _aComp;
    private DateOnly _mes;
    private string _aba = "eq";
    private List<Equipe> _equipes = [];
    private List<CustoMaoObraMes> _custos = [];
    private readonly List<Guid> _removidos = [];
    private List<Apropriacao> _aps = [];
    private List<Local> _locais = [];
    private List<LocalOrcamento> _locaisSel = [];
    private Dictionary<Guid, Local> _porId = [];
    private Dictionary<Guid, Composicao> _compsObj = [];
    private FechamentoMes? _fech;
    private Equipe? _e;
    private bool _abertoEq;
    private DateOnly _aData;
    private decimal _aHoras;
    private decimal? _aQtd;
    private string? _aObs;
    private HashSet<Guid> _aLocais = [];
    private List<(string, string)> _abas = [];
    private bool _veCustos;
    private int _versaoFiltro;
    private string _instantaneo = "";

    protected override async Task OnInitializedAsync()
    {
        _mes = new DateOnly(Hoje.Year, Hoje.Month, 1); _aData = Hoje;
        _veCustos = Perfil(PerfilUsuario.Engenharia, PerfilUsuario.Financeiro, PerfilUsuario.Diretoria);
        _abas = _veCustos
            ? [("eq", "Equipes"), ("custo", "Custos do mês"), ("apr", "Apropriação de horas"), ("fech", "Fechamento do mês")]
            : [("eq", "Equipes"), ("apr", "Apropriação de horas")];
        // A obra inicial (?obra= ou a única permitida) vem do SeletorObra, que chama TrocarObra.
        _funcoes = await Cat.FuncoesAsync();
    }

    /// <summary>Custos do mês alterados e não salvos (compara com o que veio do banco).</summary>
    private bool Sujo => _veCustos && _obraId is not null && Instantaneo() != _instantaneo;
    private string Instantaneo() => string.Join("|", _custos.Select(c => $"{c.Id};{c.EquipeId};{c.ColaboradorId};{c.FuncaoObraId};{c.Descricao};{c.Pessoas};{c.Salario};{c.EncargosPerc};{c.Beneficios};{c.Outros};{c.HorasMes}")) + "#" + _removidos.Count;

    /// <summary>Roda a troca (obra/mês/replicar) só depois de confirmar o descarte dos custos não salvos.</summary>
    private async Task DescartarSeConfirmar(Func<Task> acao, string oQue)
    {
        if (!Sujo) { await acao(); return; }
        Confirmar($"Os custos do mês têm alterações não salvas. {oQue} descarta essas alterações. Continuar?", acao,
            () => { _versaoFiltro++; return Task.CompletedTask; }); // recria os filtros para voltarem ao valor atual
    }

    private Task Atualizar() => DescartarSeConfirmar(Carregar, "Atualizar a tela");

    // Se o usuário recusar o descarte, _obraId não muda e o SeletorObra volta a mostrar a obra atual.
    private async Task TrocarObra(Guid? nova)
    {
        await DescartarSeConfirmar(async () =>
        {
            _obraId = nova;
            _aLocais = []; _aEquipe = null; _aColab = null; _aComp = null; _aHoras = 0; _aQtd = null; _aObs = null;
            _equipes = []; _custos = []; _aps = []; _locais = []; _locaisSel = []; _porId = []; _fech = null; _e = null; _abertoEq = false;
            _removidos.Clear(); _instantaneo = Instantaneo();
            await Carregar();
        }, "Trocar de obra");
    }

    private async Task Carregar()
    {
        if (_obraId is not { } o) return;
        _removidos.Clear();
        var d = await Consultas.CarregarAsync(o, _mes, Cancelamento);
        _equipes = d.Equipes;
        _colabs = d.Colaboradores;
        _custos = d.Custos;
        _aps = d.Apropriacoes;
        _locais = d.Locais;
        _porId = _locais.ToDictionary(l => l.Id);
        _locaisSel = _locais.Select(LocalOrcamento.De).ToList();
        var usados = d.ComposicoesUsadas;
        _compsObj = d.Composicoes;
        _comps = _compsObj.Values.Where(c => c.Ativa && (usados.Contains(c.Id) || usados.Count == 0)).OrderBy(c => c.Codigo).Select(c => new Opcao(c.Id, $"{c.Codigo} – {c.Descricao}", c.Unidade)).ToList();
        _fech = _veCustos ? await Mo.FechamentoAsync(o, _mes, ct: Cancelamento) : null;
        _instantaneo = Instantaneo();
    }

    private async Task TrocarMes(ChangeEventArgs e)
    {
        if (LerMes(e.Value) is not { } d) { _versaoFiltro++; return; }
        await DescartarSeConfirmar(async () => { _mes = d; await Carregar(); }, "Trocar de competência");
    }

    private void NovaEquipe() { _e = new Equipe { ObraId = _obraId!.Value }; ErrosForm = null; _abertoEq = true; }
    private async Task SalvarEquipe()
    {
        _e!.Membros = _e.Membros.Where(m => m.ColaboradorId != Guid.Empty).ToList();
        if (await Executar(() => Mo.SalvarEquipeAsync(_e, ct: Cancelamento), "Equipe salva.")) { _abertoEq = false; await Carregar(); }
    }

    private void AddCusto() => _custos.Add(new CustoMaoObraMes { ObraId = _obraId!.Value, Competencia = _mes, EncargosPerc = 80, HorasMes = 220 });
    private void RemoverCusto(CustoMaoObraMes c) { _custos.Remove(c); if (c.CriadoEm != default) _removidos.Add(c.Id); }

    private async Task SalvarCustos()
    {
        if (!_veCustos) return;
        if (await Executar(() => Mo.SalvarCustosMesAsync(_obraId!.Value, _mes, _custos, _removidos.ToList(), ct: Cancelamento), "Custos do mês salvos.")) await Carregar();
    }

    private Task Replicar() => DescartarSeConfirmar(ReplicarConfirmado, "Replicar o mês anterior");

    private async Task ReplicarConfirmado()
    {
        int n = 0;
        if (await Executar(async () => n = await Mo.ReplicarAsync(_obraId!.Value, _mes.AddMonths(-1), _mes, ct: Cancelamento)))
        { Notif.Sucesso($"{n} linha(s) replicada(s) de {_mes.AddMonths(-1):MM/yyyy}. Ajuste e salve."); await Carregar(); }
    }

    private async Task Apropriar()
    {
        int n = 0;
        if (await Executar(async () => n = await Mo.ApropriarAsync(_obraId!.Value, _aData, _aEquipe, _aColab, _aComp ?? Guid.Empty, _aLocais, _aHoras, _aQtd, _aObs, ct: Cancelamento)))
        { Notif.Sucesso($"Horas lançadas em {n} local(is)."); _aLocais = []; _aHoras = 0; _aQtd = null; _aObs = null; await Carregar(); }
    }

    private void Excluir(Guid lanc, int locais) =>
        Confirmar($"Excluir este lançamento de horas ({locais} local(is))?", () => ExcluirConfirmado(lanc));

    private async Task ExcluirConfirmado(Guid lanc)
    {
        if (await Executar(() => Mo.ExcluirLancamentoAsync(lanc, ct: Cancelamento), "Lançamento excluído.")) await Carregar();
    }
}
