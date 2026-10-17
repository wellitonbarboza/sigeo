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
using Sigeo.Domain.Planejamento;
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

// Código da tela DiarioDeObra.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class DiarioDeObra
{
    [SupplyParameterFromQuery(Name = "obra")] public Guid? ObraQuery { get; set; }
    [SupplyParameterFromQuery(Name = "data")] public string? DataQuery { get; set; }
    private static readonly string[] Meses = ["Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho", "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"];
    private Guid? _obraId;
    private DateOnly _mes, _data;
    private List<DiarioObraServico.DiaMes> _dias = [];
    private Sigeo.Domain.Planejamento.DiarioObra? _d;
    private HashSet<Guid> _salvas = [];
    private List<Opcao> _funcoes = [], _fornecedores = [], _locais = [];
    private bool _podeAssinar, _podePreencher;
    private List<Opcao> _equipObra = [], _equipTodos = [];

    private void TrazerEquipamentos()
    {
        foreach (var e in _equipObra.Where(e => _d!.EquipamentosDia.All(x => x.EquipamentoId != e.Id)))
            _d!.EquipamentosDia.Add(new DiarioEquipamento { DiarioId = _d.Id, EquipamentoId = e.Id });
    }

    protected override async Task OnInitializedAsync()
    {
        _data = DateOnly.TryParse(DataQuery, out var d) && d <= Hoje ? d : Hoje;
        _mes = new DateOnly(_data.Year, _data.Month, 1);
        _podePreencher = Perfil(PerfilUsuario.Engenharia);
        _funcoes = await Cat.FuncoesAsync();
        _fornecedores = await Cat.FornecedoresAsync();
    }

    private async Task TrocarObra(Guid? obra)
    {
        _obraId = obra; _d = null;
        if (obra is { } o)
        {
            _locais = await Cat.LocaisAsync(o);
            _podeAssinar = await Diario.PodeAssinarAsync(o, Cancelamento);
            await using var db = Fabrica.CriarLeitura();
            var almox = await db.Almoxarifados.AsNoTracking().Where(a => a.ObraId == o).Select(a => (Guid?)a.Id).FirstOrDefaultAsync(Cancelamento);
            var eqs = await db.Ativos.AsNoTracking().Where(e => e.Situacao != SituacaoEquipamento.Baixado && e.Situacao != SituacaoEquipamento.Devolvido).OrderBy(e => e.Patrimonio)
                .Select(e => new { e.Id, e.Patrimonio, e.Nome, e.AlmoxarifadoId }).ToListAsync(Cancelamento);
            _equipTodos = eqs.Select(e => new Opcao(e.Id, $"{e.Patrimonio} – {e.Nome}")).ToList();
            _equipObra = almox is null ? [] : eqs.Where(e => e.AlmoxarifadoId == almox).Select(e => new Opcao(e.Id, $"{e.Patrimonio} – {e.Nome}")).ToList();
        }
        await Recarregar();
    }

    private async Task Recarregar()
    {
        if (_obraId is not { } o) return;
        _dias = await Diario.MesAsync(o, _mes.Year, _mes.Month, Cancelamento);
        await Abrir(_data);
    }

    private async Task TrocarMes(int delta)
    {
        _mes = _mes.AddMonths(delta);
        if (_obraId is { } o) _dias = await Diario.MesAsync(o, _mes.Year, _mes.Month, Cancelamento);
    }

    private async Task Abrir(DateOnly data)
    {
        if (_obraId is not { } o) return;
        _data = data; ErrosForm = null;
        _d = await Diario.AbrirAsync(o, data, Cancelamento);
        _salvas = _d.Numero > 0 ? _d.Atividades.Select(a => a.Id).ToHashSet() : [];
        _d.Atividades = _d.Atividades.OrderBy(a => a.Ordem).ToList();
    }

    private void AddAtividade() => _d!.Atividades.Add(new DiarioAtividade { Ordem = _d.Atividades.Count + 1 });

    private void DefinirFuncao(DiarioEfetivo e, ChangeEventArgs ev)
    {
        e.FuncaoObraId = Guid.TryParse(ev.Value?.ToString(), out var g) ? g : null;
        e.Nome = _funcoes.FirstOrDefault(f => f.Id == e.FuncaoObraId)?.Texto ?? "";
    }

    private static void DefinirLocal(DiarioAtividade a, ChangeEventArgs ev) => a.LocalId = Guid.TryParse(ev.Value?.ToString(), out var g) ? g : null;

    private async Task CopiarEfetivo()
    {
        var ant = await Diario.EfetivoAnteriorAsync(_obraId!.Value, _data, Cancelamento);
        if (ant.Count == 0) { Notif.Erro("Nenhum diário anterior com efetivo."); return; }
        _d!.Efetivo = ant;
        Notif.Sucesso($"Efetivo copiado ({ant.Sum(e => e.Quantidade)} pessoas). Ajuste as quantidades do dia.");
    }

    private async Task TrazerPlano()
    {
        var lista = await Diario.AtividadesDoPlanoAsync(_obraId!.Value, _data, Cancelamento);
        var novas = lista.Where(n => _d!.Atividades.All(a => a.Descricao != n.Descricao)).ToList();
        foreach (var n in novas) _d!.Atividades.Add(n);
        if (novas.Count == 0) Notif.Erro("Sem compromissos novos no plano semanal desta semana.");
        else Notif.Sucesso($"{novas.Count} atividade(s) do plano semanal. Descreva o que foi feito e adicione as fotos.");
    }

    private async Task Salvar()
    {
        Guid id = default;
        if (await Executar(async () => id = await Diario.SalvarAsync(_d!, Cancelamento), "Rascunho salvo."))
        {
            _dias = await Diario.MesAsync(_obraId!.Value, _mes.Year, _mes.Month, Cancelamento);
            await Abrir(_data);
        }
    }

    private async Task Assinar()
    {
        var ok = await Executar(async () => { await Diario.SalvarAsync(_d!, Cancelamento); await Diario.AssinarAsync(_d!.Id, Cancelamento); }, "Diário assinado e fechado.");
        if (ok) { _dias = await Diario.MesAsync(_obraId!.Value, _mes.Year, _mes.Month, Cancelamento); await Abrir(_data); }
    }

    private static string Clima(ClimaDia c) => c switch { ClimaDia.Bom => "Bom", ClimaDia.Nublado => "Nublado", ClimaDia.ChuvaFraca => "Chuva fraca", _ => "Chuva forte" };

    private static string Dica(DiarioObraServico.DiaMes? r) => r is null ? "" :
        (r.Status == StatusDiario.Assinado ? "Assinado" : "Rascunho") + (r.Trabalhado == DiaTrabalhado.Sim ? "" : r.Trabalhado == DiaTrabalhado.NaoChuva ? " · não trabalhado (chuva)" : " · não trabalhado") + (r.Chuva ? " · chuva" : "");
}
