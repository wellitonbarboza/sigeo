using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Sst;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Tests;

/// <summary>Qualidade da obra (Q01–Q15), SST (S01–S13) e aditivo (T28). Dados fictícios.</summary>
public class QualidadeObraTests
{
    private static readonly DateTimeOffset Agora = new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

    private static ModeloInspecao ModeloPreLaje(Guid servico, Guid? seguinte = null)
    {
        var m = new ModeloInspecao { Tipo = TipoModelo.FVS, Codigo = "FVS ITS 03.05", Titulo = "Pré-lajes", ServicoControladoId = servico, NivelLocal = "Pavimento", ServicoSeguinteId = seguinte };
        m.Itens.Add(new ItemVerificacao { Ordem = 1, Descricao = "Apoio mínimo sobre a viga", Metodo = "Trena", Criterio = "≥ 5 cm", ValorMin = 5, ExigeEquipamento = true, Bloqueia = true });
        m.Itens.Add(new ItemVerificacao { Ordem = 2, Descricao = "Limpeza antes da concretagem", Metodo = "Visual", Criterio = "sem detritos", Bloqueia = false, Foto = FotoItem.Opcional });
        return m;
    }

    private static Local Pavimento() => new() { ObraId = Guid.NewGuid(), Tipo = "Pavimento", Nome = "Torre 2 › Pav. 3", Codigo = "T2-P03" };

    [Fact]
    public void Modelo_so_publica_completo_e_revisao_do_documento_coloca_em_revisao()
    {
        var m = new ModeloInspecao { Tipo = TipoModelo.FVS, Codigo = "FVS X" };
        Assert.Throws<ValidacaoFalha>(() => m.Publicar(null));
        m = ModeloPreLaje(Guid.NewGuid());
        m.Publicar(null);
        Assert.Equal(SituacaoModelo.Vigente, m.Situacao);
        m.DocumentoRevisado();
        Assert.Equal(SituacaoModelo.EmRevisao, m.Situacao);
        m.Publicar(null);
        Assert.Equal(1, m.Revisao);
    }

    [Fact]
    public void Fvs_nao_abre_com_modelo_nao_vigente_nem_em_nivel_errado()
    {
        var m = ModeloPreLaje(Guid.NewGuid());
        Assert.Throws<RegraDeNegocioException>(() => Fvs.Abrir(m, Pavimento(), Guid.NewGuid(), Guid.NewGuid(), "Inspetor", Agora, "FVS-0001"));
        m.Publicar(null);
        var unidade = new Local { Tipo = "Apartamento", Nome = "Apto 31", Codigo = "T2-31" };
        Assert.Throws<RegraDeNegocioException>(() => Fvs.Abrir(m, unidade, Guid.NewGuid(), Guid.NewGuid(), "Inspetor", Agora, "FVS-0001"));
    }

    [Fact]
    public void Fvs_valor_fora_da_tolerancia_reprova_exige_disposicao_e_bloqueia_etapa()
    {
        var m = ModeloPreLaje(Guid.NewGuid(), seguinte: Guid.NewGuid()); m.Publicar(null);
        var f = Fvs.Abrir(m, Pavimento(), Guid.NewGuid(), Guid.NewGuid(), "Inspetor", Agora, "FVS-0001");
        f.EquipamentoId = Guid.NewGuid();
        f.Itens.First().ValorMedido = 3;                       // < 5 cm → não conforme automático
        f.Itens.First().Observacao = "Apoio de 3 cm na L5";
        f.Itens.Last().Resultado = ResultadoItem.Conforme;
        var erros = f.Concluir(equipamentoValido: true);
        Assert.Contains(erros, e => e.Contains("disposição"));
        f.Disposicao = "Retrabalho"; f.PrazoCorrecao = new DateOnly(2026, 9, 28);
        Assert.Empty(f.Concluir(true));
        Assert.Equal(StatusFvs.Reprovada, f.Status);
        Assert.True(f.BloqueiaEtapaSeguinte);
        Assert.NotNull(RegrasQualidadeObra.EtapaBloqueada(f.LocalId, m.ServicoSeguinteId!.Value, [(f, m.ServicoSeguinteId)]));
    }

