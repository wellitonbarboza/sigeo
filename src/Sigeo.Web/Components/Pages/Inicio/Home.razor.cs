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
    private sealed record Pend(EntidadeAprovavel Entidade, Guid EntidadeId, string Titulo, string Solicitante, decimal Valor, DateTimeOffset Enviado, DateTimeOffset? Prazo, bool Atrasada);
    private List<Pend> _aprovar = [];
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

    protected override async Task OnInitializedAsync()
    {
        await Rotina.GarantirHojeAsync(Usuario.EmpresaId, Cancelamento); // qualificação que venceu hoje etc. nos processos em andamento
        _usuarios = await Cat.UsuariosAsync(); await Carregar();
    }

    private async Task Carregar()
    {
        await using var db = Fabrica.CriarLeitura();
        var agora = DateTimeOffset.UtcNow;
        var alcadas = await db.Alcadas.AsNoTracking().ToListAsync(Cancelamento);
        var abertas = await db.SolicitacoesAprovacao.AsNoTracking().Include(s => s.Etapas).Where(s => s.Status == StatusAprovacao.EmAprovacao).OrderBy(s => s.EnviadoEm).ToListAsync(Cancelamento);
        _aprovar = [];
        if (Usuario.UsuarioId is not null)
            foreach (var s in abertas)
                if (await Aprov.PodeDecidirAsync(db, s, Cancelamento))
                {
                    var horas = alcadas.FirstOrDefault(a => a.Entidade == s.Entidade && a.Ordem == s.EtapaAtual)?.PrazoHoras;
                    var desde = s.Etapas.Where(e => e.DecididoEm != null).Select(e => e.DecididoEm).Max() ?? s.EnviadoEm;
                    DateTimeOffset? prazo = horas is { } h ? desde.AddHours(h) : null;
                    _aprovar.Add(new Pend(s.Entidade, s.EntidadeId, s.Titulo, s.SolicitanteNome, s.Valor, s.EnviadoEm, prazo, prazo < agora));
                }
        _pagamentos = _aprovar.Where(a => a.Entidade == EntidadeAprovavel.Pagamento).Select(a => (a.Valor, a.EntidadeId)).ToList();
        var eu = Usuario.UsuarioId;
        _leituras = (await db.Ciencias.AsNoTracking().Include(c => c.Revisao).ThenInclude(r => r.Documento).Where(c => c.UsuarioId == eu && c.LidoEm == null).ToListAsync(Cancelamento))
            .Select(c => ($"{c.Revisao.Documento.Codigo} {c.Revisao.Rotulo} – {c.Revisao.Documento.Titulo}", c.Prazo)).ToList();
        _tarefas = await db.Tarefas.AsNoTracking().Include(t => t.Leituras).Where(t => t.ResponsavelId == eu && (t.Status == StatusTarefa.Aberta || t.Status == StatusTarefa.EmAndamento))
                                   .OrderBy(t => t.Prazo).ToListAsync(Cancelamento);
        if (Usuario.UsuarioId is not null) _msgs = await Mensagens.CaixaAsync(MensagensServico.Pasta.Entrada, null, 50, Cancelamento);
        var ha60 = agora.AddDays(-60);
        _meus = await db.SolicitacoesAprovacao.AsNoTracking().Include(s => s.Etapas).Where(s => s.SolicitanteUsuarioId == eu && s.EnviadoEm >= ha60).OrderByDescending(s => s.EnviadoEm).Take(LimiteMeus).ToListAsync(Cancelamento);
        var del = await db.DelegacoesAprovacao.AsNoTracking().Where(d => d.TitularUsuarioId == eu && !d.Revogada && d.Inicio <= agora && d.Fim > agora).FirstOrDefaultAsync(Cancelamento);
        _delegacao = del is null ? null : (_usuarios.FirstOrDefault(u => u.Id == del.SubstitutoUsuarioId)?.Texto ?? "?", del.Fim);

        var p = await ParametrosServico.CarregarAsync(db, Cancelamento);
        _alertas = [];
        void A(int n, string texto, string tom, string link) { if (n > 0) _alertas.Add((texto, n, tom, link)); }
        A(await db.FornecedorDocumentos.CountAsync(d => d.Bloqueia && d.DataValidade != null && d.DataValidade < Hoje && d.Status != StatusDocumento.Reprovado, Cancelamento), "documento(s) de fornecedor vencido(s) — pedidos e medições bloqueados", "erro", "fornecedores/documentos");
        A(await db.FornecedorDocumentos.CountAsync(d => d.Status == StatusDocumento.EmValidacao, Cancelamento), "documento(s) enviados pelo portal aguardando validação", "aviso", "fornecedores/documentos");
        A(await db.Documentos.CountAsync(d => d.ProximaAnalise != null && d.ProximaAnalise < Hoje, Cancelamento), "documento(s) do SGQ com análise crítica vencida", "erro", "sgq/documentos");
        A(await db.Copias.CountAsync(c => c.Status == StatusCopia.RecolhimentoPendente && c.PrazoRecolhimento < Hoje, Cancelamento), "cópia(s) obsoleta(s) com recolhimento atrasado", "erro", "sgq/copias");
        A(await db.Grds.CountAsync(g => g.ConfirmadoEm == null && g.EnviadoEm < agora.AddDays(-1), Cancelamento), "GRD sem confirmação há mais de 1 dia", "aviso", "sgq/grd");
        A(await db.Fvss.CountAsync(f => f.Status == StatusFvs.Reprovada, Cancelamento), "FVS reprovada(s) aguardando reinspeção", "aviso", "obra/fvs");
        A(await db.Fvss.CountAsync(f => f.Status == StatusFvs.Aprovada, Cancelamento), "FVS aprovada(s) aguardando liberação do engenheiro", "info", "obra/fvs");
        A(await db.Rncs.CountAsync(r => (r.Status == StatusRnc.Aberta || r.Status == StatusRnc.EmTratamento) && r.Prazo < Hoje, Cancelamento), "RNC vencida(s)", "erro", "obra/rnc");
        A(await db.AcoesCorretivas.CountAsync(a => a.Status == StatusAcaoCorretiva.AguardandoEficacia && a.DataEficacia <= Hoje, Cancelamento), "ação(ões) corretiva(s) com eficácia a verificar", "aviso", "obra/acoes");
        A((await db.Equipamentos.AsNoTracking().ToListAsync(Cancelamento)).Count(e => e.Status(Hoje, p.Int(Parametros.AlertaCalibracaoDias)) is StatusEquipamento.VenceEmBreve or StatusEquipamento.Bloqueado), "equipamento(s) de medição vencendo ou bloqueados", "aviso", "obra/equipamentos");
        A(await db.EnsaiosRecebimento.CountAsync(x => x.Situacao == SituacaoEnsaio.Aguardando && x.DataPrevista < Hoje, Cancelamento), "ensaio(s) de recebimento com resultado atrasado", "erro", "recebimentos");
        A(await db.Tarefas.CountAsync(t => (t.Status == StatusTarefa.Aberta || t.Status == StatusTarefa.EmAndamento) && t.Prazo < Hoje, Cancelamento), "tarefa(s) atrasada(s) na empresa", "erro", "tarefas");
        A(await db.PlanosAcao.CountAsync(x => x.Status != StatusPlanoAcao.Concluida && x.Prazo < Hoje, Cancelamento), "plano(s) de ação de fornecedor vencido(s)", "erro", "fornecedores/planos");
        A(await db.Contratos.CountAsync(k => k.Status == StatusContrato.Ativo && k.VigenciaFim <= Hoje.AddDays(30), Cancelamento), "contrato(s) vencendo em 30 dias", "aviso", "contratos");
        A(await db.Colaboradores.CountAsync(c => c.Situacao == SituacaoColaborador.Bloqueado && (c.DataSaida == null || c.DataSaida > Hoje), Cancelamento), "colaborador(es) bloqueados (ASO/integração)", "erro", "sst/colaboradores");
        A(await db.DocumentosColaborador.CountAsync(d => d.Validacao == ValidacaoDoc.Aguardando, Cancelamento), "documento(s) de colaborador aguardando validação do SST", "aviso", "sst/documentos");
        A(await db.PermissoesTrabalho.CountAsync(x => x.Status == StatusPermissaoTrabalho.Liberada && x.Fim < agora, Cancelamento), "PT sem encerramento", "erro", "sst/pt");
        A(await db.Epis.CountAsync(e => e.Ativo && e.ValidadeCa <= Hoje.AddDays(p.Int(Parametros.AlertaCaDias)), Cancelamento), "EPI com CA vencendo/vencido", "aviso", "sst/epi");
        _vazio = !await db.Obras.AnyAsync(Cancelamento) || !await db.Fornecedores.AnyAsync(Cancelamento);
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
