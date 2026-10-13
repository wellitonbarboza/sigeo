using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Regras;
using QualyHub.Domain.Suprimentos;
using Xunit;

namespace QualyHub.Tests;

/// <summary>
/// Cenário fictício: Condomínio "QA" (SPE fictícia), torre de 4 pavimentos (térreo + 3) com 2 aptos 2Q por pavimento,
/// apartamento com 4 cômodos medidos; hall por pavimento, escada na torre e guarita no condomínio.
/// </summary>
public class EngenhariaTests
{
    private static readonly Guid Obra = Guid.NewGuid(), Spe = Guid.NewGuid();

    private static Tipologia Apto()
    {
        var t = new Tipologia { Codigo = "2Q-40", Nome = "2 quartos", Tipo = TipoTipologia.Apartamento, AreaPrivativa = 40, Dormitorios = 2, PeDireitoPadrao = 2.90m };
        t.Ambientes.Add(new TipologiaAmbiente { TipologiaId = t.Id, Ordem = 1, Nome = "Sala de estar", Categoria = "Sala de estar", AreaPiso = 12, Perimetro = 14, AreaVaos = 3 });
        t.Ambientes.Add(new TipologiaAmbiente { TipologiaId = t.Id, Ordem = 2, Nome = "Dormitório 01", Categoria = "Dormitório", AreaPiso = 10, Perimetro = 13 });
        t.Ambientes.Add(new TipologiaAmbiente { TipologiaId = t.Id, Ordem = 3, Nome = "Dormitório 02", Categoria = "Dormitório", AreaPiso = 8, Perimetro = 11.6m });
        t.Ambientes.Add(new TipologiaAmbiente { TipologiaId = t.Id, Ordem = 4, Nome = "BWC", Categoria = "BWC", AreaPiso = 2.5m, Perimetro = 6.4m, PeDireito = 2.40m });
        t.CompletarSiglas();
        return t;
    }

    private static Tipologia Torre(Tipologia apto)
    {
        var t = new Tipologia { Codigo = "T-2Q", Nome = "Torre 2Q", Tipo = TipoTipologia.Torre, Pavimentos = 4, PrimeiroTerreo = true, PeDireitoPadrao = 2.90m };
        t.Mix.Add(new TipologiaMix { TipologiaId = t.Id, Ordem = 1, PavimentoDe = 1, PavimentoAte = 4, ApartamentoTipologiaId = apto.Id, Quantidade = 2 });
        t.Ambientes.Add(new TipologiaAmbiente { TipologiaId = t.Id, Ordem = 1, Escopo = EscopoAmbiente.AreaComumPavimento, Nome = "Hall", AreaPiso = 5, Perimetro = 9 });
        t.Ambientes.Add(new TipologiaAmbiente { TipologiaId = t.Id, Ordem = 2, Escopo = EscopoAmbiente.AreaComumTorre, Nome = "Escada", AreaPiso = 11.7m, Perimetro = 14 });
        t.CompletarSiglas();
        return t;
    }

    private static Tipologia Implantacao()
    {
        var t = new Tipologia { Codigo = "IMP-A", Nome = "Implantação A", Tipo = TipoTipologia.Implantacao };
        t.Ambientes.Add(new TipologiaAmbiente { TipologiaId = t.Id, Ordem = 1, Escopo = EscopoAmbiente.AreaComumCondominio, Nome = "Guarita", AreaPiso = 6, Perimetro = 10 });
        t.CompletarSiglas();
        return t;
    }

    private static (List<Local> L, Tipologia Apto, Tipologia Torre, Tipologia Impl) Gerar(int torres = 1)
    {
        var a = Apto(); var t = Torre(a); var i = Implantacao();
        var q = new Quadra { Codigo = "QA", Nome = "Condomínio Teste", Ordem = 1 };
        var l = GeradorEmpreendimento.Gerar(Obra, q, Spe, i, Enumerable.Range(1, torres).Select(n => new TorreGerar($"Torre {n:00}", t)).ToList(), new Dictionary<Guid, Tipologia> { [a.Id] = a });
        return (l, a, t, i);
    }

