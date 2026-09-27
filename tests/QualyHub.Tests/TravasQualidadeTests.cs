using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Regras;
using QualyHub.Domain.Suprimentos;
using QualyHub.Infrastructure.Importacao;

namespace QualyHub.Tests;

/// <summary>Dados 100% fictícios. Nenhum dado de cliente real.</summary>
internal static class Fake
{
    public static readonly DateOnly Hoje = new(2026, 9, 26);
    public static readonly ParametrosVigentes P = ParametrosVigentes.Padrao;

    public static MaterialControlado Aco() => new() { Codigo = "01", Nome = "Aços e armaduras", EspecificacaoModelo = "CA-50, certificado do lote", DocumentosExigidos = "Certificado do lote" };
    public static ServicoControlado Armadura() => new() { Codigo = "ITS 03.02", Nome = "Montagem de armadura", ExigeFvsParaMedicao = true };

    public static Fornecedor Fornecedor(string nome = "Fornecedor Exemplo Ltda") => new() { RazaoSocial = nome, CpfCnpj = "11.222.333/0001-81", Emails = "compras@exemplo.com" };

    public static Fornecedor Qualificado(MaterialControlado? m = null, ServicoControlado? s = null)
    {
        var f = Fornecedor();
        var q = new FornecedorQualificacao
        {
            Fornecedor = f, FornecedorId = f.Id, TipoFornecimento = m is not null ? TipoFornecimento.Material : TipoFornecimento.ServicoObra,
            MaterialControladoId = m?.Id, ServicoControladoId = s?.Id, CriterioQualificacao = "Análise de documentos",
            DataQualificacao = Hoje.AddDays(-10), Resultado = ResultadoQualificacao.Qualificado, ResponsavelNome = "Comprador"
        };
        q.CalcularValidade(P);
        f.Qualificacoes.Add(q);
        return f;
    }
}

public class TravasQualidadeTests
{
    [Fact]
    public void Qualificacao_de_material_vale_3_meses_e_laboratorio_nao_acreditado_12()
    {
        var q = new FornecedorQualificacao { TipoFornecimento = TipoFornecimento.Material, DataQualificacao = Fake.Hoje };
        q.CalcularValidade(Fake.P);
        Assert.Equal(Fake.Hoje.AddMonths(3), q.DataValidade);

        var lab = new FornecedorQualificacao { TipoFornecimento = TipoFornecimento.Laboratorio, LaboratorioEnquadramento = EnquadramentoLaboratorio.C_Avaliado, DataQualificacao = Fake.Hoje };
        lab.CalcularValidade(Fake.P);
        Assert.Equal(Fake.Hoje.AddMonths(12), lab.DataValidade);
    }

    [Fact]
    public void Qualificacao_PSQ_nao_conforme_nao_pode_ser_qualificada_e_lab_nao_acreditado_exige_avaliador()
    {
        var q = new FornecedorQualificacao
        {
            FornecedorId = Guid.NewGuid(), TipoFornecimento = TipoFornecimento.Material, MaterialControladoId = Guid.NewGuid(),
            CriterioQualificacao = "PSQ", Psq = SituacaoPsq.NaoConforme, DataConsultaPsq = Fake.Hoje, Resultado = ResultadoQualificacao.Qualificado, ResponsavelNome = "X"
        };
        Assert.Contains(q.Validar(), e => e.Contains("PSQ"));

        var lab = new FornecedorQualificacao
        {
            FornecedorId = Guid.NewGuid(), TipoFornecimento = TipoFornecimento.Laboratorio, CriterioQualificacao = "Laboratório",
            LaboratorioEnquadramento = EnquadramentoLaboratorio.D_InstituicaoPesquisa, ResponsavelNome = "X"
        };
        Assert.Contains(lab.Validar(), e => e.Contains("avaliador"));
    }

    [Fact]
    public void Status_do_fornecedor_vem_da_ultima_qualificacao()
    {
        var aco = Fake.Aco();
        var f = Fake.Qualificado(aco);
        Assert.Equal(StatusQualificacao.Qualificado, f.CalcularStatus(Fake.Hoje));
        Assert.Equal(StatusQualificacao.Vencida, f.CalcularStatus(Fake.Hoje.AddMonths(4)));
        Assert.Equal(StatusQualificacao.EmQualificacao, Fake.Fornecedor().CalcularStatus(Fake.Hoje));
    }

    [Fact]
    public void Pedido_de_material_controlado_com_fornecedor_sem_qualificacao_vigente_bloqueia()
    {
        var aco = Fake.Aco();
        var pedido = new Pedido { Itens = [new PedidoItem { Descricao = "Vergalhão CA-50 10mm", MaterialControladoId = aco.Id, EspecificacaoTecnica = "CA-50" }] };

        Assert.False(TravasQualidade.VerificarPedido(pedido, Fake.Fornecedor(), Fake.Hoje).Liberado);
        Assert.True(TravasQualidade.VerificarPedido(pedido, Fake.Qualificado(aco), Fake.Hoje).Liberado);
        Assert.False(TravasQualidade.VerificarPedido(pedido, Fake.Qualificado(aco), Fake.Hoje.AddMonths(4)).Liberado); // qualificação vencida
    }

    [Fact]
    public void Documento_bloqueante_vencido_bloqueia_mas_documento_por_entrega_nao()
    {
        var f = Fake.Qualificado(Fake.Aco());
        var cnd = new TipoDocumentoExigido { Nome = "CND / regularidade fiscal", BloqueiaQuandoVencido = true };
        var lote = new TipoDocumentoExigido { Nome = "Certificado do lote (aço)", PorEntrega = true };
        f.Documentos.Add(new FornecedorDocumento { TipoDocumento = lote, TipoDocumentoExigidoId = lote.Id, ArquivoBlob = "x.pdf", Bloqueia = true });
        Assert.True(TravasQualidade.VerificarFornecedor(f, Fake.Hoje).Liberado);

        f.Documentos.Add(new FornecedorDocumento { TipoDocumento = cnd, TipoDocumentoExigidoId = cnd.Id, ArquivoBlob = "cnd.pdf", DataValidade = Fake.Hoje.AddDays(-1), Bloqueia = true });
        Assert.Contains(TravasQualidade.VerificarFornecedor(f, Fake.Hoje).Bloqueios, b => b.Contains("CND"));
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
