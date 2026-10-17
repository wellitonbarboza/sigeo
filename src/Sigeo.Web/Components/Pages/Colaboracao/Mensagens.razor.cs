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

namespace Sigeo.Web.Components.Pages.Colaboracao;

// Código da tela Mensagens.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Mensagens
{
    private static readonly (MensagensServico.Pasta, string, string)[] Pastas =
    [
        (MensagensServico.Pasta.Entrada, "Entrada", "mensagem"), (MensagensServico.Pasta.Enviadas, "Enviadas", "enviar"),
        (MensagensServico.Pasta.Arquivadas, "Arquivadas", "arquivar"), (MensagensServico.Pasta.Registros, "Ligadas a registro", "corrente")
    ];
    private MensagensServico.Pasta _pasta = MensagensServico.Pasta.Entrada;
    private List<MensagensServico.ResumoConversa>? _lista;
    private MensagensServico.ConversaVista? _aberta;
    private List<MensagensServico.Destinatario> _opcoes = [];
    private List<string> _incluir = [];
    private string? _busca, _resposta;
    private bool _nova, _incluindo;
    private int _naoLidas;

    protected override async Task OnInitializedAsync()
    {
        Canal.Novidade += AoNovidade;
        await Carregar();
        var args = System.Web.HttpUtility.ParseQueryString(new Uri(Nav.Uri).Query);
        if (Guid.TryParse(args["id"], out var id)) await Abrir(id);
    }

    private async Task Carregar()
    {
        _lista = await Caixa.CaixaAsync(_pasta, _busca, ct: Cancelamento);
        _naoLidas = await Caixa.NaoLidasAsync(Cancelamento);
    }

    private async Task TrocarPasta(MensagensServico.Pasta p) { _pasta = p; _aberta = null; await Carregar(); }
    private Task Buscar() => Carregar();

    private async Task Abrir(Guid id)
    {
        ErrosForm = null; _resposta = null; _incluindo = false; _incluir = [];
        await Executar(async () => { _aberta = await Caixa.AbrirAsync(id, Cancelamento); });
        await Carregar();
    }

    private async Task Responder()
    {
        if (_aberta is null) return;
        var id = _aberta.Id;
        if (await Executar(() => Caixa.ResponderAsync(id, _resposta ?? "", _incluir, Cancelamento), "Resposta enviada."))
        {
            _resposta = null; _incluindo = false; _incluir = [];
            _aberta = await Caixa.AbrirAsync(id, Cancelamento);
            await Carregar();
        }
    }

    private async Task Arquivar()
    {
        if (_aberta is null) return;
        var arquivar = !_aberta.Arquivada;
        if (await Executar(() => Caixa.ArquivarAsync(_aberta.Id, arquivar, Cancelamento), arquivar ? "Arquivada (continua no histórico)." : "Voltou para a entrada."))
        {
            _aberta = null;
            await Carregar();
        }
    }

    private async Task IncluirPessoas()
    {
        _incluindo = !_incluindo;
        if (_incluindo && _opcoes.Count == 0) _opcoes = await Caixa.DestinatariosAsync(Cancelamento);
    }

    private async Task AoEnviar(Guid id)
    {
        _pasta = MensagensServico.Pasta.Enviadas;
        await Abrir(id);
    }

    private void AoNovidade(Guid empresa, IReadOnlyCollection<Guid> usuarios)
    {
        if (empresa != Usuario.EmpresaId || Usuario.UsuarioId is not { } eu || !usuarios.Contains(eu)) return;
        _ = InvokeAsync(async () =>
        {
            try
            {
                await Carregar();
                if (_aberta is { } a && _lista?.FirstOrDefault(x => x.Id == a.Id) is { NaoLidas: > 0 }) _aberta = await Caixa.AbrirAsync(a.Id, Cancelamento);
                StateHasChanged();
            }
            catch (OperationCanceledException) { }
        });
    }

    public override void Dispose()
    {
        Canal.Novidade -= AoNovidade;
        base.Dispose();
    }

    private string LinkTarefa(MensagensServico.ConversaVista c) =>
        c.RegistroEntidade is { } e && c.RegistroId is { } r
            ? $"tarefas?origem={Uri.EscapeDataString(e)}&origemId={r}&desc={Uri.EscapeDataString(c.RegistroDescricao ?? e)}&titulo={Uri.EscapeDataString(c.Assunto)}"
            : $"tarefas?origem=Conversa&origemId={c.Id}&desc={Uri.EscapeDataString($"Mensagem nº {c.Numero}")}&titulo={Uri.EscapeDataString(c.Assunto)}";

    private static string Quando(DateTimeOffset d)
    {
        var l = d.ToLocalTime();
        return l.Date == DateTime.Today ? l.ToString("HH:mm") : l.Date == DateTime.Today.AddDays(-1) ? "ontem" : l.ToString("dd/MM");
    }

    private static string Iniciais(string nome) => string.Concat(nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));

    private static readonly string[] Cores = ["#4f46e5", "#0f8a6b", "#b45309", "#7c3aed", "#be185d", "#0369a1", "#4d7c0f", "#9f1239"];
    private static string Cor(string nome) => Cores[(int)((uint)nome.Aggregate(17, (h, ch) => h * 31 + ch) % (uint)Cores.Length)];
}