    [Fact]
    public void Fvs_equipamento_vencido_nao_pode_ser_usado()
    {
        var m = ModeloPreLaje(Guid.NewGuid()); m.Publicar(null);
        var f = Fvs.Abrir(m, Pavimento(), Guid.NewGuid(), Guid.NewGuid(), "Inspetor", Agora, "FVS-0002");
        foreach (var i in f.Itens) i.Resultado = ResultadoItem.Conforme;
        Assert.Contains(f.Concluir(true), e => e.Contains("equipamento"));
        f.EquipamentoId = Guid.NewGuid();
        Assert.Contains(f.Concluir(false), e => e.Contains("vencida"));
        Assert.Empty(f.Concluir(true));
        Assert.Equal(StatusFvs.Aprovada, f.Status);
    }

    [Fact]
    public void Reinspecao_aprova_so_com_itens_nc_corrigidos_e_liberacao_so_de_aprovada()
    {
        var m = ModeloPreLaje(Guid.NewGuid()); m.Publicar(null);
        var f = Fvs.Abrir(m, Pavimento(), Guid.NewGuid(), Guid.NewGuid(), "Inspetor", Agora, "FVS-0003");
        f.EquipamentoId = Guid.NewGuid();
        f.Itens.First().Resultado = ResultadoItem.NaoConforme; f.Itens.First().Observacao = "curto";
        f.Itens.Last().Resultado = ResultadoItem.Conforme;
        f.Disposicao = "Reparo"; f.PrazoCorrecao = new DateOnly(2026, 9, 27);
        f.Concluir(true);
        Assert.Throws<RegraDeNegocioException>(() => f.Liberar(Guid.NewGuid(), "Eng", Agora));
        Assert.Empty(f.Reinspecionar(new DateOnly(2026, 9, 27), new Dictionary<Guid, ResultadoItem> { [f.Itens.First().Id] = ResultadoItem.Conforme }));
        Assert.Equal(StatusFvs.Aprovada, f.Status);
        f.Liberar(Guid.NewGuid(), "Eng", Agora);
        Assert.True(f.Liberada);
        Assert.False(f.Aprovada1aInspecao);
    }

    [Fact]
    public void Rnc_gravidade_alta_exige_acao_e_so_encerra_com_ac_encerrada()
    {
        var r = new Rnc { Descricao = "Fissura", Disposicao = "Reparo", ResponsavelNome = "Eng", Gravidade = Gravidade.Alta, Prazo = new DateOnly(2026, 10, 1) };
        Assert.Contains(r.Validar(), e => e.Contains("ação corretiva"));
        r.AvaliarExigencia(0, 2);
        Assert.True(r.ExigeAcao);
        Assert.Throws<RegraDeNegocioException>(() => r.Encerrar("Q", Agora, false));
        r.AcaoCorretivaId = Guid.NewGuid();
        r.Encerrar("Q", Agora, acaoEncerrada: false);
        Assert.Equal(StatusRnc.AguardandoEficacia, r.Status);
        r.Encerrar("Q", Agora, acaoEncerrada: true);
        Assert.Equal(StatusRnc.Encerrada, r.Status);
    }

    [Fact]
    public void Rnc_reincidencia_obriga_acao_corretiva()
    {
        var r = new Rnc { Gravidade = Gravidade.Baixa };
        r.AvaliarExigencia(2, 2);
        Assert.True(r.ExigeAcao);
    }