    [Fact]
    public void Gera_condominio_torre_pavimento_apto_comodo_e_areas_comuns()
    {
        var (l, _, _, _) = Gerar();
        Assert.Single(l, x => x.Tipo == GeradorEmpreendimento.TipoCondominio);
        Assert.Single(l, x => x.Tipo == GeradorEmpreendimento.TipoTorre);
        Assert.Equal(4, l.Count(x => x.Tipo == GeradorEmpreendimento.TipoPavimento));
        Assert.Equal(8, l.Count(x => x.Tipo == GeradorEmpreendimento.TipoApartamento));
        Assert.Equal(32, l.Count(x => x.Tipo == GeradorEmpreendimento.TipoComodo));
        Assert.Equal(4 + 1 + 1, l.Count(x => x.Tipo == GeradorEmpreendimento.TipoAreaComum)); // hall × 4, escada, guarita
        Assert.All(l, x => Assert.Equal(Spe, x.SpeId));
        Assert.Contains(l, x => x.Codigo == "QA-T01-P04-402-BWC");
        Assert.Contains(l, x => x.Codigo == "QA-T01-P01-HAL" && x.Tipo == GeradorEmpreendimento.TipoAreaComum);
        Assert.Equal("Térreo", l.First(x => x.Codigo == "QA-T01-P01").Nome);
        Assert.Equal("1º pavimento", l.First(x => x.Codigo == "QA-T01-P02").Nome);
    }

    [Fact]
    public void Siglas_dos_comodos_sao_unicas_e_legiveis()
    {
        var a = Apto();
        Assert.Equal(["SAL", "DOR1", "DOR2", "BWC"], a.Ambientes.OrderBy(x => x.Ordem).Select(x => x.Sigla).ToList());
        Assert.Empty(a.Validar());
    }

    [Fact]
    public void Medidas_do_comodo_vem_da_tipologia_com_parede_liquida()
    {
        var (l, _, _, _) = Gerar();
        var sala = l.First(x => x.Codigo == "QA-T01-P01-101-SAL");
        Assert.Equal(12, sala.AreaPiso);
        Assert.Equal(2.90m, sala.PeDireito);
        Assert.Equal(14 * 2.90m - 3, sala.AreaParede);
        Assert.Equal(2.40m, l.First(x => x.Codigo == "QA-T01-P01-101-BWC").PeDireito); // pé-direito próprio (forro rebaixado)
    }

    [Fact]
    public void Fracao_ideal_fecha_100_no_condominio()
    {
        var (l, _, _, _) = Gerar(2);
        var aptos = l.Where(x => x.Tipo == GeradorEmpreendimento.TipoApartamento).ToList();
        GeradorEmpreendimento.DistribuirFracao(aptos);
        Assert.Empty(GeradorEmpreendimento.ConferirFracao(l));
        Assert.Equal(1m, aptos.Sum(x => x.FracaoIdeal));
        aptos[0].FracaoIdeal += 0.01m;
        Assert.Contains("Condomínio Teste", GeradorEmpreendimento.ConferirFracao(l).Single());
    }

    [Fact]
    public void Validacao_exige_spe_e_tipologia_de_torre()
    {
        var a = Apto();
        var q = new Quadra { Codigo = "QA", Nome = "Condomínio Teste" };
        var aptos = new Dictionary<Guid, Tipologia> { [a.Id] = a };
        var e = GeradorEmpreendimento.Validar(q, null, [new TorreGerar("Torre 01", a)], aptos);
        Assert.Contains(e, x => x.Contains("SPE"));
        Assert.Contains(e, x => x.Contains("tipologia de torre"));
        Assert.Empty(GeradorEmpreendimento.Validar(q, Spe, [new TorreGerar("Torre 01", Torre(a))], aptos));
    }

    [Fact]
    public void Acrescenta_torre_em_condominio_existente()
    {
        var (l, a, t, i) = Gerar();
        var cond = l[0];
        var q = new Quadra { Codigo = "QA", Nome = "Condomínio Teste" };
        var novos = GeradorEmpreendimento.Gerar(Obra, q, Spe, i, [new TorreGerar("Torre 02", t)], new Dictionary<Guid, Tipologia> { [a.Id] = a }, cond, 1);
        Assert.DoesNotContain(novos, x => x.Tipo == GeradorEmpreendimento.TipoCondominio);
        var torre = Assert.Single(novos, x => x.Tipo == GeradorEmpreendimento.TipoTorre);
        Assert.Equal("QA-T02", torre.Codigo);
        Assert.Equal(cond.Id, torre.LocalPaiId);
        Assert.DoesNotContain(novos, x => x.Nome == "Guarita"); // áreas comuns do condomínio não duplicam
    }

    // ───── Orçamento: serviço × local ─────

