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

// Código da tela Medicoes.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Medicoes
{
    private sealed record Linha(Guid Id, string Contrato, int Numero, string Fornecedor, DateOnly Inicio, DateOnly Fim, decimal Valor, decimal? Nota, FaixaNota Faixa, StatusMedicao Status, string CriadoPor);
    private Grade<Linha>? _grade;
    private List<Linha>? _itens;
    private Medicao? _m;
    private CompetenciaDocumental? _comp;
    private ParametrosVigentes? _p;
    private bool _aberto, _nova;
    private int _versao;
    private Dictionary<Guid, (decimal Qtd, Guid? LocalId, Guid? FvsId)> _qtd = [];
    private int _versaoQtd;

    /// <summary>Número inválido não vira zero: avisa e mantém o valor anterior.</summary>
    private void DefinirQtd(Guid item, object? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor?.ToString()) && LerDecimal(valor, out var d) && d >= 0) { var q = _qtd[item]; _qtd[item] = (d, q.LocalId, q.FvsId); ErrosForm = null; return; }
        ErrosForm = [$"Quantidade do período inválida: \"{valor}\". Mantido o valor anterior."];
        Notif.Erro(ErrosForm[0]);
        _versaoQtd++;
    }

    private void DefinirNota(string criterio, Action<decimal?> set, object? valor)
    {
        if (string.IsNullOrWhiteSpace(valor?.ToString())) { set(null); return; }
        if (LerDecimal(valor, out var d) && d is >= 0 and <= 10) { set(d); ErrosForm = null; return; }
        ErrosForm = [$"Nota inválida em \"{criterio}\": \"{valor}\" (0 a 10). Mantida a nota anterior."];
        Notif.Erro(ErrosForm[0]);
        _versaoQtd++;
    }
    private List<Fvs> _fvsLib = [];
    private Dictionary<Guid, string> _locNomes = [];
    private int _colabBloq;
    private string? _sugestao;
    private List<Opcao> _contratos = [];
    private Guid? _novoContrato;
    private DateOnly _ini, _fim;

    private bool Editavel => _m is { Status: StatusMedicao.EmElaboracao or StatusMedicao.Reprovada };
    private string Pct(string chave) => _p is null ? "" : $"{_p[chave] * 100:0}%";
    private string Pct(decimal v) => $"{v * 100:0}%";

    private IEnumerable<(string, decimal, Func<decimal?>, Action<decimal?>, bool)> Notas() =>
    [
        ("Cumprimento do escopo/prazo", _p![Parametros.PesoEscopo], () => _m!.NotaEscopo, v => _m!.NotaEscopo = v, false),
        ("Material (quando fornece)", _p[Parametros.PesoMaterial], () => _m!.NotaMaterial, v => _m!.NotaMaterial = v, true),
        ("Qualidade do serviço", _p[Parametros.PesoServico], () => _m!.NotaServico, v => _m!.NotaServico = v, false),
        ("Limpeza e organização", _p[Parametros.PesoLimpeza], () => _m!.NotaLimpeza, v => _m!.NotaLimpeza = v, false),
        ("Documentação e segurança", _p[Parametros.PesoDocSeg], () => _m!.NotaDocSeg, v => _m!.NotaDocSeg = v, false),
    ];

    protected override async Task OnInitializedAsync()
    {
        await using (var db = Fabrica.CriarLeitura()) _p = await ParametrosServico.CarregarAsync(db, Cancelamento);
        await Carregar();
        if (Nav.Uri.Contains("?id=") && Guid.TryParse(Nav.Uri.Split("?id=")[1], out var id)) await Abrir(id);
        else if (Nav.Uri.Contains("?contrato=") && Guid.TryParse(Nav.Uri.Split("?contrato=")[1], out var cid)) { await AbrirNova(); _novoContrato = cid; }
    }

    private async Task Carregar()
    {
        await using var db = Fabrica.CriarLeitura();
        _itens = await db.Medicoes.AsNoTracking().OrderByDescending(m => m.CriadoEm).Take(LimiteAtual)
            .Select(m => new Linha(m.Id, m.Contrato.Numero, m.NumeroMedicao, m.Contrato.Fornecedor.RazaoSocial, m.PeriodoInicio, m.PeriodoFim, m.ValorPeriodo, m.NotaFinal, m.Faixa, m.Status, m.CriadoPor)).ToListAsync(Cancelamento);
    }

    private async Task AbrirNova()
    {
        await using var db = Fabrica.CriarLeitura();
        _contratos = await db.Contratos.AsNoTracking().Where(k => k.Status == StatusContrato.Ativo).OrderBy(k => k.Numero)
            .Select(k => new Opcao(k.Id, k.Numero + " – " + k.Fornecedor.RazaoSocial)).ToListAsync(Cancelamento);
        _fim = Hoje; _ini = Hoje.AddDays(-Hoje.Day + 1); _novoContrato = null; ErrosForm = null; _nova = true;
    }

    private async Task CriarMedicao()
    {
        Guid id = default;
        if (await Executar(async () => id = await MedicoesSrv.NovaMedicaoAsync(_novoContrato!.Value, _ini, _fim, Cancelamento), "Medição criada.")) { _nova = false; await Carregar(); await Abrir(id); }
    }

    private async Task Abrir(Guid id)
    {
        await using var db = Fabrica.CriarLeitura();
        _m = await MedicoesServico.CarregarMedicaoAsync(db, id, Cancelamento);
        _qtd = _m.Itens.ToDictionary(i => i.Id, i => (i.QtdPeriodo, i.LocalId, i.FvsId));
        var fornId = _m.Contrato.FornecedorId;
        _fvsLib = await db.Fvss.AsNoTracking().Include(f => f.Itens).Where(f => f.ObraId == _m.ObraId && f.FornecedorId == fornId).ToListAsync(Cancelamento);
        _locNomes = new Sigeo.Application.Comum.ArvoreLocais(await db.Locais.AsNoTracking().Where(l => l.ObraId == _m.ObraId).ToListAsync(Cancelamento)).Nomes();
        var doPeriodo = _fvsLib.Where(f => f.Status != StatusFvs.EmInspecao && DateOnly.FromDateTime(f.DataInspecao.Date) <= _m.PeriodoFim && DateOnly.FromDateTime(f.DataInspecao.Date) >= _m.PeriodoInicio.AddDays(-30)).ToList();
        var rncs = await db.Rncs.AsNoTracking().CountAsync(r => r.FornecedorId == fornId && r.Origem == OrigemRnc.Fvs, Cancelamento);
        _sugestao = doPeriodo.Count == 0 ? null : $"Qualidade do serviço sugerida pelas FVS: {10m * doPeriodo.Count(f => f.Aprovada1aInspecao) / doPeriodo.Count:0.0} ({doPeriodo.Count(f => f.Aprovada1aInspecao)}/{doPeriodo.Count} aprovadas na 1ª inspeção; {rncs} RNC de FVS do prestador).";
        _fvsLib = _fvsLib.Where(f => f.Status == StatusFvs.Liberada).ToList();
        var comps = await CompetenciasServico.DoContratoAsync(db, _m.ContratoId, Hoje, _m.PeriodoFim, Cancelamento);
        var mesMed = new DateOnly(_m.PeriodoFim.Year, _m.PeriodoFim.Month, 1);
        _comp = comps.FirstOrDefault(c => c.Mes == mesMed) ?? comps.OrderByDescending(c => c.Mes).FirstOrDefault();
        _colabBloq = await db.Colaboradores.CountAsync(c => c.FornecedorId == fornId && c.ObraId == _m.ObraId && c.Situacao == SituacaoColaborador.Bloqueado && (c.DataSaida == null || c.DataSaida >= _m.PeriodoInicio), Cancelamento);
        ErrosForm = null; _versao++; _aberto = true;
    }

    private async Task Recarregar() { if (_m is not null) await Abrir(_m.Id); await Carregar(); }

    private Task Gravar() => MedicoesSrv.SalvarMedicaoAsync(_m!.Id, _qtd, _m.NotaEscopo, _m.NotaMaterial, _m.NotaServico, _m.NotaLimpeza, _m.NotaDocSeg, _m.DataReuniao, Cancelamento);

    private async Task Salvar() { if (await Executar(Gravar, "Medição calculada.")) await Recarregar(); }

    private async Task Enviar()
    {
        if (await Executar(async () => { await Gravar(); await MedicoesSrv.EnviarMedicaoAsync(_m!.Id, Cancelamento); }, "Medição enviada para aprovação.")) await Recarregar();
        else await Recarregar();
    }
}
