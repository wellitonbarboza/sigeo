using Sigeo.Domain.Common;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Suprimentos;
using Xunit;

namespace Sigeo.Tests;

/// <summary>Regras puras do estoque, da manutenção e da cobrança recorrente.</summary>
public class EstoqueEquipamentosTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);
    private static MovimentoEstoque Mov(decimal q, decimal custo = 0, bool bloq = false, TipoMovimentoEstoque t = TipoMovimentoEstoque.Entrada) =>
        new() { Quantidade = q, CustoUnitario = custo, Bloqueado = bloq, Tipo = t, Data = Agora };

    [Fact]
    public void Custo_medio_pondera_entradas_e_saida_usa_o_medio()
    {
        var s = new SaldoEstoque();
        RegrasEstoque.Aplicar(s, Mov(100, 10));
        RegrasEstoque.Aplicar(s, Mov(100, 12));
        Assert.Equal(11, s.CustoMedio);
        var saida = Mov(-50, 99, t: TipoMovimentoEstoque.Saida);
        RegrasEstoque.Aplicar(s, saida);
        Assert.Equal(150, s.Disponivel);
        Assert.Equal(11, saida.CustoUnitario);
        Assert.Equal(-550, saida.Valor);
        Assert.Equal(150, saida.SaldoApos);
    }

    [Fact]
    public void Saida_maior_que_saldo_e_recusada_e_bloqueado_nao_sai_como_disponivel()
    {
        var s = new SaldoEstoque();
        RegrasEstoque.Aplicar(s, Mov(40, 5, bloq: true));
        Assert.Equal(0, s.Disponivel);
        Assert.Equal(40, s.Bloqueado);
        Assert.Throws<RegraDeNegocioException>(() => RegrasEstoque.Aplicar(s, Mov(-1, t: TipoMovimentoEstoque.Saida)));
        RegrasEstoque.Aplicar(s, Mov(30, t: TipoMovimentoEstoque.Liberacao));
        Assert.Equal(30, s.Disponivel);
        Assert.Equal(10, s.Bloqueado);
        Assert.Throws<RegraDeNegocioException>(() => RegrasEstoque.Aplicar(s, Mov(20, t: TipoMovimentoEstoque.Liberacao)));
    }

    [Theory]
    [InlineData(StatusLote.Liberado, false)]
    [InlineData(StatusLote.AceitoSobConcessao, false)]
    [InlineData(StatusLote.EmQuarentena, true)]
    [InlineData(StatusLote.EmVerificacao, true)]
    [InlineData(StatusLote.Devolvido, null)]
    public void Lote_define_se_entra_bloqueado(StatusLote lote, bool? esperado) => Assert.Equal(esperado, RegrasEstoque.EntraBloqueado(lote));

    [Fact]
    public void Sugestao_de_minimo_pelo_consumo_e_prazo_de_entrega()
    {
        var (min, pp, max) = RegrasEstoque.Sugerir(consumoPeriodo: 600, diasPeriodo: 60, leadTimeDias: 7);
        Assert.Equal(70, min);
        Assert.Equal(105, pp);
        Assert.Equal(405, max);
        Assert.Equal(355, RegrasEstoque.Reposicao(new SaldoEstoque { Disponivel = 50, PontoPedido = 105, Maximo = 405 }));
    }

    [Fact]
    public void Kit_manual_valida_itens_e_automatico_exige_obra_e_filtro()
    {
        Assert.Contains(new KitMaterial { Nome = "x", Origem = OrigemKit.Automatico }.Validar(), e => e.Contains("obra"));
        Assert.Contains(new KitMaterial { Nome = "x", Origem = OrigemKit.Automatico, ObraId = Guid.NewGuid() }.Validar(), e => e.Contains("etapa"));
        Assert.Contains(new KitMaterial { Nome = "x", Origem = OrigemKit.Manual }.Validar(), e => e.Contains("itens"));
    }

    // ───── Manutenção ─────

    [Fact]
    public void Os_segue_o_fluxo_e_corretiva_exige_causa_e_verificador_diferente()
    {
        var os = new OrdemServico { Tipo = TipoOrdemServico.Corretiva, ParadoDesde = Agora.AddHours(-5) };
        Assert.Throws<RegraDeNegocioException>(() => os.Avancar(StatusOrdemServico.Concluida, "ana", Agora));
        os.Avancar(StatusOrdemServico.EmExecucao, "ana", Agora);
        Assert.Throws<RegraDeNegocioException>(() => os.Avancar(StatusOrdemServico.Concluida, "ana", Agora));
        os.Causa = "Rolamento";
        os.Avancar(StatusOrdemServico.Concluida, "ana", Agora);
        Assert.Equal(5, os.HorasParado(Agora.AddHours(10)), 3);
        Assert.Throws<RegraDeNegocioException>(() => os.Avancar(StatusOrdemServico.Verificada, "ana", Agora));
        os.Avancar(StatusOrdemServico.Verificada, "bruno", Agora);
        Assert.Equal(StatusOrdemServico.Verificada, os.Status);
    }

    [Fact]
    public void Bloqueio_por_documento_preventiva_vencida_e_corretiva_de_critico()
    {
        var hoje = new DateOnly(2026, 10, 12);
        var e = new Equipamento { Criticidade = Criticidade.A, Situacao = SituacaoEquipamento.EmUso, Horimetro = 520 };
        e.Documentos.Add(new EquipamentoDocumento { Nome = "Laudo NR-18", Vencimento = hoje.AddDays(-1) });
        var porTempo = new PlanoManutencao { EquipamentoId = e.Id, Nome = "Mensal", Gatilho = GatilhoManutencao.Tempo, IntervaloDias = 30, UltimaData = hoje.AddDays(-31) };
        var porUso = new PlanoManutencao { EquipamentoId = e.Id, Nome = "500 h", Gatilho = GatilhoManutencao.Uso, IntervaloUso = 500, UltimaLeitura = 0 };
        var os = new OrdemServico { EquipamentoId = e.Id, Tipo = TipoOrdemServico.Corretiva, Numero = 7 };
        var b = RegrasManutencao.Bloqueios(e, [porTempo, porUso], [os], hoje);
        Assert.Equal(4, b.Count);
        Assert.True(porUso.Vencido(hoje, 520));
        Assert.False(new PlanoManutencao { Gatilho = GatilhoManutencao.Uso, IntervaloUso = 500, UltimaLeitura = 0 }.Vencido(hoje, 460));
        Assert.True(new PlanoManutencao { Gatilho = GatilhoManutencao.Uso, IntervaloUso = 500, UltimaLeitura = 0, DiasAntecedencia = 7 }.PertoDeVencer(hoje, 460));
    }

    [Fact]
    public void Disponibilidade_e_mttr()
    {
        var ini = Agora.AddDays(-10); var fim = Agora;
        var os = new OrdemServico { Tipo = TipoOrdemServico.Corretiva, ParadoDesde = Agora.AddDays(-2), ParadoAte = Agora.AddDays(-2).AddHours(12) };
        Assert.Equal(0.95m, RegrasManutencao.Disponibilidade([os], ini, fim, 1));
        var (mttr, mtbf) = RegrasManutencao.Indicadores([os], 240);
        Assert.Equal(12, mttr);
        Assert.Equal(240, mtbf);
    }

    // ───── Contrato recorrente ─────

    private static Contrato Recorrente(Periodicidade p, int corte = 25) => new()
    {
        Recorrente = true, FormaCobranca = FormaCobranca.ValorFixo, Periodicidade = p, DiaCorte = corte, DiaVencimento = 10,
        VigenciaInicio = new DateOnly(2026, 7, 1), VigenciaFim = new DateOnly(2026, 9, 30),
        Itens = [new ContratoItem { Descricao = "Banheiro químico", Unidade = "mês", Quantidade = 3, PrecoUnitario = 420 }]
    };

    [Fact]
    public void Periodos_mensais_com_dia_de_corte_e_vencimento()
    {
        var p = RegrasRecorrencia.Periodos(Recorrente(Periodicidade.Mensal));
        Assert.Equal(4, p.Count);
        Assert.Equal((new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 25)), p[0]);
        Assert.Equal((new DateOnly(2026, 7, 26), new DateOnly(2026, 8, 25)), p[1]);
        Assert.Equal((new DateOnly(2026, 9, 26), new DateOnly(2026, 9, 30)), p[3]);
        Assert.Equal(new DateOnly(2026, 8, 10), RegrasRecorrencia.Vencimento(new DateOnly(2026, 7, 25), 10));
        Assert.Equal(new DateOnly(2026, 7, 28), RegrasRecorrencia.Vencimento(new DateOnly(2026, 7, 25), 28));
        Assert.Equal(1260m, RegrasRecorrencia.Previsto(Recorrente(Periodicidade.Mensal), p[0].Inicio, p[0].Fim, [], []).Sum(l => l.Total));
    }

    [Fact]
    public void Periodos_quinzenais_e_por_datas_combinadas()
    {
        Assert.Equal(7, RegrasRecorrencia.Periodos(Recorrente(Periodicidade.Quinzenal)).Count);
        var c = Recorrente(Periodicidade.DatasCombinadas);
        c.DatasCombinadas = "15/07/2026; 20/08/2026;30/09/2026;10/12/2026";
        var p = RegrasRecorrencia.Periodos(c);
        Assert.Equal(3, p.Count);
        Assert.Equal((new DateOnly(2026, 7, 16), new DateOnly(2026, 8, 20)), p[1]);
    }

    [Fact]
    public void Previsto_por_uso_soma_lancamentos_do_periodo()
    {
        var c = Recorrente(Periodicidade.Mensal);
        c.FormaCobranca = FormaCobranca.Uso;
        var item = c.Itens.First(); item.PrecoUnitario = 290; item.Descricao = "Caçamba 5 m³";
        var usos = new[] { new UsoContrato { ContratoItemId = item.Id, Data = new DateOnly(2026, 7, 3), Quantidade = 4 }, new UsoContrato { ContratoItemId = item.Id, Data = new DateOnly(2026, 7, 28), Quantidade = 2 } };
        Assert.Equal(1160m, RegrasRecorrencia.Previsto(c, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 25), [], usos).Sum(l => l.Total));
    }

    [Fact]
    public void Parcela_fora_da_tolerancia_exige_justificativa_e_manutencao_exige_os()
    {
        var p = new ParcelaContrato { ValorPrevisto = 1000 };
        Assert.Throws<RegraDeNegocioException>(() => p.Aprovar(2, false, "eng", Agora)); // sem nota
        p.LancarNota("55", 1015, "Almoxarife", null, "almox", Agora);
        Assert.False(p.ForaDaTolerancia(2));
        p.Aprovar(2, false, "eng", Agora);
        Assert.Equal(StatusParcela.Aprovada, p.Status);

        var q = new ParcelaContrato { ValorPrevisto = 1000 };
        q.LancarNota("56", 1100, "SIEG", null, "Sistema", Agora);
        Assert.Throws<RegraDeNegocioException>(() => q.Aprovar(2, false, "eng", Agora));
        q.Justificativa = "Reajuste antecipado";
        Assert.Throws<RegraDeNegocioException>(() => q.Aprovar(2, true, "eng", Agora)); // contrato de manutenção sem OS
        q.OrdemServicoId = Guid.NewGuid();
        q.Aprovar(2, true, "eng", Agora);
        q.Liberar("fin", Agora);
        q.MarcarPaga(new DateOnly(2026, 10, 15));
        Assert.Equal(StatusParcela.Paga, q.Status);
    }

    // ───── Marca ─────

    [Fact]
    public void Marca_obrigatoria_no_item_cotado_de_material_e_somente_esta_marca()
    {
        var i = new CotacaoItem { Descricao = "Cimento", ValorOfertado = 30 };
        Assert.Contains("marca", i.ErroMarca(true, null, false));
        Assert.Null(i.ErroMarca(false, null, false));                       // serviço: sem marca
        Assert.Null(new CotacaoItem { Descricao = "x", ValorOfertado = 0 }.ErroMarca(true, null, false)); // não cotado
        i.Marca = "votoran";
        Assert.Contains("somente a marca", i.ErroMarca(true, "Votorantim", true));
        Assert.Null(i.ErroMarca(true, "Votorantim", false));                // aceita similar
        i.Marca = "  VOTORÂNTIM ";
        Assert.Null(i.ErroMarca(true, "Votorantim", true));
        Assert.Equal("TIGRE PVC", Sigeo.Domain.Cadastros.Marca.Chave(" tigre   pvc "));
    }
}