    [Fact]
    public void Acao_corretiva_nao_eficaz_volta_para_analise_e_eficaz_encerra()
    {
        var a = new AcaoCorretiva { Problema = "Fissuras", AnaliseCausa = "5 porquês", CausaRaiz = "sem critério de umidade", DataEficacia = new DateOnly(2026, 10, 30), CriterioEficacia = "sem fissuras" };
        a.Itens.Add(new AcaoCorretivaItem { Acao = "Revisar ITS", Responsavel = "Qualidade", Prazo = new DateOnly(2026, 9, 30) });
        a.Atualizar();
        Assert.Equal(StatusAcaoCorretiva.EmExecucao, a.Status);
        a.Itens.Single().Status = StatusItemPlano.Feito; a.Atualizar();
        Assert.Equal(StatusAcaoCorretiva.AguardandoEficacia, a.Status);
        a.ResultadoEficacia = Eficacia.NaoEficaz; a.Atualizar();
        Assert.Equal(StatusAcaoCorretiva.EmAnalise, a.Status);
        a.Itens.Single().Status = StatusItemPlano.Feito; a.ResultadoEficacia = Eficacia.Eficaz; a.Atualizar();
        Assert.Equal(StatusAcaoCorretiva.Encerrada, a.Status);
    }

    [Fact]
    public void Equipamento_vencido_ou_reprovado_fica_bloqueado()
    {
        var hoje = new DateOnly(2026, 9, 26);
        var e = new EquipamentoMedicao { Codigo = "TR-07", PeriodicidadeMeses = 6, TipoControle = TipoControleEquipamento.CalibracaoExterna };
        Assert.Equal(StatusEquipamento.Bloqueado, e.Status(hoje, 30));
        Assert.Throws<RegraDeNegocioException>(() => e.Registrar(new Calibracao { Tipo = TipoControleEquipamento.CalibracaoExterna, Data = hoje.AddMonths(-1), Resultado = ResultadoCalibracao.Aprovado }));
        e.Registrar(new Calibracao { Tipo = TipoControleEquipamento.CalibracaoExterna, Data = hoje.AddMonths(-1), Certificado = "C-1", Resultado = ResultadoCalibracao.Aprovado });
        Assert.Equal(StatusEquipamento.EmUso, e.Status(hoje, 30));
        Assert.Equal(StatusEquipamento.VenceEmBreve, e.Status(hoje.AddMonths(5), 30));
        Assert.Equal(StatusEquipamento.Bloqueado, e.Status(hoje.AddMonths(6), 30));
        Assert.Throws<RegraDeNegocioException>(() => e.Registrar(new Calibracao { Tipo = TipoControleEquipamento.VerificacaoInterna, Data = hoje, Resultado = ResultadoCalibracao.Reprovado }));
    }

    [Fact]
    public void Lote_em_quarentena_so_aplica_com_ensaio_aprovado_e_nao_passa_do_recebido()
    {
        var pend = new EnsaioRecebimento { Situacao = SituacaoEnsaio.Aguardando };
        var status = RegrasQualidadeObra.StatusDoLote(true, false, false, quarentena: true, [pend]);
        Assert.Equal(StatusLote.EmQuarentena, status);
        Assert.NotEmpty(RegrasQualidadeObra.PodeAplicar(status, true, [pend], 10, 0, 2));
        pend.Situacao = SituacaoEnsaio.Aprovado;
        status = RegrasQualidadeObra.StatusDoLote(true, false, false, true, [pend]);
        Assert.Equal(StatusLote.Liberado, status);
        Assert.Empty(RegrasQualidadeObra.PodeAplicar(status, true, [pend], 10, 8, 2));
        Assert.NotEmpty(RegrasQualidadeObra.PodeAplicar(status, true, [pend], 10, 9, 2));
    }

    [Fact]
    public void Checklist_da_fvm_com_item_bloqueante_nc_reprova_qualidade()
    {
        var r = new Recebimento { FvmPrazo = ResultadoCriterio.Aprovado, FvmAtendimento = ResultadoCriterio.Aprovado };
        r.FvmItens.Add(new FvmItem { Ordem = 1, Descricao = "Abatimento", ValorMin = 8, ValorMax = 12, ValorMedido = "15", Bloqueia = true });
        r.AplicarChecklist();
        Assert.Equal(ResultadoCriterio.Reprovado, r.FvmQualidade);
        Assert.False(r.FvmAprovada);
    }

