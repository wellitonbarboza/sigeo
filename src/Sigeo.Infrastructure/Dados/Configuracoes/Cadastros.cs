using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Common;
using Sigeo.Domain.Documentos;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Financeiro;
using Sigeo.Domain.Fiscal;
using Sigeo.Domain.Importacao;
using Sigeo.Domain.Incorporacao;
using Sigeo.Domain.Planejamento;
using Sigeo.Domain.Qualidade;
using Sigeo.Domain.Sst;
using Sigeo.Domain.Suprimentos;
using Sigeo.Infrastructure.Dados;

namespace Sigeo.Infrastructure.Dados.Configuracoes;

internal sealed class FornecedorGrupoCfg : IEntityTypeConfiguration<FornecedorGrupo>
{
    public void Configure(EntityTypeBuilder<FornecedorGrupo> b)
    {
        b.ToTable("FornecedorGrupos");
        b.Property(x => x.Subgrupos).HasMaxLength(2000);
        b.HasOne<CategoriaProduto>().WithMany().HasForeignKey(x => x.CategoriaProdutoId);
        b.HasIndex(x => new { x.FornecedorId, x.CategoriaProdutoId }).IsUnique();
    }
}

internal sealed class FornecedorServicoCfg : IEntityTypeConfiguration<FornecedorServico>
{
    public void Configure(EntityTypeBuilder<FornecedorServico> b)
    {
        b.ToTable("FornecedorServicos");
        b.HasOne<ServicoControlado>().WithMany().HasForeignKey(x => x.ServicoControladoId);
        b.HasIndex(x => new { x.FornecedorId, x.ServicoControladoId }).IsUnique();
    }
}

internal sealed class FornecedorExigenciaCfg : IEntityTypeConfiguration<FornecedorExigencia>
{
    public void Configure(EntityTypeBuilder<FornecedorExigencia> b)
    {
        b.ToTable("FornecedorExigencias");
        b.HasOne<TipoDocumentoExigido>().WithMany().HasForeignKey(x => x.TipoDocumentoExigidoId);
        b.HasIndex(x => new { x.FornecedorId, x.TipoDocumentoExigidoId }).IsUnique();
    }
}

internal sealed class CodigoAcessoFornecedorCfg : IEntityTypeConfiguration<CodigoAcessoFornecedor>
{
    public void Configure(EntityTypeBuilder<CodigoAcessoFornecedor> b)
    {
        b.ToTable("CodigosAcessoFornecedor");
        b.Property(x => x.Documento).HasMaxLength(14);
        b.Property(x => x.Destino).HasMaxLength(200);
        b.Property(x => x.CodigoHash).HasMaxLength(64);
        b.Property(x => x.Ip).HasMaxLength(64);
        b.HasIndex(x => new { x.Documento, x.CriadoEm });
        b.HasIndex(x => new { x.Ip, x.CriadoEm });
        b.HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId);
    }
}

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
        b.Property(x => x.Norma).HasMaxLength(500);
        b.Property(x => x.FormasQualificacao).HasMaxLength(40);
        b.Property(x => x.CriterioQualificacao).HasMaxLength(600);
        b.Property(x => x.Armazenamento).HasMaxLength(1000);
        b.Property(x => x.ServicosQueUtilizam).HasMaxLength(300);
    }
}

internal sealed class CategoriaProdutoCfg : IEntityTypeConfiguration<CategoriaProduto>
{
    public void Configure(EntityTypeBuilder<CategoriaProduto> b)
    {
        b.Property(x => x.FasePadrao).HasMaxLength(120);
        b.Property(x => x.Sigla).HasMaxLength(3);
        b.Property(x => x.Nome).HasMaxLength(120);
        b.HasOne(x => x.Pai).WithMany(x => x.Subgrupos).HasForeignKey(x => x.PaiId);
        b.HasIndex(x => new { x.EmpresaId, x.PaiId, x.Sigla }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.EmpresaId, x.PaiId, x.Nome }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class MarcaCfg : IEntityTypeConfiguration<Marca>
{
    public void Configure(EntityTypeBuilder<Marca> b)
    {
        b.ToTable("Marcas");
        b.Property(x => x.Nome).HasMaxLength(120);
        b.HasIndex(x => new { x.EmpresaId, x.Nome }).IsUnique().HasFilter("[Excluido] = 0");
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
        b.HasOne<Produto>().WithMany().HasForeignKey(x => x.SubstituidoPorId).OnDelete(DeleteBehavior.NoAction);
    }
}
