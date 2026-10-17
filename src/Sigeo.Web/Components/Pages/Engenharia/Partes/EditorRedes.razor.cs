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

namespace Sigeo.Web.Components.Pages.Engenharia.Partes;

// Código da tela EditorRedes.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class EditorRedes
{
    [Parameter, EditorRequired] public Guid ObraId { get; set; }
    [Parameter] public bool Editavel { get; set; }
    [Parameter] public EventCallback AoMudar { get; set; }

    private sealed class PacoteForm
    {
        public Guid? Id; public string Nome = ""; public Guid? ComposicaoId; public int Duracao = 2; public int Equipes = 1;
        public Dictionary<Guid, bool> Ajustes = [];
    }
    private sealed class RedeForm
    {
        public Guid? Id; public string Nome = ""; public TipoRede Tipo = TipoRede.PorApartamento; public string Cor = "#4f52d6"; public int Pulmao;
        public HashSet<Guid> Locais = []; public List<PacoteForm> Pacotes = [];
    }

    private List<RedePlanejamento> _redes = [];
    private List<VinculoRede> _vinculos = [];
    private List<Local> _locais = [];
    private Dictionary<Guid, Local> _porId = [];
    private ILookup<Guid?, Local> _filhos = Array.Empty<Local>().ToLookup(l => l.LocalPaiId);
    private List<Opcao> _comps = [];
    private Dictionary<Guid, string> _nomeComp = [];
    private List<Opcao> _pacotes = [];
    private Func<Guid, Guid, decimal> _custo = (_, _) => 0;
    private RedeForm? _form;
    private bool _verMatriz, _todasTorres;
    private Guid? _vOrigem, _vDestino;
    private ModoVinculoRede _vModo = ModoVinculoRede.PorPavimento;
    private int _vDesl = 2, _vLag;
    private Guid _obraCarregada;

    protected override async Task OnParametersSetAsync()
    {
        if (_obraCarregada == ObraId) return;
        _obraCarregada = ObraId; _form = null;
        await Carregar();
    }

    private async Task Carregar()
    {
        await using var db = Fabrica.CriarLeitura();
        _redes = await db.RedesPlanejamento.AsNoTracking().Include(r => r.Locais).Include(r => r.Pacotes).ThenInclude(p => p.Ajustes).AsSplitQuery()
            .Where(r => r.ObraId == ObraId).OrderBy(r => r.Ordem).ToListAsync(Cancelamento);
        _vinculos = await db.VinculosRede.AsNoTracking().Where(v => v.ObraId == ObraId).ToListAsync(Cancelamento);
        var defs = await PlanejamentoServico.DefinicoesAsync(db, ObraId, Cancelamento);
        _custo = defs.Custo; _locais = defs.Locais;
        _porId = _locais.ToDictionary(l => l.Id);
        _filhos = _locais.ToLookup(l => l.LocalPaiId);
        var usados = await db.ServicosLocais.AsNoTracking().Where(s => s.ObraId == ObraId).Select(s => s.ComposicaoId).Distinct().ToListAsync(Cancelamento);
        var comps = await db.Composicoes.AsNoTracking().Where(c => c.Ativa).OrderBy(c => c.Codigo).Select(c => new { c.Id, c.Codigo, c.Descricao }).ToListAsync(Cancelamento);
        _nomeComp = comps.ToDictionary(c => c.Id, c => c.Descricao);
        _comps = comps.OrderBy(c => usados.Contains(c.Id) ? 0 : 1).ThenBy(c => c.Codigo).Select(c => new Opcao(c.Id, $"{c.Codigo} – {c.Descricao}{(usados.Contains(c.Id) ? "" : " (fora do orçamento)")}")).ToList();
        _pacotes = _redes.SelectMany(r => r.Pacotes.OrderBy(p => p.Ordem).Select(p => new Opcao(p.Id, $"{r.Nome} › {p.Nome}"))).ToList();
    }

    private IEnumerable<Local> Torres => _locais.Where(l => l.Tipo == GeradorEmpreendimento.TipoTorre).OrderBy(l => l.Codigo);
    private bool TemSelecao(Local torre) => _form is not null && MotorOrcamento.Subarvore(torre.Id, _filhos).Any(_form.Locais.Contains);
    private IEnumerable<Local> Filhos(Guid pai, string tipo) => _filhos[pai].Where(l => l.Tipo == tipo);
    private string NomePai(Local l) => l.LocalPaiId is { } p && _porId.TryGetValue(p, out var pai) ? (pai.LocalPaiId is { } pp && _porId.TryGetValue(pp, out var avo) ? $"{avo.Nome} › {pai.Nome}" : pai.Nome) : "Obra";

    private IEnumerable<Local> Candidatos(TipoRede tipo) => tipo switch
    {
        TipoRede.PorPavimento => OrdemArvore().Where(l => l.Tipo == GeradorEmpreendimento.TipoPavimento),
        _ => OrdemArvore().Where(l => l.Tipo == GeradorEmpreendimento.TipoAreaComum || l.Tipo == GeradorEmpreendimento.TipoTorre
                                      || l.Tipo == GeradorEmpreendimento.TipoCondominio || !GeradorEmpreendimento.Gerados.Contains(l.Tipo))
    };

    /// <summary>Locais em ordem da árvore (torre, pavimento de baixo para cima, apartamento).</summary>
    private List<Local> OrdemArvore()
    {
        var r = new List<Local>();
        void V(Guid? pai) { foreach (var l in _filhos[pai].OrderBy(l => l.Tipo == GeradorEmpreendimento.TipoPavimento ? l.Ordem : 0).ThenBy(l => l.Ordem).ThenBy(l => l.Codigo)) { r.Add(l); V(l.Id); } }
        V(null);
        return r;
    }

    private List<Guid> OrdemLocais(RedeForm f) => OrdemArvore().Where(l => f.Locais.Contains(l.Id)).Select(l => l.Id).ToList();

    private void Marcar(Guid id, bool v) { if (_form is null) return; if (v) _form.Locais.Add(id); else _form.Locais.Remove(id); }
    private void Alternar(IEnumerable<Guid> ids)
    {
        if (_form is null) return;
        var l = ids.ToList(); var todos = l.All(_form.Locais.Contains);
        foreach (var id in l) Marcar(id, !todos);
    }

    private bool Incluido(PacoteForm p, Guid local, TipoRede tipo) =>
        p.Ajustes.TryGetValue(local, out var m) ? m : p.ComposicaoId is not { } c || tipo != TipoRede.PorLocal || _custo(c, local) > 0;

    private decimal CustoRede(RedePlanejamento r) =>
        r.Pacotes.Where(p => p.ComposicaoId != null).Sum(p => r.Locais.Sum(l => _custo(p.ComposicaoId!.Value, l.LocalId)));

    private string NomePacote(Guid id) => _pacotes.FirstOrDefault(p => p.Id == id)?.Texto ?? "(pacote excluído)";

    private void Nova() { ErrosForm = null; _verMatriz = false; _form = new RedeForm { Nome = "Nova rede", Cor = new[] { "#4f52d6", "#64748b", "#0f8a6b", "#d4691e", "#a23ea8" }[_redes.Count % 5] }; }

    private void Editar(RedePlanejamento r)
    {
        ErrosForm = null; _todasTorres = false; _verMatriz = r.Pacotes.Any(p => p.Ajustes.Count > 0);
        _form = new RedeForm
        {
            Id = r.Id, Nome = r.Nome, Tipo = r.Tipo, Cor = r.Cor, Pulmao = r.Pulmao, Locais = r.Locais.Select(l => l.LocalId).ToHashSet(),
            Pacotes = r.Pacotes.OrderBy(p => p.Ordem).Select(p => new PacoteForm { Id = p.Id, Nome = p.Nome, ComposicaoId = p.ComposicaoId, Duracao = p.DuracaoPorLocal, Equipes = p.Equipes, Ajustes = p.Ajustes.ToDictionary(a => a.LocalId, a => a.Incluido) }).ToList()
        };
    }

    private void AddPacote() => _form?.Pacotes.Add(new PacoteForm());
    private void Trocar(int a, int b) { if (_form is null || b < 0 || b >= _form.Pacotes.Count) return; (_form.Pacotes[a], _form.Pacotes[b]) = (_form.Pacotes[b], _form.Pacotes[a]); }
    private void EscolherServico(PacoteForm p, string? v)
    {
        p.ComposicaoId = Guid.TryParse(v, out var g) ? g : null;
        if (p.ComposicaoId is { } c && string.IsNullOrWhiteSpace(p.Nome) && _nomeComp.TryGetValue(c, out var n)) p.Nome = n;
    }

    private async Task Salvar()
    {
        if (_form is not { } f) return;
        var e = new PlanejamentoServico.RedeEdicao(f.Id, ObraId, f.Nome, f.Tipo, f.Cor, f.Pulmao, OrdemLocais(f),
            f.Pacotes.Select(p => new PlanejamentoServico.PacoteEdicao(p.Id, p.Nome, p.ComposicaoId, p.Duracao, p.Equipes, p.Ajustes)).ToList());
        Guid id = default;
        if (await Executar(async () => id = await Plan.SalvarRedeAsync(e, Cancelamento), "Rede salva. Gere o cronograma pelas redes para aplicar."))
        {
            await Carregar();
            if (_redes.FirstOrDefault(r => r.Id == id) is { } r) Editar(r);
            await AoMudar.InvokeAsync();
        }
    }

    private async Task Excluir(Guid id)
    {
        if (await Executar(() => Plan.ExcluirRedeAsync(id, Cancelamento), "Rede excluída.")) { _form = null; await Carregar(); await AoMudar.InvokeAsync(); }
    }

    private async Task Sugerir()
    {
        var n = 0;
        if (await Executar(async () => n = await Plan.SugerirRedesAsync(ObraId, Cancelamento)))
        {
            Notif.Sucesso($"{n} rede(s) sugerida(s) pelo orçamento. Revise durações, equipes e a ordem dos pacotes.");
            await Carregar(); if (_redes.Count > 0) Editar(_redes[0]);
            await AoMudar.InvokeAsync();
        }
    }

    private async Task AddVinculo()
    {
        if (_vOrigem is not { } o || _vDestino is not { } d) { ErrosForm = ["Escolha os pacotes de origem e destino."]; return; }
        var v = new VinculoRede { ObraId = ObraId, PacoteOrigemId = o, PacoteDestinoId = d, Modo = _vModo, Deslocamento = _vModo == ModoVinculoRede.PorPavimento ? _vDesl : 0, Lag = _vLag };
        if (await Executar(() => Plan.SalvarVinculoRedeAsync(v, Cancelamento), "Vínculo incluído.")) { _vOrigem = _vDestino = null; await Carregar(); }
    }

    private async Task ExcluirVinculo(Guid id)
    {
        if (await Executar(() => Plan.ExcluirVinculoRedeAsync(id, Cancelamento), "Vínculo excluído.")) await Carregar();
    }
}