    [Fact]
    public void Auditoria_so_emite_relatorio_com_checklist_avaliado_e_evidencia()
    {
        var a = new AuditoriaInterna { Escopo = "Suprimentos", Criterio = "SiAC B", Auditores = "Externo", Independencia = "ok", Inicio = new(2026, 10, 20), Fim = new(2026, 10, 21) };
        Assert.Throws<RegraDeNegocioException>(a.EmitirRelatorio);
        a.Constatacoes.Add(new Constatacao { Requisito = "8.4.1", Pergunta = "Fornecedores qualificados?", Tipo = TipoConstatacao.NaoConformidade });
        Assert.Throws<RegraDeNegocioException>(a.EmitirRelatorio);
        a.Constatacoes.Single().Evidencia = "Pedido 8.303 sem qualificação";
        a.EmitirRelatorio();
        Assert.Equal(StatusAuditoria.RelatorioEmitido, a.Status);
        Assert.Equal(1, a.Ncs);
    }

    // ───── SST ─────

    private static (Colaborador C, List<FuncaoExigencia> M, Dictionary<Guid, Exigencia> E, Exigencia Aso, Exigencia Nr35, Treinamento T35) Armador()
    {
        var t35 = new Treinamento { Descricao = "NR-35", ValidadeMeses = 24 };
        var aso = new Exigencia { Descricao = "ASO", Tipo = TipoExigencia.ExameAso, ValidadeMeses = 12, EfeitoVencimento = EfeitoVencimento.BloqueiaPessoa };
        var nr35 = new Exigencia { Descricao = "NR-35", Tipo = TipoExigencia.Treinamento, TreinamentoId = t35.Id, EfeitoVencimento = EfeitoVencimento.RestringeAtividade, Atividades = AtividadeRisco.Altura };
        var f = new FuncaoObra { Descricao = "Armador" };
        var m = new List<FuncaoExigencia>
        {
            new() { FuncaoObraId = f.Id, ExigenciaId = aso.Id, Obrigatoriedade = Obrigatoriedade.Obrigatorio },
            new() { FuncaoObraId = f.Id, ExigenciaId = nr35.Id, Obrigatoriedade = Obrigatoriedade.Obrigatorio }
        };
        var c = new Colaborador { Nome = "Colaborador Teste", FuncaoObraId = f.Id, DataInicio = new(2026, 8, 4) };
        return (c, m, new() { [aso.Id] = aso, [nr35.Id] = nr35 }, aso, nr35, t35);
    }

    [Fact]
    public void Colaborador_sem_aso_bloqueado_nr35_vencida_restringe_altura()
    {
        var hoje = new DateOnly(2026, 9, 26);
        var (c, m, e, aso, _, t35) = Armador();
        Assert.Equal(SituacaoColaborador.Bloqueado, RegrasSst.Calcular(c, m, e, [], [], false, hoje).Situacao);
        var docAso = new DocumentoColaborador { ExigenciaId = aso.Id, DataEmissao = hoje.AddMonths(-1), Validade = hoje.AddMonths(11), ConclusaoAso = ConclusaoAso.Apto, AptoAtividades = AtividadeRisco.Altura, Validacao = ValidacaoDoc.Validado };
        var s = RegrasSst.Calcular(c, m, e, [docAso], [(t35.Id, hoje.AddDays(-1))], false, hoje);
        Assert.Equal(SituacaoColaborador.AptoComRestricao, s.Situacao);
        Assert.True(s.Restricoes.HasFlag(AtividadeRisco.Altura));
        c.Situacao = s.Situacao; c.Restricoes = s.Restricoes;
        Assert.False(RegrasSst.PodeExecutar(c, TipoPermissaoTrabalho.Altura).Liberado);
        Assert.True(RegrasSst.PodeExecutar(c, TipoPermissaoTrabalho.AQuente).Liberado);
        s = RegrasSst.Calcular(c, m, e, [docAso], [(t35.Id, hoje.AddMonths(20))], false, hoje);
        Assert.Equal(SituacaoColaborador.Apto, s.Situacao);
    }

