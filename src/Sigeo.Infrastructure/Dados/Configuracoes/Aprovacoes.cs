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
