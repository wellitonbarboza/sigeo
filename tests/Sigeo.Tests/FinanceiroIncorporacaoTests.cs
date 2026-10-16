using Sigeo.Application.Servicos;
using Sigeo.Domain.Financeiro;
using Sigeo.Domain.Importacao;
using Sigeo.Domain.Incorporacao;
using Xunit;

namespace Sigeo.Tests;

/// <summary>Regras puras do Financeiro e da Incorporação: condição de pagamento, título, correção, condição de venda, de-para de contas.</summary>
public class FinanceiroIncorporacaoTests
{
    [Fact]
    public void Condicao_gera_parcelas_com_entrada_e_fecha_o_centavo()
    {
        var c = new CondicaoPagamento { Nome = "1+3", EntradaPerc = 25, Prazos = "30;60;90" };
        var p = c.Gerar(1000m, new DateOnly(2026, 10, 1));
        Assert.Equal(4, p.Count);
        Assert.Equal(250m, p[0].Valor);
        Assert.Equal(new DateOnly(2026, 10, 1), p[0].Vencimento);
        Assert.Equal(new DateOnly(2026, 12, 30), p[^1].Vencimento);
        Assert.Equal(1000m, p.Sum(x => x.Valor));
        var tres = new CondicaoPagamento { Nome = "30/60/90", Prazos = "30/60/90" }.Gerar(100m, new DateOnly(2026, 1, 1));
        Assert.Equal(100m, tres.Sum(x => x.Valor));
        Assert.Equal(33.34m, tres[^1].Valor);
        Assert.Contains(new CondicaoPagamento { Nome = "x", Prazos = "" }.Validar(), e => e.Contains("prazos"));
    }

    [Fact]
    public void Titulo_valida_parcelas_e_calcula_situacao()
    {
        var t = new Titulo { Tipo = TipoTitulo.Pagar, Descricao = "Energia", Emissao = new DateOnly(2026, 10, 1), Pessoa = "Concessionária", ValorBruto = 1000, RetencaoIss = 50 };
        t.Parcelas.Add(new TituloParcela { Numero = 1, Vencimento = new DateOnly(2026, 10, 10), Valor = 500 });
        Assert.Contains(t.Validar(), e => e.Contains("Soma das parcelas"));
        t.Parcelas.Add(new TituloParcela { Numero = 2, Vencimento = new DateOnly(2026, 11, 10), Valor = 450 });
        Assert.Empty(t.Validar());
        Assert.Equal(StatusTitulo.Aberto, t.Status);
        t.Parcelas.First().Status = StatusParcelaTitulo.Paga;
        Assert.Equal(StatusTitulo.Parcial, t.Status);
        foreach (var p in t.Parcelas) p.Status = StatusParcelaTitulo.Paga;
        Assert.Equal(StatusTitulo.Liquidado, t.Status);
        var parcela = new TituloParcela { Vencimento = new DateOnly(2026, 10, 10), Valor = 100, Correcao = 5, Juros = 2, Multa = 1, Desconto = 3 };
        Assert.Equal(105m, parcela.TotalBaixa);
        Assert.Equal(5, parcela.DiasAtraso(new DateOnly(2026, 10, 15)));
        Assert.Equal("31–60 dias", RegrasFinanceiro.Faixa(45));
    }

    [Fact]
    public void Conta_bancaria_mostra_numero_mascarado()
    {
        var c = new ContaBancaria { Nome = "Banco Exemplo", Banco = "Exemplo", Numero = "12345-6" };
        Assert.Equal("•••456", c.NumeroMascarado);
        Assert.DoesNotContain("12345", c.Rotulo);
        var (nome, num) = SugestoesImportacao.SepararContaBancaria("BANCO EXEMPLO - 98765-4");
        Assert.Equal("BANCO EXEMPLO", nome);
        Assert.Equal("98765-4", num);
        Assert.Equal("CAIXA ECONOMICA", SugestoesImportacao.SepararContaBancaria("CAIXA ECONOMICA 1234-5 - 000111222-3").Nome);
    }

    [Theory]
    [InlineData("2.1.3 - Custo com materiais aplicados em OBRAS", "2.3")]
    [InlineData("2.1.4 - Custo com serviços aplicados em OBRAS", "2.4")]
    [InlineData("2.1.1 - Custo com MO aplicada em OBRAS", "2.1")]
    [InlineData("2.2.1.21 - Frete", "2.6")]
    [InlineData("2.2.1.22 - Energia", "3.6")]
    [InlineData("2.2.6.9 - INSS", "6.2")]
    [InlineData("4 - EQUIPAMENTO SEGURANÇA", "3.4")]
    [InlineData("2.2.1.52 - PROJETOS", "3.1")]
    [InlineData("2.2.1.15 - Pagamento de Salários/Diárias", "3.2")]
    [InlineData("1.1.1 - Recebimento de Clientes", "1.1")]
    [InlineData("9.9 - Algo desconhecido", null)]
    public void Sugere_conta_do_plano_padrao(string origem, string? esperado)
    {
        var codigos = Sigeo.Application.Carga.CargaFinanceiro.PlanoContas.Select(c => c.Codigo).ToList();
        Assert.Equal(esperado, SugestoesImportacao.SugerirContaGerencial(origem, codigos));
    }

