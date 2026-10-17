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

namespace Sigeo.Web.Components.Pages.Cadastros;

// Código da tela Controlados.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Controlados
{
    private IGrade? GradeAtual => _aba == "mat" ? _gMat : _aba == "srv" ? _gSrv : null;
    private Dictionary<Guid, string> _fvm = [], _fvs = [];
    private List<Opcao> _docs = [];
    private sealed record LinhaSemVinculo(string Codigo, string Nome, string? Grupo, string? SubGrupo);
    private const int LimiteSemVinculo = 500;
    private List<LinhaSemVinculo> _semVinculo = [];
    private bool _semVinculoCortado;
    private string SitMat(MaterialControlado m) => m.CategoriaProdutoId is null && string.IsNullOrWhiteSpace(m.SubGrupos) ? "Sem vínculo" : string.IsNullOrWhiteSpace(m.EspecificacaoModelo) ? "Sem especificação" : !_fvm.ContainsKey(m.Id) ? "Sem modelo FVM" : "Completo";
    private Grade<MaterialControlado>? _gMat;
    private Grade<ServicoControlado>? _gSrv;
    private List<MaterialControlado>? _mats;
    private List<ServicoControlado>? _srvs;
    private MaterialControlado? _m;
    private ServicoControlado? _s;
    private bool _abertoMat, _abertoSrv;
    private string _aba = "mat";
    private static readonly List<(string, string)> _abas = [("mat", "Materiais controlados"), ("srv", "Serviços controlados (ITS)"), ("gen", "Produtos sem vínculo")];

    private List<Opcao> _grupos = [];
    private Dictionary<Guid, int> _qtdProd = [];
    private bool _abertoMapa;
    private byte[]? _mapa;
    private Sigeo.Infrastructure.Importacao.ResultadoMapa? _previaMapa;

    private bool _podeImportar;

    protected override async Task OnInitializedAsync() { _podeImportar = Perfil(PerfilUsuario.Qualidade, PerfilUsuario.Suprimentos); _grupos = await Cat.GruposProdutoAsync(); await Carregar(); }

    private async Task<byte[]> Ler(InputFileChangeEventArgs e)
    {
        if (e.File.Size > 20 * 1024 * 1024) throw new RegraDeNegocioException("Planilha acima de 20 MB.");
        using var ms = new MemoryStream();
        await e.File.OpenReadStream(20 * 1024 * 1024).CopyToAsync(ms, Cancelamento);
        return ms.ToArray();
    }

    private async Task ImportarTabela(InputFileChangeEventArgs e)
    {
        if (!_podeImportar || Ocupado) return;
        Sigeo.Infrastructure.Importacao.ResultadoEspecificacao? r = null;
        if (await Executar(async () => r = await Imp.ImportarEspecificacaoAsync(new MemoryStream(await Ler(e)), ct: Cancelamento)))
        { Notif.Sucesso($"Tabela importada: {r!.Materiais} materiais ({r.MateriaisNovos} novos), {r.TiposDocumento} tipos de documento novos, {r.Ensaios} ensaios, {r.GruposLigados} grupos ligados."); _grupos = await Cat.GruposProdutoAsync(); await Carregar(); }
    }

    private void FecharMapa() { _mapa = null; _previaMapa = null; }

    private async Task PreverMapa(InputFileChangeEventArgs e)
    {
        if (!_podeImportar || Ocupado) return;
        _mapa = null; _previaMapa = null;
        if (await Executar(async () => { _mapa = await Ler(e); _previaMapa = await Imp.AplicarMapaAsync(new MemoryStream(_mapa), false, ct: Cancelamento); })) _abertoMapa = true;
    }

    private async Task AplicarMapa()
    {
        if (!_podeImportar || _mapa is null) return;
        if (await Executar(async () => _previaMapa = await Imp.AplicarMapaAsync(new MemoryStream(_mapa!), true, ct: Cancelamento)))
        {
            Notif.Sucesso($"Catálogo ajustado: {_previaMapa!.Movidos} produtos movidos, {_previaMapa.Controlados} ligados a material controlado, {_previaMapa.Codigos?.Recodificados ?? 0} recodificados.");
            _abertoMapa = false; _mapa = null; _previaMapa = null; _grupos = await Cat.GruposProdutoAsync(); await Carregar();
        }
    }

    private async Task Vincular()
    {
        if (!_podeImportar) return;
        int n = 0;
        if (await Executar(async () => n = await Cad.VincularPorGrupoAsync(Cancelamento))) { Notif.Sucesso($"{n} produto(s) atualizados pelo grupo."); await Carregar(); }
    }

    private async Task Carregar()
    {
        Cat.InvalidarProdutos(); // produto/controlado mudou: a busca de produtos de todos relê o catálogo
        await using var db = Fabrica.CriarLeitura();
        _mats = await db.MateriaisControlados.OrderBy(m => m.Codigo).ToListAsync(Cancelamento);
        _srvs = await db.ServicosControlados.OrderBy(m => m.Codigo).ToListAsync(Cancelamento);
        var modelos = await db.ModelosInspecao.Where(m => m.Situacao == SituacaoModelo.Vigente)
            .Select(m => new { m.MaterialControladoId, m.ServicoControladoId, m.Tipo, m.Codigo, m.Revisao }).ToListAsync(Cancelamento);
        _fvm = modelos.Where(m => m.MaterialControladoId != null).GroupBy(m => m.MaterialControladoId!.Value).ToDictionary(g => g.Key, g => $"{g.First().Codigo} R{g.First().Revisao:00}");
        _fvs = modelos.Where(m => m.ServicoControladoId != null && m.Tipo == TipoModelo.FVS).GroupBy(m => m.ServicoControladoId!.Value).ToDictionary(g => g.Key, g => $"{g.First().Codigo} R{g.First().Revisao:00}");
        // Um a mais que o limite: só para saber se a lista foi cortada.
        _semVinculo = await db.Produtos.Where(p => p.Ativo && p.MaterialControladoId == null).OrderBy(p => p.Nome)
            .Select(p => new LinhaSemVinculo(p.Codigo, p.Nome, p.Grupo, p.SubGrupo)).Take(LimiteSemVinculo + 1).ToListAsync(Cancelamento);
        _semVinculoCortado = _semVinculo.Count > LimiteSemVinculo;
        if (_semVinculoCortado) _semVinculo.RemoveAt(LimiteSemVinculo);
        _docs = await Cat.DocumentosSgqAsync();
        _qtdProd = await db.Produtos.Where(p => p.MaterialControladoId != null).GroupBy(p => p.MaterialControladoId!.Value).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, Cancelamento);
    }

    private void Novo()
    {
        ErrosForm = null;
        if (_aba == "mat") { _m = new MaterialControlado(); _abertoMat = true; } else { _s = new ServicoControlado(); _abertoSrv = true; }
    }

    private async Task SalvarMat()
    {
        if (await Executar(() => Cad.SalvarMaterialControladoAsync(_m!, Cancelamento), "Material salvo.")) { _abertoMat = false; await Carregar(); }
    }

    private async Task SalvarSrv()
    {
        if (await Executar(() => Cad.SalvarServicoControladoAsync(_s!, Cancelamento), "Serviço salvo.")) { _abertoSrv = false; await Carregar(); }
    }
}
