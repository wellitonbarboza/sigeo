using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Documentos;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Infrastructure.Dados;

// ───────────── Estrutura / apoio ─────────────
internal sealed class CentroCustoCfg : IEntityTypeConfiguration<CentroCusto>
{
    public void Configure(EntityTypeBuilder<CentroCusto> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasOne<CentroCusto>().WithMany().HasForeignKey(x => x.PaiId);
        b.Property(x => x.EstabelecimentosPermitidos).HasMaxLength(2000);
        b.Property(x => x.CodigoErp).HasMaxLength(60);
    }
}

internal sealed class ParametroCfg : IEntityTypeConfiguration<Parametro>
{
    public void Configure(EntityTypeBuilder<Parametro> b)
    {
        b.Property(x => x.Chave).HasMaxLength(60);
        b.HasIndex(x => new { x.EmpresaId, x.Chave }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class ItemListaCfg : IEntityTypeConfiguration<ItemLista>
{
    public void Configure(EntityTypeBuilder<ItemLista> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Tipo, x.Nome }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class AnexoCfg : IEntityTypeConfiguration<Anexo>
{
    public void Configure(EntityTypeBuilder<Anexo> b)
    {
        b.Property(x => x.Entidade).HasMaxLength(60);
        b.Property(x => x.Categoria).HasMaxLength(60);
        b.Property(x => x.ArquivoHashSha256).HasMaxLength(64);
        b.HasIndex(x => new { x.Entidade, x.EntidadeId });
    }
}

internal sealed class SequenciaCfg : IEntityTypeConfiguration<Sequencia>
{
    public void Configure(EntityTypeBuilder<Sequencia> b)
    {
        b.Property(x => x.Chave).HasMaxLength(120);
        b.HasIndex(x => new { x.EmpresaId, x.Chave }).IsUnique();
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

// ───────────── Cadastros ─────────────
internal sealed class FornecedorCfg : IEntityTypeConfiguration<Fornecedor>
{
    public void Configure(EntityTypeBuilder<Fornecedor> b)
    {
        b.Property(x => x.CpfCnpj).HasMaxLength(18);
        b.Property(x => x.Uf).HasMaxLength(2);
        b.Property(x => x.Cep).HasMaxLength(9);
        b.HasIndex(x => new { x.EmpresaId, x.CpfCnpj }).IsUnique().HasFilter("[Excluido] = 0 AND [CpfCnpj] <> ''");
        b.HasIndex(x => new { x.EmpresaId, x.RazaoSocial });
        b.Ignore(x => x.NomeExibicao);
        b.HasMany(x => x.Documentos).WithOne(x => x.Fornecedor).HasForeignKey(x => x.FornecedorId);
        b.HasMany(x => x.Qualificacoes).WithOne(x => x.Fornecedor).HasForeignKey(x => x.FornecedorId);
        b.HasMany(x => x.Grupos).WithOne().HasForeignKey(x => x.FornecedorId);
        b.HasMany(x => x.Servicos).WithOne().HasForeignKey(x => x.FornecedorId);
        b.HasMany(x => x.Exigencias).WithOne().HasForeignKey(x => x.FornecedorId);
    }
}

internal sealed class FornecedorQualificacaoCfg : IEntityTypeConfiguration<FornecedorQualificacao>
{
    public void Configure(EntityTypeBuilder<FornecedorQualificacao> b)
    {
        b.HasOne(x => x.MaterialControlado).WithMany().HasForeignKey(x => x.MaterialControladoId);
        b.HasOne(x => x.ServicoControlado).WithMany().HasForeignKey(x => x.ServicoControladoId);
        b.HasIndex(x => new { x.FornecedorId, x.DataQualificacao });
        b.HasIndex(x => x.DataValidade);
    }
}

internal sealed class FornecedorDocumentoCfg : IEntityTypeConfiguration<FornecedorDocumento>
{
    public void Configure(EntityTypeBuilder<FornecedorDocumento> b)
    {
        b.HasOne(x => x.TipoDocumento).WithMany().HasForeignKey(x => x.TipoDocumentoExigidoId);
        b.Property(x => x.ArquivoHashSha256).HasMaxLength(64);
        b.HasIndex(x => x.DataValidade);
        // Documento mensal: uma entrega ativa por competência (reenvio substitui a anterior).
        b.HasIndex(x => new { x.FornecedorId, x.TipoDocumentoExigidoId, x.Competencia }).IsUnique()
         .HasFilter("[Excluido] = 0 AND [Competencia] IS NOT NULL").HasDatabaseName("UX_FornecedorDocumentos_Competencia");
    }
}

internal sealed class LinkPortalCfg : IEntityTypeConfiguration<LinkPortalFornecedor>
{
    public void Configure(EntityTypeBuilder<LinkPortalFornecedor> b)
    {
        b.Property(x => x.TokenHash).HasMaxLength(64);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId);
    }
}

internal sealed class ServicoCfg : IEntityTypeConfiguration<ServicoControlado>
{
    public void Configure(EntityTypeBuilder<ServicoControlado> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Documento>().WithMany().HasForeignKey(x => x.DocumentoItsId);
        b.Property(x => x.Sigla).HasMaxLength(8);
    }
}

internal sealed class MaterialCfg : IEntityTypeConfiguration<MaterialControlado>
{
    public void Configure(EntityTypeBuilder<MaterialControlado> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<CategoriaProduto>().WithMany().HasForeignKey(x => x.CategoriaProdutoId);
    }
}

internal sealed class CategoriaProdutoCfg : IEntityTypeConfiguration<CategoriaProduto>
{
    public void Configure(EntityTypeBuilder<CategoriaProduto> b)
    {
        b.Property(x => x.Sigla).HasMaxLength(3);
        b.Property(x => x.Nome).HasMaxLength(120);
        b.HasOne(x => x.Pai).WithMany(x => x.Subgrupos).HasForeignKey(x => x.PaiId);
        b.HasIndex(x => new { x.EmpresaId, x.PaiId, x.Sigla }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.EmpresaId, x.PaiId, x.Nome }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class ProdutoCfg : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => x.Ncm);
        b.Property(x => x.Ncm).HasMaxLength(10);
        b.Property(x => x.Ean).HasMaxLength(20);
        b.Ignore(x => x.AClassificar);
        b.HasOne(x => x.MaterialControlado).WithMany().HasForeignKey(x => x.MaterialControladoId);
    }
}

// ───────────── Suprimentos ─────────────
internal sealed class RequisicaoCfg : IEntityTypeConfiguration<Requisicao>
{
    public void Configure(EntityTypeBuilder<Requisicao> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.EmpresaId, x.Status });
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasOne(x => x.CentroCusto).WithMany().HasForeignKey(x => x.CentroCustoId);
        b.Property(x => x.ChaveNFe).HasMaxLength(44);
        b.Ignore(x => x.ValorEstimado);
        b.HasMany(x => x.Itens).WithOne(x => x.Requisicao).HasForeignKey(x => x.RequisicaoId);
        b.HasOne<Estabelecimento>().WithMany().HasForeignKey(x => x.EstabelecimentoFaturamentoId);
    }
}

internal sealed class RequisicaoItemCfg : IEntityTypeConfiguration<RequisicaoItem>
{
    public void Configure(EntityTypeBuilder<RequisicaoItem> b)
    {
        b.HasOne(x => x.Produto).WithMany().HasForeignKey(x => x.ProdutoId);
        b.HasOne(x => x.ServicoControlado).WithMany().HasForeignKey(x => x.ServicoControladoId);
        b.HasOne(x => x.MaterialControladoRef).WithMany().HasForeignKey(x => x.MaterialControladoId);
        b.Property(x => x.Quantidade).HasPrecision(18, 3);
        b.HasMany(x => x.Locais).WithOne().HasForeignKey(l => l.RequisicaoItemId);
        b.HasOne<Domain.Engenharia.Composicao>().WithMany().HasForeignKey(x => x.ComposicaoId);
    }
}

internal sealed class CotacaoCfg : IEntityTypeConfiguration<Cotacao>
{
    public void Configure(EntityTypeBuilder<Cotacao> b)
    {
        b.Property(x => x.AnaliseIa).HasMaxLength(8000);
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasOne(x => x.Requisicao).WithMany().HasForeignKey(x => x.RequisicaoId);
        b.HasOne(x => x.FornecedorVencedor).WithMany().HasForeignKey(x => x.FornecedorVencedorId);
        b.Property(x => x.ChaveNFe).HasMaxLength(44);
        b.HasMany(x => x.Fornecedores).WithOne(x => x.Cotacao).HasForeignKey(x => x.CotacaoId);
        b.HasMany(x => x.Itens).WithOne(x => x.Cotacao).HasForeignKey(x => x.CotacaoId);
    }
}

internal sealed class CotacaoFornecedorCfg : IEntityTypeConfiguration<CotacaoFornecedor>
{
    public void Configure(EntityTypeBuilder<CotacaoFornecedor> b)
    {
        b.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasIndex(x => new { x.CotacaoId, x.FornecedorId }).IsUnique();
        b.HasIndex(x => x.LinkResposta).IsUnique();
    }
}

internal sealed class CotacaoItemCfg : IEntityTypeConfiguration<CotacaoItem>
{
    public void Configure(EntityTypeBuilder<CotacaoItem> b)
    {
        b.HasOne(x => x.RequisicaoItem).WithMany().HasForeignKey(x => x.RequisicaoItemId);
        b.HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId);
        b.Property(x => x.Quantidade).HasPrecision(18, 3);
        b.Ignore(x => x.ValorUnitarioFinal); b.Ignore(x => x.ValorTotal); b.Ignore(x => x.Economia);
    }
}

internal sealed class PedidoCfg : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasOne(x => x.Cotacao).WithMany().HasForeignKey(x => x.CotacaoId);
        b.HasOne<Requisicao>().WithMany().HasForeignKey(x => x.RequisicaoId);
        b.HasOne<Contrato>().WithMany().HasForeignKey(x => x.ContratoId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.Property(x => x.ChaveNFe).HasMaxLength(44);
        b.Ignore(x => x.Valor);
        b.HasMany(x => x.Itens).WithOne(x => x.Pedido).HasForeignKey(x => x.PedidoId);
        b.HasOne<Estabelecimento>().WithMany().HasForeignKey(x => x.EstabelecimentoFaturamentoId);
    }
}

