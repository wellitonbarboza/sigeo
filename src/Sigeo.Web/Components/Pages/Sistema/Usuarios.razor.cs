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

// Código da tela Usuarios.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Usuarios
{
    private IGrade? GradeAtual => _aba switch { "usuarios" => _gU, "alcadas" => _gA, "delegacoes" => _gD, _ => null };
    private Grade<Usuario>? _gU;
    private Grade<Alcada>? _gA;
    private Grade<DelegacaoAprovacao>? _gD;
    private List<Usuario>? _usuarios;
    private List<Alcada>? _alcadas;
    private List<DelegacaoAprovacao>? _delegacoes;
    private Usuario? _u;
    private Alcada? _a;
    private DelegacaoAprovacao? _d;
    private bool _abertoU, _abertoA, _abertoD;
    private string _aba = "usuarios";
    private List<Opcao> _obras = [], _usuariosOp = [];
    private List<string> _funcoes = [];
    private static readonly List<(string, string)> _abas = [("usuarios", "Usuários e limites"), ("alcadas", "Alçadas (etapas)"), ("delegacoes", "Delegações"), ("obras", "Acesso por obra")];
    private Guid? _obraAcesso;
    private Dictionary<Guid, bool> _acesso = [];

    private void CarregarAcessoObra()
    {
        _acesso = (_usuarios ?? []).Where(u => !u.VeTodasObras).ToDictionary(u => u.Id, u => _obraAcesso is { } o && u.Obras.Any(x => x.ObraId == o));
    }

    private async Task SalvarAcessoObra()
    {
        if (await Executar(() => Sistema.DefinirAcessoObraAsync(_obraAcesso!.Value, _acesso, Cancelamento), "Acessos da obra salvos."))
        { await Carregar(); CarregarAcessoObra(); }
    }

    private string NomeObra(Guid id) => _obras.FirstOrDefault(o => o.Id == id)?.Texto ?? "?";
    private string NomeUsuario(Guid id) => _usuarios?.FirstOrDefault(u => u.Id == id)?.Nome ?? "?";

    protected override async Task OnInitializedAsync()
    {
        _obras = await Cat.ObrasAsync(); _funcoes = await Cat.ListaAsync(TipoLista.Funcao);
        await Carregar();
    }

    private async Task Carregar()
    {
        var dados = await Consultas.CarregarAsync(Cancelamento);
        _usuarios = dados.Usuarios;
        _alcadas = dados.Alcadas;
        _delegacoes = dados.Delegacoes;
        _credenciais = await Conta.SituacaoAsync(Cancelamento);
        _usuariosOp = _usuarios.Where(u => u.Ativo).Select(u => new Opcao(u.Id, $"{u.Nome} ({De(u.Perfil)})")).ToList();
    }

    private void Novo()
    {
        if (!Perfil()) { Notif.Aviso("Somente o Administrador."); return; }
        ErrosForm = null;
        switch (_aba)
        {
            case "usuarios": _u = new Usuario { Perfil = PerfilUsuario.Campo }; _senhaGerada = null; _abertoU = true; break;
            case "alcadas": _a = new Alcada { Entidade = EntidadeAprovavel.Pedido, Ordem = 1, PerfilAprovador = PerfilUsuario.Diretoria }; _abertoA = true; break;
            case "delegacoes": _d = new DelegacaoAprovacao { Inicio = DateTimeOffset.Now, Fim = DateTimeOffset.Now.AddDays(15) }; _abertoD = true; break;
        }
    }

    private void AbrirUsuario(Usuario u) { _u = u; ErrosForm = null; _senhaGerada = null; _abertoU = true; }

    private Dictionary<Guid, CredencialUsuario> _credenciais = [];
    private string? _senhaGerada;

    private string SituacaoAcesso(Guid id)
    {
        if (!_credenciais.TryGetValue(id, out var c)) return "Sem senha";
        if (c.BloqueadoAte > DateTimeOffset.UtcNow) return $"Bloqueado até {Rotulos.DataHora(c.BloqueadoAte.Value)}";
        if (c.TrocarSenhaNoProximoAcesso) return "Senha provisória (troca pendente)";
        return c.UltimoAcessoEm is { } a ? $"Último acesso {Rotulos.DataHora(a)}" : "Senha definida";
    }

    private async Task GerarSenha()
    {
        string? senha = null;
        if (await Executar(async () => senha = await Conta.DefinirSenhaProvisoriaAsync(_u!.Id, Cancelamento), null)) { _senhaGerada = senha; _credenciais = await Conta.SituacaoAsync(Cancelamento); }
    }

    private async Task Desbloquear()
    {
        if (await Executar(() => Conta.DesbloquearAsync(_u!.Id, Cancelamento), "Usuário desbloqueado.")) _credenciais = await Conta.SituacaoAsync(Cancelamento);
    }

    private void AlternarObra(Guid obra, bool marcado)
    {
        var v = _u!.Obras.FirstOrDefault(x => x.ObraId == obra);
        if (marcado && v is null) _u.Obras.Add(new UsuarioObra { UsuarioId = _u.Id, ObraId = obra });
        else if (!marcado && v is not null) _u.Obras.Remove(v);
    }

    private async Task SalvarUsuario()
    {
        var novo = _u!.CriadoEm == default;
        Guid id = default;
        var ok = await Executar(async () => id = await Sistema.SalvarUsuarioAsync(_u!, Cancelamento), "Usuário salvo.");
        if (ok)
        {
            await Carregar();
            // Usuário novo: mantém aberto para gerar a senha provisória.
            if (novo && _usuarios?.FirstOrDefault(x => x.Id == id) is { } salvo) { _u = salvo; Notif.Sucesso("Usuário criado. Gere a senha provisória."); }
            else _abertoU = false;
        }
    }

    private async Task SalvarAlcada()
    {
        if (await Executar(() => Sistema.SalvarAlcadaAsync(_a!, Cancelamento), "Alçada salva — vale para novos envios e para as etapas ainda não decididas das aprovações em andamento.")) { _abertoA = false; await Carregar(); }
    }

    private async Task SalvarDelegacao()
    {
        if (await Executar(() => Sistema.SalvarDelegacaoAsync(_d!, Cancelamento), "Delegação salva.")) { _abertoD = false; await Carregar(); }
    }

    private async Task Revogar()
    {
        if (await Executar(() => Sistema.RevogarDelegacaoAsync(_d!.Id, Cancelamento), "Delegação revogada.")) { _abertoD = false; await Carregar(); }
    }
}
