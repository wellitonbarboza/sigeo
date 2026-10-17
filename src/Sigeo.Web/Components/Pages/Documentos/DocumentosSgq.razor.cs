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

namespace Sigeo.Web.Components.Pages.Documentos;

// Código da tela DocumentosSgq.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class DocumentosSgq
{
    private sealed record Linha(Guid Id, string Codigo, string Titulo, string Tipo, Guid PastaId, string Pasta, string? Revisao, DateOnly? Vigencia, DateOnly? ProximaAnalise, string? Responsavel, StatusDocumentoSgq Status, string CriadoPor);
    private Grade<Linha>? _grade;
    private List<Linha> _todos = [];
    private List<Linha>? _itens;
    private List<Pasta> _pastas = [];
    private Guid? _pasta;
    private int _aRecolher;
    private Documento? _d, _novo;
    private DocumentoRevisao? _emAndamento;
    private PermissaoEfetiva? _perm;
    private CheckoutDocumento? _checkout;
    private List<Ciencia> _ciencias = [];
    private List<EventoDocumento> _eventos = [];
    private const int LimiteEventos = 300;
    private Pasta? _novaPasta;
    private bool _aberto, _abertoNovo, _abertoRev, _abertoPasta;
    private string _aba = "revisao";
    private string? _comentario, _descricao, _origem;
    private MotivoRevisao _motivo = MotivoRevisao.Melhoria;
    private DateOnly _vigencia;
    private List<string> _tipos = [];
    private List<Opcao> _obras = [], _usuarios = [], _retencoes = [];
    private static readonly List<(string, string)> _abasDoc = [("revisao", "Revisão / aprovação"), ("historico", "Histórico de revisões"), ("copias", "Cópias"), ("leituras", "Leituras"), ("log", "Log")];
    private bool Gestor => Perfil(PerfilUsuario.Qualidade, PerfilUsuario.Diretoria);

    protected override async Task OnInitializedAsync()
    {
        _tipos = await Cat.ListaAsync(TipoLista.TipoDocumentoSgq); _obras = await Cat.ObrasAsync(); _usuarios = await Cat.UsuariosAsync();
        await using (var db = Fabrica.CriarLeitura()) _retencoes = await db.RegrasRetencao.OrderBy(r => r.Identificacao).Select(r => new Opcao(r.Id, r.Identificacao + " – " + r.TempoRetencao)).ToListAsync(Cancelamento);
        await Carregar();
        if (Nav.Uri.Contains("?id=") && Guid.TryParse(Nav.Uri.Split("?id=")[1], out var id)) await Abrir(id);
    }

    private IEnumerable<(Pasta, int)> Arvore(Guid? pai, int nivel)
    {
        foreach (var p in _pastas.Where(p => p.PastaPaiId == pai).OrderBy(p => p.Ordem).ThenBy(p => p.Nome))
        {
            yield return (p, nivel);
            foreach (var f in Arvore(p.Id, nivel + 1)) yield return f;
        }
    }

    private async Task Carregar()
    {
        await using var db = Fabrica.CriarLeitura();
        _pastas = await db.Pastas.ToListAsync(Cancelamento);
        var nomes = _pastas.ToDictionary(p => p.Id, p => p.Nome);
        var docs = await db.Documentos.Include(d => d.Revisoes).OrderBy(d => d.Codigo).ToListAsync(Cancelamento);
        _todos = docs.Select(d => new Linha(d.Id, d.Codigo, d.Titulo, d.Tipo, d.PastaId, nomes.GetValueOrDefault(d.PastaId, ""), d.RevisaoVigente?.Rotulo, d.DataVigencia, d.ProximaAnalise, d.ResponsavelNome, d.Status, d.CriadoPor)).ToList();
        _aRecolher = await db.Copias.CountAsync(c => c.Status == StatusCopia.RecolhimentoPendente, Cancelamento);
        Filtrar(_pasta);
    }

    private void Filtrar(Guid? pasta)
    {
        _pasta = pasta;
        var ids = pasta is null ? null : Arvore(pasta, 0).Select(x => x.Item1.Id).Append(pasta.Value).ToHashSet();
        _itens = ids is null ? _todos : _todos.Where(d => ids.Contains(d.PastaId)).ToList();
    }

    private void Novo()
    {
        _novo = new Documento { PastaId = _pasta ?? Guid.Empty, ResponsavelUsuarioId = Usuario.UsuarioId, PeriodicidadeMeses = 12 };
        ErrosForm = null; _abertoNovo = true;
    }

    private async Task Criar()
    {
        _novo!.ResponsavelNome = _usuarios.FirstOrDefault(u => u.Id == _novo.ResponsavelUsuarioId)?.Texto;
        _novo.AprovadorNome = _usuarios.FirstOrDefault(u => u.Id == _novo.AprovadorUsuarioId)?.Texto;
        Guid id = default;
        if (await Executar(async () => id = await Docs.CriarDocumentoAsync(_novo, Cancelamento), "Documento criado — R00 em elaboração.")) { _abertoNovo = false; await Carregar(); await Abrir(id); }
    }

    // ───────── Importação em lote ─────────
    private bool _abertoLote;
    private Guid _lotePasta;
    private string? _loteTipo;
    private List<ArquivoLoteSgq> _loteArquivos = [];
    private ResumoLoteSgq? _lote;

    private void AbrirLote() { ErrosForm = null; _lote = null; _loteArquivos = []; _lotePasta = _pasta ?? Guid.Empty; _abertoLote = true; }

    private static string AcaoLote(AcaoLoteSgq a) => a switch
    {
        AcaoLoteSgq.Criar => "Criar", AcaoLoteSgq.NovaRevisao => "Nova revisão", AcaoLoteSgq.AnexarNaRevisao => "Anexar", _ => "Ignorar"
    };

    private async Task LerLote(InputFileChangeEventArgs e)
    {
        _lote = null;
        await Executar(async () =>
        {
            var lista = new List<ArquivoLoteSgq>();
            foreach (var f in e.GetMultipleFiles(200))
            {
                using var ms = new MemoryStream();
                await f.OpenReadStream(30 * 1024 * 1024, Cancelamento).CopyToAsync(ms, Cancelamento);
                var bytes = ms.ToArray();
                var id = Sigeo.Infrastructure.Importacao.LeitorDocumentoSgq.Identificar(new MemoryStream(bytes), f.Name);
                lista.Add(new ArquivoLoteSgq(f.Name, bytes, id));
            }
            _loteArquivos = lista;
            _lote = await Docs.AnalisarLoteAsync(_lotePasta, _loteTipo, _tipos, _loteArquivos, Cancelamento);
        });
    }

    private async Task ImportarLote()
    {
        ResumoLoteSgq? r = null;
        if (await Executar(async () => r = await Docs.ImportarLoteAsync(_lotePasta, _loteTipo, _tipos, _loteArquivos, Cancelamento)))
        {
            Notif.Sucesso($"{r!.Criar} documentos criados, {r.Revisoes} revisões abertas — siga a análise crítica, aprovação e publicação de cada um.");
            _abertoLote = false; _loteArquivos = []; _lote = null;
            await Carregar();
        }
    }

    private async Task SalvarPasta()
    {
        if (await Executar(() => Docs.CriarPastaAsync(_novaPasta!, Cancelamento), "Pasta criada.")) { _abertoPasta = false; await Carregar(); }
    }

    private async Task Abrir(Guid id)
    {
        await using var db = Fabrica.CriarLeitura();
        _d = await db.Documentos.Include(d => d.Revisoes).ThenInclude(r => r.Etapas).Include(d => d.Revisoes).ThenInclude(r => r.Copias).AsSplitQuery().FirstAsync(d => d.Id == id, Cancelamento);
        _emAndamento = _d.Revisoes.FirstOrDefault(r => r.Status is < StatusRevisao.Vigente and not StatusRevisao.Recusada);
        _perm = await Docs.PermissaoAsync(db, _d, Cancelamento);
        var revIds = _d.Revisoes.Select(r => r.Id).ToList();
        _checkout = _emAndamento is null ? null : await db.Checkouts.FirstOrDefaultAsync(c => c.DocumentoRevisaoId == _emAndamento.Id && c.CheckinEm == null && c.LiberadoForcadoPor == null, Cancelamento);
        _ciencias = await db.Ciencias.Where(c => revIds.Contains(c.DocumentoRevisaoId)).OrderBy(c => c.UsuarioNome).ToListAsync(Cancelamento);
        _eventos = await db.EventosDocumento.Where(e => e.DocumentoId == id).OrderByDescending(e => e.DataHora).Take(LimiteEventos).ToListAsync(Cancelamento);
        // Só a primeira abertura conta como acesso; recarregar após uma ação na mesma ficha não é nova visualização.
        if (!_aberto) await Docs.RegistrarVisualizacaoAsync(_d.Id, _d.RevisaoVigenteId, Cancelamento);
        _vigencia = Hoje; _comentario = null; ErrosForm = null; _aberto = true;
    }

    private async Task Recarregar() { if (_d is not null) await Abrir(_d.Id); await Carregar(); }

    private List<Etapas.Passo> PassosRev(DocumentoRevisao r)
    {
        var lista = new List<Etapas.Passo>();
        foreach (var e in r.Etapas.Where(e => !e.Historico).OrderBy(e => e.Etapa))
        {
            var estado = e.Decisao switch
            {
                DecisaoEtapa.Aprovado or DecisaoEtapa.AprovadoComRessalvas => "feita",
                DecisaoEtapa.Recusado => "erro",
                _ => r.EtapaAtual?.Id == e.Id ? "atual" : "pendente"
            };
            if (r.Status is StatusRevisao.Vigente or StatusRevisao.VigenteComRessalvas or StatusRevisao.Obsoleta) estado = "feita";
            lista.Add(new(De(e.Etapa), e.ResponsavelNome ?? (e.DataDecisao is null ? "pendente" : ""), estado));
        }
        return lista;
    }

    private async Task Anexar(InputFileChangeEventArgs e, bool publicado)
    {
        if (await Executar(async () => { await using var s = e.File.OpenReadStream(50 * 1024 * 1024, Cancelamento); await Docs.AnexarArquivoAsync(_emAndamento!.Id, s, e.File.Name, publicado, Cancelamento); }, "Arquivo anexado."))
            await Abrir(_d!.Id);
    }

    private async Task Checkout() { if (await Executar(() => Docs.CheckoutAsync(_emAndamento!.Id, Cancelamento), "Check-out feito — só você edita até o check-in.")) await Abrir(_d!.Id); }
    private async Task Checkin(bool forcado) { if (await Executar(() => Docs.CheckinAsync(_emAndamento!.Id, forcado, Cancelamento), "Check-in registrado.")) await Abrir(_d!.Id); }

    private async Task Decidir(DecisaoEtapa decisao)
    {
        if (await Executar(() => Docs.DecidirEtapaAsync(_emAndamento!.Id, decisao, _comentario, Cancelamento), "Etapa registrada.")) await Recarregar();
    }

    private async Task Publicar()
    {
        if (await Executar(() => Docs.PublicarAsync(_emAndamento!.Id, _vigencia, Cancelamento), "Revisão publicada — leituras e recolhimentos gerados.")) await Recarregar();
    }

    private async Task NovaRevisao()
    {
        if (await Executar(() => Docs.NovaRevisaoAsync(_d!.Id, _motivo, _descricao ?? "", _origem, Cancelamento), "Revisão aberta em elaboração.")) { _abertoRev = false; _aba = "revisao"; await Recarregar(); }
    }
}