    private static Composicao Contrapiso(Guid cimento) => new()
    {
        Codigo = "CTP-01", Descricao = "Contrapiso convencional", Unidade = "m²", Regra = RegraQuantidade.AreaPiso, ServicoControladoId = Guid.NewGuid(),
        Itens =
        {
            new ComposicaoItem { Tipo = TipoInsumo.Material, ProdutoId = cimento, Descricao = "Cimento", Unidade = "kg", Coeficiente = 7.5m, PerdaPerc = 10, CustoUnitario = 0.80m },
            new ComposicaoItem { Tipo = TipoInsumo.MaoObraPropria, FuncaoObraId = Guid.NewGuid(), Descricao = "Pedreiro", Unidade = "h", Coeficiente = 0.35m, CustoUnitario = 30 },
            new ComposicaoItem { Tipo = TipoInsumo.MaoObraEmpreitada, Descricao = "Empreitada", Unidade = "m²", Coeficiente = 1, CustoUnitario = 12 },
        }
    };

    [Fact]
    public void Composicao_calcula_custo_e_horas()
    {
        var c = Contrapiso(Guid.NewGuid());
        Assert.Equal(Math.Round(7.5m * 1.1m * 0.8m, 4) + 10.5m + 12m, c.CustoUnitario);
        Assert.Equal(0.35m, c.HorasPorUnidade);
        Assert.Equal(82.5m, c.Itens.First().QuantidadePor(10));
        Assert.Empty(c.Validar());
        Assert.Contains(new Composicao { Codigo = "X", Descricao = "Y", Itens = { new ComposicaoItem { Tipo = TipoInsumo.Material, Descricao = "Areia", Coeficiente = 1 } } }.Validar(),
            e => e.Contains("produto do cadastro"));
        Assert.Contains(new Composicao { Codigo = "X", Descricao = "Y", Unidade = "m²", Itens = { new ComposicaoItem { Tipo = TipoInsumo.MaoObraPropria, Descricao = "Servente", Unidade = "h", Coeficiente = 1 } } }.Validar(),
            e => e.Contains("precisa da função"));
    }

    [Fact]
    public void Matriz_explode_servicos_da_tipologia_para_os_locais()
    {
        var (l, a, t, _) = Gerar();
        var ctp = Contrapiso(Guid.NewGuid());
        var pintura = new Composicao { Codigo = "PIN-01", Descricao = "Pintura de parede", Regra = RegraQuantidade.AreaParede };
        var kit = new Composicao { Codigo = "KIT-01", Descricao = "Kit de entrega", Unidade = "un", Regra = RegraQuantidade.Unidade };
        var escada = new Composicao { Codigo = "ESC-01", Descricao = "Corrimão", Unidade = "m", Regra = RegraQuantidade.Fixa };
        var comps = new[] { ctp, pintura, kit, escada }.ToDictionary(c => c.Id);
        var amb = a.Ambientes.ToDictionary(x => x.Sigla!);
        var aps = new List<ServicoAplicacao>
        {
            new() { TipologiaId = a.Id, ComposicaoId = ctp.Id, Alvo = AlvoServico.Ambiente, AmbienteModeloId = amb["SAL"].Id },
            new() { TipologiaId = a.Id, ComposicaoId = ctp.Id, Alvo = AlvoServico.Ambiente, AmbienteModeloId = amb["DOR1"].Id },
            new() { TipologiaId = a.Id, ComposicaoId = pintura.Id, Alvo = AlvoServico.Apartamento },
            new() { TipologiaId = a.Id, ComposicaoId = kit.Id, Alvo = AlvoServico.Apartamento },
            new() { TipologiaId = t.Id, ComposicaoId = escada.Id, Alvo = AlvoServico.Pavimento, QuantidadeFixa = 6.5m },
        };
        var cels = MotorOrcamento.Gerar(l, aps, comps);
        Assert.Equal(8 * 2, cels.Count(c => c.ComposicaoId == ctp.Id));
        Assert.Equal(8 * (12 + 10), cels.Where(c => c.ComposicaoId == ctp.Id).Sum(c => c.Quantidade));
        // Pintura no apartamento: soma das paredes dos cômodos.
        var paredeApto = a.Ambientes.Sum(x => x.AreaParede(a.PeDireitoPadrao));
        Assert.All(cels.Where(c => c.ComposicaoId == pintura.Id), c => Assert.Equal(paredeApto, c.Quantidade));
        Assert.Equal(8, cels.Where(c => c.ComposicaoId == kit.Id).Sum(c => c.Quantidade));
        Assert.Equal(4 * 6.5m, cels.Where(c => c.ComposicaoId == escada.Id).Sum(c => c.Quantidade));
        Assert.All(cels, c => Assert.Equal(Spe, c.SpeId));
    }

    // ───── Mão de obra própria ─────

