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
    [Inject] private ParcelasConsultas Consultas { get; set; } = default!;
    private Grade<ParcelaLinha>? _grade;
    private List<ParcelaLinha>? _linhas;
    private List<ContratoRecorrenteResumo> _contratos = [];
    private Guid? _contratoId;
    private ContratoRecorrenteResumo? ContratoAtual => _contratos.FirstOrDefault(c => c.Id == _contratoId);

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
        var r = await Consultas.ListaAsync(_contratoId, LimiteAtual, Cancelamento);
        _contratos = r.Contratos;
        _linhas = r.Linhas;
    }

    private async Task Abrir(Guid id)
    {
        var r = await Consultas.AbrirAsync(id, Cancelamento);
        _p = r.Parcela; _ct = r.Contrato; _usos = r.Usos;
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
        _itensContrato = await Consultas.ItensContratoAsync(_contratoId, Cancelamento);
        _uso = new UsoContrato { ContratoId = _contratoId!.Value, Data = Hoje, Quantidade = 1, ContratoItemId = _itensContrato.FirstOrDefault()?.Id ?? Guid.Empty };
        ErrosForm = null; _abertoUso = true;
    }

    private async Task LancarUso()
    {
        if (await Executar(() => RecSrv.LancarUsoAsync(_uso!, Cancelamento), "Uso lançado — previsto da parcela recalculado.")) { _abertoUso = false; await Carregar(); }
    }
}