internal sealed class PedidoItemCfg : IEntityTypeConfiguration<PedidoItem>
{
    public void Configure(EntityTypeBuilder<PedidoItem> b)
    {
        b.HasOne(x => x.MaterialControlado).WithMany().HasForeignKey(x => x.MaterialControladoId);
        b.HasOne<RequisicaoItem>().WithMany().HasForeignKey(x => x.RequisicaoItemId);
        b.HasOne<Produto>().WithMany().HasForeignKey(x => x.ProdutoId);
        b.Property(x => x.Quantidade).HasPrecision(18, 3);
        b.Property(x => x.QuantidadeRecebidaAceita).HasPrecision(18, 3);
        b.HasIndex(x => x.CodigoInsumo);
    }
}

internal sealed class ContratoCfg : IEntityTypeConfiguration<Contrato>
{
    public void Configure(EntityTypeBuilder<Contrato> b)
    {
        b.Property(x => x.Numero).HasMaxLength(30);
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasOne<CentroCusto>().WithMany().HasForeignKey(x => x.CentroCustoId);
        b.HasOne<Requisicao>().WithMany().HasForeignKey(x => x.RequisicaoId);
        b.HasOne<Cotacao>().WithMany().HasForeignKey(x => x.CotacaoId);
        b.Ignore(x => x.ValorTotal);
        b.HasMany(x => x.Itens).WithOne(x => x.Contrato).HasForeignKey(x => x.ContratoId);
        b.HasMany(x => x.ServicosVinculados).WithOne().HasForeignKey(x => x.ContratoId);
        b.HasOne<Estabelecimento>().WithMany().HasForeignKey(x => x.EstabelecimentoFaturamentoId);
    }
}