    [Fact]
    public void Custo_mensal_e_replica()
    {
        var c = new CustoMaoObraMes { Competencia = new DateOnly(2026, 9, 1), Pessoas = 3, Salario = 2500, EncargosPerc = 80, Beneficios = 600, Outros = 300, HorasMes = 220 };
        Assert.Equal(3 * (2500 * 1.8m + 600) + 300, c.Total);
        Assert.Equal(Math.Round(c.Total / 660, 4), c.CustoHora);
        var n = c.CopiarPara(new DateOnly(2026, 10, 15));
        Assert.Equal(new DateOnly(2026, 10, 1), n.Competencia);
        Assert.Equal(c.Total, n.Total);
        Assert.NotEqual(c.Id, n.Id);
    }

    [Fact]
    public void Fechamento_rateia_custo_da_equipe_pelas_horas_apropriadas()
    {
        var eq = Guid.NewGuid();
        var comp = Guid.NewGuid();
        var (l1, l2) = (Guid.NewGuid(), Guid.NewGuid());
        var custos = new[] { new CustoMaoObraMes { EquipeId = eq, Pessoas = 2, Salario = 3000, EncargosPerc = 100, HorasMes = 200 } }; // 12.000
        var aps = new[]
        {
            new Apropriacao { EquipeId = eq, ComposicaoId = comp, LocalId = l1, Horas = 100 },
            new Apropriacao { EquipeId = eq, ComposicaoId = comp, LocalId = l2, Horas = 50 },
        };
        var (eqs, cels, sem) = FechamentoMaoObra.Calcular(custos, aps);
        Assert.Equal(80m, eqs.Single().CustoHoraReal);
        Assert.Equal(8000m, cels.Single(c => c.LocalId == l1).Custo);
        Assert.Equal(4000m, cels.Single(c => c.LocalId == l2).Custo);
        Assert.Equal(0, sem);
        var (eq2, _, sem2) = FechamentoMaoObra.Calcular([.. custos, new CustoMaoObraMes { Descricao = "Servente avulso", Salario = 2000, HorasMes = 220 }], aps);
        Assert.Equal(2000, sem2);
        Assert.Contains(eq2, e => e.EquipeId is null && e.CustoMes == 2000);
        var r = FechamentoMaoObra.Ratear(10, [(l1, 30), (l2, 10)]);
        Assert.Equal([7.5m, 2.5m], r.Select(x => x.Horas).ToList());
        Assert.Equal(10, FechamentoMaoObra.Ratear(10, [(l1, 1), (l2, 1), (Guid.NewGuid(), 1)]).Sum(x => x.Horas));
    }

    // ───── Fornecedores: documentos mensais (dia 20) ─────

    [Fact]
    public void Competencia_mensal_vence_no_dia_20()
    {
        var t = new TipoDocumentoExigido { Nome = "Guia INSS", Frequencia = FrequenciaDocumento.Mensal, DiaLimite = 20 };
        Assert.Equal(new DateOnly(2026, 8, 1), MatrizExigencias.CompetenciaExigida(t, new DateOnly(2026, 10, 20)));
        Assert.Equal(new DateOnly(2026, 9, 1), MatrizExigencias.CompetenciaExigida(t, new DateOnly(2026, 10, 21)));
        Assert.Null(MatrizExigencias.CompetenciaExigida(new TipoDocumentoExigido(), new DateOnly(2026, 10, 21)));
    }

    [Fact]
    public void Mensal_pendente_trava_medicao_e_documento_de_funcionario_so_com_opcao()
    {
        var f = new Fornecedor { RazaoSocial = "Prestadora Exemplo", TipoFornecimento = TipoFornecimento.ServicoObra };
        var guia = new TipoDocumentoExigido { Nome = "Guia INSS", Frequencia = FrequenciaDocumento.Mensal, DiaLimite = 20, ExigidoPara = TipoFornecimento.ServicoObra, Obrigatorio = true, AplicaA = AplicaA.Fornecedor };
        var aso = new TipoDocumentoExigido { Nome = "ASO", DeFuncionario = true, ExigidoPara = TipoFornecimento.ServicoObra, AplicaA = AplicaA.Fornecedor };
        var hoje = new DateOnly(2026, 10, 25);
        Assert.Single(TravasQualidade.DocumentosMensaisPendentes(f, [guia], hoje));
        f.Documentos.Add(new FornecedorDocumento { TipoDocumentoExigidoId = guia.Id, Competencia = new DateOnly(2026, 9, 1), ArquivoBlob = "x", Status = StatusDocumento.EmValidacao });
        Assert.Empty(TravasQualidade.DocumentosMensaisPendentes(f, [guia], hoje));
        Assert.False(MatrizExigencias.Aplica(aso, f.TipoFornecimento, false));
        Assert.True(MatrizExigencias.Aplica(aso, f.TipoFornecimento, true));
        Assert.False(MatrizExigencias.Aplica(guia, TipoFornecimento.Material, true));
    }

