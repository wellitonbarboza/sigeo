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

namespace Sigeo.Web.Components.Pages.Engenharia;

// Código da tela Tipologias.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Tipologias
{
    private bool _abertoKit;
    private KitTipologia? _kit;
    private ResumoKit? _resumoKit;

    private async Task PreverKit(InputFileChangeEventArgs e)
    {
        if (_obraId is not { } o || Ocupado) return;
        _kit = null; _resumoKit = null;
        await Executar(async () =>
        {
            using var ms = new MemoryStream();
            await e.File.OpenReadStream(10 * 1024 * 1024).CopyToAsync(ms, Cancelamento);
            ms.Position = 0;
            _kit = Sigeo.Infrastructure.Importacao.LeitorKitTipologia.Ler(ms);
            _resumoKit = await Kit.AnalisarAsync(o, _kit, Cancelamento);
        });
        _abertoKit = true;
    }

    private async Task AplicarKit()
    {
        if (_obraId is not { } o || _kit is null) return;
        if (await Executar(() => Kit.AplicarAsync(o, _kit, Cancelamento), "Kit importado. Gere as torres em Estrutura e gerador."))
        {
            _abertoKit = false; _kit = null; _resumoKit = null;
            await Carregar();
        }
    }

    private sealed record Col(string Rotulo, string Titulo, AlvoServico Alvo, Guid? AmbienteId);
    private Grade<Tipologia>? _grade;
    private Guid? _obraId;
    private TipoTipologia _tipo = TipoTipologia.Apartamento;
    private EstruturaLocais _n = EstruturaLocais.Condominios;
    /// <summary>Nome do tipo de tipologia com os níveis da construtora (unidade, bloco/torre, implantação do condomínio).</summary>
    private string NomeTipo(TipoTipologia t) => t switch
    {
        TipoTipologia.Apartamento => _n.Unidade, TipoTipologia.Torre => _n.Torre, _ => $"Implantação (áreas comuns do {_n.Condominio.ToLower()})"
    };
    private List<Tipologia> _lista = [];
    private List<ServicoAplicacao> _aplic = [];
    private Dictionary<Guid, int> _usos = [];
    private List<Composicao> _comps = [];
    private List<string> _comodos = [], _grupos = [];
    private Tipologia? _t;
    private bool _aberto, _soMarcados;
    private string _abaEd = "dados";
    private string? _fGrupo, _fTexto;
    [SupplyParameterFromQuery(Name = "obra")] public Guid? ObraQuery { get; set; }
    /// <summary>Marcações (tipologia, serviço, alvo, ambiente) para consulta O(1) na matriz.</summary>
    private HashSet<(Guid Tip, Guid Comp, AlvoServico Alvo, Guid? Amb)> _marcados = [];
    private HashSet<(Guid Tip, Guid Comp)> _marcadosComp = [];
    private Dictionary<Guid, int> _qtdMarcados = [];
    private HashSet<Guid> _ambSalvos = [];
    private int _versaoMatriz;
    private static readonly List<(string, string)> _abasEd = [("dados", "Dados e ambientes"), ("serv", "Serviços")];

    protected override async Task OnInitializedAsync()
    {
        // A obra inicial (?obra= ou a única permitida) vem do SeletorObra, que chama TrocarObra.
        _n = await Cat.EstruturaAsync();
        _comodos = await Cat.ListaAsync(TipoLista.TipoComodo);
        _grupos = await Cat.ListaAsync(TipoLista.GrupoServico);
    }

    private async Task TrocarObra(Guid? obra)
    {
        _obraId = obra; _t = null; _aberto = false; _fTexto = null; _fGrupo = null; _soMarcados = false;
        await Carregar();
    }

    private async Task Carregar()
    {
        if (_obraId is not { } o) { _lista = []; return; }
        await using var db = Fabrica.CriarLeitura();
        _lista = await db.Tipologias.AsNoTracking().Include(t => t.Ambientes).Include(t => t.Mix).Where(t => t.ObraId == o).OrderBy(t => t.Codigo).ToListAsync(Cancelamento);
        _aplic = await db.ServicoAplicacoes.AsNoTracking().Where(a => a.ObraId == o).ToListAsync(Cancelamento);
        Indexar();
        _usos = await db.Locais.AsNoTracking().Where(l => l.ObraId == o && l.TipologiaId != null).GroupBy(l => l.TipologiaId!.Value).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, Cancelamento);
        _comps = await db.Composicoes.AsNoTracking().Where(c => c.Ativa).OrderBy(c => c.Grupo).ThenBy(c => c.Codigo).ToListAsync(Cancelamento);
    }

    private void Indexar()
    {
        _marcados = _aplic.Select(a => (a.TipologiaId, a.ComposicaoId, a.Alvo, a.AmbienteModeloId)).ToHashSet();
        _marcadosComp = _aplic.Select(a => (a.TipologiaId, a.ComposicaoId)).ToHashSet();
        _qtdMarcados = _aplic.GroupBy(a => a.TipologiaId).ToDictionary(g => g.Key, g => g.Count());
    }

    private void Novo()
    {
        if (_obraId is not { } o) return;
        _t = new Tipologia { ObraId = o, Tipo = _tipo, PeDireitoPadrao = 2.90m, Pavimentos = _tipo == TipoTipologia.Torre ? 4 : 0 };
        _ambSalvos = [];
        _abaEd = "dados"; ErrosForm = null; _aberto = true;
    }

    private async Task Editar(Tipologia t)
    {
        await using var db = Fabrica.CriarLeitura();
        _t = await db.Tipologias.AsNoTracking().Include(x => x.Ambientes).Include(x => x.Mix).FirstAsync(x => x.Id == t.Id, Cancelamento);
        _ambSalvos = _t.Ambientes.Select(a => a.Id).ToHashSet();
        _abaEd = "dados"; ErrosForm = null; _aberto = true;
    }

    private void AddAmbiente()
    {
        var escopo = _t!.Tipo switch { TipoTipologia.Apartamento => EscopoAmbiente.Comodo, TipoTipologia.Torre => EscopoAmbiente.AreaComumPavimento, _ => EscopoAmbiente.AreaComumCondominio };
        _t.Ambientes.Add(new TipologiaAmbiente { TipologiaId = _t.Id, Escopo = escopo, Ordem = (_t.Ambientes.Count == 0 ? 0 : _t.Ambientes.Max(a => a.Ordem)) + 1 });
    }

    private void AddMix() => _t!.Mix.Add(new TipologiaMix { TipologiaId = _t.Id, PavimentoDe = 1, PavimentoAte = Math.Max(1, _t.Pavimentos), Quantidade = 1, Ordem = _t.Mix.Count + 1,
        ApartamentoTipologiaId = _lista.FirstOrDefault(x => x.Tipo == TipoTipologia.Apartamento)?.Id ?? Guid.Empty });

    private void Mover(TipologiaAmbiente a, int d)
    {
        var ord = _t!.Ambientes.OrderBy(x => x.Ordem).ToList();
        var i = ord.IndexOf(a);
        if (i + d < 0 || i + d >= ord.Count) return;
        (ord[i].Ordem, ord[i + d].Ordem) = (ord[i + d].Ordem, ord[i].Ordem);
    }

    private async Task Salvar()
    {
        if (await Executar(() => Eng.SalvarTipologiaAsync(_t!, ct: Cancelamento), "Tipologia salva."))
        {
            await Carregar();
            await Editar(_t!);
        }
    }

    private async Task Copiar()
    {
        Guid id = default;
        if (await Executar(async () => id = await Eng.CopiarTipologiaAsync(_t!.Id, _t.Codigo + "-B", ct: Cancelamento), "Tipologia copiada (código com -B) — ajuste o código e as diferenças."))
        { await Carregar(); await Editar(_lista.First(x => x.Id == id)); }
    }

    private IEnumerable<Col> Colunas()
    {
        var t = _t!;
        foreach (var a in t.Ambientes.Where(a => _ambSalvos.Contains(a.Id)).OrderBy(a => a.Ordem)) yield return new Col(a.Sigla ?? a.Nome, a.Nome, AlvoServico.Ambiente, a.Id);
        if (t.Tipo == TipoTipologia.Apartamento) yield return new Col("APTO", "Apartamento inteiro", AlvoServico.Apartamento, null);
        if (t.Tipo == TipoTipologia.Torre) { yield return new Col("PAV", "Cada pavimento", AlvoServico.Pavimento, null); yield return new Col("TORRE", "Torre", AlvoServico.Torre, null); }
        if (t.Tipo == TipoTipologia.Implantacao) yield return new Col("COND", "Condomínio", AlvoServico.Condominio, null);
    }

    private IEnumerable<Composicao> ServicosFiltrados()
    {
        var termo = Rotulos.SemAcento(_fTexto);
        return _comps.Where(c => (string.IsNullOrEmpty(_fGrupo) || c.Grupo == _fGrupo) && (termo.Length == 0 || Rotulos.SemAcento(c.Codigo + " " + c.Descricao).Contains(termo))
                                && (!_soMarcados || _marcadosComp.Contains((_t!.Id, c.Id))));
    }

    private bool Marcado(Guid comp, AlvoServico alvo, Guid? amb) => _marcados.Contains((_t!.Id, comp, alvo, amb));

    private async Task Marcar(Guid comp, AlvoServico alvo, Guid? amb, bool on)
    {
        var ok = await Executar(() => Orc.MarcarAplicacaoAsync(_t!.Id, comp, alvo, amb, on, ct: Cancelamento));
        await using var db = Fabrica.CriarLeitura();
        _aplic = await db.ServicoAplicacoes.AsNoTracking().Where(a => a.ObraId == _obraId).ToListAsync(Cancelamento);
        Indexar();
        if (!ok) _versaoMatriz++; // falhou: recria as linhas para o checkbox voltar ao estado gravado
    }
}
