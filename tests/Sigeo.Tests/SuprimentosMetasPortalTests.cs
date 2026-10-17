using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;
using Xunit;

namespace Sigeo.Tests;

/// <summary>Regras de atendimento da requisição, encaminhamento por comprador, economia real, histórico de preço,
/// vencedor por item, situação da proposta no portal, documentação mensal (competência) e nota do placar de metas.</summary>
public class SuprimentosMetasPortalTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 17);
    private static readonly DateTimeOffset Agora = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Obra = Guid.NewGuid();

    // ---------------- Saldo e situação da requisição ----------------

    private static RequisicaoItem Item(decimal qtd, decimal cancelada = 0) => new() { Descricao = "Cimento CP-II", Quantidade = qtd, QtdCancelada = cancelada };

    [Fact]
    public void Saldo_desconta_pedido_e_cancelado_e_cotacao_aberta_nao_soma_duas_vezes()
    {
        var i = Item(100);
        var cot = Guid.NewGuid();
        var s = AtendimentoRequisicao.Calcular([i], [new(cot, i.Id, 40), new(cot, i.Id, 40)], [new(i.Id, 30, 10)]).Single();
        Assert.Equal(70, s.APedir);
        Assert.Equal(40, s.EmCotacao);
        Assert.Equal(30, s.DisponivelCotacao);
        Assert.Equal(20, s.AReceber);
        Assert.Equal("Parcial", s.Situacao);
    }

    [Fact]
    public void Requisicao_parcial_atendida_e_encerrada_com_saldo()
    {
        var a = Item(10); var b = Item(5);
        var parcial = AtendimentoRequisicao.Calcular([a, b], [], [new(a.Id, 10, 10)]);
        Assert.Equal(StatusRequisicao.ParcialmenteAtendida, AtendimentoRequisicao.Situacao(StatusRequisicao.EmCotacao, parcial));
        var total = AtendimentoRequisicao.Calcular([a, b], [], [new(a.Id, 10, 10), new(b.Id, 5, 5)]);
        Assert.Equal(StatusRequisicao.Atendida, AtendimentoRequisicao.Situacao(StatusRequisicao.ParcialmenteAtendida, total));

        var saldoB = AtendimentoRequisicao.Calcular([b], [], [new(b.Id, 2, 2)]).Single();
        Assert.Equal(3, AtendimentoRequisicao.EncerrarSaldo(b, saldoB, "Obra trocou a especificação"));
        var fim = AtendimentoRequisicao.Calcular([a, b], [], [new(a.Id, 10, 10), new(b.Id, 2, 2)]);
        Assert.Equal(StatusRequisicao.EncerradaComSaldo, AtendimentoRequisicao.Situacao(StatusRequisicao.ParcialmenteAtendida, fim));
        Assert.Equal(SituacaoLote.Encerrado, AtendimentoRequisicao.SituacaoLote(fim));
    }

    [Fact]
    public void Encerrar_saldo_exige_motivo_e_sem_cotacao_aberta()
    {
        var i = Item(10);
        var s = AtendimentoRequisicao.Calcular([i], [new(Guid.NewGuid(), i.Id, 10)], []).Single();
        Assert.Throws<RegraDeNegocioException>(() => AtendimentoRequisicao.EncerrarSaldo(i, s, " "));
        Assert.Throws<RegraDeNegocioException>(() => AtendimentoRequisicao.EncerrarSaldo(i, s, "motivo"));
    }

    [Fact]
    public void Situacao_nao_muda_requisicao_em_aprovacao()
    {
        var i = Item(10);
        var s = AtendimentoRequisicao.Calcular([i], [], [new(i.Id, 10, 10)]);
        Assert.Equal(StatusRequisicao.AguardandoEngenheiro, AtendimentoRequisicao.Situacao(StatusRequisicao.AguardandoEngenheiro, s));
    }

    // ---------------- Encaminhamento por carteira ----------------

    private static CarteiraComprador Carteira(string nome, params CarteiraCategoria[] cats) => new() { UsuarioId = Guid.NewGuid(), Nome = nome, Categorias = cats.ToList() };

    [Fact]
    public void Encaminha_subgrupo_da_obra_antes_do_grupo_geral()
    {
        var geral = Carteira("Ana", new CarteiraCategoria { Grupo = "Hidráulica" });
        var obra = Carteira("Bruno", new CarteiraCategoria { Grupo = "Hidráulica", SubGrupo = "Conexões", ObraId = Obra });
        var d = EncaminhamentoCompras.Encaminhar("HIDRÁULICA", "conexões", null, Obra, [geral, obra], Hoje);
        Assert.Equal("Bruno", d.CompradorNome);
        var outra = EncaminhamentoCompras.Encaminhar("Hidráulica", "Conexões", null, Guid.NewGuid(), [geral, obra], Hoje);
        Assert.Equal("Ana", outra.CompradorNome);
    }

    [Fact]
    public void Ausente_vai_para_substituto_e_sem_carteira_vai_para_fila_do_gestor()
    {
        var sub = Carteira("Carla");
        var tit = Carteira("Ana", new CarteiraCategoria { Grupo = "Elétrica" });
        tit.SubstitutoUsuarioId = sub.UsuarioId; tit.SubstitutoNome = "Carla";
        tit.Ausencias.Add(new AusenciaComprador { Inicio = Hoje.AddDays(-1), Fim = Hoje.AddDays(5) });
        var d = EncaminhamentoCompras.Encaminhar("Elétrica", null, null, Obra, [tit, sub], Hoje);
        Assert.Equal(sub.UsuarioId, d.CompradorId);
        Assert.Equal(tit.UsuarioId, d.TitularId);
        Assert.Null(EncaminhamentoCompras.Encaminhar("Pintura", null, null, Obra, [tit, sub], Hoje).CompradorId);
    }

    [Fact]
    public void Conflito_quando_mesma_categoria_em_duas_carteiras()
    {
        var a = Carteira("Ana", new CarteiraCategoria { Grupo = "Elétrica" });
        var b = Carteira("Bruno", new CarteiraCategoria { Grupo = "elétrica " });
        Assert.Single(EncaminhamentoCompras.Conflitos(b, [a]));
        Assert.Empty(EncaminhamentoCompras.Conflitos(Carteira("Carla", new CarteiraCategoria { Grupo = "Elétrica", ObraId = Obra }), [a]));
    }

    [Fact]
    public void Data_limite_desconta_lead_time_e_letra_do_lote()
    {
        var i = new RequisicaoItem { DataNecessidade = Hoje.AddDays(20), LeadTimeDias = 7 };
        var j = new RequisicaoItem { DataNecessidade = Hoje.AddDays(10), LeadTimeDias = 1 };
        Assert.Equal(Hoje.AddDays(9), EncaminhamentoCompras.DataLimite([i, j], Hoje.AddDays(30)));
        Assert.Equal("A", EncaminhamentoCompras.Letra(0));
        Assert.Equal("C", EncaminhamentoCompras.Letra(2));
    }

    // ---------------- Economia real e histórico de preço ----------------

    private static readonly Guid Produto = Guid.NewGuid();
    private static CompraPreco Compra(int dias, decimal preco, decimal qtd = 10, bool emerg = false, string un = "SC") =>
        new(Guid.NewGuid(), Guid.NewGuid(), 100 + dias, Produto, "Cimento", un, Hoje.AddDays(dias), Guid.NewGuid(), "Fornecedor Teste", Obra, "Obra Teste", qtd, preco, emerg);

    [Fact]
    public void Economia_usa_ultimo_preco_pago_e_ignora_emergencial_e_desvio()
    {
        var l = EconomiaReal.Avaliar([Compra(-90, 30), Compra(-60, 28), Compra(-40, 50, emerg: true), Compra(-30, 60), Compra(-10, 27)]);
        Assert.Equal(BaseEconomia.SemBase, l[0].Base);
        Assert.Equal(BaseEconomia.ComBase, l[1].Base); Assert.Equal(20, l[1].Economia);
        Assert.Equal(BaseEconomia.Emergencial, l[2].Base);
        Assert.Equal(BaseEconomia.Desvio, l[3].Base);
        // base da última = 28 (emergencial e desvio não viram base)
        Assert.Equal(28, l[4].PrecoBase); Assert.Equal(10, l[4].Economia);
        var r = EconomiaReal.Resumir(l);
        Assert.Equal(30, r.Economia);
        Assert.Equal(3, r.SemBase);
    }

    [Fact]
    public void Economia_sem_base_quando_unidade_muda()
    {
        var l = EconomiaReal.Avaliar([Compra(-30, 30), Compra(-10, 1.5m, un: "KG")]);
        Assert.Equal(BaseEconomia.SemBase, l[1].Base);
        Assert.Contains("Unidade", l[1].Motivo);
    }

    [Fact]
    public void Historico_mostra_aumentos_e_variacao_sem_indice()
    {
        var h = HistoricoPreco.Calcular([Compra(-300, 25), Compra(-150, 26), Compra(-60, 28), Compra(-20, 30)], Hoje);
        Assert.Equal(30, h.Ultimo!.PrecoUnitario);
        Assert.Equal(3, h.AumentosSeguidos);
        Assert.Equal(20, h.Variacao12m);           // 25 → 30
        Assert.Equal(15.4m, h.Variacao6m);         // 26 → 30
        Assert.Equal(7.7m, h.MaiorAumento!.VariacaoPerc);
        Assert.True(h.Linhas[1].Aumento);
    }

    // ---------------- Vencedor por item ----------------

    [Fact]
    public void Vencedor_por_item_gera_um_pedido_por_fornecedor()
    {
        var f1 = Guid.NewGuid(); var f2 = Guid.NewGuid(); var r1 = Guid.NewGuid(); var r2 = Guid.NewGuid();
        var c = new Cotacao();
        c.Itens.Add(new CotacaoItem { RequisicaoItemId = r1, FornecedorId = f1, Descricao = "A", Quantidade = 10, ValorOfertado = 5 });
        c.Itens.Add(new CotacaoItem { RequisicaoItemId = r1, FornecedorId = f2, Descricao = "A", Quantidade = 10, ValorOfertado = 6 });
        c.Itens.Add(new CotacaoItem { RequisicaoItemId = r2, FornecedorId = f1, Descricao = "B", Quantidade = 2, ValorOfertado = 90 });
        c.Itens.Add(new CotacaoItem { RequisicaoItemId = r2, FornecedorId = f2, Descricao = "B", Quantidade = 2, ValorOfertado = 80 });
        c.DefinirVencedorItem(r1, "A", f1, 6);
        c.DefinirVencedorItem(r2, "B", f2);
        c.AtualizarVencedorGeral();
        Assert.True(c.VencedorPorItem);
        Assert.Equal(2, c.FornecedoresVencedores().Count);
        Assert.Equal(f2, c.FornecedorVencedorId);          // 160 > 30
        Assert.Equal(6, c.ItensVencedores().Single(i => i.FornecedorId == f1).QuantidadePedido);
        c.DefinirVencedorItem(r1, "A", f2);                 // troca: só um vencedor por item
        Assert.Single(c.Itens, i => i.RequisicaoItemId == r1 && i.Vencedor);
        Assert.Throws<RegraDeNegocioException>(() => c.DefinirVencedorItem(r1, "A", f1, 20));
    }

    // ---------------- Situação da proposta no portal ----------------

    [Fact]
    public void Situacao_da_proposta_vista_pelo_fornecedor()
    {
        var conv = new CotacaoFornecedor { Status = StatusConvite.Convidado };
        Assert.Equal(SituacaoProposta.ConviteRecebido, SituacaoPropostaFornecedor.Calcular(StatusCotacao.AguardandoPropostas, Agora.AddDays(1), conv, 0, 3, false, Agora).Situacao);
        conv.RascunhoEm = Agora;
        Assert.Equal(SituacaoProposta.EmPreenchimento, SituacaoPropostaFornecedor.Calcular(StatusCotacao.AguardandoPropostas, null, conv, 0, 3, false, Agora).Situacao);
        Assert.Equal(SituacaoProposta.PrazoEncerrado, SituacaoPropostaFornecedor.Calcular(StatusCotacao.AguardandoPropostas, Agora.AddHours(-1), conv, 0, 3, false, Agora).Situacao);
        conv.Status = StatusConvite.Respondido;
        var enviada = SituacaoPropostaFornecedor.Calcular(StatusCotacao.AguardandoPropostas, Agora.AddDays(1), conv, 0, 3, false, Agora);
        Assert.Equal(SituacaoProposta.PropostaEnviada, enviada.Situacao); Assert.True(enviada.PodeEditar);
        Assert.Equal(SituacaoProposta.VencedoraParcial, SituacaoPropostaFornecedor.Calcular(StatusCotacao.Aprovado, null, conv, 1, 3, true, Agora).Situacao);
        Assert.Equal(SituacaoProposta.NaoVencedora, SituacaoPropostaFornecedor.Calcular(StatusCotacao.Aprovado, null, conv, 0, 3, true, Agora).Situacao);
        Assert.Equal(SituacaoProposta.Cancelada, SituacaoPropostaFornecedor.Calcular(StatusCotacao.Cancelado, null, conv, 0, 3, false, Agora).Situacao);
    }

    // ---------------- Documentação mensal ----------------

    private static CompetenciaDocumental Competencia(params ItemCompetencia[] itens) => new() { Mes = new DateOnly(2026, 9, 1), Itens = itens.ToList() };

    [Fact]
    public void Competencia_so_libera_medicao_conferida_e_adiado_nao_trava()
    {
        var hol = new ItemCompetencia { Descricao = "Holerite" };
        var ponto = new ItemCompetencia { Descricao = "Cartão de ponto", PodeAdiar = true };
        var c = Competencia(hol, ponto);
        Assert.Throws<RegraDeNegocioException>(() => c.Enviar("Fornecedor", Agora));
        hol.Enviado("blob", "holerite.pdf", "Fornecedor", Agora);
        ponto.SolicitarAdiamento("Relógio de ponto em manutenção", Hoje.AddDays(10), Hoje);
        c.Enviar("Fornecedor", Agora);
        Assert.Single(RegrasCompetencia.BloqueiosMedicao(c.Mes, [c], Hoje));
        hol.Conferir(true, null, "Engenheiro", Agora);
        Assert.Throws<RegraDeNegocioException>(() => c.Concluir("Engenheiro", Agora)); // adiamento ainda não decidido
        ponto.DecidirAdiamento(true, null, "Engenheiro", Agora);
        c.Concluir("Engenheiro", Agora);
        Assert.True(c.LiberaMedicao);
        Assert.Empty(RegrasCompetencia.BloqueiosMedicao(c.Mes, [c], Hoje));
        // adiamento vencido volta a travar
        Assert.Single(RegrasCompetencia.BloqueiosMedicao(c.Mes, [c], Hoje.AddDays(11)));
    }

    [Fact]
    public void Adiamento_so_para_item_que_permite_com_justificativa_e_ate_60_dias()
    {
        var hol = new ItemCompetencia { Descricao = "Holerite" };
        Assert.Throws<RegraDeNegocioException>(() => hol.SolicitarAdiamento("x", Hoje.AddDays(5), Hoje));
        var epi = new ItemCompetencia { Descricao = "Recibo de EPI", PodeAdiar = true };
        Assert.Throws<RegraDeNegocioException>(() => epi.SolicitarAdiamento(" ", Hoje.AddDays(5), Hoje));
        Assert.Throws<RegraDeNegocioException>(() => epi.SolicitarAdiamento("ok", Hoje.AddDays(61), Hoje));
        epi.SolicitarAdiamento("ok", Hoje.AddDays(60), Hoje);
        Assert.Throws<RegraDeNegocioException>(() => epi.DecidirAdiamento(false, null, "Eng", Agora));
        epi.DecidirAdiamento(false, "Precisa entregar", "Eng", Agora);
        Assert.Equal(SituacaoItemCompetencia.Pendente, epi.Situacao);
        Assert.Contains("Adiamento recusado", epi.MotivoRecusa);
    }

    [Fact]
    public void Recusa_exige_motivo_e_reabrir_exige_motivo()
    {
        var i = new ItemCompetencia { Descricao = "GFIP" };
        Assert.Throws<RegraDeNegocioException>(() => i.Conferir(true, null, "Adm", Agora)); // nada enviado
        i.Enviado("b", "gfip.pdf", "F", Agora);
        Assert.Throws<RegraDeNegocioException>(() => i.Conferir(false, "", "Adm", Agora));
        i.Conferir(true, null, "Adm", Agora);
        var c = Competencia(i);
        c.Concluir("Adm", Agora);
        Assert.Throws<RegraDeNegocioException>(() => c.Reabrir(null));
        c.Reabrir("Documento com data errada");
        Assert.Equal(SituacaoCompetencia.ComPendencia, c.Situacao);
    }

    [Fact]
    public void Meses_do_contrato_ate_o_mes_anterior()
    {
        var m = RegrasCompetencia.Meses(new DateOnly(2026, 6, 15), null, Hoje).ToList();
        Assert.Equal([new DateOnly(2026, 6, 1), new(2026, 7, 1), new(2026, 8, 1), new(2026, 9, 1)], m);
        Assert.Equal(new DateOnly(2026, 10, 20), Competencia().Limite(20));
    }

    // ---------------- Metas e nota ----------------

    [Fact]
    public void Atingido_e_situacao_da_meta()
    {
        Assert.Equal(1.2m, RegrasMeta.Atingido(5, 6, false));
        Assert.Equal(0.5m, RegrasMeta.Atingido(5, 10, true));   // tempo de compra: menor é melhor
        Assert.Equal(1, RegrasMeta.Atingido(5, 0, true));
        Assert.Equal("Atingida", RegrasMeta.Situacao(1).Texto);
        Assert.Equal("Atenção", RegrasMeta.Situacao(0.9m).Texto);
        Assert.Equal("Em risco", RegrasMeta.Situacao(0.5m).Texto);
    }

    [Fact]
    public void Nota_60_metas_40_tarefas_limitada_a_100_por_meta()
    {
        // metas: (min(1,1.5)·2 + 0.5·1) / 3 = 0,8333 → ·0,6 = 0,5; tarefas 6/10 → ·0,4 = 0,24; nota 7,4
        Assert.Equal(7.4m, RegrasMeta.Nota([(1.5m, 2), (0.5m, 1)], 6, 10));
    }

    [Fact]
    public void Nota_com_um_lado_so()
    {
        Assert.Equal(7.5m, RegrasMeta.Nota([], 3, 4));
        Assert.Equal(10, RegrasMeta.Nota([(2m, 1)], 0, 0));
        Assert.Null(RegrasMeta.Nota([(null, 1)], 0, 0));
    }
}
