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

namespace Sigeo.Web.Components.Pages.Inicio;

// Código da tela Home.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Home
{
    private List<PendenciaAprovacao> _aprovar = [];
    private List<(decimal Valor, Guid Id)> _pagamentos = [];
    private List<(string Texto, int Qtd, string Tom, string Link)> _alertas = [];
    private List<(string Documento, DateOnly Prazo)> _leituras = [];
    private List<Sigeo.Domain.Colaboracao.Tarefa> _tarefas = [];
    private List<MensagensServico.ResumoConversa> _msgs = [];
    private const int LimiteMeus = 50;
    private List<SolicitacaoAprovacao> _meus = [];
    private (string Substituto, DateTimeOffset Fim)? _delegacao;
    private List<Opcao> _usuarios = [];
    private DelegacaoAprovacao _d = new();
    private bool _vazio, _abertoDel;
    private string _aba = "aprovar";
    private List<(string, string)> Abas => [("aprovar", $"Aprovar ({_aprovar.Count(a => a.Entidade != EntidadeAprovavel.Pagamento)})"), ("pagar", $"Liberar pagamento ({_pagamentos.Count})"),
        ("tarefas", $"Tarefas ({_tarefas.Count})"), ("mensagens", $"Mensagens ({_msgs.Count(m => m.NaoLidas > 0)})"), ("alertas", $"Alertas ({_alertas.Sum(a => a.Qtd)})"), ("leituras", $"Leituras ({_leituras.Count})"), ("meus", "Acompanhar (meus pedidos)")];
    private static string Saudacao() => DateTime.Now.Hour < 12 ? "Bom dia" : DateTime.Now.Hour < 18 ? "Boa tarde" : "Boa noite";
    private static string Espera(DateTimeOffset d) { var h = (DateTimeOffset.UtcNow - d).TotalHours; return h < 24 ? $"{h:0} h" : $"{h / 24:0} dia(s)"; }

    [Inject] private RotinaCadastros Rotina { get; set; } = default!;
    [Inject] private HomeConsultas Consultas { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await Rotina.GarantirHojeAsync(Usuario.EmpresaId, Cancelamento); // qualificação que venceu hoje etc. nos processos em andamento
        _usuarios = await Cat.UsuariosAsync(); await Carregar();
    }

    private async Task Carregar()
    {
        var d = await Consultas.CarregarAsync(Usuario.UsuarioId, Hoje, LimiteMeus, Cancelamento);
        _aprovar = d.Aprovar;
        _pagamentos = _aprovar.Where(a => a.Entidade == EntidadeAprovavel.Pagamento).Select(a => (a.Valor, a.EntidadeId)).ToList();
        _leituras = d.Leituras;
        _tarefas = d.Tarefas;
        if (Usuario.UsuarioId is not null) _msgs = await Mensagens.CaixaAsync(MensagensServico.Pasta.Entrada, null, 50, Cancelamento);
        _meus = d.Meus;
        var del = d.Delegacao;
        _delegacao = del is null ? null : (_usuarios.FirstOrDefault(u => u.Id == del.SubstitutoUsuarioId)?.Texto ?? "?", del.Fim);
        _alertas = d.Alertas;
        _vazio = d.Vazio;
    }

    private void Ir(EntidadeAprovavel e, Guid id)
    {
        var rota = e switch
        {
            EntidadeAprovavel.Requisicao => "requisicoes", EntidadeAprovavel.Cotacao => "cotacoes", EntidadeAprovavel.Pedido => "pedidos",
            EntidadeAprovavel.Contrato => "contratos", EntidadeAprovavel.Medicao => "medicoes", EntidadeAprovavel.Pagamento => "pagamentos",
            EntidadeAprovavel.Aditivo => "contratos", EntidadeAprovavel.DocumentoSgq => "sgq/documentos", EntidadeAprovavel.Cronograma => "engenharia/cronograma?aba=versoes", _ => ""
        };
        Nav.NavigateTo(e == EntidadeAprovavel.Aditivo ? $"contratos?aditivo={id}" : $"{rota}?id={id}");
    }

    private void AbrirDelegacao()
    {
        _d = new DelegacaoAprovacao { TitularUsuarioId = Usuario.UsuarioId ?? Guid.Empty, Inicio = DateTimeOffset.Now.Date, Fim = DateTimeOffset.Now.Date.AddDays(15).AddHours(23) };
        ErrosForm = null; _abertoDel = true;
    }

    private async Task SalvarDelegacao()
    {
        if (await Executar(() => Sistema.DelegarMinhasAprovacoesAsync(_d, Cancelamento), "Delegação registrada.")) { _abertoDel = false; await Carregar(); }
    }
}