    [Fact]
    public void Aso_so_vale_validado_e_inapto_bloqueia()
    {
        var hoje = new DateOnly(2026, 9, 26);
        var (c, m, e, aso, _, t35) = Armador();
        var d = new DocumentoColaborador { ExigenciaId = aso.Id, DataEmissao = hoje, Validade = hoje.AddYears(1), ConclusaoAso = ConclusaoAso.Apto, Validacao = ValidacaoDoc.Aguardando };
        Assert.Equal(SituacaoColaborador.Bloqueado, RegrasSst.Calcular(c, m, e, [d], [(t35.Id, hoje.AddYears(1))], false, hoje).Situacao);
        Assert.Throws<RegraDeNegocioException>(() => d.Decidir(false, null, "SST", Agora));
        d.Decidir(true, null, "SST", Agora); d.ConclusaoAso = ConclusaoAso.Inapto;
        Assert.Equal(SituacaoColaborador.Bloqueado, RegrasSst.Calcular(c, m, e, [d], [(t35.Id, hoje.AddYears(1))], false, hoje).Situacao);
    }

    [Fact]
    public void Epi_com_ca_vencido_nao_e_entregue()
    {
        var epi = new Epi { Descricao = "Óculos", Ca = "12345", ValidadeCa = new(2026, 9, 1), VidaUtilDias = 90 };
        Assert.Throws<RegraDeNegocioException>(() => EntregaEpi.Registrar(epi, Guid.NewGuid(), 1, new(2026, 9, 26), MotivoEntregaEpi.PrimeiraEntrega, "assinado", "Almox"));
        epi.ValidadeCa = new(2027, 1, 1);
        var ent = EntregaEpi.Registrar(epi, Guid.NewGuid(), 1, new(2026, 9, 26), MotivoEntregaEpi.PrimeiraEntrega, "assinado", "Almox");
        Assert.Equal(new DateOnly(2026, 12, 25), ent.TrocaPrevista);
    }

    [Fact]
    public void Pt_so_libera_com_todas_as_medidas_e_executantes_aptos()
    {
        var pt = new PermissaoTrabalho
        {
            Tipo = TipoPermissaoTrabalho.Altura, Medidas = "A;B", MedidasMarcadas = "0", Emitente = "TST", ResponsavelArea = "Eng",
            Inicio = Agora, Fim = Agora.AddHours(10)
        };
        pt.Executantes.Add(new ExecutantePt { Liberado = false });
        var e = pt.Liberar(Agora);
        Assert.Contains(e, x => x.Contains("medidas"));
        Assert.Contains(e, x => x.Contains("não pode entrar"));
        pt.MedidasMarcadas = "0;1"; pt.Executantes.Single().Liberado = true;
        Assert.Empty(pt.Liberar(Agora));
        Assert.Equal(StatusPermissaoTrabalho.Liberada, pt.Status);
    }

    [Fact]
    public void Inspecao_risco_grave_exige_paralisacao_e_calcula_conformidade()
    {
        var i = new InspecaoSeguranca();
        i.Itens.Add(new InspecaoSegurancaItem { Ordem = 1, Resultado = ResultadoItem.NaoConforme, Gravidade = Gravidade.RiscoGraveIminente, Responsavel = "Empreiteira", Prazo = new(2026, 9, 26) });
        i.Itens.Add(new InspecaoSegurancaItem { Ordem = 2, Resultado = ResultadoItem.Conforme });
        Assert.Contains(i.Concluir(), x => x.Contains("paralisação"));
        i.Paralisacao = "Pav. 5 paralisado";
        Assert.Empty(i.Concluir());
        Assert.Equal(50m, i.Conformidade);
        Assert.Equal(StatusInspecaoSst.EmTratamento, i.Status);
    }