    [Fact]
    public void Plano_padrao_sem_contas_pessoais_e_hierarquia_completa()
    {
        var plano = Sigeo.Application.Carga.CargaFinanceiro.PlanoContas;
        var codigos = plano.Select(c => c.Codigo).ToHashSet();
        Assert.All(plano, c => Assert.True(ContaGerencial.CodigoPai(c.Codigo) is null || codigos.Contains(ContaGerencial.CodigoPai(c.Codigo)!)));
        Assert.DoesNotContain(plano, c => c.Nome.Contains("pessoal", StringComparison.OrdinalIgnoreCase) && !c.Nome.Contains("administrativo"));
        var ordenados = new[] { "2.10", "2.9", "10", "2" }.OrderBy(x => x, ComparadorCodigo.Instancia).ToArray();
        Assert.Equal(["2", "2.9", "2.10", "10"], ordenados);
    }

    [Fact]
    public void Correcao_usa_incc_ate_chaves_e_ipca_mais_juros_depois()
    {
        var idx = new Dictionary<(string, DateOnly), decimal>
        {
            [("INCC", new DateOnly(2026, 1, 1))] = 1m, [("INCC", new DateOnly(2026, 2, 1))] = 1m,
            [("IPCA", new DateOnly(2026, 3, 1))] = 0.5m,
        };
        // Base jan/26, vencimento abr/26 → corrige jan, fev (INCC) e mar (IPCA, já após as chaves em mar/26) + 1% de juros em mar.
        var r = MotorCorrecao.Corrigir(1000m, new DateOnly(2026, 1, 10), new DateOnly(2026, 4, 10), new DateOnly(2026, 3, 1), "INCC", "IPCA", 1m, idx);
        Assert.Equal(Math.Round(1000m * 1.01m * 1.01m * 1.005m, 2), r.ValorCorrigido - r.Juros);
        Assert.Equal(Math.Round(Math.Round(1000m * 1.01m * 1.01m * 1.005m, 2) * 1.01m, 2), r.ValorCorrigido);
        Assert.Empty(r.MesesSemIndice);
        var semIndice = MotorCorrecao.Corrigir(1000m, new DateOnly(2026, 1, 10), new DateOnly(2026, 6, 10), null, "INCC", "IPCA", 0, idx);
        Assert.Equal([new DateOnly(2026, 3, 1), new DateOnly(2026, 4, 1), new DateOnly(2026, 5, 1)], semIndice.MesesSemIndice);
        Assert.Equal(1000m, MotorCorrecao.Corrigir(1000m, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 20), null, "INCC", "IPCA", 0, idx).ValorCorrigido); // mesmo mês: sem correção
    }

    [Fact]
    public void Contrato_de_venda_soma_blocos_e_exige_permuta_com_fornecedor()
    {
        var c = new ContratoVenda { UnidadeComercialId = Guid.NewGuid(), CompradorId = Guid.NewGuid(), DataContrato = new DateOnly(2026, 10, 1), ValorVenda = 300_000 };
        c.Blocos.Add(new BlocoCondicaoVenda { Tipo = TipoParcelaVenda.Entrada, Quantidade = 1, ValorParcela = 30_000, PrimeiroVencimento = new DateOnly(2026, 10, 1) });
        c.Blocos.Add(new BlocoCondicaoVenda { Tipo = TipoParcelaVenda.Mensal, Quantidade = 24, ValorParcela = 2_000, PrimeiroVencimento = new DateOnly(2026, 11, 1) });
        var baloes = new BlocoCondicaoVenda { Tipo = TipoParcelaVenda.Intermediaria, Quantidade = 2, ValorParcela = 15_000, PrimeiroVencimento = new DateOnly(2027, 6, 1), IntervaloMeses = 12 };
        c.Blocos.Add(baloes);
        c.Blocos.Add(new BlocoCondicaoVenda { Tipo = TipoParcelaVenda.Financiamento, Quantidade = 1, ValorParcela = 192_000, PrimeiroVencimento = new DateOnly(2028, 10, 1) });
        Assert.Equal([new DateOnly(2027, 6, 1), new DateOnly(2028, 6, 1)], baloes.Parcelas().Select(p => p.Vencimento).ToArray());
        Assert.Empty(c.Validar());
        c.Blocos.Add(new BlocoCondicaoVenda { Tipo = TipoParcelaVenda.Permuta, Quantidade = 1, ValorParcela = 10_000, PrimeiroVencimento = new DateOnly(2026, 12, 1) });
        var e = c.Validar();
        Assert.Contains(e, x => x.Contains("Permuta"));
        Assert.Contains(e, x => x.Contains("diferente do valor"));
        c.Comissoes.Add(new ComissaoVenda { FornecedorId = Guid.NewGuid(), Valor = 1000, Forma = FormaComissao.AposPercentualPago });
        Assert.Contains(c.Validar(), x => x.Contains("após % pago"));
    }

    [Fact]
    public void Modulos_adicionais_ficam_fora_dos_perfis_operacionais()
    {
        var campo = Sigeo.Contratos.PerfisPadrao.Regras(PerfilUsuarioTeste.Campo);
        Assert.DoesNotContain(campo.Keys, k => k.StartsWith("financeiro/") || k.StartsWith("incorporacao/"));
        var fin = Sigeo.Contratos.PerfisPadrao.Regras(PerfilUsuarioTeste.Financeiro);
        Assert.Equal("VIEAX", fin["financeiro/pagar"]);
        Assert.Equal("VIX", fin["incorporacao/carteira"]);
        Assert.Contains("financeiro/pagar", Sigeo.Contratos.CatalogoTelas.RotasAdicionais);
        Assert.Contains("incorporacao/ret", Sigeo.Contratos.CatalogoTelas.RotasAdicionais);
    }
}

internal static class PerfilUsuarioTeste
{
    public const Sigeo.Domain.Common.PerfilUsuario Campo = Sigeo.Domain.Common.PerfilUsuario.Campo;
    public const Sigeo.Domain.Common.PerfilUsuario Financeiro = Sigeo.Domain.Common.PerfilUsuario.Financeiro;
}
