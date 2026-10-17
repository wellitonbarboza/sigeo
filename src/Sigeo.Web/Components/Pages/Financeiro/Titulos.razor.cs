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

namespace Sigeo.Web.Components.Pages.Financeiro;

// Código da tela Titulos.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Titulos
{
    private bool Pagar => Nav.Uri.Contains("/financeiro/pagar", StringComparison.OrdinalIgnoreCase);
    private TipoTitulo Tipo => Pagar ? TipoTitulo.Pagar : TipoTitulo.Receber;
    private Grade<LinhaParcelaFin>? _grade;
    private Guid? _obraId;
    private DateOnly? _de, _ate;
    private bool _pagas;
    private List<LinhaParcelaFin> _linhas = [];
    private List<Opcao> _fornecedores = [], _obras = [];
    private List<ContaGerencial> _contas = [];
    private List<CondicaoPagamento> _condicoes = [];
    private List<ContaBancaria> _bancos = [];
    private Titulo? _t;
    private bool _abertoTit, _abertoBaixa, _cancelar;
    private string _condId = "", _motivo = "";
    private LinhaParcelaFin? _baixa, _estornar;
    private DateOnly _bxData;
    private FormaPagamento _bxForma = FormaPagamento.Pix;
    private string _bxConta = "";
    private decimal _bxCorrecao, _bxJuros, _bxMulta, _bxDesconto;
    private string? _bxComprovante;
    private bool TemPaga => _t?.Parcelas.Any(p => p.Status == StatusParcelaTitulo.Paga) == true;

    private static string NomeOrigem(OrigemTitulo o) => o switch
    {
        OrigemTitulo.LiberacaoPagamento => "liberação de pagamento", OrigemTitulo.ImportacaoErp => "importação do ERP", OrigemTitulo.VendaUnidade => "venda de unidade",
        OrigemTitulo.Comissao => "comissão de venda", OrigemTitulo.ApuracaoRet => "apuração do RET", OrigemTitulo.Distrato => "distrato", _ => "lançamento manual"
    };

    private string Situacao(LinhaParcelaFin l) => l.Status == StatusParcelaTitulo.Paga ? (Pagar ? "Paga" : "Recebida") : l.DiasAtraso > 0 ? $"Vencida {l.DiasAtraso}d" : "A vencer";

    protected override async Task OnInitializedAsync()
    {
        _fornecedores = await Cat.FornecedoresAsync();
        _obras = await Cat.ObrasAsync();
        _contas = await Fin.ContasGerenciaisAsync(true, Cancelamento);
        _condicoes = await Fin.CondicoesAsync(Cancelamento);
        _bancos = await Fin.ContasBancariasAsync(true, Cancelamento);
    }

    private TipoTitulo _tipoCarregado;
    protected override async Task OnParametersSetAsync()
    {
        if (_tipoCarregado != Tipo) { _tipoCarregado = Tipo; await Carregar(); }
    }

    private async Task Carregar() => _linhas = await Fin.ParcelasAsync(Tipo, _obraId, _de, _ate, _pagas, Cancelamento);

    private void Novo()
    {
        ErrosForm = null; _condId = "";
        _t = new Titulo { Tipo = Tipo, Emissao = Hoje, ObraId = _obraId };
        _abertoTit = true;
    }

    private async Task AbrirTitulo(Guid id)
    {
        ErrosForm = null; _condId = "";
        _t = await Fin.ObterTituloAsync(id, Cancelamento);
        _abertoTit = _t is not null;
    }

    private void GerarParcelas()
    {
        if (_t is null) return;
        var c = _condicoes.FirstOrDefault(x => x.Id.ToString() == _condId);
        if (c is null) { Notif.Erro("Escolha a condição de pagamento."); return; }
        if (_t.ValorLiquido <= 0) { Notif.Erro("Informe o valor antes."); return; }
        _t.Parcelas = FinanceiroServico.ParcelasPorCondicao(c, _t.ValorLiquido, _t.Emissao);
    }

    private void AddParcela()
    {
        if (_t is null) return;
        var resto = _t.ValorLiquido - _t.Parcelas.Where(p => p.Status != StatusParcelaTitulo.Cancelada).Sum(p => p.Valor);
        _t.Parcelas.Add(new TituloParcela { Numero = _t.Parcelas.Count + 1, Vencimento = _t.Parcelas.Count == 0 ? _t.Emissao : _t.Parcelas.Max(p => p.Vencimento).AddMonths(1), Valor = Math.Max(resto, 0) });
    }

    private async Task Salvar()
    {
        if (_t is null) return;
        if (await Executar(() => Fin.SalvarTituloAsync(_t, Cancelamento), "Título salvo.")) { _abertoTit = false; await Carregar(); }
    }

    private async Task Cancelar()
    {
        if (_t is null) return;
        if (await Executar(() => Fin.CancelarTituloAsync(_t.Id, _motivo, Cancelamento), "Título cancelado.")) { _cancelar = false; _abertoTit = false; _motivo = ""; await Carregar(); }
    }

    private void AbrirBaixa(LinhaParcelaFin l)
    {
        ErrosForm = null; _baixa = l; _bxData = Hoje; _bxCorrecao = _bxJuros = _bxMulta = _bxDesconto = 0; _bxComprovante = null;
        _bxConta = _bancos.Count == 1 ? _bancos[0].Id.ToString() : "";
        _abertoBaixa = true;
    }

    private async Task Baixar()
    {
        if (_baixa is null) return;
        var b = new BaixaParcela(_baixa.ParcelaId, _bxData, Guid.TryParse(_bxConta, out var c) ? c : null, _bxForma, _bxCorrecao, _bxJuros, _bxMulta, _bxDesconto, _bxComprovante);
        if (await Executar(() => Fin.BaixarAsync(b, Cancelamento), Pagar ? "Pagamento registrado." : "Recebimento registrado.")) { _abertoBaixa = false; await Carregar(); }
    }

    private async Task Estornar()
    {
        if (_estornar is null) return;
        var id = _estornar.ParcelaId; _estornar = null;
        if (await Executar(() => Fin.EstornarBaixaAsync(id, Cancelamento), "Baixa estornada.")) await Carregar();
    }
}
