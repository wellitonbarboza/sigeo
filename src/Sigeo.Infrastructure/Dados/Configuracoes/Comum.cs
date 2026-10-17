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

// Ajudantes de configuração e entidades comuns (anexos, sequências, auditoria).
internal static class Texto
{
    public static PropertyBuilder<T> Longo<T>(this PropertyBuilder<T> p) => p.HasColumnType("nvarchar(max)");
}

internal sealed class RegistroAuditoriaCfg : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> b)
    {
        b.ToTable("Auditoria");
        b.Property(x => x.ValoresAnterioresJson).Longo();
        b.Property(x => x.ValoresNovosJson).Longo();
        b.Property(x => x.CamposAlterados).Longo();
        b.Property(x => x.Entidade).HasMaxLength(100);
        b.Property(x => x.EntidadeId).HasMaxLength(50);
        b.Property(x => x.Operacao).HasMaxLength(20);
        b.HasIndex(x => new { x.Entidade, x.EntidadeId });
        b.HasIndex(x => x.Quando);
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
