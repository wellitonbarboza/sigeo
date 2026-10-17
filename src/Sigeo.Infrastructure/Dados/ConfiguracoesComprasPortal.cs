using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Infrastructure.Dados;

// ───────────── Atendimento parcial, carteira de compras, portal (competências) e metas ─────────────

internal sealed class LoteCompraCfg : IEntityTypeConfiguration<LoteCompra>
{
    public void Configure(EntityTypeBuilder<LoteCompra> b)
    {
        b.ToTable("LotesCompra");
        b.Property(x => x.Letra).HasMaxLength(4);
        b.Property(x => x.Categoria).HasMaxLength(200);
        b.Property(x => x.CompradorNome).HasMaxLength(150);
        b.Property(x => x.TitularNome).HasMaxLength(150);
        b.HasIndex(x => new { x.RequisicaoId, x.Letra }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.CompradorUsuarioId, x.Situacao });
        b.HasOne<Requisicao>().WithMany().HasForeignKey(x => x.RequisicaoId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.Ignore(x => x.ComSubstituto); b.Ignore(x => x.NaFilaDoGestor);
    }
}

internal sealed class CarteiraCompradorCfg : IEntityTypeConfiguration<CarteiraComprador>
{
    public void Configure(EntityTypeBuilder<CarteiraComprador> b)
    {
        b.ToTable("CarteirasComprador");
        b.Property(x => x.Nome).HasMaxLength(150);
        b.Property(x => x.SubstitutoNome).HasMaxLength(150);
        b.HasIndex(x => new { x.EmpresaId, x.UsuarioId }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasMany(x => x.Categorias).WithOne().HasForeignKey(x => x.CarteiraId);
        b.HasMany(x => x.Ausencias).WithOne().HasForeignKey(x => x.CarteiraId);
    }
}

internal sealed class CarteiraCategoriaCfg : IEntityTypeConfiguration<CarteiraCategoria>
{
    public void Configure(EntityTypeBuilder<CarteiraCategoria> b)
    {
        b.ToTable("CarteirasCategoria");
        b.Property(x => x.Grupo).HasMaxLength(120);
        b.Property(x => x.SubGrupo).HasMaxLength(120).HasAnnotation(SigeoDbContext.TamanhoFixo, true);
        b.Property(x => x.CategoriaServico).HasMaxLength(120);
        b.Ignore(x => x.Rotulo);
    }
}

internal sealed class AusenciaCompradorCfg : IEntityTypeConfiguration<AusenciaComprador>
{
    public void Configure(EntityTypeBuilder<AusenciaComprador> b)
    {
        b.ToTable("AusenciasComprador");
        b.Property(x => x.Motivo).HasMaxLength(200).HasAnnotation(SigeoDbContext.TamanhoFixo, true);
    }
}

internal sealed class PropostaVersaoCfg : IEntityTypeConfiguration<PropostaVersao>
{
    public void Configure(EntityTypeBuilder<PropostaVersao> b)
    {
        b.ToTable("PropostasVersao");
        b.HasOne<CotacaoFornecedor>().WithMany(x => x.Versoes).HasForeignKey(x => x.CotacaoFornecedorId);
        b.HasIndex(x => new { x.CotacaoFornecedorId, x.Versao }).IsUnique();
        b.Property(x => x.EnviadaPor).HasMaxLength(150);
        b.Property(x => x.CondicaoPagamento).HasMaxLength(200);
        b.Property(x => x.ItensJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.ValorItens).HasPrecision(18, 2);
        b.Property(x => x.ValorFrete).HasPrecision(18, 2);
    }
}

internal sealed class CompetenciaDocumentalCfg : IEntityTypeConfiguration<CompetenciaDocumental>
{
    public void Configure(EntityTypeBuilder<CompetenciaDocumental> b)
    {
        b.ToTable("CompetenciasDocumentais");
        b.HasIndex(x => new { x.ContratoId, x.Mes }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.FornecedorId, x.Situacao });
        b.HasOne<Contrato>().WithMany().HasForeignKey(x => x.ContratoId);
        b.HasOne<Domain.Cadastros.Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.Property(x => x.EnviadaPor).HasMaxLength(150);
        b.Property(x => x.ConferidaPor).HasMaxLength(150);
        b.HasMany(x => x.Itens).WithOne().HasForeignKey(x => x.CompetenciaId);
    }
}

internal sealed class ItemCompetenciaCfg : IEntityTypeConfiguration<ItemCompetencia>
{
    public void Configure(EntityTypeBuilder<ItemCompetencia> b)
    {
        b.ToTable("ItensCompetencia");
        b.Property(x => x.Descricao).HasMaxLength(300).HasAnnotation(SigeoDbContext.TamanhoFixo, true);
        b.Property(x => x.ArquivoBlob).HasMaxLength(400);
        b.Property(x => x.ArquivoNome).HasMaxLength(260);
        b.Property(x => x.EnviadoPor).HasMaxLength(150);
        b.Property(x => x.ConferidoPor).HasMaxLength(150);
        b.Property(x => x.AdiamentoAprovadoPor).HasMaxLength(150);
        b.HasOne<Domain.Cadastros.TipoDocumentoExigido>().WithMany().HasForeignKey(x => x.TipoDocumentoExigidoId);
        b.HasIndex(x => new { x.CompetenciaId, x.TipoDocumentoExigidoId, x.ColaboradorId }).IsUnique();
    }
}

internal sealed class MetaCfg : IEntityTypeConfiguration<Meta>
{
    public void Configure(EntityTypeBuilder<Meta> b)
    {
        b.ToTable("Metas");
        b.Property(x => x.Nome).HasMaxLength(200);
        b.Property(x => x.Setor).HasMaxLength(80);
        b.Property(x => x.UsuarioNome).HasMaxLength(150);
        b.Property(x => x.EquipeNome).HasMaxLength(150);
        b.Property(x => x.Servico).HasMaxLength(150);
        b.Property(x => x.Unidade).HasMaxLength(20);
        b.HasIndex(x => new { x.EmpresaId, x.Inicio, x.Fim });
        b.HasMany(x => x.Apuracoes).WithOne().HasForeignKey(x => x.MetaId);
        b.Ignore(x => x.DonoNome);
    }
}

internal sealed class MetaApuracaoCfg : IEntityTypeConfiguration<MetaApuracao>
{
    public void Configure(EntityTypeBuilder<MetaApuracao> b)
    {
        b.ToTable("MetasApuracao");
        b.HasIndex(x => new { x.MetaId, x.Mes }).IsUnique();
        b.Property(x => x.ApuradoPor).HasMaxLength(150);
    }
}
