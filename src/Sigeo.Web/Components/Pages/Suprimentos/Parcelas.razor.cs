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

// Código da tela Parcelas.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Parcelas
{
    private sealed record Linha(ParcelaContrato P, string Contrato, string Fornecedor, bool Fora);
    private sealed record ContratoRec(Guid Id, string Numero, string Fornecedor, FormaCobranca FormaCobranca, decimal Tolerancia);
    private Grade<Linha>? _grade;
    private List<Linha>? _linhas;
    private List<ContratoRec> _contratos = [];
    private Guid? _contratoId;
    private ContratoRec? ContratoAtual => _contratos.FirstOrDefault(c => c.Id == _contratoId);

    private ParcelaContrato? _p;
    private Contrato? _ct;
    private List<UsoContrato> _usos = [];
    private bool _aberto;
    private string? _notaNumero, _justificativa, _motivo;
    private decimal _notaValor;
    private DateOnly _pagoEm;
    private UsoContrato? _uso;
    private bool _abertoUso;
    private List<Opcao> _itensContrato = [];

    private static string Periodo(ParcelaContrato p) => $"{p.Inicio:dd/MM} a {p.Fim:dd/MM/yyyy}";

    protected override async Task OnInitializedAsync()
    {
        var q = Nav.ToAbsoluteUri(Nav.Uri).Query;
        if (q.Contains("contrato=") && Guid.TryParse(q.Split("contrato=")[1].Split('&')[0], out var c)) _contratoId = c;
        await Carregar();
    }

    private async Task Carregar()
    {
        await using var db = Fabrica.CriarLeitura();
        _contratos = await db.Contratos.AsNoTracking().Where(c => c.Recorrente && c.FormaCobranca != FormaCobranca.Medicao).OrderBy(c => c.Numero)
            .Select(c => new ContratoRec(c.Id, c.Numero, c.Fornecedor.RazaoSocial, c.FormaCobranca, c.ToleranciaPerc)).ToListAsync(Cancelamento);
        var porId = _contratos.ToDictionary(c => c.Id);
        var q = db.ParcelasContrato.AsNoTracking().Where(p => p.Status != StatusParcela.Cancelada);
        if (_contratoId is { } id) q = q.Where(p => p.ContratoId == id);
        var lista = await q.OrderBy(p => p.Vencimento).ThenBy(p => p.Numero).Take(LimiteAtual).ToListAsync(Cancelamento);
        _linhas = lista.Where(p => porId.ContainsKey(p.ContratoId)).Select(p =>
        {
            var c = porId[p.ContratoId];
            return new Linha(p, c.Numero, c.Fornecedor, p.ForaDaTolerancia(c.Tolerancia));
        }).ToList();
    }

    private async Task Abrir(Guid id)
    {
        await using var db = Fabrica.CriarLeitura();
        _p = await db.ParcelasContrato.AsNoTracking().FirstAsync(p => p.Id == id, Cancelamento);
        _ct = await db.Contratos.AsNoTracking().Include(c => c.Fornecedor).Include(c => c.Itens).FirstAsync(c => c.Id == _p.ContratoId, Cancelamento);
        _usos = _ct.FormaCobranca == FormaCobranca.Uso
            ? await db.UsosContrato.AsNoTracking().Where(u => u.ContratoId == _ct.Id && u.Data >= _p.Inicio && u.Data <= _p.Fim).OrderBy(u => u.Data).ToListAsync(Cancelamento)
            : [];
        _notaNumero = _p.NotaNumero; _notaValor = _p.NotaValor ?? _p.ValorPrevisto; _justificativa = _p.Justificativa; _motivo = null; _pagoEm = Hoje;
        ErrosForm = null; _aberto = true;
    }

    private async Task Depois(string? msg = null) { await Carregar(); if (_p is not null) await Abrir(_p.Id); }

    private async Task Gerar()
    {
        var n = 0;
        if (await Executar(async () => n = await RecSrv.GerarParcelasAsync(_contratoId!.Value, Cancelamento)))
        { Notif.Sucesso(n == 0 ? "Parcelas atualizadas." : $"{n} parcela(s) criada(s)."); await Carregar(); }
    }

    private async Task Vincular()
    {
        var n = 0;
        if (await Executar(async () => n = await RecSrv.VincularNotasAsync(Cancelamento)))
        { Notif.Sucesso(n == 0 ? "Nenhuma nota nova para vincular." : $"{n} nota(s) vinculada(s)."); await Carregar(); }
    }

    private async Task LancarNota()
    {
        if (string.IsNullOrWhiteSpace(_notaNumero) || _notaValor <= 0) { ErrosForm = ["Informe o número e o valor da nota."]; return; }
        if (await Executar(() => RecSrv.LancarNotaAsync(_p!.Id, _notaNumero.Trim(), _notaValor, Cancelamento), "Nota lançada — segue para aprovação.")) await Depois();
    }

    private async Task Aprovar() { if (await Executar(() => RecSrv.AprovarAsync(_p!.Id, _justificativa, Cancelamento), "Parcela aprovada.")) await Depois(); }
    private async Task Liberar() { if (await Executar(() => RecSrv.LiberarAsync(_p!.Id, Cancelamento), "Pagamento liberado.")) await Depois(); }
    private async Task Pagar() { if (await Executar(() => RecSrv.MarcarPagaAsync(_p!.Id, _pagoEm, Cancelamento), "Parcela paga.")) await Depois(); }

    private async Task Devolver()
    {
        if (string.IsNullOrWhiteSpace(_motivo)) { ErrosForm = ["Informe o motivo da devolução."]; return; }
        if (await Executar(() => RecSrv.DevolverAsync(_p!.Id, _motivo, Cancelamento), "Parcela devolvida.")) await Depois();
    }

    private async Task CancelarParcela()
    {
        if (string.IsNullOrWhiteSpace(_motivo)) { ErrosForm = ["Informe o motivo do cancelamento."]; return; }
        if (await Executar(() => RecSrv.CancelarParcelaAsync(_p!.Id, _motivo, Cancelamento), "Parcela cancelada.")) { _aberto = false; await Carregar(); }
    }

    private void ExcluirUso(UsoContrato u) => Confirmar($"Excluir o uso de {Data(u.Data)} ({u.Quantidade:N2})? O previsto da parcela é recalculado.", async () =>
    {
        if (await Executar(() => RecSrv.ExcluirUsoAsync(u.Id, Cancelamento), "Uso excluído.")) await Depois();
    });

    private async Task AbrirUso()
    {
        await using var db = Fabrica.CriarLeitura();
        _itensContrato = (await db.ContratoItens.AsNoTracking().Where(i => i.ContratoId == _contratoId).OrderBy(i => i.Item).ToListAsync(Cancelamento))
            .Select(i => new Opcao(i.Id, $"{i.Descricao} ({i.Unidade}) · {i.PrecoUnitario:N2}")).ToList();
        _uso = new UsoContrato { ContratoId = _contratoId!.Value, Data = Hoje, Quantidade = 1, ContratoItemId = _itensContrato.FirstOrDefault()?.Id ?? Guid.Empty };
        ErrosForm = null; _abertoUso = true;
    }

    private async Task LancarUso()
    {
        if (await Executar(() => RecSrv.LancarUsoAsync(_uso!, Cancelamento), "Uso lançado — previsto da parcela recalculado.")) { _abertoUso = false; await Carregar(); }
    }
}
