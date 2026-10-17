using Sigeo.Domain.Common;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Qualidade;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;
using Xunit;

namespace Sigeo.Tests;

/// <summary>Regras corrigidas na revisão de código (rateio, medição por local, contrato, documentos mensais, transferência).</summary>
public class RevisaoTests
{
    [Theory]
    [InlineData(0.07, 10)]
    [InlineData(100, 3)]
    [InlineData(8, 7)]
    [InlineData(1234.56, 250)]
    public void Rateio_fecha_o_total_sem_parcela_negativa(decimal total, int partes)
    {
        var pesos = Enumerable.Repeat(1m, partes).ToList();
        var r = Rateio.Proporcional(total, pesos);
        Assert.Equal(Math.Round(total, 2), r.Sum());
        Assert.All(r, x => Assert.True(x >= 0));
    }

    [Fact]
    public void Rateio_proporcional_aos_pesos_e_iguais_sem_peso()
    {
        Assert.Equal([2.5m, 7.5m], Rateio.Proporcional(10, [1, 3]));
        Assert.Equal([3.34m, 3.33m, 3.33m], Rateio.Proporcional(10, [0, 0, 0]));
        var h = FechamentoMaoObra.Ratear(10, [(Guid.NewGuid(), 1), (Guid.NewGuid(), 3)]);
        Assert.Equal(10, h.Sum(x => x.Horas));
    }

    [Fact]
    public void Fechamento_custo_das_celulas_fecha_com_o_custo_do_mes()
    {
        var eq = Guid.NewGuid();
        var custos = new[] { new CustoMaoObraMes { EquipeId = eq, Pessoas = 3, Salario = 2000, EncargosPerc = 70, Beneficios = 600 } };
        var comp = Guid.NewGuid();
        var aps = Enumerable.Range(0, 7).Select(_ => new Apropriacao { EquipeId = eq, ComposicaoId = comp, LocalId = Guid.NewGuid(), Horas = 13 }).ToList();
        var (equipes, celulas, sem) = FechamentoMaoObra.Calcular(custos, aps);
        Assert.Equal(custos[0].Total, celulas.Sum(c => c.Custo));
        Assert.Equal(0, sem);
        Assert.Single(equipes);
    }

    private static (Contrato C, ContratoItem Ci, Guid L1, Guid L2) ContratoPorLocal()
    {
        var f = Fake.Qualificado();
        var c = new Contrato { Fornecedor = f, FornecedorId = f.Id, Status = StatusContrato.Ativo };
        var ci = new ContratoItem { Item = "1.1", Descricao = "Contrapiso", Quantidade = 20, PrecoUnitario = 10 };
        Guid l1 = Guid.NewGuid(), l2 = Guid.NewGuid();
        ci.Locais.Add(new ContratoItemLocal { ContratoItemId = ci.Id, LocalId = l1, Quantidade = 10 });
        ci.Locais.Add(new ContratoItemLocal { ContratoItemId = ci.Id, LocalId = l2, Quantidade = 10 });
        c.Itens.Add(ci);
        return (c, ci, l1, l2);
    }

    [Fact]
    public void Medicao_linhas_repetidas_do_mesmo_local_somam_no_acumulado()
    {
        var (c, ci, l1, _) = ContratoPorLocal();
        var m = new Medicao { Contrato = c, NumeroMedicao = 1, NotaEscopo = 10, NotaServico = 10, NotaLimpeza = 10, NotaDocSeg = 10 };
        m.Itens.Add(new MedicaoItem { ContratoItemId = ci.Id, LocalId = l1, QtdPeriodo = 8 });
        m.Itens.Add(new MedicaoItem { ContratoItemId = ci.Id, LocalId = l1, QtdPeriodo = 8 });
        m.Recalcular([]); m.CalcularNota(Fake.P);
        Assert.Equal(16, m.Itens.Last().QtdAcumulada);
        Assert.Contains(TravasQualidade.VerificarMedicao(m, [], Fake.Hoje).Bloqueios, b => b.Contains("aditivo"));
    }

    [Fact]
    public void Medicao_em_local_fora_do_contrato_bloqueia()
    {
        var (c, ci, _, _) = ContratoPorLocal();
        var m = new Medicao { Contrato = c, NumeroMedicao = 1, NotaEscopo = 10, NotaServico = 10, NotaLimpeza = 10, NotaDocSeg = 10 };
        m.Itens.Add(new MedicaoItem { ContratoItemId = ci.Id, LocalId = Guid.NewGuid(), QtdPeriodo = 1 });
        m.Recalcular([]); m.CalcularNota(Fake.P);
        Assert.Contains(TravasQualidade.VerificarMedicao(m, [], Fake.Hoje).Bloqueios, b => b.Contains("não faz parte do contrato"));
    }

    [Fact]
    public void Contrato_valida_quebra_por_local()
    {
        var (c, ci, l1, _) = ContratoPorLocal();
        c.Categoria = "Serviço"; c.Objeto = "Contrapiso"; c.VigenciaInicio = Fake.Hoje; c.VigenciaFim = Fake.Hoje.AddMonths(2);
        Assert.DoesNotContain(c.Validar(), e => e.Contains("local"));
        ci.Locais.Add(new ContratoItemLocal { ContratoItemId = ci.Id, LocalId = l1, Quantidade = 1 });
        var e = c.Validar();
        Assert.Contains(e, x => x.Contains("local repetido"));
        Assert.Contains(e, x => x.Contains("soma dos locais"));
    }

    [Fact]
    public void Documento_mensal_nao_entra_na_trava_de_validade()
    {
        var f = Fake.Qualificado();
        var tipo = new TipoDocumentoExigidoFake().Mensal();
        f.Documentos.Add(new Sigeo.Domain.Cadastros.FornecedorDocumento
        {
            FornecedorId = f.Id, TipoDocumento = tipo, TipoDocumentoExigidoId = tipo.Id, Bloqueia = true, Status = StatusDocumento.Reprovado,
            Competencia = new DateOnly(Fake.Hoje.Year, Fake.Hoje.Month, 1).AddMonths(-3), ArquivoBlob = "x"
        });
        Assert.Empty(TravasQualidade.DocumentosBloqueantes(f, Fake.Hoje));
    }

    [Fact]
    public void Transferencia_nao_aceita_quantidade_negativa()
    {
        var t = new TransferenciaMaterial { ObraId = Guid.NewGuid(), Data = Fake.Hoje };
        var i = new TransferenciaItem { TransferenciaId = t.Id, Descricao = "Areia", Quantidade = 10, CustoUnitario = 1 };
        t.Itens.Add(i);
        t.Enviar();
        t.Observacao = "x";
        Assert.Throws<Sigeo.Domain.Aprovacoes.RegraDeNegocioException>(() => t.Conferir("A", DateTimeOffset.Now, new Dictionary<Guid, decimal> { [i.Id] = -1 }));
    }

    private sealed class TipoDocumentoExigidoFake
    {
        public Sigeo.Domain.Cadastros.TipoDocumentoExigido Mensal() => new()
        {
            Nome = "Guia FGTS", AplicaA = AplicaA.Fornecedor, Frequencia = FrequenciaDocumento.Mensal, BloqueiaQuandoVencido = true, Ativo = true
        };
    }
}
