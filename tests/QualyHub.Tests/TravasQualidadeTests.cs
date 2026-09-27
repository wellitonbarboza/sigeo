using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Medicoes;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Regras;
using QualyHub.Domain.Suprimentos;
using QualyHub.Infrastructure.Importacao;

namespace QualyHub.Tests;

public class TravasQualidadeTests
{
    private static readonly DateOnly Hoje = new(2026, 9, 25);

    private static ServicoControlado Alvenaria() => new() { Codigo = "SC-07", Nome = "Alvenaria de vedação", ExigeFvsParaMedicao = true };

    [Fact]
    public void Medicao_sem_FVS_aprovada_fica_bloqueada()
    {
        var servico = Alvenaria();
        var medicao = new Medicao { Itens = [new MedicaoItem { CodigoItem = "03.02", ServicoControlado = servico, ServicoControladoId = servico.Id }] };
        var fvsRascunho = new Ficha { Tipo = TipoFicha.FVS, ServicoControladoId = servico.Id, Status = StatusFicha.Rascunho };

        var r = TravasQualidade.VerificarMedicao(medicao, [fvsRascunho], new Fornecedor(), Hoje);

        Assert.False(r.Liberado);
        Assert.Contains("sem FVS aprovada", r.Resumo);
    }

    [Fact]
    public void Medicao_com_FVS_aprovada_no_mesmo_local_libera()
    {
        var servico = Alvenaria();
        var local = Guid.CreateVersion7();
        var medicao = new Medicao { Itens = [new MedicaoItem { CodigoItem = "03.02", ServicoControlado = servico, ServicoControladoId = servico.Id, LocalId = local }] };
        var fvs = new Ficha { Tipo = TipoFicha.FVS, ServicoControladoId = servico.Id, LocalId = local, Status = StatusFicha.Aprovada };

        Assert.True(TravasQualidade.VerificarMedicao(medicao, [fvs], new Fornecedor(), Hoje).Liberado);
    }

    [Fact]
    public void Pedido_de_material_controlado_com_fornecedor_nao_qualificado_bloqueia()
    {
        var material = new MaterialControlado { Nome = "Bloco cerâmico", ExigeFornecedorQualificado = true };
        var pedido = new Pedido { Itens = [new PedidoItem { MaterialControlado = material }] };
        var fornecedor = new Fornecedor { RazaoSocial = "Cerâmica X", Situacao = SituacaoFornecedor.EmQualificacao };

        Assert.False(TravasQualidade.VerificarPedido(pedido, fornecedor, Hoje).Liberado);
    }

    [Fact]
    public void Documento_vencido_de_fornecedor_bloqueia_pedido()
    {
        var tipo = new TipoDocumentoExigido { Nome = "CND Federal", BloqueiaQuandoVencido = true };
        var fornecedor = new Fornecedor
        {
            Situacao = SituacaoFornecedor.Qualificado,
            Documentos = [new FornecedorDocumento { TipoDocumento = tipo, ArquivoBlob = "x.pdf", DataValidade = Hoje.AddDays(-1) }]
        };

        var r = TravasQualidade.VerificarPedido(new Pedido(), fornecedor, Hoje);
        Assert.Contains("CND Federal", r.Resumo);
    }

    [Fact]
    public void Aprovacao_percorre_alcadas_e_exige_justificativa_na_reprovacao()
    {
        Alcada[] alcadas =
        [
            new() { Entidade = EntidadeAprovavel.Pedido, Ordem = 1, PerfilAprovador = PerfilUsuario.Engenharia },
            new() { Entidade = EntidadeAprovavel.Pedido, Ordem = 2, PerfilAprovador = PerfilUsuario.Diretoria, ValorMinimo = 5000 }
        ];
        var s = SolicitacaoAprovacao.Criar(EntidadeAprovavel.Pedido, Guid.NewGuid(), Guid.NewGuid(), null, "PC 123", 8000m, Guid.NewGuid(), "Comprador", alcadas, DateTimeOffset.UtcNow);
        Assert.Equal(2, s.Etapas.Count);

        s.Decidir(new Aprovador(Guid.NewGuid(), "Eng.", PerfilUsuario.Engenharia), DecisaoAprovacao.Aprovado, null, DateTimeOffset.UtcNow);
        Assert.Equal(StatusAprovacao.EmAprovacao, s.Status);

        Assert.Throws<RegraDeNegocioException>(() =>
            s.Decidir(new Aprovador(Guid.NewGuid(), "Dir.", PerfilUsuario.Diretoria), DecisaoAprovacao.Reprovado, null, DateTimeOffset.UtcNow));

        s.Decidir(new Aprovador(Guid.NewGuid(), "Dir.", PerfilUsuario.Diretoria), DecisaoAprovacao.Aprovado, null, DateTimeOffset.UtcNow);
        Assert.Equal(StatusAprovacao.Aprovado, s.Status);
    }

    [Theory]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1,234", 1.234)]
    [InlineData("12.345", 12345)]
    [InlineData("R$ 980,00", 980)]
    public void Conversor_entende_numeros_brasileiros(string entrada, double esperado) =>
        Assert.Equal((decimal)esperado, Conversor.Decimal(entrada));

    [Fact]
    public void Cnpj_e_cpf_sao_validados() 
    {
        Assert.True(Conversor.CnpjValido("11222333000181"));
        Assert.False(Conversor.CnpjValido("11222333000182"));
        Assert.True(Conversor.CpfValido("52998224725"));
        Assert.Equal("***.982.247-**", ProtecaoCpf.Mascarar("529.982.247-25"));
    }
}
