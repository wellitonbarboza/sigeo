using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Regras;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Tests;

/// <summary>Fluxo de compras: requisição → cotação → pedido/contrato → recebimento/medição → pagamento. Dados fictícios.</summary>
public class SuprimentosTests
{
    // ───── T01/T02 Requisição ─────

    [Fact]
    public void Requisicao_de_material_controlado_exige_especificacao_e_herda_do_produto()
    {
        var aco = Fake.Aco();
        var produto = new Produto { Nome = "Vergalhão CA-50 10mm", Unidade = "T", LeadTimeDias = 12, MaterialControladoId = aco.Id, MaterialControlado = aco };
        var r = new Requisicao { ObraId = Guid.NewGuid(), Atividade = "Estrutura – Armação", DataEntrega = Fake.Hoje.AddDays(5) };
        var item = new RequisicaoItem { Quantidade = 4.5m };
        item.HerdarDoProduto(produto);
        r.Itens.Add(item);
        r.RecalcularPrazo();

        Assert.True(item.MaterialControlado);
        Assert.Equal("CA-50, certificado do lote", item.EspecificacaoTecnica);
        Assert.Equal(12, r.PrazoCronogramaDias);
        Assert.NotNull(r.AlertaCronograma(Fake.Hoje));          // entrega em 5 dias < 12 de lead time
        Assert.Empty(r.ValidarEnvio(false));

        item.EspecificacaoTecnica = "";
        Assert.Contains(r.ValidarEnvio(false), e => e.Contains("especificação"));
    }

    [Fact]
    public void Requisicao_de_servico_exige_categoria_e_escopo_anexo()
    {
        var r = new Requisicao { ObraId = Guid.NewGuid(), Tipo = TipoRequisicao.Servico, Atividade = "Pintura", DataEntrega = Fake.Hoje.AddDays(30) };
        r.Itens.Add(new RequisicaoItem { Descricao = "Pintura externa", Quantidade = 1 });
        var erros = r.ValidarEnvio(temAnexoEscopo: false);
        Assert.Contains(erros, e => e.Contains("categoria"));
        Assert.Contains(erros, e => e.Contains("edital/escopo"));
    }

    // ───── T04–T06 Cotação ─────

    private static (Cotacao C, Fornecedor Vencedor) CotacaoCom(decimal preco, int respondidas, bool atende = true)
    {
        var aco = Fake.Aco();
        var reqItem = new RequisicaoItem { Descricao = "Vergalhão", Quantidade = 1, MaterialControlado = true, MaterialControladoId = aco.Id };
        var vencedor = Fake.Qualificado(aco);
        var c = new Cotacao { FornecedorVencedor = vencedor, FornecedorVencedorId = vencedor.Id, CriterioSelecao = CriterioSelecao.MenorPreco };
        for (var n = 0; n < respondidas; n++)
        {
            var fid = n == 0 ? vencedor.Id : Guid.NewGuid();
            c.Fornecedores.Add(new CotacaoFornecedor { FornecedorId = fid, Status = StatusConvite.Respondido });
            c.Itens.Add(new CotacaoItem
            {
                FornecedorId = fid, RequisicaoItem = reqItem, RequisicaoItemId = reqItem.Id, Descricao = "Vergalhão", Quantidade = 1,
                ValorOfertado = preco + n * 10, MaterialControlado = true, AtendeEspecificacao = atende
            });
        }
        return (c, vencedor);
    }

    [Fact]
    public void Cotacao_acima_do_limite_exige_3_propostas_ou_justificativa()
    {
        var (c, _) = CotacaoCom(2500m, respondidas: 1);
        var r = TravasQualidade.VerificarCotacao(c, Fake.P, Fake.Hoje);
        Assert.Equal(3, c.PropostasExigidas);
        Assert.Contains(r.Bloqueios, b => b.Contains("proposta"));

        c.JustificativaPropostas = "Fornecedor exclusivo na região";
        Assert.True(TravasQualidade.VerificarCotacao(c, Fake.P, Fake.Hoje).Liberado);
    }

    [Fact]
    public void Cotacao_ate_o_limite_aceita_1_proposta_mas_exige_especificacao_atendida()
    {
        var (c, _) = CotacaoCom(800m, respondidas: 1, atende: false);
        var r = TravasQualidade.VerificarCotacao(c, Fake.P, Fake.Hoje);
        Assert.Equal(1, c.PropostasExigidas);
        Assert.Single(r.Bloqueios);
        Assert.Contains("especificação", r.Bloqueios[0]);
    }

