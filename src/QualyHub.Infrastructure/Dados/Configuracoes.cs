using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Importacao;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Documentos;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Infrastructure.Dados;

internal static class Texto
{
    public static PropertyBuilder<T> Longo<T>(this PropertyBuilder<T> p) => p.HasColumnType("nvarchar(max)");
}

internal sealed class EmpresaCfg : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> b)
    {
        b.Property(x => x.Cnpj).HasMaxLength(18);
        b.Property(x => x.NivelSiac).HasMaxLength(2);
        b.HasIndex(x => x.Cnpj).IsUnique();
    }
}

internal sealed class ObraCfg : IEntityTypeConfiguration<Obra>
{
    public void Configure(EntityTypeBuilder<Obra> b)
    {
        b.Property(x => x.Codigo).HasMaxLength(30);
        b.Property(x => x.Uf).HasMaxLength(2);
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasMany(x => x.Locais).WithOne().HasForeignKey(l => l.ObraId);
    }
}

internal sealed class LocalCfg : IEntityTypeConfiguration<Local>
{
    public void Configure(EntityTypeBuilder<Local> b)
    {
        b.HasOne(x => x.LocalPai).WithMany(x => x.Filhos).HasForeignKey(x => x.LocalPaiId);
        b.Property(x => x.FracaoIdeal).HasPrecision(18, 8);
        b.HasOne<QualyHub.Domain.Engenharia.Spe>().WithMany().HasForeignKey(x => x.SpeId);
        b.HasOne<QualyHub.Domain.Engenharia.Tipologia>().WithMany().HasForeignKey(x => x.TipologiaId);
        b.HasIndex(x => new { x.ObraId, x.SpeId, x.Tipo });
    }
}

