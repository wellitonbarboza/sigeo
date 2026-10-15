using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Financeiro;
using Groundkey.Domain.Incorporacao;
using Groundkey.Domain.Suprimentos;

namespace Groundkey.Tests.Integracao;

/// <summary>Financeiro (títulos, baixa, banco, fluxo, DRE, liberação → título) e Incorporação (venda em blocos, comissões, carteira corrigida, RET, distrato). Dados fictícios.</summary>
[Collection(ColecaoBanco.Nome)]
public sealed class FinanceiroIncorporacaoIntegracaoTests(BancoFixture f)
{
    private FinanceiroServico Fin => new(f.Fabrica, f.Relogio);
    private IncorporacaoServico Inc => new(f.Fabrica, f.Relogio);
    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Today);

    private async Task<Guid> ContaAsync(string codigo)
    {
        await using var db = f.Fabrica.CriarLeitura();
        return await db.ContasGerenciais.Where(c => c.Codigo == codigo).Select(c => c.Id).FirstAsync();
    }

    [FatoComBanco]
    public async Task Titulo_a_pagar_baixa_movimenta_conta_e_entra_no_fluxo_e_dre()
    {
        var obra = await Dados.CriarObraAsync(f);
        var forn = await Dados.CriarFornecedorAsync(f);
        var banco = new ContaBancaria { Nome = $"Banco Ficticio {Dados.Sufixo()}", Numero = "00000-0", SaldoInicial = 10_000, DataSaldoInicial = Hoje.AddDays(-30) };
        await Fin.SalvarContaBancariaAsync(banco);
        var cond = (await Fin.CondicoesAsync()).First(c => c.Nome == "30/60");
        var t = new Titulo
        {
            Tipo = TipoTitulo.Pagar, FornecedorId = forn, ObraId = obra, ContaGerencialId = await ContaAsync("2.3"), Descricao = "Cimento (teste)",
            Emissao = Hoje, ValorBruto = 2_000, RetencaoIss = 100
        };
        t.Parcelas = FinanceiroServico.ParcelasPorCondicao(cond, t.ValorLiquido, Hoje);
        var id = await Fin.SalvarTituloAsync(t);
        var parcelas = await Fin.ParcelasDoTituloAsync(id);
        Assert.Equal(2, parcelas.Count);
        Assert.Equal(1_900m, parcelas.Sum(p => p.Valor));

        await Fin.BaixarAsync(new BaixaParcela(parcelas[0].ParcelaId, Hoje, banco.Id, FormaPagamento.Pix, 0, 5, 0, 0, "comprovante-teste"));
        var saldo = (await Fin.SaldosAsync()).Single(s => s.Id == banco.Id);
        Assert.Equal(10_000m - 955m, saldo.Saldo); // 950 + 5 de juros
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Fin.CancelarTituloAsync(id, "teste"));

        var fluxo = await Fin.FluxoAsync(obra, new DateOnly(Hoje.Year, Hoje.Month, 1), new DateOnly(Hoje.Year, Hoje.Month, 1).AddMonths(3));
        Assert.Equal(955m, fluxo.Sum(x => x.SaidasRealizadas));
        Assert.Equal(950m, fluxo.Sum(x => x.SaidasPrevistas));
        var dre = await Fin.DreAsync(obra, Hoje.AddDays(-1), Hoje.AddDays(1));
        Assert.Equal(955m, dre.Single(l => l.Codigo == "2").Valor);
        Assert.Equal(955m, dre.Single(l => l.Codigo == "2.3").Valor);

        await Fin.EstornarBaixaAsync(parcelas[0].ParcelaId);
        Assert.Equal(10_000m, (await Fin.SaldosAsync()).Single(s => s.Id == banco.Id).Saldo);
        await Fin.TransferirAsync(banco.Id, (await NovaContaAsync()).Id, Hoje, 1_000, null);
        Assert.Equal(9_000m, (await Fin.SaldosAsync()).Single(s => s.Id == banco.Id).Saldo);
    }

    private async Task<ContaBancaria> NovaContaAsync()
    {
        var c = new ContaBancaria { Nome = $"Caixa Ficticio {Dados.Sufixo()}", Tipo = TipoContaBancaria.Caixa, DataSaldoInicial = Hoje };
        await Fin.SalvarContaBancariaAsync(c);
        return c;
    }

    [FatoComBanco]
    public async Task Liberacao_de_pagamento_vira_titulo_e_baixa_marca_pago()
    {
        var obra = await Dados.CriarObraAsync(f);
        var forn = await Dados.CriarFornecedorAsync(f);
        var l = new LiberacaoPagamento
        {
            Numero = Random.Shared.Next(100_000, 900_000), ObraId = obra, Origem = OrigemPagamento.NotaMaterial, ChaveNFe = "chave-teste", FornecedorId = forn,
            ValorNF = 500, ValorLiberado = 500, Vencimento = Hoje.AddDays(10), Status = StatusPagamento.Liberado, LiberadoEm = DateTimeOffset.Now
        };
        await using (var db = f.Fabrica.Criar())
        {
            db.Pagamentos.Add(l);
            await FinanceiroServico.GerarDaLiberacaoAsync(db, l);
            await FinanceiroServico.GerarDaLiberacaoAsync(db, l); // idempotente
            await db.SaveChangesAsync();
        }
        var abertas = (await Fin.ParcelasAsync(TipoTitulo.Pagar, obra, null, null, false)).Where(p => p.Origem == OrigemTitulo.LiberacaoPagamento).ToList();
        var p = Assert.Single(abertas);
        Assert.Equal(500m, p.Valor);
        var caixa = await NovaContaAsync();
        await Fin.BaixarAsync(new BaixaParcela(p.ParcelaId, Hoje, caixa.Id, FormaPagamento.Boleto, 0, 0, 0, 0, null));
        await using (var db = f.Fabrica.CriarLeitura())
            Assert.Equal(StatusPagamento.Pago, (await db.Pagamentos.FirstAsync(x => x.Id == l.Id)).Status);
    }

    [FatoComBanco]
    public async Task Venda_em_blocos_gera_carteira_comissoes_corrige_apura_ret_e_distrata()
    {
        var obra = await Dados.CriarObraAsync(f);
        var corretor = await Dados.CriarFornecedorAsync(f);
        var permutante = await Dados.CriarFornecedorAsync(f);
        Guid apto1, apto2;
        await using (var db = f.Fabrica.Criar())
        {
            var torre = new Domain.Estrutura.Local { ObraId = obra, Tipo = GeradorEmpreendimento.TipoTorre, Nome = "Torre Ficticia", Codigo = "T1", Ordem = 1 };
            var pav = new Domain.Estrutura.Local { ObraId = obra, Tipo = GeradorEmpreendimento.TipoPavimento, Nome = "1º Pavimento", Codigo = "T1-P1", Ordem = 1, LocalPaiId = torre.Id };
            var a1 = new Domain.Estrutura.Local { ObraId = obra, Tipo = GeradorEmpreendimento.TipoApartamento, Nome = "Apto 101", Codigo = "T1-P1-101", Ordem = 1, LocalPaiId = pav.Id };
            var a2 = new Domain.Estrutura.Local { ObraId = obra, Tipo = GeradorEmpreendimento.TipoApartamento, Nome = "Apto 102", Codigo = "T1-P1-102", Ordem = 2, LocalPaiId = pav.Id };
            db.Locais.AddRange(torre, pav, a1, a2);
            await db.SaveChangesAsync();
            apto1 = a1.Id; apto2 = a2.Id;
        }
        await Inc.SalvarAsync(new IncorporacaoObra { ObraId = obra, Nome = "Residencial Ficticio", PatrimonioAfetacao = true, Regime = RegimeTributario.RetGeral, PrevisaoChaves = new DateOnly(2028, 6, 1) });
        Assert.Equal(2, await Inc.GerarUnidadesAsync(obra));
        Assert.Equal(0, await Inc.GerarUnidadesAsync(obra));
        var esp = await Inc.EspelhoAsync(obra);
        var u1 = esp.Single(u => u.LocalId == apto1);
        Assert.Equal("Torre Ficticia", u1.Torre);
        await Inc.SalvarUnidadeAsync(u1.Id, 300_000, SituacaoUnidade.Disponivel, "1 vaga", null);
        var comprador = new Comprador { Nome = "Comprador Ficticio", CpfCnpj = "", Email = "comprador@exemplo.com" };
        await Inc.SalvarCompradorAsync(comprador);

        var base0 = new DateOnly(2026, 1, 10);
        await Inc.SalvarIndiceAsync("INCC", new DateOnly(2026, 1, 1), 1m);
        var c = new ContratoVenda
        {
            ObraId = obra, UnidadeComercialId = u1.Id, CompradorId = comprador.Id, DataContrato = base0, DataBase = base0, ValorVenda = 300_000,
            IndiceAteChaves = "INCC", IndiceAposChaves = "IPCA", DataChaves = new DateOnly(2028, 6, 1),
            Blocos =
            [
                new BlocoCondicaoVenda { Tipo = TipoParcelaVenda.Entrada, Quantidade = 1, ValorParcela = 30_000, PrimeiroVencimento = base0 },
                new BlocoCondicaoVenda { Tipo = TipoParcelaVenda.Mensal, Quantidade = 10, ValorParcela = 3_000, PrimeiroVencimento = base0.AddMonths(1) },
                new BlocoCondicaoVenda { Tipo = TipoParcelaVenda.Intermediaria, Quantidade = 2, ValorParcela = 20_000, PrimeiroVencimento = base0.AddMonths(6), IntervaloMeses = 12 },
                new BlocoCondicaoVenda { Tipo = TipoParcelaVenda.Permuta, Quantidade = 1, ValorParcela = 10_000, PrimeiroVencimento = base0.AddMonths(2), FornecedorPermutaId = permutante },
                new BlocoCondicaoVenda { Tipo = TipoParcelaVenda.Financiamento, Quantidade = 1, ValorParcela = 190_000, PrimeiroVencimento = new DateOnly(2028, 6, 1) },
            ],
            Comissoes =
            [
                new ComissaoVenda { FornecedorId = corretor, Papel = "Corretor", Percentual = 3, Forma = FormaComissao.Parcelada, Parcelas = 3 },
                new ComissaoVenda { FornecedorId = corretor, Papel = "Imobiliária", Valor = 1_000, Forma = FormaComissao.AposPercentualPago, PercentualPagoMinimo = 10 },
            ]
        };
        var cid = await Inc.SalvarContratoAsync(c);
        Assert.Equal(SituacaoUnidade.Vendida, (await Inc.EspelhoAsync(obra)).Single(u => u.Id == u1.Id).Situacao);
        var cart = await Inc.CarteiraAsync(obra);
        Assert.Equal(15, cart.Count);
        Assert.Equal(300_000m, cart.Sum(p => p.ValorBase));
        var comissoes = (await Fin.ParcelasAsync(TipoTitulo.Pagar, obra, null, null, false)).Where(p => p.Origem == OrigemTitulo.Comissao).ToList();
        Assert.Equal(3, comissoes.Count); // parcelada em 3; a "após 10% pago" ainda não existe
        Assert.Equal(9_000m, comissoes.Sum(p => p.Valor));

        // 2ª mensal (vence 10/03/26): corrige jan e fev; fev sem índice conta 0%.
        var mensal2 = cart.Where(p => p.Tipo == TipoParcelaVenda.Mensal).OrderBy(p => p.Vencimento).Skip(1).First();
        var calc = await Inc.CalcularParcelaAsync(mensal2.ParcelaId, mensal2.Vencimento);
        Assert.Equal(30m, calc.Correcao);
        Assert.Contains(new DateOnly(2026, 2, 1), calc.MesesSemIndice);
        var permuta = cart.Single(p => p.Tipo == TipoParcelaVenda.Permuta);
        Assert.Equal(0m, (await Inc.CalcularParcelaAsync(permuta.ParcelaId, Hoje)).Correcao);

        // Recebe a entrada (10%) → libera a comissão condicionada; recebe a permuta (sem conta).
        var conta = await NovaContaAsync();
        var entrada = cart.Single(p => p.Tipo == TipoParcelaVenda.Entrada);
        await Fin.BaixarAsync(new BaixaParcela(entrada.ParcelaId, base0, conta.Id, FormaPagamento.Transferencia, 0, 0, 0, 0, null));
        await Fin.BaixarAsync(new BaixaParcela(permuta.ParcelaId, base0.AddDays(5), null, FormaPagamento.Permuta, 0, 0, 0, 0, null));
        comissoes = (await Fin.ParcelasAsync(TipoTitulo.Pagar, obra, null, null, false)).Where(p => p.Origem == OrigemTitulo.Comissao).ToList();
        Assert.Equal(4, comissoes.Count);
        Assert.Equal(30_000m, (await Fin.SaldosAsync()).Single(s => s.Id == conta.Id).Saldo);

        // RET de jan/26: 4% sobre 40.000 recebidos (entrada + permuta) → DARF em contas a pagar.
        var ret = await Inc.ApurarAsync(obra, new DateOnly(2026, 1, 1));
        Assert.Equal(40_000m, ret.ReceitaRecebida);
        Assert.Equal(1_600m, ret.Total);
        var darf = (await Fin.ParcelasAsync(TipoTitulo.Pagar, obra, null, null, false)).Single(p => p.Origem == OrigemTitulo.ApuracaoRet);
        Assert.Equal(new DateOnly(2026, 2, 20), darf.Vencimento);
        ret = await Inc.ApurarAsync(obra, new DateOnly(2026, 1, 1)); // reapurar substitui o DARF
        Assert.Single((await Fin.ParcelasAsync(TipoTitulo.Pagar, obra, null, null, false)).Where(p => p.Origem == OrigemTitulo.ApuracaoRet));

        // Unidade vendida não vende de novo; distrato devolve ao estoque e gera a devolução.
        var outra = new ContratoVenda
        {
            ObraId = obra, UnidadeComercialId = u1.Id, CompradorId = comprador.Id, DataContrato = Hoje, ValorVenda = 1_000,
            Blocos = [new BlocoCondicaoVenda { Tipo = TipoParcelaVenda.Entrada, Quantidade = 1, ValorParcela = 1_000, PrimeiroVencimento = Hoje }]
        };
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Inc.SalvarContratoAsync(outra));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Inc.DistratarAsync(cid, Hoje, 50_000, "teste"));
        await Inc.DistratarAsync(cid, Hoje, 20_000, "Desistência (teste)");
        Assert.Equal(SituacaoUnidade.Disponivel, (await Inc.EspelhoAsync(obra)).Single(u => u.Id == u1.Id).Situacao);
        Assert.Empty((await Inc.CarteiraAsync(obra)).Where(p => p.Status == StatusParcelaTitulo.Aberta));
        var abertasPagar = await Fin.ParcelasAsync(TipoTitulo.Pagar, obra, null, null, false);
        Assert.Contains(abertasPagar, p => p.Origem == OrigemTitulo.Distrato && p.Valor == 20_000m);
        Assert.DoesNotContain(abertasPagar, p => p.Origem == OrigemTitulo.Comissao); // comissões não pagas canceladas
        Assert.NotEqual(Guid.Empty, apto2);
    }
}