    // ───── T11/T12 Contrato ─────

    [Fact]
    public void Contrato_valida_conteudo_minimo_por_categoria_e_formalizacao()
    {
        var c = new Contrato { FornecedorId = Guid.NewGuid(), Categoria = "Laboratório", Objeto = "Ensaios de concreto", VigenciaInicio = Fake.Hoje, VigenciaFim = Fake.Hoje.AddMonths(6), Formalizacao = FormalizacaoContrato.AceiteProposta };
        c.Itens.Add(new ContratoItem { Item = "1.1", Descricao = "Rompimento de CP", Unidade = "un", Quantidade = 100, PrecoUnitario = 25 });
        var erros = c.Validar();
        Assert.Contains(erros, e => e.Contains("Laboratório"));
        Assert.Contains(erros, e => e.Contains("Aceite de proposta"));
        Assert.Equal(2500m, c.ValorTotal);
    }

    [Fact]
    public void Contrato_so_ativa_com_aprovacao_e_duas_assinaturas()
    {
        var c = new Contrato();
        Assert.Throws<QualyHub.Domain.Aprovacoes.RegraDeNegocioException>(() => c.Ativar());
        c.AprovadoDiretoriaEm = DateTimeOffset.UtcNow; c.AssinaturaEmpresa = Fake.Hoje;
        Assert.Throws<QualyHub.Domain.Aprovacoes.RegraDeNegocioException>(() => c.Ativar());
        c.AssinaturaFornecedor = Fake.Hoje;
        c.Ativar();
        Assert.Equal(StatusContrato.Ativo, c.Status);
    }

    // ───── T13 Recebimento + FVM ─────

    [Fact]
    public void FVM_aprova_so_com_os_3_criterios_e_reprovada_gera_plano_pontual()
    {
        var r = new Recebimento
        {
            PedidoId = Guid.NewGuid(), FornecedorId = Guid.NewGuid(), NumeroNotaFiscal = "123",
            FvmPrazo = ResultadoCriterio.Aprovado, FvmQualidade = ResultadoCriterio.Reprovado, FvmAtendimento = ResultadoCriterio.Aprovado,
            Destino = DestinoRecebimento.Aceito
        };
        r.Itens.Add(new RecebimentoItem { Descricao = "Vergalhão", QtdPedida = 4.5m, QtdRecebida = 4.5m, MaterialControlado = true, Lote = "88231", DocumentosRecebidos = true });

        Assert.False(r.FvmAprovada);
        var erros = r.Validar();
        Assert.Contains(erros, e => e.Contains("ocorrência"));
        Assert.Contains(erros, e => e.Contains("destino"));

        r.Ocorrencia = "Barras com oxidação acima do aceitável";
        var plano = RegrasAvaliacao.PlanoPorFvm(r, Fake.Hoje, "PA-0001");
        Assert.NotNull(plano);
        Assert.Equal(TipoPlanoAcao.Pontual, plano!.TipoPlano);
        Assert.Equal(OrigemPlanoAcao.Fvm, plano.Origem);
    }

    // ───── T14/T15 Medição ─────

    private static (Medicao M, Contrato C, ServicoControlado S) MedicaoBase()
    {
        var s = Fake.Armadura();
        var f = Fake.Qualificado(s: s);
        var c = new Contrato { Fornecedor = f, FornecedorId = f.Id, RetencaoPerc = 5, Status = StatusContrato.Ativo };
        var ci = new ContratoItem { Item = "1.1", Descricao = "Armação", ServicoControlado = s, ServicoControladoId = s.Id, Unidade = "kg", Quantidade = 1000, PrecoUnitario = 10 };
        c.Itens.Add(ci);
        var m1 = new Medicao { Contrato = c, NumeroMedicao = 1, Status = StatusMedicao.Aprovada };
        m1.Itens.Add(new MedicaoItem { ContratoItemId = ci.Id, QtdPeriodo = 300, ValorPeriodo = 3000 });
        var m2 = new Medicao { Contrato = c, NumeroMedicao = 2 };
        m2.Itens.Add(new MedicaoItem { ContratoItemId = ci.Id, QtdPeriodo = 200 });
        m2.Recalcular([m1]);
        return (m2, c, s);
    }