internal sealed class ContratoItemCfg : IEntityTypeConfiguration<ContratoItem>
{
    public void Configure(EntityTypeBuilder<ContratoItem> b)
    {
        b.HasOne(x => x.ServicoControlado).WithMany().HasForeignKey(x => x.ServicoControladoId);
        b.Property(x => x.Quantidade).HasPrecision(18, 3);
        b.Ignore(x => x.Total);
        b.HasMany(x => x.Locais).WithOne().HasForeignKey(l => l.ContratoItemId);
        b.HasOne<Domain.Engenharia.Composicao>().WithMany().HasForeignKey(x => x.ComposicaoId);
    }
}

internal sealed class ContratoServicoCfg : IEntityTypeConfiguration<ContratoServico>
{
    public void Configure(EntityTypeBuilder<ContratoServico> b)
    {
        b.HasOne(x => x.ServicoControlado).WithMany().HasForeignKey(x => x.ServicoControladoId);
        b.HasIndex(x => new { x.ContratoId, x.ServicoControladoId }).IsUnique();
    }
}

internal sealed class RecebimentoCfg : IEntityTypeConfiguration<Recebimento>
{
    public void Configure(EntityTypeBuilder<Recebimento> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne(x => x.Pedido).WithMany().HasForeignKey(x => x.PedidoId);
        b.HasOne<Contrato>().WithMany().HasForeignKey(x => x.ContratoId);
        b.HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.Property(x => x.ChaveNFe).HasMaxLength(44);
        b.Ignore(x => x.FvmAprovada); b.Ignore(x => x.NotaEntrega); b.Ignore(x => x.TemDivergencia);
        b.HasIndex(x => new { x.FornecedorId, x.DataRecebimento });
        b.HasMany(x => x.Itens).WithOne(x => x.Recebimento).HasForeignKey(x => x.RecebimentoId);
        b.HasOne<Estabelecimento>().WithMany().HasForeignKey(x => x.EstabelecimentoId);
    }
}