    [Fact]
    public void Ocorrencia_acidente_exige_cat_e_investigacao_para_encerrar()
    {
        var o = new Ocorrencia { Tipo = TipoOcorrencia.AcidenteComAfastamento, Descricao = "Corte superficial", Status = StatusOcorrencia.Encerrada };
        var e = o.Validar();
        Assert.Contains(e, x => x.Contains("dias perdidos"));
        Assert.Contains(e, x => x.Contains("CAT"));
        Assert.Contains(e, x => x.Contains("investigação"));
    }

    [Fact]
    public void Turma_concluida_gera_competencia_com_validade()
    {
        var t = new Treinamento { Descricao = "NR-35", ValidadeMeses = 24, MetodoEficacia = MetodoEficaciaTreinamento.Prova };
        var turma = new Turma { TreinamentoId = t.Id, Data = Agora, Instrutor = "TST", Local = "Canteiro" };
        turma.Participantes.Add(new Participante { ColaboradorId = Guid.NewGuid(), Presenca = true });
        turma.Participantes.Add(new Participante { ColaboradorId = Guid.NewGuid(), Presenca = false });
        Assert.Throws<RegraDeNegocioException>(() => turma.Concluir(t));
        turma.Participantes.First().Avaliacao = "9,0";
        turma.Concluir(t);
        Assert.Equal(new DateOnly(2028, 9, 26), turma.Participantes.First().ValidoAte);
        Assert.Equal(ResultadoParticipante.NaoCompareceu, turma.Participantes.Last().Resultado);
    }

    // ───── Aditivo ─────

    [Fact]
    public void Aditivo_calcula_percentual_e_so_vigora_aprovado_e_assinado()
    {
        var k = new Contrato { Status = StatusContrato.Ativo, VigenciaFim = new(2027, 6, 30) };
        var ci = new ContratoItem { Item = "1.1", Descricao = "Painéis", Quantidade = 12400, PrecoUnitario = 66 };
        k.Itens.Add(ci);
        var a = new Aditivo { Tipo = TipoAditivo.Combinado, Justificativa = "Torre 4", NovaVigencia = new(2027, 8, 31) };
        a.Itens.Add(new AditivoItem { ContratoItemId = ci.Id, QtdOriginal = 12400, PrecoOriginal = 66, QtdDelta = 850, PrecoUnitario = 66 });
        Assert.Empty(a.Validar(k));
        a.Calcular(k.ValorTotal, 0);
        Assert.Equal(56100m, a.Valor);
        Assert.Equal(6.85m, a.PercAcumulado);
        Assert.Throws<RegraDeNegocioException>(() => a.Aplicar(k));
        a.AprovadoEm = Agora;
        Assert.Throws<RegraDeNegocioException>(() => a.Aplicar(k));
        a.AssinaturaEmpresa = a.AssinaturaFornecedor = new(2026, 9, 26);
        a.Aplicar(k);
        Assert.Equal(13250, ci.Quantidade);
        Assert.Equal(new DateOnly(2027, 8, 31), k.VigenciaFim);
        Assert.Equal(StatusAditivo.Vigente, a.Status);
    }

    [Fact]
    public void Aditivo_de_prazo_exige_nova_vigencia_posterior()
    {
        var k = new Contrato { Status = StatusContrato.Ativo, VigenciaFim = new(2027, 6, 30) };
        var a = new Aditivo { Tipo = TipoAditivo.Prazo, Justificativa = "chuvas", NovaVigencia = new(2027, 5, 1) };
        Assert.Contains(a.Validar(k), e => e.Contains("nova vigência"));
    }
}
