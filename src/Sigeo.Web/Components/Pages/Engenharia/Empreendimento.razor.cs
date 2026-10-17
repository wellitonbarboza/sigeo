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
using Sigeo.Web.Components.Pages.Engenharia.Partes;
using Sigeo.Web.Components.Shared;
using Sigeo.Web.Components;
using Sigeo.Web.Seguranca;
using Sigeo.Web.Servicos;
using Sigeo.Web;
using static Microsoft.AspNetCore.Components.Web.RenderMode;
using static Sigeo.Web.Servicos.Rotulos;

namespace Sigeo.Web.Components.Pages.Engenharia;

// Código da tela Empreendimento.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Empreendimento
{
    [Inject] private EmpreendimentoConsultas Consultas { get; set; } = default!;
    private sealed class TorreForm { public string Nome { get; set; } = ""; public Guid TipologiaId { get; set; } }

    private List<Spe> _spes = [];
    private List<Tipologia> _tips = [];
    private List<Local> _unid = [];
    private List<LinhaResumoCondominio> _resumo = [];
    private readonly Dictionary<Guid, decimal> _editadas = [];
    private Spe? _s;
    private Quadra? _q;
    private List<Quadra> _quadras = [];
    private List<string> _tiposAgrup = [];
    private Guid? _obraId, _gQuadraId, _fLocal, _fCond;
    private bool _abertoCond, _abertoLoc;
    private string _aba = "obra";
    private List<TorreForm> _gTorres = [];
    private List<(string, string)> _abas => [("obra", "Dados da obra"), ("cond", $"{Pl(_n.Condominio)} (SPE)"), ("ger", "Gerador e resumo"), ("uni", $"{Pl(_n.Unidade)} e fração ideal"), ("loc", "Locais")];
    private EstruturaLocais _n = EstruturaLocais.Condominios;
    /// <summary>Plural simples dos nomes de nível (Torre → Torres, Nível → Níveis, Casa → Casas).</summary>
    private static string Pl(string s) => s.Length == 0 ? s : s.EndsWith('l') ? s[..^1] + "is" : s.EndsWith('m') ? s[..^1] + "ns" : s[^1] is 'r' or 'z' or 's' ? s + "es" : s + "s";
    private static readonly string[] TiposManuais = [GeradorEmpreendimento.TipoAreaComum, "Elemento", "Infraestrutura", "Canteiro"];
    private List<Obra> _todasObras = [];
    /// <summary>O cadastro lista todas as obras (inclusive inativas), não só as do catálogo.</summary>
    private List<Opcao> _opcoesObras = [];
    private bool ObraPropria => _o?.DonoCusto == DonoCustoObra.Construtora;
    private Obra? _o;
    private Local? _l;
    private List<Local> _locais = [];
    private List<Opcao> _usuarios = [], _docs = [], _estabsProprios = [];
    [SupplyParameterFromQuery(Name = "obra")] public Guid? ObraQuery { get; set; }
    private const int TamPag = 200;
    private static readonly string[] UnidadesRef = ["m²", "m", "m³", "un", "kg", "vb"];
    private int _pagLoc, _pagUni;
    private List<(Local, int)>? _arvore;
    private List<(Local, int)> ArvoreLista => _arvore ??= Arvore().ToList();
    private List<Local>? _unidFiltradas;
    private Dictionary<Guid, string> _nomeTip = [];
    private Spe? _sp;
    private bool _abertoSpe;

    internal static bool SpePreenchida(Spe s) => !string.IsNullOrWhiteSpace(s.Cnpj) || !string.IsNullOrWhiteSpace(s.RazaoSocial);

    private int AptosTorre(Tipologia t) => Enumerable.Range(1, Math.Max(0, t.Pavimentos)).Sum(p => t.Mix.Where(m => p >= m.PavimentoDe && p <= m.PavimentoAte).Sum(m => m.Quantidade));

    private IEnumerable<(Local, int)> Arvore()
    {
        var filhos = _locais.ToLookup(l => l.LocalPaiId);
        IEnumerable<(Local, int)> Desce(Guid? pai, int n, bool completo)
        {
            foreach (var l in filhos[pai].OrderBy(l => l.Tipo == GeradorEmpreendimento.TipoAreaComum ? 0 : 1).ThenBy(l => l.Ordem).ThenBy(l => l.Codigo))
            {
                yield return (l, n);
                var abrir = completo || l.Tipo == GeradorEmpreendimento.TipoCondominio || (l.Tipo == GeradorEmpreendimento.TipoTorre && l.Id == _fLocal);
                if (!abrir && l.Tipo == GeradorEmpreendimento.TipoTorre) { foreach (var f in filhos[l.Id].Where(f => f.Tipo == GeradorEmpreendimento.TipoAreaComum)) yield return (f, n + 1); continue; }
                foreach (var f in Desce(l.Id, n + 1, completo || l.Id == _fLocal)) yield return f;
            }
        }
        var ids = _locais.Select(l => l.Id).ToHashSet();
        var raizes = _locais.Where(l => l.LocalPaiId is null || !ids.Contains(l.LocalPaiId.Value)).Select(l => l.LocalPaiId).Distinct();
        return raizes.SelectMany(r => Desce(r, 0, false));
    }

    private async Task TrocarObra(Guid? obra)
    {
        _obraId = obra;
        _gTorres = []; _gQuadraId = null; _aba = "obra"; _o = null; ErrosForm = null;
        _fLocal = null; _fCond = null; _pagLoc = 0; _pagUni = 0;
        _locais = []; _quadras = []; _spes = []; _tips = []; _unid = []; _resumo = []; _arvore = null; _unidFiltradas = null;
        await Carregar();
    }

    private void NovaObra() { _obraId = null; _aba = "obra"; ErrosForm = null; _o = new Obra { Status = StatusObra.EmExecucao }; }

    private async Task SalvarObra()
    {
        var ok = await Executar(() => Eng.SalvarObraAsync(_o!, Cancelamento), "Obra salva.");
        if (ok) { Cat.Invalidar(); _obraId = _o!.Id; await Carregar(); }
    }

    private void NovoLocal() { _l = new Local { ObraId = _obraId!.Value, Tipo = TiposManuais[0] }; ErrosForm = null; _abertoLoc = true; }
    private void EditarLocal(Local l) { _l = l; ErrosForm = null; _abertoLoc = true; }
    private async Task SalvarLocal()
    {
        if (await Executar(() => Qob.SalvarLocalAsync(_l!, ct: Cancelamento), "Local salvo.")) { _abertoLoc = false; Cat.Invalidar(); await Carregar(); }
    }

    private List<Local> UnidadesFiltradas
    {
        get
        {
            if (_unidFiltradas is not null) return _unidFiltradas;
            if (_fCond is not { } q || _quadras.FirstOrDefault(x => x.Id == q)?.LocalId is not { } raiz) return _unidFiltradas = _unid;
            var sub = MotorOrcamento.Subarvore(raiz, _locais.ToLookup(l => l.LocalPaiId));
            return _unidFiltradas = _unid.Where(u => sub.Contains(u.Id)).ToList();
        }
    }

    private void DefinirFracao(Local u, object? valor)
    {
        if (LerDecimal(valor, out var v) && v >= 0 && v <= 100) { u.FracaoIdeal = Math.Round(v / 100, 8); _editadas[u.Id] = u.FracaoIdeal.Value; return; }
        ErrosForm = [$"Fração ideal inválida em {u.Codigo}: \"{valor}\" (0 a 100%)."];
        Notif.Erro(ErrosForm[0]);
    }

    protected override async Task OnInitializedAsync()
    {
        _n = await Cat.EstruturaAsync();
        _usuarios = await Cat.UsuariosAsync(); _docs = await Cat.DocumentosSgqAsync();
        _estabsProprios = (await Cat.EstabelecimentosAsync()).Where(e => e.Extra is "Matriz" or "Filial").ToList();
        _tiposAgrup = await Cat.ListaAsync(TipoLista.TipoAgrupamento);
        if (_tiposAgrup.Count == 0) _tiposAgrup = [.. CargaPadrao.TiposAgrupamento];
        // A obra inicial (?obra= ou a única permitida) vem do SeletorObra, que chama TrocarObra; aqui só a lista do seletor.
        DefinirObras(await Consultas.ObrasAsync(Cancelamento));
    }

    private void DefinirObras(List<Obra> obras)
    {
        _todasObras = obras;
        _opcoesObras = obras.Select(o => new Opcao(o.Id, $"{o.Codigo} – {o.Nome}{(o.Ativa ? "" : " (inativa)")}")).ToList();
    }

    private async Task Carregar()
    {
        _editadas.Clear(); _arvore = null; _unidFiltradas = null;
        var dados = await Consultas.CarregarAsync(_obraId, Cancelamento);
        DefinirObras(dados.Obras);
        if (_obraId is not { } o || dados.DaObra is not { } d) return;
        _o = _todasObras.FirstOrDefault(x => x.Id == o);
        _locais = d.Locais;
        _quadras = d.Quadras;
        if (_gQuadraId is null && _quadras.Count(q => q.Ativa) == 1) _gQuadraId = _quadras.First(q => q.Ativa).Id;
        _spes = d.Spes;
        _tips = d.Tipologias;
        _nomeTip = _tips.ToDictionary(t => t.Id, t => t.Codigo);
        _unid = _locais.Where(l => l.Tipo == GeradorEmpreendimento.TipoApartamento).OrderBy(l => l.Codigo).ToList();
        _pagLoc = Math.Min(_pagLoc, Math.Max(0, (ArvoreLista.Count - 1) / TamPag));
        _pagUni = Math.Min(_pagUni, Math.Max(0, (UnidadesFiltradas.Count - 1) / TamPag));
        _resumo = await Eng.ResumoAsync(o, Cancelamento);
        if (_gTorres.Count == 0) AddTorre();
    }

    private void AddTorre()
    {
        var n = _gQuadraId is { } q && _quadras.FirstOrDefault(x => x.Id == q)?.LocalId is { } raiz ? _locais.Count(l => l.LocalPaiId == raiz && l.Tipo == GeradorEmpreendimento.TipoTorre) : 0;
        _gTorres.Add(new TorreForm { Nome = $"{_n.Torre} {n + _gTorres.Count + 1:00}", TipologiaId = _tips.FirstOrDefault(t => t.Tipo == TipoTipologia.Torre)?.Id ?? Guid.Empty });
    }

    private void Novo()
    {
        if (_obraId is not { } o) { NovaObra(); return; }
        ErrosForm = null;
        if (_aba == "obra") { NovaObra(); return; }
        if (_aba == "loc") { NovoLocal(); return; }
        _q = new Quadra { ObraId = o, Tipo = _tiposAgrup.FirstOrDefault() ?? GeradorEmpreendimento.TipoCondominio, Ordem = _quadras.Count + 1 };
        _s = new Spe { ObraId = o };
        _aba = "cond"; _abertoCond = true;
    }

    private void EditarCond(Quadra q)
    {
        _q = q;
        var sp = _spes.FirstOrDefault(x => x.QuadraId == q.Id);
        _s = sp ?? new Spe { ObraId = q.ObraId, QuadraId = q.Id };
        ErrosForm = null; _abertoCond = true;
    }

    private async Task SalvarCond()
    {
        var ok = await Executar(async () =>
        {
            var temCnpj = !string.IsNullOrWhiteSpace(_s!.Cnpj);
            var temRazao = !string.IsNullOrWhiteSpace(_s.RazaoSocial);
            if (temCnpj != temRazao)
                ValidacaoException.Se([temCnpj ? "SPE: informe a razão social (ou deixe CNPJ e razão social em branco para cadastrar depois)." : "SPE: informe o CNPJ (ou deixe CNPJ e razão social em branco para cadastrar depois)."]);
            Spe? spe = null;
            if (temCnpj) { _s.QuadraId = _q!.Id; spe = _s; }
            await Eng.SalvarCondominioAsync(_q!, spe, ct: Cancelamento); // condomínio + SPE numa transação só
        }, "Condomínio salvo.");
        if (ok) { _abertoCond = false; Cat.Invalidar(); await Carregar(); }
    }

    private IEnumerable<Local> Torres => _locais.Where(l => l.Tipo == GeradorEmpreendimento.TipoTorre).OrderBy(l => l.Codigo);
    private Local? _torre;
    private string _nomeTorre = "";
    private bool _abertoTorre;

    private void RenomearTorre(Local t) { _torre = t; _nomeTorre = t.Nome; ErrosForm = null; _abertoTorre = true; }

    private async Task SalvarTorre()
    {
        if (await Executar(() => Eng.RenomearTorreAsync(_torre!.Id, _nomeTorre, Cancelamento), "Torre renomeada.")) { _abertoTorre = false; Cat.Invalidar(); await Carregar(); }
    }

    private async Task ExcluirTorre(Local t)
    {
        List<string> uso = [];
        if (!await Executar(async () => uso = await Eng.UsoDaTorreAsync(t.Id, Cancelamento))) return;
        if (uso.Count > 0) { ErrosForm = [$"{t.Codigo} – {t.Nome} não pode ser excluída: {string.Join(", ", uso)}. Renomeie ou cancele esses registros antes."]; Notif.Erro(ErrosForm[0]); return; }
        Confirmar($"Excluir {t.Codigo} – {t.Nome} com pavimentos, apartamentos, cômodos e áreas comuns? A distribuição do orçamento desses locais também sai. Fica no histórico de auditoria.", async () =>
        {
            var n = 0;
            if (await Executar(async () => n = await Eng.ExcluirTorreAsync(t.Id, Cancelamento), null)) { Notif.Sucesso($"Torre excluída ({n} locais)."); Cat.Invalidar(); await Carregar(); }
        });
    }

    private void EditarSpe(Spe s) { _sp = s; ErrosForm = null; _abertoSpe = true; }

    private async Task SalvarSpe()
    {
        if (await Executar(() => Eng.SalvarSpeAsync(_sp!, ct: Cancelamento), "SPE salva.")) { _abertoSpe = false; await Carregar(); }
    }

    private async Task Gerar()
    {
        int n = 0;
        if (_gQuadraId is not { } qid) { ErrosForm = ["Selecione o condomínio."]; return; }
        var torres = _gTorres.Select(t => (t.Nome, t.TipologiaId)).ToList();
        if (await Executar(async () => n = await Eng.GerarAsync(_obraId!.Value, qid, torres, ct: Cancelamento), null))
        {
            Notif.Sucesso($"{n} locais gerados (torres, pavimentos, apartamentos, cômodos e áreas comuns).");
            _gTorres = []; await Carregar(); _aba = "ger";
        }
    }

    private async Task Sincronizar()
    {
        ResultadoSincronizacao? r = null;
        if (await Executar(async () => r = await Eng.SincronizarAsync(_obraId!.Value, ct: Cancelamento)))
        { Notif.Sucesso($"Sincronizado: {r!.Atualizados} locais atualizados, {r.Criados} criados."); await Carregar(); }
    }

    private async Task SalvarFracoes()
    {
        if (await Executar(() => Eng.SalvarFracoesAsync(_obraId!.Value, _editadas, ct: Cancelamento), "Frações salvas — soma confere 100% por condomínio.")) await Carregar();
    }

    private async Task Recalcular(Guid quadraId)
    {
        if (await Executar(() => Eng.RecalcularFracaoAsync(quadraId, ct: Cancelamento), "Fração recalculada pela área privativa.")) await Carregar();
    }
}