internal sealed class SpeCfg : IEntityTypeConfiguration<QualyHub.Domain.Engenharia.Spe>
{
    public void Configure(EntityTypeBuilder<QualyHub.Domain.Engenharia.Spe> b)
    {
        b.Property(x => x.Codigo).HasMaxLength(10);
        b.Property(x => x.Cnpj).HasMaxLength(20);
        b.Property(x => x.PatrimonioAfetacao).HasMaxLength(20);
        b.HasIndex(x => new { x.EmpresaId, x.Cnpj }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.ObraId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<QualyHub.Domain.Engenharia.Quadra>().WithMany().HasForeignKey(x => x.QuadraId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class QuadraCfg : IEntityTypeConfiguration<QualyHub.Domain.Engenharia.Quadra>
{
    public void Configure(EntityTypeBuilder<QualyHub.Domain.Engenharia.Quadra> b)
    {
        b.Property(x => x.Codigo).HasMaxLength(6);
        b.Property(x => x.Nome).HasMaxLength(120);
        b.Property(x => x.Tipo).HasMaxLength(40);
        b.Property(x => x.CnpjCondominio).HasMaxLength(20);
        b.Property(x => x.AreaTerreno).HasPrecision(18, 2);
        b.HasIndex(x => new { x.ObraId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class TipologiaCfg : IEntityTypeConfiguration<QualyHub.Domain.Engenharia.Tipologia>
{
    public void Configure(EntityTypeBuilder<QualyHub.Domain.Engenharia.Tipologia> b)
    {
        b.Property(x => x.Codigo).HasMaxLength(20);
        b.HasIndex(x => new { x.ObraId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class CredencialUsuarioCfg : IEntityTypeConfiguration<CredencialUsuario>
{
    public void Configure(EntityTypeBuilder<CredencialUsuario> b)
    {
        b.ToTable("CredenciaisUsuario");
        b.Property(x => x.SenhaHash).HasMaxLength(300);
        b.HasIndex(x => x.UsuarioId).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class UsuarioCfg : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.Property(x => x.Login).HasMaxLength(60);
        b.HasIndex(x => x.Login).IsUnique().HasFilter("[Excluido] = 0 AND [Login] <> ''");
        b.Property(x => x.Email).HasMaxLength(200);
        b.HasMany(x => x.Obras).WithOne(x => x.Usuario).HasForeignKey(x => x.UsuarioId);
        b.HasMany(x => x.LimitesAprovacao).WithOne().HasForeignKey(x => x.UsuarioId);
    }
}

internal sealed class UsuarioObraCfg : IEntityTypeConfiguration<UsuarioObra>
{
    public void Configure(EntityTypeBuilder<UsuarioObra> b) => b.HasIndex(x => new { x.UsuarioId, x.ObraId }).IsUnique();
}

internal sealed class SolicitacaoAprovacaoCfg : IEntityTypeConfiguration<SolicitacaoAprovacao>
{
    public void Configure(EntityTypeBuilder<SolicitacaoAprovacao> b)
    {
        b.HasIndex(x => new { x.Entidade, x.EntidadeId });
        b.HasIndex(x => new { x.EmpresaId, x.Status });
        b.Ignore(x => x.Pendente);
        b.Property(x => x.SolicitanteNome).HasMaxLength(150);
        b.HasIndex(x => x.SolicitanteUsuarioId);
        b.HasMany(x => x.Etapas).WithOne(x => x.Solicitacao).HasForeignKey(x => x.SolicitacaoAprovacaoId);
    }
}

internal sealed class EtapaAprovacaoCfg : IEntityTypeConfiguration<EtapaAprovacao>
{
    public void Configure(EntityTypeBuilder<EtapaAprovacao> b)
    {
        b.Property(x => x.Justificativa).HasMaxLength(2000);
        b.Property(x => x.EmNomeDeNome).HasMaxLength(150);
        b.HasIndex(x => new { x.PerfilAprovador, x.Decisao });
        b.HasIndex(x => x.UsuarioAprovadorId);
        b.HasOne<DelegacaoAprovacao>().WithMany().HasForeignKey(x => x.DelegacaoAprovacaoId);
    }
}

internal sealed class LimiteAprovacaoCfg : IEntityTypeConfiguration<LimiteAprovacao>
{
    public void Configure(EntityTypeBuilder<LimiteAprovacao> b)
    {
        // Um limite ativo por usuário × tipo × obra (obra nula = geral).
        b.HasIndex(x => new { x.UsuarioId, x.Entidade, x.ObraId }).IsUnique().HasFilter("[Excluido] = 0 AND [Ativo] = 1");
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
    }
}

internal sealed class DelegacaoAprovacaoCfg : IEntityTypeConfiguration<DelegacaoAprovacao>
{
    public void Configure(EntityTypeBuilder<DelegacaoAprovacao> b)
    {
        b.Property(x => x.Motivo).HasMaxLength(500);
        b.HasIndex(x => new { x.SubstitutoUsuarioId, x.Inicio, x.Fim });
        b.HasIndex(x => x.TitularUsuarioId);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.TitularUsuarioId);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.SubstitutoUsuarioId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.ToTable(t => t.HasCheckConstraint("CK_Delegacao_Pessoas", "[TitularUsuarioId] <> [SubstitutoUsuarioId]"));
        b.ToTable(t => t.HasCheckConstraint("CK_Delegacao_Periodo", "[Fim] > [Inicio]"));
    }
}

internal sealed class LayoutImportacaoCfg : IEntityTypeConfiguration<LayoutImportacao>
{
    public void Configure(EntityTypeBuilder<LayoutImportacao> b)
    {
        b.Property(x => x.MapeamentoJson).Longo();
        b.Property(x => x.Delimitador).HasMaxLength(3);
    }
}

internal sealed class LoteImportacaoCfg : IEntityTypeConfiguration<LoteImportacao>
{
    public void Configure(EntityTypeBuilder<LoteImportacao> b)
    {
        b.Property(x => x.ArquivoHashSha256).HasMaxLength(64);
        b.HasIndex(x => new { x.EmpresaId, x.ArquivoHashSha256 });   // detecta o mesmo arquivo importado duas vezes
        b.HasMany(x => x.Linhas).WithOne().HasForeignKey(x => x.LoteImportacaoId);
    }
}

internal sealed class LinhaImportacaoCfg : IEntityTypeConfiguration<LinhaImportacao>
{
    public void Configure(EntityTypeBuilder<LinhaImportacao> b)
    {
        b.Property(x => x.DadosJson).Longo();
        b.Property(x => x.Erros).Longo();
        b.Property(x => x.ChaveNatural).HasMaxLength(100);
    }
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