    [Fact]
    public void Medicao_calcula_anterior_acumulado_saldo_e_retencao()
    {
        var (m, _, _) = MedicaoBase();
        var i = m.Itens.Single();
        Assert.Equal(300, i.QtdAnterior);
        Assert.Equal(500, i.QtdAcumulada);
        Assert.Equal(50.0m, i.PercExecutado);
        Assert.Equal(2000m, m.ValorPeriodo);
        Assert.Equal(5000m, m.ValorAcumulado);
        Assert.Equal(5000m, m.Saldo);
        Assert.Equal(100m, m.ValorRetencao);
    }

    [Fact]
    public void Nota_ponderada_com_pesos_padrao_e_faixa_de_acao()
    {
        var m = new Medicao { NotaEscopo = 9, NotaMaterial = 8, NotaServico = 7.5m, NotaLimpeza = 6, NotaDocSeg = 9 };
        m.CalcularNota(Fake.P);
        Assert.Equal(8.00m, m.NotaFinal);
        Assert.Equal(FaixaNota.PlanoPontual, m.Faixa);

        var ruim = new Medicao { NotaEscopo = 5, NotaServico = 6, NotaLimpeza = 6, NotaDocSeg = 6 }; // material N/A
        ruim.CalcularNota(Fake.P);
        Assert.Equal(FaixaNota.ReuniaoPlanoCompleto, ruim.Faixa);
        // Sem o material, o peso 0,25 é redistribuído: (5×0,25 + 6×0,30 + 6×0,10 + 6×0,10)/0,75 = 5,67
        Assert.Equal(5.67m, ruim.NotaFinal);
    }

    [Fact]
    public void Medicao_sem_FVS_aprovada_bloqueia_e_com_FVS_libera()
    {
        var (m, _, s) = MedicaoBase();
        m.NotaEscopo = m.NotaServico = m.NotaLimpeza = m.NotaDocSeg = 10;
        m.CalcularNota(Fake.P);

        var r = TravasQualidade.VerificarMedicao(m, [], Fake.Hoje);
        Assert.Contains(r.Bloqueios, b => b.Contains("sem FVS liberada"));

        var aprovadaNaoLiberada = new Fvs { ServicoControladoId = s.Id, Status = StatusFvs.Aprovada };
        Assert.False(TravasQualidade.VerificarMedicao(m, [aprovadaNaoLiberada], Fake.Hoje).Liberado);
        var fvs = new Fvs { ServicoControladoId = s.Id, Status = StatusFvs.Liberada };
        Assert.True(TravasQualidade.VerificarMedicao(m, [fvs], Fake.Hoje).Liberado);
    }

    [Fact]
    public void Medicao_acima_do_contratado_exige_aditivo()
    {
        var (m, _, _) = MedicaoBase();
        m.Itens.Single().QtdPeriodo = 800;
        m.Recalcular([]);
        m.Itens.Single().QtdAnterior = 300; m.Itens.Single().QtdAcumulada = 1100;
        m.NotaEscopo = m.NotaServico = m.NotaLimpeza = m.NotaDocSeg = 10; m.CalcularNota(Fake.P);
        Assert.Contains(TravasQualidade.VerificarMedicao(m, [], Fake.Hoje).Bloqueios, b => b.Contains("aditivo"));
    }

    [Fact]
    public void Nota_abaixo_de_9_abre_plano_automatico()
    {
        var m = new Medicao { NumeroMedicao = 3, NotaEscopo = 9, NotaMaterial = 8, NotaServico = 7.5m, NotaLimpeza = 6, NotaDocSeg = 9, AvaliadorNome = "Eng. da obra" };
        m.CalcularNota(Fake.P);
        var p = RegrasAvaliacao.PlanoPorMedicao(m, Guid.NewGuid(), Fake.Hoje, "PA-0087");
        Assert.NotNull(p);
        Assert.Contains("limpeza e organização", p!.Problema);
        Assert.Null(RegrasAvaliacao.PlanoPorMedicao(new Medicao { NotaFinal = 9.5m, Faixa = FaixaNota.Nenhuma }, Guid.NewGuid(), Fake.Hoje, "x"));
    }

