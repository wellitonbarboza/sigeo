using QualyHub.Domain.Documentos;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Tests;

/// <summary>Validações das gravações de SGQ movidas das telas para os serviços. Dados fictícios.</summary>
public class QualidadeSgqServicoTests
{
    [Fact]
    public void Pasta_exige_nome()
    {
        Assert.Single(DocumentosServico.ValidarPasta(new Pasta { Nome = "  " }));
        Assert.Empty(DocumentosServico.ValidarPasta(new Pasta { Nome = "Suprimentos" }));
    }

    [Fact]
    public void Externo_exige_identificacao_titulo_edicao_e_emissor()
    {
        Assert.Equal(4, DocumentosServico.ValidarExterno(new DocumentoExterno()).Count);
        var ok = new DocumentoExterno { Identificacao = "ABNT NBR 0000-1", Titulo = "Norma fictícia", Edicao = "2024", Emissor = "ABNT" };
        Assert.Empty(DocumentosServico.ValidarExterno(ok));
    }

    [Fact]
    public void Regra_de_retencao_exige_todos_os_campos_de_controle()
    {
        var r = new RegraRetencao { Identificacao = "Registro fictício", Armazenamento = "Pasta", Recuperacao = "Por data", Protecao = "Armário", TempoRetencao = "5 anos" };
        Assert.Single(DocumentosServico.ValidarRegraRetencao(r));
        r.Descarte = "Fragmentação";
        Assert.Empty(DocumentosServico.ValidarRegraRetencao(r));
    }

    [Fact]
    public void Plano_de_acao_exige_fornecedor_problema_acao_e_responsavel()
    {
        Assert.Equal(4, PlanosAcaoServico.Validar(new PlanoAcaoFornecedor()).Count);
        var p = new PlanoAcaoFornecedor { FornecedorId = Guid.NewGuid(), Problema = "Atraso", Acao = "Replanejar entrega", Responsavel = "Comprador" };
        Assert.Empty(PlanosAcaoServico.Validar(p));
    }
}
