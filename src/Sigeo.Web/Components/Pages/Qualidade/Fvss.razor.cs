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

// Código da tela Fvss.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Fvss
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private FvssConsultas Consultas { get; set; } = default!;
    private sealed record Linha(Guid Id, string Numero, string Servico, string Local, string Prestador, string Inspetor, DateTimeOffset Data, string Resumo, StatusFvs Status, bool Primeira);
    public static readonly string[] Disposicoes = ["Retrabalho", "Reparo", "Refazer (demolir)", "Aceite sob concessão", "Devolução ao fornecedor", "Reclassificação"];
    private static readonly (ResultadoItem, string, string)[] Opcoes = [(ResultadoItem.Conforme, "Conforme", "ok"), (ResultadoItem.NaoConforme, "Não conf.", "perigo-cheio"), (ResultadoItem.NaoAplica, "N/A", "prim")];
    private Grade<Linha>? _grade;
    private List<Linha>? _itens;
    private Fvs? _f;
    private Rnc? _rnc;
    private bool _aberto, _nova;
    private decimal _meta = 85;
    private Guid? _obra, _local, _modelo, _prestador, _contrato;
    private DateOnly _dataReinsp;
    private readonly Dictionary<Guid, ResultadoItem> _reinsp = [];
    private List<Opcao> _obras = [], _locais = [], _modelos = [], _fornecedores = [], _contratos = [], _equipamentos = [];
    private bool EmInspecao => _f?.Status == StatusFvs.EmInspecao;
    private DateTimeOffset InicioMes => new(Hoje.Year, Hoje.Month, 1, 0, 0, 0, TimeSpan.Zero);
    private static string Nome(List<Opcao> l, Guid id) => l.FirstOrDefault(x => x.Id == id)?.Texto ?? "—";
    private decimal Pct1aNum()
    {
        var concl = _itens?.Where(f => f.Status is not (StatusFvs.EmInspecao or StatusFvs.Cancelada)).ToList() ?? [];
        return concl.Count == 0 ? 0 : Math.Round(100m * concl.Count(f => f.Primeira) / concl.Count, 0);
    }
    private string Pct1a() => _itens is { Count: > 0 } ? $"{Pct1aNum():0}%" : "—";

    protected override async Task OnInitializedAsync()
    {
        _obras = await Cat.ObrasAsync(); _modelos = await Cat.ModelosAsync(TipoModelo.FVS); _fornecedores = await Cat.FornecedoresAsync(); _contratos = await Cat.ContratosAtivosAsync();
        var ini = await Consultas.InicioAsync(Hoje, Cancelamento);
        _meta = ini.Meta; _equipamentos = ini.Equipamentos;
        await Carregar();
        var q = System.Web.HttpUtility.ParseQueryString(new Uri(Nav.Uri).Query);
        if (Guid.TryParse(q["id"], out var id)) await Abrir(id);
        else if (Guid.TryParse(q["local"], out var loc)) await NovaFvs(loc, Guid.TryParse(q["servico"], out var s) ? s : null);
    }

    private async Task Carregar()
    {
        var (locais, forn, lista) = await Consultas.ListarAsync(LimiteAtual, Cancelamento);
        _itens = lista.Select(f => new Linha(f.Id, f.Numero, f.Modelo?.Titulo ?? "", locais.GetValueOrDefault(f.LocalId, ""), forn.GetValueOrDefault(f.FornecedorId, ""),
            f.InspetorNome, f.DataInspecao, f.Status == StatusFvs.EmInspecao ? "—" : f.Resumo, f.Status, f.Aprovada1aInspecao)).ToList();
    }

    private async Task NovaFvs(Guid? local, Guid? servico)
    {
        _obra = null; _local = null; _modelo = null; _prestador = null; _contrato = null; _locais = [];
        if (local is { } l)
        {
            var loc = await Consultas.LocalAsync(l, Cancelamento);
            if (loc is not null) { _obra = loc.ObraId; _locais = await Cat.LocaisAsync(loc.ObraId); _local = l; }
            if (servico is { } s) _modelo = _modelos.FirstOrDefault(m => m.Extra == s.ToString())?.Id;
        }
        else if (_obras.Count == 1) { _obra = _obras[0].Id; _locais = await Cat.LocaisAsync(_obras[0].Id); }
        ErrosForm = null; _nova = true;
    }

    private async Task CriarFvs()
    {
        var e = new List<string>();
        if (_local is null) e.Add("Informe o local.");
        if (_modelo is null) e.Add("Informe o modelo.");
        if (_prestador is null) e.Add("Informe o prestador.");
        if (e.Count > 0) { ErrosForm = e; return; }
        Guid id = default;
        if (await Executar(async () => id = await Qob.AbrirFvsAsync(_modelo!.Value, _local!.Value, _prestador!.Value, _contrato, OrigemRegistro.Web, Cancelamento), "FVS aberta."))
        { _nova = false; await Carregar(); await Abrir(id); }
    }

    private async Task Abrir(Guid id)
    {
        var r = await Consultas.AbrirAsync(id, _equipamentos.Select(x => x.Id).ToList(), Cancelamento);
        _f = r.Fvs; _rnc = r.Rnc;
        if (r.EquipamentoFora is { } e)
            _equipamentos.Add(new Opcao(e.Id, $"{e.Codigo} – {e.Descricao} (fora da validade)"));
        _reinsp.Clear(); _dataReinsp = Hoje; ErrosForm = null; _aberto = true;
    }

    private async Task Salvar() { if (await Executar(() => Qob.SalvarFvsAsync(_f!, Cancelamento), "FVS salva.")) await Abrir(_f!.Id); }

    private async Task Concluir()
    {
        if (await Executar(() => Qob.ConcluirFvsAsync(_f!, Cancelamento)))
        {
            await Carregar(); await Abrir(_f!.Id);
            if (_f.Status == StatusFvs.Reprovada) Notif.Aviso($"FVS reprovada: {_rnc?.Numero} aberta e etapa seguinte bloqueada."); else Notif.Sucesso("FVS aprovada — aguardando liberação do engenheiro.");
        }
    }

    private async Task Reinspecionar()
    {
        if (await Executar(() => Qob.ReinspecionarAsync(_f!.Id, _dataReinsp, _reinsp, Cancelamento), "Reinspeção registrada.")) { await Carregar(); await Abrir(_f!.Id); }
    }

    private async Task Liberar()
    {
        if (await Executar(() => Qob.LiberarFvsAsync(_f!.Id, Cancelamento), "FVS liberada — local disponível para medição e próxima etapa.")) { await Carregar(); await Abrir(_f!.Id); }
    }
}
