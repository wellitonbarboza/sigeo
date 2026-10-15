using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Cadastros;
using Groundkey.Domain.Planejamento;
using Groundkey.Domain.Qualidade;
using Groundkey.Domain.Suprimentos;

namespace Groundkey.Tests.Integracao;

/// <summary>
/// Estoque físico e gerencial no banco real: recebimento entra no almoxarifado onde foi recebido, transferência baixa na origem e
/// entra no destino conferido, inventário de implantação, kit por apartamento (baixa no apto/serviço e consumo × orçado),
/// equipamentos/OS, contrato recorrente e cautela/aferição das ferramentas de medição.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public sealed class EstoqueTests(BancoFixture f)
{
    private EstoqueServico Estoque => new(f.Fabrica, f.Usuario, f.Relogio);
    private TransferenciasServico Transferencias => new(f.Fabrica, f.Usuario, f.Relogio);
    private RecebimentosServico Recebimentos => new(f.Fabrica, f.Usuario, f.Relogio);
    private EquipamentosServico Equipamentos => new(f.Fabrica, f.Usuario, f.Relogio);
    private ContratosRecorrentesServico Recorrentes => new(f.Fabrica, f.Usuario, f.Relogio);
    private QualidadeObraServico Qob => new(f.Fabrica, f.Usuario, f.Relogio);
    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Today);

    private async Task<SaldoEstoque?> SaldoAsync(Guid almox, Guid produto)
    {
        await using var db = f.Fabrica.CriarLeitura();
        return await db.SaldosEstoque.AsNoTracking().FirstOrDefaultAsync(s => s.AlmoxarifadoId == almox && s.ProdutoId == produto);
    }

    private async Task<Guid> AlmoxDaObraAsync(Guid obra)
    {
        await using var db = f.Fabrica.Criar();
        var a = await MotorEstoque.DaObraAsync(db, obra);
        await db.SaveChangesAsync();
        return a.Id;
    }

    [FatoComBanco]
    public async Task Recebimento_entra_no_almoxarifado_da_obra_e_transferencia_vai_para_outra_obra()
    {
        var obraA = await Dados.CriarObraAsync(f);
        var obraB = await Dados.CriarObraAsync(f);
        var forn = await Dados.CriarFornecedorAsync(f);
        var prod = await Dados.CriarProdutoAsync(f, "Cimento ficticio", 30m);
        var pedido = new Pedido { ObraId = obraA, Numero = Random.Shared.Next(100_000, 900_000), FornecedorId = forn, Entrega = Hoje, Status = StatusPedido.EnviadoFornecedor };
        var pi = new PedidoItem { PedidoId = pedido.Id, ProdutoId = prod.Id, Descricao = prod.Nome, Unidade = "SC", Quantidade = 100, ValorUnitario = 34 };
        pedido.Itens.Add(pi);
        await using (var db = f.Fabrica.Criar()) { db.Pedidos.Add(pedido); await db.SaveChangesAsync(); }

        var r = new Recebimento
        {
            PedidoId = pedido.Id, NumeroNotaFiscal = "1001", DataRecebimento = DateTimeOffset.Now,
            FvmPrazo = ResultadoCriterio.Aprovado, FvmQualidade = ResultadoCriterio.Aprovado, FvmAtendimento = ResultadoCriterio.Aprovado,
            Itens = [new RecebimentoItem { PedidoItemId = pi.Id, Descricao = pi.Descricao, Unidade = "SC", QtdPedida = 100, QtdRecebida = 100 }]
        };
        await Recebimentos.RegistrarRecebimentoAsync(r);
        var almA = await AlmoxDaObraAsync(obraA);
        var almB = await AlmoxDaObraAsync(obraB);
        var sA = await SaldoAsync(almA, prod.Id);
        Assert.NotNull(sA);
        Assert.Equal(100, sA.Disponivel);
        Assert.Equal(34, sA.CustoMedio);

        // Saída maior que o saldo é recusada.
        await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            Estoque.LancarAsync(new(almA, prod.Id, 150, TipoMovimentoEstoque.Saida, null, null, null, null, "Equipe própria", null)));

        // Obra B pede 40 à obra A, com restrição no Last Planner até a conferência.
        var tId = await Estoque.SolicitarTransferenciaAsync(almA, almB, [new(prod.Id, 40)], "Reposição");
        await Estoque.CriarRestricaoAsync(obraB, tId, "Alvenaria pav. 2", "Cimento vindo da obra A", Hoje.AddDays(5));
        await Transferencias.EnviarTransferenciaAsync(tId);
        Assert.Equal(60, (await SaldoAsync(almA, prod.Id))!.Disponivel);
        Assert.Null(await SaldoAsync(almB, prod.Id)); // em trânsito: ainda não entrou

        Guid itemId;
        await using (var db = f.Fabrica.CriarLeitura()) itemId = (await db.TransferenciaItens.SingleAsync(i => i.TransferenciaId == tId)).Id;
        await Transferencias.ConferirTransferenciaAsync(tId, new Dictionary<Guid, decimal> { [itemId] = 38 }, "2 sacos rasgados no transporte");
        var sB = await SaldoAsync(almB, prod.Id);
        Assert.Equal(38, sB!.Disponivel);
        Assert.Equal(34, sB.CustoMedio);
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var rest = await db.Restricoes.SingleAsync(x => x.OrigemId == tId);
            Assert.Equal(StatusRestricao.Removida, rest.Status);
            Assert.Equal(3, await db.MovimentosEstoque.CountAsync(m => m.ProdutoId == prod.Id)); // entrada, saída (envio), entrada (conferência)
        }
    }

    [FatoComBanco]
    public async Task Inventario_de_implantacao_e_kit_por_apartamento_baixa_no_apto_e_compara_com_o_orcado()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var (rev, _, prod, _) = await Dados.MontarOrcamentoAsync(f, e);
        await using (var db = f.Fabrica.Criar())
        {
            var c = await db.Composicoes.SingleAsync(x => x.Id == rev.Id);
            c.Grupo = "Revestimentos"; c.Disciplina = "Revestimento";
            await db.SaveChangesAsync();
        }
        var alm = await AlmoxDaObraAsync(e.ObraId);

        // Implantação: 500 kg a R$ 2,10.
        var inv = await Estoque.AbrirInventarioAsync(alm, TipoInventario.Implantacao, null);
        await Estoque.SalvarContagemAsync(inv, [new(prod.Id, 500, 2.10m, null)], true);
        await Estoque.AprovarInventarioAsync(inv);
        var s = await SaldoAsync(alm, prod.Id);
        Assert.Equal(500, s!.Disponivel);
        Assert.Equal(2.10m, s.CustoMedio);

        var kitId = await Estoque.SalvarKitAsync(new KitMaterial { Nome = "Revestimento de piso · apto", Origem = OrigemKit.Automatico, ObraId = e.ObraId, Etapa = "Revestimentos", Disciplina = "Revestimento" });
        // Apartamento AP2D: sala de 20 m² × 5 kg/m² = 100 kg.
        Guid apto;
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var sala = await db.Locais.FirstAsync(l => l.ObraId == e.ObraId && l.AmbienteModeloId == e.SalaAp2d);
            apto = sala.LocalPaiId!.Value;
        }
        var nec = await Estoque.NecessidadeAsync(kitId, [apto]);
        var linha = Assert.Single(nec);
        Assert.Equal(100, linha.Quantidade);
        Assert.Equal(rev.Id, linha.ComposicaoId);

        await Estoque.EntregarKitAsync(kitId, alm, apto, "Empreiteira ficticia · encarregado", null, null);
        Assert.Equal(400, (await SaldoAsync(alm, prod.Id))!.Disponivel);
        // Sobra devolvida: 10 kg.
        await Estoque.LancarAsync(new(alm, prod.Id, 10, TipoMovimentoEstoque.Devolucao, apto, rev.Id, null, null, null, "sobra"));
        var consumo = Assert.Single(await Estoque.ConsumoAsync(kitId, [apto]));
        Assert.Equal(200m, consumo.Orcado);   // 100 kg × R$ 2,00 (custo da composição)
        Assert.Equal(189m, consumo.Real);     // 90 kg × R$ 2,10
        Assert.Equal(-5.5m, consumo.DiferencaPerc);
    }

    [FatoComBanco]
    public async Task Equipamento_parado_bloqueia_gera_restricao_e_verificacao_libera()
    {
        var obra = await Dados.CriarObraAsync(f);
        var alm = await AlmoxDaObraAsync(obra);
        Guid familia;
        await using (var db = f.Fabrica.CriarLeitura()) familia = (await db.FamiliasEquipamento.FirstAsync(x => x.Ativa)).Id;
        var eq = new Equipamento { Patrimonio = "PAT-" + Random.Shared.Next(10000, 99999), Nome = "Betoneira ficticia", FamiliaId = familia, Criticidade = Criticidade.A, AlmoxarifadoId = alm, Situacao = SituacaoEquipamento.EmUso };
        await Equipamentos.SalvarAsync(eq);
        var osId = await Equipamentos.AbrirCorretivaAsync(eq.Id, "Não gira o tambor", parado: true);
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var sit = (await EquipamentosServico.SituacoesAsync(db, Hoje)).Single(x => x.Equipamento.Id == eq.Id);
            Assert.Contains(sit.Bloqueios, b => b.Contains("corretiva"));
            Assert.Equal(StatusRestricao.Aberta, (await db.Restricoes.SingleAsync(r => r.OrigemId == osId)).Status);
        }
        await Equipamentos.AvancarOsAsync(osId, StatusOrdemServico.EmExecucao);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Equipamentos.AvancarOsAsync(osId, StatusOrdemServico.Concluida)); // sem causa
        OrdemServico os;
        await using (var db = f.Fabrica.CriarLeitura()) os = await db.OrdensServico.AsNoTracking().SingleAsync(x => x.Id == osId);
        os.Causa = "Rolamento travado"; os.ModoFalha = "Desgaste"; os.CustoPecas = 240; os.ExecutadoPor = "Mecânico ficticio";
        await Equipamentos.SalvarOsAsync(os);
        await Equipamentos.AvancarOsAsync(osId, StatusOrdemServico.Concluida);
        await Equipamentos.AvancarOsAsync(osId, StatusOrdemServico.Verificada);
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var sit = (await EquipamentosServico.SituacoesAsync(db, Hoje)).Single(x => x.Equipamento.Id == eq.Id);
            Assert.Empty(sit.Bloqueios);
            Assert.Equal(StatusRestricao.Removida, (await db.Restricoes.SingleAsync(r => r.OrigemId == osId)).Status);
        }
    }

    [FatoComBanco]
    public async Task Contrato_por_diaria_gera_parcelas_e_nota_fora_da_tolerancia_exige_justificativa()
    {
        var obra = await Dados.CriarObraAsync(f);
        var forn = await Dados.CriarFornecedorAsync(f);
        var ini = new DateOnly(2026, 7, 1);
        var c = new Contrato
        {
            ObraId = obra, Numero = "CT-T" + Random.Shared.Next(1000, 9999), FornecedorId = forn, Categoria = "Locação", Objeto = "Locação de betoneira", RequisitosEquipamento = "Revisada",
            VigenciaInicio = ini, VigenciaFim = new DateOnly(2026, 9, 30), Status = StatusContrato.Ativo,
            Recorrente = true, FormaCobranca = FormaCobranca.Diaria, Periodicidade = Periodicidade.Mensal, DiaCorte = 31, DiaVencimento = 10, ToleranciaPerc = 2
        };
        var item = new ContratoItem { ContratoId = c.Id, Item = "1", Descricao = "Betoneira 400 L", Unidade = "dia", Quantidade = 1, PrecoUnitario = 18, PrecoMensal = 390 };
        c.Itens.Add(item);
        await using (var db = f.Fabrica.Criar()) { db.Contratos.Add(c); await db.SaveChangesAsync(); }
        Guid familia;
        await using (var db = f.Fabrica.CriarLeitura()) familia = (await db.FamiliasEquipamento.FirstAsync(x => x.Ativa)).Id;
        var alm = await AlmoxDaObraAsync(obra);
        var eq = new Equipamento { Patrimonio = "LOC-" + Random.Shared.Next(10000, 99999), Nome = "Betoneira locada", FamiliaId = familia, Propriedade = PropriedadeEquipamento.Locada, ContratoId = c.Id, ContratoItemId = item.Id, FornecedorId = forn };
        await Equipamentos.SalvarAsync(eq);
        await Equipamentos.MovimentarAsync(eq.Id, TipoMovimentoEquipamento.Chegada, alm, new DateOnly(2026, 7, 21), null, null);
        await Equipamentos.MovimentarAsync(eq.Id, TipoMovimentoEquipamento.Devolucao, null, new DateOnly(2026, 8, 31), null, null);

        Assert.Equal(3, await Recorrentes.GerarParcelasAsync(c.Id));
        List<ParcelaContrato> ps;
        await using (var db = f.Fabrica.CriarLeitura()) ps = await db.ParcelasContrato.AsNoTracking().Where(p => p.ContratoId == c.Id).OrderBy(p => p.Numero).ToListAsync();
        Assert.Equal(198m, ps[0].ValorPrevisto);  // julho: 11 diárias × 18 (abaixo do mensal)
        Assert.Equal(390m, ps[1].ValorPrevisto);  // agosto: mensal (31 diárias × 18 = 558 > 390)
        Assert.Equal(0m, ps[2].ValorPrevisto);    // setembro: devolvido
        Assert.Equal(new DateOnly(2026, 8, 10), ps[0].Vencimento);

        await Recorrentes.LancarNotaAsync(ps[1].Id, "4455", 420);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Recorrentes.AprovarAsync(ps[1].Id, null));
        await Recorrentes.AprovarAsync(ps[1].Id, "Cobrou o frete de retirada");
        await Recorrentes.LiberarAsync(ps[1].Id);
        await Recorrentes.MarcarPagaAsync(ps[1].Id, Hoje);
        await using (var db = f.Fabrica.CriarLeitura())
            Assert.Equal(StatusParcela.Paga, (await db.ParcelasContrato.SingleAsync(p => p.Id == ps[1].Id)).Status);
    }

    [FatoComBanco]
    public async Task Ferramenta_de_medicao_tipo_configuravel_codigo_cautela_e_reprovacao_recolhe()
    {
        TipoEquipamentoMedicao trena;
        await using (var db = f.Fabrica.CriarLeitura()) trena = await db.TiposEquipamentoMedicao.AsNoTracking().FirstAsync(t => t.Prefixo == "TR");
        var padrao = new EquipamentoMedicao { Codigo = "PAD-" + Random.Shared.Next(1000, 9999), Descricao = "Trena padrão", TipoControle = TipoControleEquipamento.Padrao, Criterio = "RBC", PeriodicidadeMeses = 12 };
        await Qob.SalvarEquipamentoAsync(padrao);
        await Qob.RegistrarCalibracaoAsync(padrao.Id, new Calibracao { Data = Hoje, Tipo = TipoControleEquipamento.CalibracaoExterna, Executante = "Lab ficticio", Certificado = "C-1", Erro = "0" });

        var forn = await Dados.CriarFornecedorAsync(f);
        var e = new EquipamentoMedicao { TipoMedicaoId = trena.Id, Descricao = "Trena 5 m", TipoControle = TipoControleEquipamento.VerificacaoInterna, PadraoId = padrao.Id, Dono = DonoFerramenta.Terceiro, FornecedorId = forn };
        await Qob.SalvarEquipamentoAsync(e);
        Assert.StartsWith("TR-", e.Codigo);
        Assert.Equal(trena.PeriodicidadeMeses, e.PeriodicidadeMeses);
        Assert.Equal(trena.Criterio, e.Criterio);

        // Sem aferição está bloqueada: não pode ser entregue.
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Qob.EntregarFerramentaAsync(e.Id, "Empreiteira ficticia", null, forn, Hoje, null));
        await Qob.RegistrarCalibracaoAsync(e.Id, new Calibracao { Data = Hoje, Tipo = TipoControleEquipamento.VerificacaoInterna, Executante = "Obra", Erro = "0,5 mm" });
        await Qob.EntregarFerramentaAsync(e.Id, "Empreiteira ficticia", null, forn, Hoje, null);
        await Qob.RegistrarCalibracaoAsync(e.Id, new Calibracao { Data = Hoje, Tipo = TipoControleEquipamento.VerificacaoInterna, Executante = "Obra", Erro = "3 mm", Resultado = ResultadoCalibracao.Reprovado, AvaliacaoImpacto = "Sem FVS no período" });
        await using var db2 = f.Fabrica.CriarLeitura();
        var x = await db2.Equipamentos.AsNoTracking().SingleAsync(q => q.Id == e.Id);
        Assert.Equal("Recolhida (reprovada)", x.ComQuem);
        Assert.False(x.UsavelEm(Hoje));
        Assert.All(await db2.CautelasFerramenta.Where(c => c.EquipamentoMedicaoId == e.Id).ToListAsync(), c => Assert.NotNull(c.DevolvidaEm));
    }
}
