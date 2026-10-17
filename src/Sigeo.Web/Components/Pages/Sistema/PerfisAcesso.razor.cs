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

namespace Sigeo.Web.Components.Pages.Sistema;

// Código da tela PerfisAcesso.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class PerfisAcesso
{
    private static readonly IReadOnlyList<(string, string)> _abas = [("perfis", "Perfis"), ("usuarios", "Por usuário (perfil + exceções)")];
    private string _aba = "perfis";
    private List<AcessoServico.LinhaPerfil>? _perfis;
    private List<AcessoServico.LinhaUsuario>? _usuarios;
    private MapaAcesso? _mapa;
    private AcessoServico.LinhaPerfil? _sel;
    private string _nome = "";
    private Dictionary<string, string> _regras = new(StringComparer.OrdinalIgnoreCase);
    private AcessoServico.LinhaUsuario? _u;
    private Guid? _uid, _uPerfil;
    private Dictionary<string, Dictionary<char, bool>> _exc = new(StringComparer.OrdinalIgnoreCase);

    private int QtdExcecoes => _exc.Sum(e => e.Value.Count);
    private bool Liberada(TelaSistema t) => _mapa?.NaoContratadas.Contains(t.Rota, StringComparer.OrdinalIgnoreCase) != true;

    protected override async Task OnInitializedAsync() => await Carregar();

    private async Task Carregar()
    {
        _mapa = await Acesso.MapaAsync(Cancelamento);
        _perfis = await Acesso.PerfisAsync(Cancelamento);
        _usuarios = await Acesso.UsuariosAsync(Cancelamento);
        var sel = _perfis.FirstOrDefault(p => p.Id == _sel?.Id) ?? _perfis.FirstOrDefault();
        if (sel is not null) Escolher(sel);
        if (_uid is { } id && _usuarios.FirstOrDefault(u => u.Id == id) is { } u) AbrirUsuario(u);
    }

    private void Escolher(AcessoServico.LinhaPerfil p)
    {
        _sel = p; _nome = p.Nome; ErrosForm = null;
        _regras = new Dictionary<string, string>(p.Regras, StringComparer.OrdinalIgnoreCase);
    }

    private void Marcar(TelaSistema t, char l, ChangeEventArgs e)
    {
        var s = new HashSet<char>(_regras.GetValueOrDefault(t.Rota) ?? "");
        var on = e.Value is true;
        if (on) { s.Add(l); s.Add(AcaoTela.Ver); }
        else if (l == AcaoTela.Ver) s.Clear();
        else s.Remove(l);
        _regras[t.Rota] = AcaoTela.Normalizar(new string(s.ToArray()), t.Acoes);
    }

    private async Task Salvar()
    {
        if (await Executar(() => Acesso.SalvarPerfilAsync(_sel!.Id, _nome, _regras, Cancelamento), "Perfil salvo. Vale na próxima tela que cada usuário abrir.")) await Carregar();
    }

    private async Task Duplicar()
    {
        var id = Guid.Empty;
        if (await Executar(async () => id = await Acesso.DuplicarPerfilAsync(_sel!.Id, Cancelamento), "Cópia criada."))
        {
            _sel = _sel! with { Id = id };
            await Carregar();
        }
    }

    private async Task Restaurar() { if (await Executar(() => Acesso.RestaurarPadraoAsync(_sel!.Id, Cancelamento), "Perfil voltou ao padrão.")) await Carregar(); }

    private async Task ExcluirPerfil()
    {
        if (await Executar(() => Acesso.ExcluirPerfilAsync(_sel!.Id, Cancelamento), "Perfil excluído.")) { _sel = null; await Carregar(); }
    }

    // ───── Por usuário ─────

    private void EscolherUsuario(ChangeEventArgs e)
    {
        _uid = Guid.TryParse(e.Value?.ToString(), out var g) ? g : null;
        _u = null;
        if (_uid is { } id && _usuarios?.FirstOrDefault(u => u.Id == id) is { } u) AbrirUsuario(u);
    }

    private void AbrirUsuario(AcessoServico.LinhaUsuario u)
    {
        _u = u; _uPerfil = u.PerfilAcessoId; ErrosForm = null;
        _exc = CatalogoTelas.LerExcecoes(u.Excecoes);
    }

    private void TrocarPerfilUsuario(ChangeEventArgs e) => _uPerfil = Guid.TryParse(e.Value?.ToString(), out var g) ? g : null;

    /// <summary>Regras do perfil que vale para o usuário (o escolhido; senão o padrão do perfil dele).</summary>
    private Dictionary<string, string> RegrasBaseUsuario()
    {
        var p = _uPerfil is { } id ? _perfis!.FirstOrDefault(x => x.Id == id) : _perfis!.FirstOrDefault(x => x.Base == _u!.Perfil);
        return p?.Regras ?? PerfisPadrao.Regras(_u!.Perfil);
    }

    private static string ClasseCel(bool? e, bool tem) => e == true ? "mais" : e == false ? "menos" : tem ? "base" : "";
    private static string TextoCel(bool? e, bool tem) => e == true ? "+" : e == false ? "−" : tem ? "✓" : "·";
    private static string DicaCel(bool? e, bool tem) => (tem ? "Perfil permite" : "Perfil não permite") + (e is null ? "" : " · exceção só deste usuário");

    private void Alternar(TelaSistema t, char l, bool tem)
    {
        if (!_exc.TryGetValue(t.Rota, out var d)) _exc[t.Rota] = d = [];
        if (d.ContainsKey(l)) d.Remove(l); else d[l] = !tem;
        if (d.Count == 0) _exc.Remove(t.Rota);
    }

    private async Task SalvarUsuario()
    {
        if (await Executar(() => Acesso.SalvarAcessoUsuarioAsync(_u!.Id, _uPerfil, _exc, Cancelamento), $"Acesso de {_u!.Nome} salvo. Vale na próxima tela que ele abrir.")) await Carregar();
    }
}
