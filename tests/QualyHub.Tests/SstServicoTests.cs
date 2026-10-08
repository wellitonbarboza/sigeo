using QualyHub.Domain.Sst;
using Xunit;

namespace QualyHub.Tests;

/// <summary>Validações puras dos cadastros de SST (sem banco), com dados fictícios.</summary>
public class SstServicoTests
{
    [Fact]
    public void Exigencia_sem_fonte_e_recusada()
    {
        Assert.NotEmpty(SstServico.ValidarExigencia(new Exigencia { Descricao = "NR-35 trabalho em altura", Fonte = "" }));
        Assert.NotEmpty(SstServico.ValidarExigencia(new Exigencia { Descricao = " ", Fonte = "PGR da obra" }));
        Assert.Empty(SstServico.ValidarExigencia(new Exigencia { Descricao = "NR-35 trabalho em altura", Fonte = "NR-35 · PGR da obra" }));
    }

    [Fact]
    public void Funcao_exige_descricao()
    {
        Assert.NotEmpty(SstServico.ValidarFuncao(new FuncaoObra { Descricao = "" }));
        Assert.Empty(SstServico.ValidarFuncao(new FuncaoObra { Descricao = "Armador" }));
    }

    [Fact]
    public void Treinamento_exige_descricao_e_carga()
    {
        Assert.NotEmpty(SstServico.ValidarTreinamento(new Treinamento { Descricao = "Integração", CargaHoraria = "" }));
        Assert.Empty(SstServico.ValidarTreinamento(new Treinamento { Descricao = "Integração", CargaHoraria = "4h" }));
    }

    [Fact]
    public void Epi_exige_descricao_e_ca()
    {
        Assert.NotEmpty(SstServico.ValidarEpi(new Epi { Descricao = "Capacete", Ca = "" }));
        Assert.Empty(SstServico.ValidarEpi(new Epi { Descricao = "Capacete", Ca = "00000" }));
    }
}
