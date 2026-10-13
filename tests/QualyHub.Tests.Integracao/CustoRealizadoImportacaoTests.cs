using ClosedXML.Excel;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Importacao;
using QualyHub.Domain.Suprimentos;
using QualyHub.Infrastructure.Importacao;

namespace QualyHub.Tests.Integracao;

/// <summary>Custo realizado gerado pelo recebimento e importação do histórico da obra a partir de relatórios do ERP (dados fictícios).</summary>
[Collection(ColecaoBanco.Nome)]
public sealed class CustoRealizadoImportacaoTests(BancoFixture f)
{
    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Today);
    private RecebimentosServico Recebimentos => new(f.Fabrica, f.Usuario, f.Relogio);
    private CustoRealizadoServico Custos => new(f.Fabrica);
    private ImportadorHistoricoErp Importador => new(f.Fabrica, f.Relogio);

    [FatoComBanco]
    public async Task Recebimento_gera_custo_realizado_ao_preco_do_pedido_sem_duplicar()
    {
        var obra = await Dados.CriarObraAsync(f);
        var forn = await Dados.CriarFornecedorAsync(f);
        var prod = await Dados.CriarProdutoAsync(f, "Cimento ficticio CP II", 30m);
        var pedido = new Pedido { ObraId = obra, Numero = Random.Shared.Next(100_000, 900_000), FornecedorId = forn, Entrega = Hoje, Status = StatusPedido.EnviadoFornecedor };
        var pi = new PedidoItem { PedidoId = pedido.Id, ProdutoId = prod.Id, Descricao = prod.Nome, Unidade = "SC", Quantidade = 100, ValorUnitario = 34 };
        pedido.Itens.Add(pi);
        await using (var db = f.Fabrica.Criar()) { db.Pedidos.Add(pedido); await db.SaveChangesAsync(); }

        var r = new Recebimento
        {
            PedidoId = pedido.Id, NumeroNotaFiscal = "2001", DataRecebimento = DateTimeOffset.Now,
            FvmPrazo = ResultadoCriterio.Aprovado, FvmQualidade = ResultadoCriterio.Aprovado, FvmAtendimento = ResultadoCriterio.Aprovado,
            Itens = [new RecebimentoItem { PedidoItemId = pi.Id, Descricao = pi.Descricao, Unidade = "SC", QtdPedida = 60, QtdRecebida = 60 }]
        };
        await Recebimentos.RegistrarRecebimentoAsync(r);

        var lista = await Custos.ListarAsync(obra, null, null);
        var c = Assert.Single(lista);
        Assert.Equal(2040m, c.Valor); // 60 × 34
        Assert.Equal(OrigemCusto.Recebimento, c.Origem);
        Assert.Equal(CustoRealizado.FaseAApropriar, c.Fase); // pedido sem requisição do orçamento

        // Reprocessar não duplica; lançamento de recebimento só reclassifica (valor travado) e não é excluído.
        await using (var db = f.Fabrica.Criar())
        {
            var rec = await db.Recebimentos.Include(x => x.Itens).FirstAsync(x => x.Id == r.Id);
            await CustoRealizadoServico.GerarDoRecebimentoAsync(db, rec);
            await db.SaveChangesAsync();
        }
        Assert.Single(await Custos.ListarAsync(obra, null, null));
        var ent = (await Custos.ObterAsync(c.Id))!;
        ent.Fase = "Estrutura / pré-moldados"; ent.Valor = 1;
        await Custos.SalvarAsync(ent);
        var depois = Assert.Single(await Custos.ListarAsync(obra, null, null));
        Assert.Equal("Estrutura / pré-moldados", depois.Fase);
        Assert.Equal(2040m, depois.Valor);
        Assert.NotNull(QualyHub.Application.Comum.RegrasExclusao.Validar(ent));
    }

    [FatoComBanco]
    public async Task Importa_historico_do_erp_com_de_para_corte_e_sem_duplicar()
    {
        var obra = await Dados.CriarObraAsync(f);
        Guid pav1, terreo;
        await using (var db = f.Fabrica.Criar())
        {
            var torre = new Local { ObraId = obra, Tipo = "Torre", Nome = "Torre Única", Codigo = "T1", Ordem = 1 };
            var t = new Local { ObraId = obra, Tipo = "Pavimento", Nome = "Térreo", Codigo = "T1-P0", Ordem = 1, LocalPaiId = torre.Id };
            var p1 = new Local { ObraId = obra, Tipo = "Pavimento", Nome = "1º Pavimento", Codigo = "T1-P1", Ordem = 2, LocalPaiId = torre.Id };
            db.Locais.AddRange(torre, t, p1);
            await db.SaveChangesAsync();
            pav1 = p1.Id; terreo = t.Id;
        }
        var cnpj = Dados.Cnpj();
        var sufixo = Dados.Sufixo();
        var arquivos = new List<ArquivoRelatorioErp>
        {
            new("apropriacoes.xlsx", Apropriacoes(sufixo)),
            new("medicao-oc.xlsx", MedicaoOc(sufixo)),
            new("fornecedores.xlsx", Fornecedores(sufixo, cnpj)),
            new("insumos.xlsx", Insumos(sufixo)),
        };

        // Sem data de corte: simula, mas não confirma.
        var lote = await Importador.EnviarAsync(obra, arquivos);
        var sim = await Importador.SimularAsync(lote);
        Assert.Contains(sim.Avisos, a => a.Contains("data de corte"));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Importador.ConfirmarAsync(lote));

        await Custos.DefinirCorteAsync(obra, new DateOnly(2026, 10, 15));
        sim = await Importador.SimularAsync(lote);
        Assert.Equal(4, sim.LinhasCusto);
        Assert.Equal(1, sim.LinhasAposCorte); // linha de 20/10 fica de fora
        Assert.Equal(2, sim.Pedidos);
        Assert.Equal("Infraestrutura / fundações", sim.Fases.Single(x => x.Texto.StartsWith("Fundações")).DestinoTexto);
        Assert.Equal(pav1, sim.Locais.Single(x => x.Texto == "1° PAVIMENTO").DestinoId);
        Assert.Equal(terreo, sim.Locais.Single(x => x.Texto == "TÉRREO").DestinoId);
        Assert.Null(sim.Locais.Single(x => x.Texto.StartsWith("Despesas")).DestinoId); // custo da obra toda
        var vergalhao = sim.Produtos.Single(p => p.Texto.StartsWith("Vergalhão"));
        Assert.Equal(AcaoDePara.Criar, vergalhao.Acao);
        Assert.Contains("KG", vergalhao.Info);
        Assert.Contains(sim.Produtos, p => p.Texto == "Locação de mini escavadeira"); // nome colado pelo ERP limpo

        // Fase vazia ("-") é apropriada pela tela; vale para as próximas cargas.
        var semFase = sim.Fases.Single(x => x.DestinoTexto == CustoRealizado.FaseAApropriar);
        await Importador.SalvarVinculoAsync(obra, TipoVinculoOrigem.Fase, semFase.Chave, semFase.Texto, AcaoDePara.Vincular, null, "Canteiro e instalações provisórias");

        var res = await Importador.ConfirmarAsync(lote);
        Assert.Equal(4, res.Custos);
        Assert.Equal(2, res.Pedidos);
        Assert.Equal(2, res.FornecedoresCriados); // com CNPJ do relatório e sem CNPJ (em qualificação)

        var custos = await Custos.ListarAsync(obra, null, null);
        Assert.Equal(4, custos.Count);
        Assert.All(custos, c => Assert.True(c.Historico));
        Assert.Equal(1000m + 3000m + 250m + 120m, custos.Sum(c => c.Valor));
        Assert.Contains(custos, c => c.Fase == "Canteiro e instalações provisórias" && c.LocalId == null);
        Assert.Contains(custos, c => c.LocalId == pav1 && c.Pedido == $"OC 9{sufixo[..2]}" && c.Unidade == "KG");
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var peds = await db.Pedidos.Include(p => p.Itens).Where(p => p.ObraId == obra).ToListAsync();
            Assert.Equal(2, peds.Count);
            Assert.Contains(peds, p => p.Status == StatusPedido.Entregue);
            Assert.Contains(peds, p => p.Status == StatusPedido.EntregueParcial); // OC com saldo fica viva para receber no sistema
            var forn = await db.Fornecedores.SingleAsync(x => x.RazaoSocial == $"Aço Ficticio {sufixo} Ltda");
            Assert.Equal(Conversor.FormatarCnpj(cnpj), forn.CpfCnpj);
            var verg = await db.Produtos.SingleAsync(p => p.Nome == $"Vergalhão CA-50 10mm {sufixo}");
            Assert.Equal("KG", verg.Unidade);
            Assert.Equal(TipoCusto.Material, verg.TipoCusto);
        }

        // Reenvio dos mesmos relatórios: nada duplica.
        var lote2 = await Importador.EnviarAsync(obra, arquivos);
        var sim2 = await Importador.SimularAsync(lote2);
        Assert.Equal(4, sim2.CustosJaImportados);
        Assert.Equal(2, sim2.PedidosJaImportados);
        Assert.All(sim2.Produtos.Where(p => p.Texto.Contains(sufixo)), p => Assert.True(p.Salvo));
        var res2 = await Importador.ConfirmarAsync(lote2);
        Assert.Equal(0, res2.Custos);
        Assert.Equal(0, res2.Pedidos);
        Assert.Equal(4, (await Custos.ListarAsync(obra, null, null)).Count);
    }

    // ───── Relatórios fictícios no formato do ERP (título, filtros e cabeçalho fora da linha 1) ─────

    private static byte[] Planilha(string titulo, string[] cabecalho, IEnumerable<object?[]> linhas)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Relatorio");
        ws.Cell(1, 1).Value = titulo;
        ws.Cell(3, 1).Value = "Filtros: obra ficticia";
        for (var c = 0; c < cabecalho.Length; c++) ws.Cell(6, c + 1).Value = cabecalho[c];
        var r = 7;
        foreach (var l in linhas)
        {
            for (var c = 0; c < l.Length; c++)
                switch (l[c])
                {
                    case null: break;
                    case DateTime d: ws.Cell(r, c + 1).Value = d; break;
                    case decimal m: ws.Cell(r, c + 1).Value = (double)m; break;
                    case int i: ws.Cell(r, c + 1).Value = i; break;
                    default: ws.Cell(r, c + 1).Value = l[c]!.ToString(); break;
                }
            r++;
        }
        ws.Cell(r, 11).Value = "Total geral"; // linha de total do relatório (sem insumo): ignorada
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static byte[] Apropriacoes(string s) => Planilha("Relatório de apropriações de insumos",
        ["Entrada", "O.C.", "Doc.", "Número", "Empresa", "Obra/Centro custo", "Fornecedor", "Tipo custo", "Categoria", "Insumo", "Qtd. total", "Total (R$)", "Fase", "Serviço", "Qtd.", "%"],
        [
            [new DateTime(2026, 3, 10), $"9{s[..2]}", "D1", "101", "Ficticia", "Obra", $"Aço Ficticio {s} Ltda", "Materiais", "Aço", $"Vergalhão CA-50 10mm {s}", 300m, 1000m, "Supraestrutura - Alvenaria Estrutural, Lajes e Escadas", "1° PAVIMENTO", 100m, 33.3m],
            [new DateTime(2026, 3, 10), $"9{s[..2]}", "D1", "101", "Ficticia", "Obra", $"Aço Ficticio {s} Ltda", "Materiais", "Aço", $"Vergalhão CA-50 10mm {s}", 300m, 3000m, "Fundações - Estacas, Blocos, Vigas", "TÉRREO", 200m, 66.7m],
            [new DateTime(2026, 4, 2), null, "D2", "55", "Ficticia", "Obra", $"Locadora Ficticia {s}", "Equipamentos", "Locação", $"1° PAVIMENTO [Instalações] - Locação de mini escavadeira", 1m, 250m, "-", "Despesas Administrativas", 1m, 100m],
            [new DateTime(2026, 5, 5), null, "D3", "77", "Ficticia", "Obra", $"Locadora Ficticia {s}", "Outros", "Escritório", $"# Caneta ficticia {s}", 10m, 120m, "Despesas Administrativas (Combustível/Escritório)", "Despesas Administrativas", 10m, 100m],
            [new DateTime(2026, 10, 20), null, "D4", "88", "Ficticia", "Obra", $"Locadora Ficticia {s}", "Outros", "Escritório", $"# Caneta ficticia {s}", 1m, 12m, "Despesas Administrativas (Combustível/Escritório)", "Despesas Administrativas", 1m, 100m],
        ]);

    private static byte[] MedicaoOc(string s) => Planilha("Relatório de medição de ordens de compra",
        ["Fornecedor", "Ordem compra", "Criação", "Previsão entrega", "Vlr. total (R$)", "Condição pgto.", "Obra", "Item", "Qtd.", "Vlr. unitário (R$)", "Total (R$)", "Total receb.", "Recebido", "Saldo a receber"],
        [
            [$"Aço Ficticio {s} Ltda", $"009{s[..2]}", new DateTime(2026, 3, 1), new DateTime(2026, 3, 8), 4000m, "30/60", "Obra", $"Vergalhão CA-50 10mm {s}", 300m, 13.3333m, 4000m, 300m, 100m, 0m],
            [$"Locadora Ficticia {s}", $"008{s[..2]}", new DateTime(2026, 4, 1), new DateTime(2026, 4, 3), 500m, "28 dias", "Obra", $"Caneta ficticia {s}", 20m, 25m, 500m, 10m, 50m, 10m],
        ]);

    private static byte[] Fornecedores(string s, string cnpj) => Planilha("Fornecedores",
        ["Razão social", "Nome fantasia", "CPF/CNPJ", "Cidade/UF", "Bairro", "Telefone", "E-mail"],
        [[$"Aço Ficticio {s} Ltda", "Aço Ficticio", cnpj, "Cidade Teste/PR", "Centro", "(00) 0000-0000", "contato@exemplo.com"]]);

    private static byte[] Insumos(string s) => Planilha("Insumos cadastrados",
        ["Código", "Nome", "Categoria", "Tipo de custo", "Unidade", "Vlr. Unitário (R$)"],
        [["", $"Vergalhão CA-50 10mm {s}", "Aço", "Materiais", "kg", 0m], ["", $"Caneta ficticia {s}", "Escritório", "Outros", "und", 0m]]);
}
