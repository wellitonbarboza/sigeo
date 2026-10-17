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
using Sigeo.Domain.Financeiro;
using Sigeo.Domain.Incorporacao;
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

namespace Sigeo.Web.Components.Pages.Incorporacao;

// Código da tela VendasContratos.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class VendasContratos
{
    [SupplyParameterFromQuery(Name = "obra")] public Guid? ObraQuery { get; set; }
    [SupplyParameterFromQuery(Name = "unidade")] public Guid? UnidadeQuery { get; set; }
    [SupplyParameterFromQuery(Name = "contrato")] public Guid? ContratoQuery { get; set; }
    private Grade<LinhaContratoVenda>? _grade;
    private IGrade? GradeAtiva => _aba == "contratos" ? _grade : null;
    private IGrade? _gradeVista;
    protected override void OnAfterRender(bool firstRender) { if (!ReferenceEquals(_gradeVista, GradeAtiva)) { _gradeVista = GradeAtiva; StateHasChanged(); } }

    private Guid? _obraId;
    private string _aba = "contratos";
    private readonly List<(string, string)> _abas = [("contratos", "Contratos"), ("compradores", "Compradores")];
    private List<LinhaContratoVenda> _contratos = [];
    private List<LinhaUnidade> _unidades = [];
    private List<Comprador> _compradores = [];
    private List<Opcao> _fornecedores = [];
    private List<LinhaParcelaFin> _parcelas = [];
    private ContratoVenda? _c;
    private Comprador? _comp;
    private IncorporacaoObra? _inc;
    private bool _aberto, _abertoComp, _distrato, _temRecebido, _abriuQuery;
    private DateOnly _dData;
    private decimal _dValor;
    private string? _dMotivo;
    private bool Bloqueado => _c is not null && (_c.Status != StatusContratoVenda.Ativo || _temRecebido);

    protected override async Task OnInitializedAsync()
    {
        _fornecedores = await Cat.FornecedoresAsync();
        _compradores = await Inc.CompradoresAsync(Cancelamento);
    }

    private async Task TrocarObra(Guid? id)
    {
        _obraId = id;
        await Carregar();
        if (!_abriuQuery && _obraId is not null)
        {
            _abriuQuery = true;
            if (ContratoQuery is { } c) await Abrir(c);
            else if (UnidadeQuery is { } u) { Novo(); if (_c is not null) { _c.UnidadeComercialId = u; _c.ValorVenda = _unidades.FirstOrDefault(x => x.Id == u)?.PrecoTabela ?? 0; } }
        }
    }

    private async Task Carregar()
    {
        if (_obraId is not { } o) { _contratos = []; _unidades = []; return; }
        _contratos = await Inc.ContratosAsync(o, Cancelamento);
        _unidades = await Inc.EspelhoAsync(o, Cancelamento);
        _inc = await Inc.ObterAsync(o, Cancelamento);
    }

    private void Novo()
    {
        if (_obraId is not { } o) { Notif.Erro("Escolha a obra."); return; }
        if (_aba == "compradores") { ErrosForm = null; _comp = new Comprador(); _abertoComp = true; return; }
        ErrosForm = null; _temRecebido = false; _parcelas = [];
        _c = new ContratoVenda
        {
            ObraId = o, DataContrato = Hoje, DataBase = Hoje, IndiceAteChaves = _inc?.IndiceAteChaves ?? "INCC", IndiceAposChaves = _inc?.IndiceAposChaves ?? "IPCA",
            JurosAposChavesMes = _inc?.JurosAposChavesMes ?? 0, DataChaves = _inc?.PrevisaoChaves
        };
        _aberto = true;
    }

    private async Task Abrir(Guid id)
    {
        ErrosForm = null;
        _c = await Inc.ObterContratoAsync(id, Cancelamento);
        _parcelas = _c?.TituloId is { } t ? await Fin.ParcelasDoTituloAsync(t, Cancelamento) : [];
        _temRecebido = _parcelas.Any(p => p.Status == StatusParcelaTitulo.Paga);
        _aberto = _c is not null;
    }

    private void TrocarUnidade(ChangeEventArgs e)
    {
        if (_c is null || !Guid.TryParse(e.Value?.ToString(), out var g)) return;
        _c.UnidadeComercialId = g;
        if (_c.ValorVenda == 0) _c.ValorVenda = _unidades.FirstOrDefault(u => u.Id == g)?.PrecoTabela ?? 0;
    }

    private void AddBloco(TipoParcelaVenda t)
    {
        if (_c is null) return;
        var falta = Math.Max(_c.ValorVenda - _c.Blocos.Sum(b => b.Total), 0);
        var inicio = t switch
        {
            TipoParcelaVenda.Entrada => _c.DataContrato,
            TipoParcelaVenda.Chaves or TipoParcelaVenda.Financiamento => _c.DataChaves ?? _c.DataContrato.AddMonths(24),
            TipoParcelaVenda.Intermediaria => _c.DataContrato.AddMonths(6),
            _ => _c.DataContrato.AddMonths(1)
        };
        var qtd = t switch { TipoParcelaVenda.Mensal => 24, TipoParcelaVenda.Intermediaria => 2, _ => 1 };
        _c.Blocos.Add(new BlocoCondicaoVenda
        {
            Tipo = t, Quantidade = qtd, PrimeiroVencimento = inicio, IntervaloMeses = t == TipoParcelaVenda.Intermediaria ? 12 : 1,
            ValorParcela = qtd == 0 ? 0 : Math.Round(falta / qtd, 2)
        });
    }

    private void AddComissao() => _c?.Comissoes.Add(new ComissaoVenda { Papel = "Corretor" });

    private async Task Salvar()
    {
        if (_c is null) return;
        Guid id = Guid.Empty;
        if (await Executar(async () => id = await Inc.SalvarContratoAsync(_c, Cancelamento), "Venda salva: parcelas em Contas a Receber e comissões em Contas a Pagar."))
        { await Carregar(); await Abrir(id); }
    }

    private async Task SalvarComprador()
    {
        if (_comp is null) return;
        if (await Executar(() => Inc.SalvarCompradorAsync(_comp, Cancelamento), "Comprador salvo."))
        {
            _abertoComp = false;
            _compradores = await Inc.CompradoresAsync(Cancelamento);
            if (_aberto && _c is not null && _c.CompradorId == Guid.Empty) _c.CompradorId = _comp.Id;
        }
    }

    private async Task Distratar()
    {
        if (_c is null) return;
        if (await Executar(() => Inc.DistratarAsync(_c.Id, _dData, _dValor, _dMotivo ?? "", Cancelamento), "Distrato registrado."))
        { _distrato = false; _aberto = false; await Carregar(); }
    }
}