    // ───── Medição por local com FVS do nível acima ─────

    [Fact]
    public void Medicao_por_local_exige_fvs_em_cada_comodo_orcado()
    {
        var (l, _, _, _) = Gerar();
        var porId = l.ToDictionary(x => x.Id);
        var apto = l.First(x => x.Codigo == "QA-T01-P01-101");
        var sala = l.First(x => x.Codigo == "QA-T01-P01-101-SAL");
        var dor = l.First(x => x.Codigo == "QA-T01-P01-101-DOR1");
        var s = new ServicoControlado { Codigo = "ITS 05.01", Nome = "Contrapiso", ExigeFvsParaMedicao = true };
        var f = Fake.Qualificado(s: s);
        var c = new Contrato { Fornecedor = f, FornecedorId = f.Id, Status = StatusContrato.Ativo };
        var ci = new ContratoItem { Item = "1.1", Descricao = "Contrapiso", ServicoControlado = s, ServicoControladoId = s.Id, Quantidade = 22, PrecoUnitario = 12 };
        ci.Locais.Add(new ContratoItemLocal { ContratoItemId = ci.Id, LocalId = apto.Id, Quantidade = 22 });
        c.Itens.Add(ci);
        var m = new Medicao { Contrato = c, NumeroMedicao = 1, NotaEscopo = 10, NotaServico = 10, NotaLimpeza = 10, NotaDocSeg = 10 };
        m.Itens.Add(new MedicaoItem { ContratoItemId = ci.Id, LocalId = apto.Id, QtdPeriodo = 22 });
        m.Recalcular([]); m.CalcularNota(Fake.P);
        IReadOnlyCollection<Guid> Exigidos(MedicaoItem _) => [sala.Id, dor.Id];
        var soSala = new Fvs { ServicoControladoId = s.Id, LocalId = sala.Id, Status = StatusFvs.Liberada };
        Assert.Contains(TravasQualidade.VerificarMedicao(m, [soSala], Fake.Hoje, porId, Exigidos).Bloqueios, b => b.Contains("sem FVS"));
        var noApto = new Fvs { ServicoControladoId = s.Id, LocalId = apto.Id, Status = StatusFvs.Liberada };
        Assert.True(TravasQualidade.VerificarMedicao(m, [noApto], Fake.Hoje, porId, Exigidos).Liberado);
        m.Itens.Single().QtdPeriodo = 30; m.Recalcular([]);
        Assert.Contains(TravasQualidade.VerificarMedicao(m, [noApto], Fake.Hoje, porId, Exigidos).Bloqueios, b => b.Contains("aditivo"));
    }

    // ───── Transferência central → obra ─────

    [Fact]
    public void Transferencia_conferida_com_diferenca_exige_observacao()
    {
        var t = new TransferenciaMaterial { ObraId = Obra, Data = Fake.Hoje };
        var i = new TransferenciaItem { TransferenciaId = t.Id, Descricao = "Cimento CP-II", Quantidade = 100, CustoUnitario = 40 };
        t.Itens.Add(i);
        t.Enviar();
        Assert.Equal(4000, t.Valor);
        Assert.Throws<QualyHub.Domain.Aprovacoes.RegraDeNegocioException>(() => t.Conferir("Almoxarife", DateTimeOffset.Now, new Dictionary<Guid, decimal> { [i.Id] = 95 }));
        t.Observacao = "5 sacos rasgados";
        t.Conferir("Almoxarife", DateTimeOffset.Now, new Dictionary<Guid, decimal> { [i.Id] = 95 });
        Assert.Equal(StatusTransferencia.Recebida, t.Status);
    }

    [Fact]
    public void Obra_propria_gera_sem_spe_e_obra_com_spe_exige()
    {
        var a = Apto(); var t = Torre(a);
        var q = new Quadra { Codigo = "QP", Nome = "Condomínio Próprio" };
        var torres = new List<TorreGerar> { new("Torre 01", t) };
        var aptos = new Dictionary<Guid, Tipologia> { [a.Id] = a };
        Assert.Contains(GeradorEmpreendimento.Validar(q, null, torres, aptos), e => e.Contains("SPE"));
        Assert.Empty(GeradorEmpreendimento.Validar(q, null, torres, aptos, obraPropria: true));
        var l = GeradorEmpreendimento.Gerar(Obra, q, null, null, torres, aptos);
        Assert.NotEmpty(l);
        Assert.All(l, x => Assert.Null(x.SpeId));
    }
}