    // ───── T16/T17 Avaliação ─────

    [Fact]
    public void Plano_so_conclui_com_verificacao_de_eficacia()
    {
        var p = new PlanoAcaoFornecedor();
        Assert.Throws<QualyHub.Domain.Aprovacoes.RegraDeNegocioException>(() => p.Concluir(Eficacia.NaoVerificada, null));
        p.Concluir(Eficacia.Eficaz, "Sem reincidência em 2 medições");
        Assert.Equal(StatusPlanoAcao.Concluida, p.Status);
    }

    [Fact]
    public void Avaliacao_semestral_consolida_FVM_medicoes_e_planos()
    {
        var f = Fake.Fornecedor();
        var c = new Contrato { Fornecedor = f, FornecedorId = f.Id };
        var ok = new Recebimento { FornecedorId = f.Id, DataRecebimento = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero), FvmPrazo = ResultadoCriterio.Aprovado, FvmQualidade = ResultadoCriterio.Aprovado, FvmAtendimento = ResultadoCriterio.Aprovado };
        var ruim = new Recebimento { FornecedorId = f.Id, DataRecebimento = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero), FvmPrazo = ResultadoCriterio.Reprovado, FvmQualidade = ResultadoCriterio.Aprovado, FvmAtendimento = ResultadoCriterio.Aprovado };
        var med = new Medicao { Contrato = c, PeriodoFim = new DateOnly(2026, 8, 31), NotaFinal = 8m };
        var a = new AvaliacaoSemestral { Periodo = AvaliacaoSemestral.PeriodoDe(Fake.Hoje), FornecedorId = f.Id };
        a.Consolidar([ok, ruim], [med], [new PlanoAcaoFornecedor { FornecedorId = f.Id }]);

        Assert.Equal("2026-S2", a.Periodo);
        Assert.Equal(2, a.QtdFvm);
        Assert.Equal(50.0m, a.PercFvmAprovadas);
        Assert.Equal(8m, a.NotaMedia);
        Assert.Equal(1, a.PlanosAbertos);

        a.Decisao = DecisaoSemestral.Desqualificar;
        Assert.Contains(a.ValidarDecisao(), e => e.Contains("comunicado"));
    }

    // ───── T18 Pagamento ─────

    [Fact]
    public void Pagamento_de_material_exige_recebimento_e_bloqueia_FVM_devolvida()
    {
        var pedido = new Pedido();
        var l = new LiberacaoPagamento { Origem = OrigemPagamento.NotaMaterial, PedidoId = pedido.Id, ChaveNFe = new string('4', 44), ValorNF = 100, ValorLiberado = 100 };
        Assert.Contains(TravasQualidade.VerificarPagamento(l, pedido, [], null).Bloqueios, b => b.Contains("recebimento"));

        var devolvido = new Recebimento { PedidoId = pedido.Id, FvmPrazo = ResultadoCriterio.Reprovado, FvmQualidade = ResultadoCriterio.Aprovado, FvmAtendimento = ResultadoCriterio.Aprovado, Destino = DestinoRecebimento.Devolvido, Ocorrencia = "atraso" };
        Assert.False(TravasQualidade.VerificarPagamento(l, pedido, [devolvido], null).Liberado);

        var aceito = new Recebimento { PedidoId = pedido.Id, FvmPrazo = ResultadoCriterio.Aprovado, FvmQualidade = ResultadoCriterio.Aprovado, FvmAtendimento = ResultadoCriterio.Aprovado };
        Assert.True(TravasQualidade.VerificarPagamento(l, pedido, [aceito], null).Liberado);
        Assert.True(l.RecebimentoOk);
    }

    [Fact]
    public void Pagamento_de_servico_exige_medicao_aprovada_e_justificativa_de_diferenca()
    {
        var m = new Medicao { Status = StatusMedicao.AguardandoAprovacao };
        var l = new LiberacaoPagamento { Origem = OrigemPagamento.MedicaoServico, MedicaoId = m.Id, ChaveNFe = new string('3', 44), ValorNF = 1000, ValorLiberado = 950 };
        var r = TravasQualidade.VerificarPagamento(l, null, [], m);
        Assert.Contains(r.Bloqueios, b => b.Contains("não aprovada"));
        Assert.Contains(r.Bloqueios, b => b.Contains("justifique"));
    }
}
