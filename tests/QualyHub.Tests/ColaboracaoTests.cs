using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Colaboracao;
using QualyHub.Domain.Common;
using QualyHub.Domain.Suprimentos;
using Xunit;

namespace QualyHub.Tests;

public class ColaboracaoTests
{
    private static readonly DateOnly Hoje = new(2026, 9, 27);

    private static Tarefa NovaTarefa() => new() { Titulo = "Tratar RNC-0001", ResponsavelId = Guid.NewGuid(), Prazo = Hoje.AddDays(5), CriadoEm = DateTimeOffset.UtcNow };

    [Fact]
    public void Nota_exige_texto_e_registro_salvo()
    {
        Assert.Throws<RegraDeNegocioException>(() => Nota.Criar("Rnc", Guid.NewGuid(), "   "));
        Assert.Throws<RegraDeNegocioException>(() => Nota.Criar("Rnc", Guid.Empty, "ok"));
        Assert.Equal("Fornecedor ligou", Nota.Criar("Rnc", Guid.NewGuid(), "  Fornecedor ligou ").Texto);
    }

    [Fact]
    public void Tarefa_nova_valida_campos_e_prazo_no_passado()
    {
        var t = new Tarefa { Prazo = Hoje.AddDays(-1) };
        var e = t.Validar(Hoje);
        Assert.Contains(e, x => x.Contains("título"));
        Assert.Contains(e, x => x.Contains("responsável"));
        Assert.Contains(e, x => x.Contains("passado"));
    }

    [Fact]
    public void Prorrogacao_exige_justificativa_e_data_posterior_e_guarda_original()
    {
        var t = NovaTarefa(); t.PrepararNova();
        Assert.Throws<RegraDeNegocioException>(() => t.Prorrogar(Hoje.AddDays(10), "curta", "Ana"));
        Assert.Throws<RegraDeNegocioException>(() => t.Prorrogar(Hoje.AddDays(3), "fornecedor atrasou a entrega", "Ana"));
        var p = t.Prorrogar(Hoje.AddDays(12), "fornecedor atrasou a entrega", "Ana");
        Assert.Equal(7, p.Dias);
        Assert.Equal(Hoje.AddDays(12), t.Prazo);
        Assert.Equal(Hoje.AddDays(5), t.PrazoOriginal);
    }

    [Fact]
    public void Leitura_registrada_uma_vez_e_conclusao_exige_resolucao()
    {
        var t = NovaTarefa();
        var u = Guid.NewGuid();
        Assert.NotNull(t.RegistrarLeitura(u, "Bruno", DateTimeOffset.UtcNow));
        Assert.Null(t.RegistrarLeitura(u, "Bruno", DateTimeOffset.UtcNow));
        Assert.Throws<RegraDeNegocioException>(() => t.Concluir(" ", "Bruno", DateTimeOffset.UtcNow));
        t.Concluir("Material trocado", "Bruno", DateTimeOffset.UtcNow);
        Assert.True(t.Encerrada);
        Assert.False(t.Atrasada(Hoje.AddDays(30)));
        Assert.Throws<RegraDeNegocioException>(() => t.Prorrogar(Hoje.AddDays(40), "tarde demais para isso", "Bruno"));
    }

    [Fact]
    public void Tres_economias_da_cotacao()
    {
        var itens = new[]
        {
            new CotacaoItem { Quantidade = 10, ValorOfertado = 100, ValorNegociado = 90, PrecoUltimo = 95, PrecoReferencia = 110 },
            new CotacaoItem { Quantidade = 5, ValorOfertado = 20, PrecoUltimo = 18 },
        };
        var e = EconomiaCotacao.Calcular(itens);
        Assert.Equal(100m, e.Negociada);                 // (100-90)*10
        Assert.Equal(40m, e.VsUltimo);                   // (95-90)*10 + (18-20)*5 = 50 - 10
        Assert.Equal(200m, e.VsReferencia);              // (110-90)*10, só itens com referência
        Assert.Equal(1000m, e.Total);                    // 900 + 100
        Assert.Equal(9.1m, e.NegociadaPerc);             // 100 / 1100
        Assert.Null(EconomiaCotacao.Calcular([new CotacaoItem { Quantidade = 1, ValorOfertado = 5 }]).VsUltimo);
    }
}
