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

// Código da tela MapaCotacao.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class MapaCotacao
{
    [SupplyParameterFromQuery(Name = "id")] public Guid? Id { get; set; }
    private Cotacao? _c;
    private ParametrosVigentes? _p;
    private string _obra = "";
    private bool _naoEncontrada;
    private Dictionary<Guid, string> _nomes = [];
    private List<IGrouping<Guid?, CotacaoItem>> _grupos = [];
    private Dictionary<Guid, List<LocalQuantidade>> _locaisItem = [];
    private readonly HashSet<Guid> _abertos = [];

    private bool Editavel => _c is { Status: StatusCotacao.Aberto or StatusCotacao.AguardandoPropostas or StatusCotacao.EmAnalise or StatusCotacao.ReprovadoVoltaCotacao };
    private string Nome(Guid id) => _nomes.GetValueOrDefault(id, "?");
    private decimal Total(Guid f) => _c!.Itens.Where(i => i.FornecedorId == f).Sum(i => i.ValorTotal) + (_c.Frete == TipoFrete.NaoSeAplica ? 0 : _c.Fornecedores.First(x => x.FornecedorId == f).ValorFrete ?? 0);
    private int ComPreco(Guid f) => _c!.Itens.Count(i => i.FornecedorId == f && i.ValorUnitarioFinal > 0);
    /// <summary>Menor total entre os fornecedores que cotaram todos os itens (com frete).</summary>
    private (Guid Id, decimal Total)? MenorTotal => _c!.Fornecedores.Where(f => ComPreco(f.FornecedorId) == _grupos.Count && _grupos.Count > 0)
        .Select(f => (f.FornecedorId, Total(f.FornecedorId))).OrderBy(x => x.Item2).Cast<(Guid, decimal)?>().FirstOrDefault();
    private decimal MelhorPorItem => _grupos.Sum(g => g.Where(i => i.ValorUnitarioFinal > 0).Select(i => i.ValorTotal).DefaultIfEmpty(0).Min());
    private DateOnly? MelhorPrazo => _c!.Fornecedores.Where(f => f.PrazoEntregaOfertado != null).Select(f => f.PrazoEntregaOfertado).Min();
    private string Diferenca(decimal t) => MenorTotal is { } m && m.Total > 0 && t > 0 && t != m.Total ? $"+{(t - m.Total) / m.Total:P1} vs. menor" : "";
    private void Recalcular() => _c!.Recalcular(_p!);

    private void Vencer(CotacaoItem it)
    {
        try { _c!.DefinirVencedorItem(it.RequisicaoItemId, it.Descricao, it.FornecedorId); ErrosForm = null; }
        catch (RegraDeNegocioException e) { ErrosForm = [e.Message]; }
    }

    private void DefinirQtd(CotacaoItem it, object? valor)
    {
        decimal? q = decimal.TryParse(Convert.ToString(valor, System.Globalization.CultureInfo.InvariantCulture), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
        try { _c!.DefinirVencedorItem(it.RequisicaoItemId, it.Descricao, it.FornecedorId, q); ErrosForm = null; }
        catch (RegraDeNegocioException e) { ErrosForm = [e.Message]; }
    }

    private void MenorPrecoEmTodos()
    {
        foreach (var g in _grupos)
        {
            var melhor = g.Where(i => i.ValorUnitarioFinal > 0).OrderBy(i => i.ValorUnitarioFinal).FirstOrDefault();
            if (melhor is not null) _c!.DefinirVencedorItem(melhor.RequisicaoItemId, melhor.Descricao, melhor.FornecedorId);
        }
        ErrosForm = null;
    }

    private void LimparVencedores() { foreach (var i in _c!.Itens) { i.Vencedor = false; i.QuantidadeVencida = null; } }
    private void AlternarLocais(Guid id) { if (!_abertos.Remove(id)) _abertos.Add(id); }

    private EconomiaCotacao Econ => EconomiaCotacao.Calcular(_c!.FornecedorVencedorId is { } v
        ? _c.Itens.Where(i => i.FornecedorId == v)
        : _grupos.Select(g => g.Where(i => i.ValorUnitarioFinal > 0).OrderBy(i => i.ValorTotal).FirstOrDefault()).OfType<CotacaoItem>());

    [Inject] private MapaCotacaoConsultas Consultas { get; set; } = default!;
    private Dictionary<Guid, (string? Marca, bool Mesma)> _pedidas = [];
    private List<string> _marcas = [];

    protected override async Task OnInitializedAsync()
    {
        _p = await Consultas.ParametrosAsync(Cancelamento);
        await Carregar();
    }

    private async Task Carregar()
    {
        if (Id is not { } id) { _naoEncontrada = true; return; }
        await CotacoesSrv.AtualizarQualificacoesAsync(id, Cancelamento);
        var m = await Consultas.MapaAsync(id, _p!, Cancelamento);
        if (m is null) { _c = null; _naoEncontrada = true; return; }
        (_c, _obra, _nomes, _locaisItem, _pedidas) = m;
        _marcas = await Cat.MarcasAsync();
        _grupos = _c.Itens.GroupBy(i => i.RequisicaoItemId).ToList();
    }

    private async Task Salvar()
    {
        if (_c!.Status == StatusCotacao.AguardandoPropostas && _c.Fornecedores.Any(f => f.Status != StatusConvite.Convidado)) _c.Status = StatusCotacao.EmAnalise;
        if (await Executar(() => CotacoesSrv.SalvarCotacaoAsync(_c!, Cancelamento), "Preços salvos.")) await Carregar();
    }

    private Task<bool> SalvarAntesDaIa() => Editavel ? Executar(() => CotacoesSrv.SalvarCotacaoAsync(_c!, Cancelamento)) : Task.FromResult(true);

    private async Task Imprimir2() => await JS.InvokeVoidAsync("print");

    /// <summary>Planilha do mapa: aba "Propostas" (condições por fornecedor) e aba "Itens" (preços por fornecedor, último, referência e economia).</summary>
    private async Task Excel()
    {
        var c = _c!;
        var forns = c.Fornecedores.ToList();
        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.AddWorksheet("Propostas");
        ws.Cell(1, 1).Value = $"Cotação {c.Numero} – {c.Nome} ({_obra})"; ws.Cell(1, 1).Style.Font.Bold = true;
        string[] linhas = ["Qualificação", "Situação", "Prazo de entrega", "Condição de pagamento", "Validade", "Frete", "Total (itens + frete)"];
        for (var l = 0; l < linhas.Length; l++) ws.Cell(l + 3, 1).Value = linhas[l];
        for (var k = 0; k < forns.Count; k++)
        {
            var f = forns[k]; var col = k + 2;
            ws.Cell(2, col).Value = Nome(f.FornecedorId); ws.Cell(2, col).Style.Font.Bold = true;
            ws.Cell(3, col).Value = De(f.QualificacaoNaData); ws.Cell(4, col).Value = De(f.Status);
            ws.Cell(5, col).Value = f.PrazoEntregaOfertado?.ToString("dd/MM/yyyy"); ws.Cell(6, col).Value = f.CondicaoPagamento;
            ws.Cell(7, col).Value = f.ValidadeProposta?.ToString("dd/MM/yyyy"); ws.Cell(8, col).Value = f.ValorFrete ?? 0;
            ws.Cell(9, col).Value = Total(f.FornecedorId);
            ws.Cell(8, col).Style.NumberFormat.Format = ws.Cell(9, col).Style.NumberFormat.Format = "#,##0.00";
        }
        ws.Columns().AdjustToContents();

        var wi = wb.AddWorksheet("Itens");
        string[] fixos = ["Item", "UN", "Quantidade", "Último", "Referência"];
        for (var k = 0; k < fixos.Length; k++) wi.Cell(1, k + 1).Value = fixos[k];
        for (var k = 0; k < forns.Count; k++)
        {
            var b = fixos.Length + 1 + k * 3;
            wi.Cell(1, b).Value = $"{Nome(forns[k].FornecedorId)} – ofertado"; wi.Cell(1, b + 1).Value = "negociado"; wi.Cell(1, b + 2).Value = "total";
        }
        wi.Row(1).Style.Font.Bold = true;
        var r = 2;
        foreach (var g in _grupos)
        {
            var p = g.First();
            wi.Cell(r, 1).Value = p.Descricao; wi.Cell(r, 2).Value = p.Unidade; wi.Cell(r, 3).Value = p.Quantidade;
            if (p.PrecoUltimo is { } u) wi.Cell(r, 4).Value = u;
            if (p.PrecoReferencia is { } rf) wi.Cell(r, 5).Value = rf;
            for (var k = 0; k < forns.Count; k++)
            {
                var it = g.FirstOrDefault(i => i.FornecedorId == forns[k].FornecedorId);
                if (it is null) continue;
                var b = fixos.Length + 1 + k * 3;
                wi.Cell(r, b).Value = it.ValorOfertado;
                if (!string.IsNullOrWhiteSpace(it.Marca)) wi.Cell(r, b).GetComment().AddText($"Marca: {it.Marca}");
                if (it.ValorNegociado is { } n) wi.Cell(r, b + 1).Value = n;
                wi.Cell(r, b + 2).Value = it.ValorTotal;
            }
            r++;
        }
        wi.Range(2, 3, r, fixos.Length + forns.Count * 3).Style.NumberFormat.Format = "#,##0.00";
        wi.Columns().AdjustToContents();
        if (!string.IsNullOrWhiteSpace(c.AnaliseIa))
        {
            var wa = wb.AddWorksheet("Análise IA");
            wa.Cell(1, 1).Value = c.AnaliseIa; wa.Column(1).Width = 120; wa.Cell(1, 1).Style.Alignment.WrapText = true;
        }
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        await JS.InvokeVoidAsync("qh.baixar", $"mapa-cotacao-{c.Numero}.xlsx", Convert.ToBase64String(ms.ToArray()), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