internal sealed class RecebimentoItemCfg : IEntityTypeConfiguration<RecebimentoItem>
{
    public void Configure(EntityTypeBuilder<RecebimentoItem> b)
    {
        b.HasOne<PedidoItem>().WithMany().HasForeignKey(x => x.PedidoItemId);
        b.Property(x => x.QtdPedida).HasPrecision(18, 3);
        b.Property(x => x.QtdRecebida).HasPrecision(18, 3);
    }
}

internal sealed class MedicaoCfg : IEntityTypeConfiguration<Medicao>
{
    public void Configure(EntityTypeBuilder<Medicao> b)
    {
        b.HasIndex(x => new { x.ContratoId, x.NumeroMedicao }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne(x => x.Contrato).WithMany().HasForeignKey(x => x.ContratoId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasOne<PlanoAcaoFornecedor>().WithMany().HasForeignKey(x => x.PlanoAcaoId);
        b.Property(x => x.ChaveNFe).HasMaxLength(60);
        foreach (var p in new[] { nameof(Medicao.NotaEscopo), nameof(Medicao.NotaMaterial), nameof(Medicao.NotaServico), nameof(Medicao.NotaLimpeza), nameof(Medicao.NotaDocSeg), nameof(Medicao.NotaFinal) })
            b.Property(p).HasPrecision(5, 2);
        b.HasMany(x => x.Itens).WithOne(x => x.Medicao).HasForeignKey(x => x.MedicaoId);
    }
}

internal sealed class MedicaoItemCfg : IEntityTypeConfiguration<MedicaoItem>
{
    public void Configure(EntityTypeBuilder<MedicaoItem> b)
    {
        b.HasOne(x => x.ContratoItem).WithMany().HasForeignKey(x => x.ContratoItemId);
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId);
        foreach (var p in new[] { nameof(MedicaoItem.QtdAnterior), nameof(MedicaoItem.QtdPeriodo), nameof(MedicaoItem.QtdAcumulada) })
            b.Property(p).HasPrecision(18, 3);
    }
}

internal sealed class PlanoAcaoCfg : IEntityTypeConfiguration<PlanoAcaoFornecedor>
{
    public void Configure(EntityTypeBuilder<PlanoAcaoFornecedor> b)
    {
        b.Property(x => x.Numero).HasMaxLength(20);
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasIndex(x => new { x.FornecedorId, x.Status });
    }
}

internal sealed class AvaliacaoSemestralCfg : IEntityTypeConfiguration<AvaliacaoSemestral>
{
    public void Configure(EntityTypeBuilder<AvaliacaoSemestral> b)
    {
        b.Property(x => x.Periodo).HasMaxLength(8);
        b.HasIndex(x => new { x.EmpresaId, x.Periodo, x.FornecedorId }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId);
    }
}

internal sealed class LiberacaoPagamentoCfg : IEntityTypeConfiguration<LiberacaoPagamento>
{
    public void Configure(EntityTypeBuilder<LiberacaoPagamento> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.Property(x => x.ChaveNFe).HasMaxLength(44);
        b.HasIndex(x => x.ChaveNFe);
        b.HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasOne<Pedido>().WithMany().HasForeignKey(x => x.PedidoId);
        b.HasOne<Contrato>().WithMany().HasForeignKey(x => x.ContratoId);
        b.HasOne<Medicao>().WithMany().HasForeignKey(x => x.MedicaoId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
    }
}

// ───────────── Documentos do SGQ ─────────────
internal sealed class PastaCfg : IEntityTypeConfiguration<Pasta>
{
    public void Configure(EntityTypeBuilder<Pasta> b) => b.HasOne<Pasta>().WithMany().HasForeignKey(x => x.PastaPaiId);
}

internal sealed class DocumentoCfg : IEntityTypeConfiguration<Documento>
{
    public void Configure(EntityTypeBuilder<Documento> b)
    {
        b.Property(x => x.Codigo).HasMaxLength(40);
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne(x => x.Pasta).WithMany().HasForeignKey(x => x.PastaId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasOne<RegraRetencao>().WithMany().HasForeignKey(x => x.RegraRetencaoId);
        b.HasOne<DocumentoRevisao>().WithMany().HasForeignKey(x => x.RevisaoVigenteId);
        b.Ignore(x => x.RevisaoVigente);
        b.HasMany(x => x.Revisoes).WithOne(x => x.Documento).HasForeignKey(x => x.DocumentoId);
        b.HasMany(x => x.Relacionados).WithOne().HasForeignKey(x => x.DocumentoId);
        b.HasIndex(x => x.ProximaAnalise);
    }
}

internal sealed class DocumentoRelacionadoCfg : IEntityTypeConfiguration<DocumentoRelacionado>
{
    public void Configure(EntityTypeBuilder<DocumentoRelacionado> b)
    {
        b.HasOne<Documento>().WithMany().HasForeignKey(x => x.RelacionadoId);
        b.HasIndex(x => new { x.DocumentoId, x.RelacionadoId }).IsUnique();
    }
}

internal sealed class DocumentoRevisaoCfg : IEntityTypeConfiguration<DocumentoRevisao>
{
    public void Configure(EntityTypeBuilder<DocumentoRevisao> b)
    {
        b.HasIndex(x => new { x.DocumentoId, x.Revisao }).IsUnique().HasFilter("[Excluido] = 0");
        b.Property(x => x.HashSha256).HasMaxLength(64);
        b.Ignore(x => x.Rotulo); b.Ignore(x => x.EtapaAtual);
        b.HasMany(x => x.Etapas).WithOne(x => x.Revisao).HasForeignKey(x => x.DocumentoRevisaoId);
        b.HasMany(x => x.Copias).WithOne(x => x.Revisao).HasForeignKey(x => x.DocumentoRevisaoId);
    }
}

internal sealed class CopiaControladaCfg : IEntityTypeConfiguration<CopiaControlada>
{
    public void Configure(EntityTypeBuilder<CopiaControlada> b)
    {
        b.Property(x => x.NumeroCopia).HasMaxLength(60);
        b.HasIndex(x => new { x.EmpresaId, x.NumeroCopia }).IsUnique().HasFilter("[Excluido] = 0");
        b.Property(x => x.QrToken).HasMaxLength(32);
        b.HasIndex(x => x.QrToken).IsUnique();
        b.HasOne<SolicitacaoCopia>().WithMany().HasForeignKey(x => x.SolicitacaoCopiaId);
        b.HasOne<Grd>().WithMany().HasForeignKey(x => x.GrdId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
    }
}

internal sealed class SolicitacaoCopiaCfg : IEntityTypeConfiguration<SolicitacaoCopia>
{
    public void Configure(EntityTypeBuilder<SolicitacaoCopia> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.Ignore(x => x.QtdTotal);
        b.HasMany(x => x.Itens).WithOne().HasForeignKey(x => x.SolicitacaoCopiaId);
    }
}

internal sealed class SolicitacaoCopiaItemCfg : IEntityTypeConfiguration<SolicitacaoCopiaItem>
{
    public void Configure(EntityTypeBuilder<SolicitacaoCopiaItem> b) =>
        b.HasOne(x => x.Revisao).WithMany().HasForeignKey(x => x.DocumentoRevisaoId);
}

internal sealed class CienciaCfg : IEntityTypeConfiguration<Ciencia>
{
    public void Configure(EntityTypeBuilder<Ciencia> b)
    {
        b.HasOne(x => x.Revisao).WithMany().HasForeignKey(x => x.DocumentoRevisaoId);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId);
        b.HasIndex(x => new { x.DocumentoRevisaoId, x.UsuarioId }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class PermissaoDocumentoCfg : IEntityTypeConfiguration<PermissaoDocumento>
{
    public void Configure(EntityTypeBuilder<PermissaoDocumento> b)
    {
        b.HasIndex(x => new { x.Escopo, x.AlvoId });
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId);
    }
}

internal sealed class CheckoutCfg : IEntityTypeConfiguration<CheckoutDocumento>
{
    public void Configure(EntityTypeBuilder<CheckoutDocumento> b)
    {
        b.HasOne<DocumentoRevisao>().WithMany().HasForeignKey(x => x.DocumentoRevisaoId);
        b.Ignore(x => x.Ativo);
        // Só um check-out aberto por revisão (garantia no banco, além da regra no domínio).
        b.HasIndex(x => x.DocumentoRevisaoId).IsUnique().HasFilter("[CheckinEm] IS NULL AND [LiberadoForcadoPor] IS NULL AND [Excluido] = 0");
    }
}

internal sealed class EventoDocumentoCfg : IEntityTypeConfiguration<EventoDocumento>
{
    public void Configure(EntityTypeBuilder<EventoDocumento> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.DataHora });
        b.HasIndex(x => x.DocumentoId);
    }
}

internal sealed class DocumentoExternoCfg : IEntityTypeConfiguration<DocumentoExterno>
{
    public void Configure(EntityTypeBuilder<DocumentoExterno> b) =>
        b.HasIndex(x => new { x.EmpresaId, x.Identificacao, x.Edicao }).IsUnique().HasFilter("[Excluido] = 0");
}

internal sealed class GrdCfg : IEntityTypeConfiguration<Grd>
{
    public void Configure(EntityTypeBuilder<Grd> b)
    {
        b.Property(x => x.Numero).HasMaxLength(30);
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => x.TokenConfirmacao).IsUnique();
        b.HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasOne<Contrato>().WithMany().HasForeignKey(x => x.ContratoId);
        b.HasOne<Pedido>().WithMany().HasForeignKey(x => x.PedidoId);
        b.HasMany(x => x.Itens).WithOne().HasForeignKey(x => x.GrdId);
    }
}

internal sealed class GrdItemCfg : IEntityTypeConfiguration<GrdItem>
{
    public void Configure(EntityTypeBuilder<GrdItem> b) => b.HasOne(x => x.Revisao).WithMany().HasForeignKey(x => x.DocumentoRevisaoId);
}
