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

// Código da tela Contratos.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Contratos
{
    private Dictionary<Guid, string> _locCod = [];
    private ArvoreLocais _arvore = ArvoreLocais.Vazia;
    private readonly HashSet<Guid> _abertos = [];
    private void AlternarLocais(Guid id) { if (!_abertos.Remove(id)) _abertos.Add(id); }
    [Inject] private ContratosConsultas Consultas { get; set; } = default!;
    private Grade<ContratoLinha>? _grade;
    private List<ContratoLinha>? _itens;
    private Contrato? _k;
    private List<Medicao> _medicoes = [];
    private bool _aberto;
    private int _versao;
    private string _aba = "dados";
    private DateOnly? _assEmpresa, _assFornecedor;
    private List<Opcao> _spes = [];
    private List<Opcao> _obras = [], _fornecedores = [], _ccs = [], _servicos = [];
    private List<string> _categorias = [];
    private static readonly List<(string, string)> _abas = [("dados", "Dados gerais"), ("itens", "Itens"), ("its", "ITS vinculadas"), ("anexos", "Anexos"), ("aditivos", "Aditivos"), ("medicoes", "Medições")];

    private bool Editavel => _k is { Status: StatusContrato.EmElaboracao };
    private bool Diaria => _k is { Recorrente: true, FormaCobranca: FormaCobranca.Diaria };
    private List<Aditivo> _aditivos = [];
    private Aditivo? _ad;
    private Guid? _itemAd;
    private DateOnly? _adAssEmp, _adAssForn;
    private decimal _limiteAditivo = 25;

    private async Task NovoAditivo()
    {
        Aditivo? a = null;
        if (await Executar(async () => a = await ContratosSrv.NovoAditivoAsync(_k!.Id, Cancelamento))) _ad = a;
    }

    private void ItemNovoAditivo() => _ad!.Itens.Add(new AditivoItem { AditivoId = _ad.Id, Unidade = "un" });

    private void IncluirItemAditivo()
    {
        var ci = _k!.Itens.FirstOrDefault(x => x.Id == _itemAd);
        if (ci is not null) _ad!.Itens.Add(new AditivoItem { AditivoId = _ad.Id, ContratoItemId = ci.Id, QtdOriginal = ci.Quantidade, PrecoOriginal = ci.PrecoUnitario, PrecoUnitario = ci.PrecoUnitario });
        _itemAd = null;
    }

    private async Task AbrirAditivo(Guid id)
    {
        var r = await Consultas.AditivoAsync(id, Cancelamento);
        _ad = r.Aditivo; _aditivos = r.Aditivos;
        _adAssEmp = _adAssForn = Hoje; _versao++;
    }

    private async Task SalvarAditivo()
    {
        Guid id = default;
        if (await Executar(async () => id = await ContratosSrv.SalvarAditivoAsync(_ad!, Cancelamento), "Aditivo salvo.")) await AbrirAditivo(id);
    }

    private async Task EnviarAditivo()
    {
        if (await Executar(async () => { await ContratosSrv.SalvarAditivoAsync(_ad!, Cancelamento); await ContratosSrv.EnviarAditivoAsync(_ad!.Id, Cancelamento); }, "Aditivo enviado para aprovação.")) await AbrirAditivo(_ad!.Id);
    }

    private async Task AssinarAditivo()
    {
        if (_adAssEmp is null || _adAssForn is null) { Notif.Erro("Informe as duas assinaturas."); return; }
        if (await Executar(() => ContratosSrv.AssinarAditivoAsync(_ad!.Id, _adAssEmp.Value, _adAssForn.Value, Cancelamento), "Aditivo vigente — itens e vigência do contrato atualizados."))
        { var id = _ad!.Id; await Abrir(_k!.Id); _aba = "aditivos"; await AbrirAditivo(id); }
    }

    protected override async Task OnInitializedAsync()
    {
        _obras = await Cat.ObrasAsync(); _spes = await Cat.SpesAsync(); _fornecedores = await Cat.FornecedoresAsync(); _ccs = await Cat.CentrosCustoAsync(); _servicos = await Cat.ServicosAsync();
        _categorias = await Cat.ListaAsync(TipoLista.CategoriaServico);
        await Carregar();
        if (Nav.Uri.Contains("?id=") && Guid.TryParse(Nav.Uri.Split("?id=")[1], out var id)) await Abrir(id);
        else if (Nav.Uri.Contains("?aditivo=") && Guid.TryParse(Nav.Uri.Split("?aditivo=")[1], out var aid))
        {
            var cid = await Consultas.ContratoDoAditivoAsync(aid, Cancelamento);
            if (cid != Guid.Empty) { await Abrir(cid); _aba = "aditivos"; await AbrirAditivo(aid); }
        }
    }

    private async Task Carregar()
    {
        _itens = await Consultas.ListaAsync(LimiteAtual, Cancelamento);
    }

    private void Novo()
    {
        _k = new Contrato { VigenciaInicio = Hoje, VigenciaFim = Hoje.AddMonths(6), ObraId = _obras.Count == 1 ? _obras[0].Id : Guid.Empty };
        _medicoes = []; _aba = "dados"; ErrosForm = null; _aberto = true;
    }

    private async Task Abrir(Guid id)
    {
        var r = await Consultas.AbrirAsync(id, Cancelamento);
        _k = r.Contrato;
        var locs = r.Locais;
        _locCod = locs.ToDictionary(l => l.Id, l => l.Codigo);
        _arvore = new ArvoreLocais(locs);
        _medicoes = r.Medicoes;
        _aditivos = r.Aditivos;
        _limiteAditivo = r.LimiteAditivoPerc;
        _ad = null;
        _assEmpresa = _k.AssinaturaEmpresa ?? Hoje; _assFornecedor = _k.AssinaturaFornecedor ?? Hoje;
        ErrosForm = null; _versao++; _aberto = true;
    }

    private async Task Recarregar() { if (_k is not null) await Abrir(_k.Id); await Carregar(); }

    private void AddItem() => _k!.Itens.Add(new ContratoItem { ContratoId = _k.Id, Item = $"1.{_k.Itens.Count + 1}", Quantidade = 1 });

    private void AlternarIts(Guid id, bool marcado)
    {
        var v = _k!.ServicosVinculados.FirstOrDefault(x => x.ServicoControladoId == id);
        if (marcado && v is null) _k.ServicosVinculados.Add(new ContratoServico { ContratoId = _k.Id, ServicoControladoId = id });
        else if (!marcado && v is not null) _k.ServicosVinculados.Remove(v);
    }

    private async Task Salvar()
    {
        var faltam = new List<string>();
        if (_k!.ObraId == Guid.Empty) faltam.Add("Informe a obra.");
        if (_k.FornecedorId == Guid.Empty) faltam.Add("Informe o fornecedor.");
        if (faltam.Count > 0) { ErrosForm = faltam; Notif.Erro(faltam[0]); return; }
        Guid id = default;
        if (await Executar(async () => id = await ContratosSrv.SalvarContratoAsync(_k, Cancelamento), "Contrato salvo.")) { await Abrir(id); await Carregar(); }
    }

    private async Task Enviar()
    {
        if (await Executar(async () => { await ContratosSrv.SalvarContratoAsync(_k!, Cancelamento); await ContratosSrv.EnviarContratoAprovacaoAsync(_k!.Id, Cancelamento); }, "Contrato enviado para aprovação da Diretoria.")) await Recarregar();
    }

    private async Task Ativar()
    {
        if (_assEmpresa is null || _assFornecedor is null) { Notif.Erro("Informe as duas datas de assinatura."); return; }
        if (await Executar(() => ContratosSrv.AtivarContratoAsync(_k!.Id, _assEmpresa.Value, _assFornecedor.Value, Cancelamento), "Contrato ativo.")) await Recarregar();
    }

    private async Task RegistrarEnvio()
    {
        if (await Executar(() => ContratosSrv.RegistrarEnvioContratoAsync(_k!.Id, Cancelamento), "Envio registrado.")) await Recarregar();
    }
}
