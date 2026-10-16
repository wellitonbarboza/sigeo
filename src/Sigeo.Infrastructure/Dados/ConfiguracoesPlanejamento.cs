using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Planejamento;

namespace Sigeo.Infrastructure.Dados;

// ───────────── Planejamento: versões do cronograma, atividades, vínculos, redes e calendário ─────────────

internal sealed class VersaoCronogramaCfg : IEntityTypeConfiguration<VersaoCronograma>
{
    public void Configure(EntityTypeBuilder<VersaoCronograma> b)
    {
        b.ToTable("VersoesCronograma");
        b.Property(x => x.Codigo).HasMaxLength(10);
        b.Property(x => x.EnviadoPor).HasMaxLength(150);
        b.Property(x => x.AprovadoPor).HasMaxLength(150);
        b.Property(x => x.CustoTotal).HasPrecision(18, 2);
        b.HasIndex(x => new { x.ObraId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Atividades).WithOne().HasForeignKey(a => a.VersaoId);
        b.HasMany(x => x.Vinculos).WithOne().HasForeignKey(v => v.VersaoId);
        b.HasMany(x => x.Desembolso).WithOne().HasForeignKey(d => d.VersaoId);
    }
}

internal sealed class AtividadeCronogramaCfg : IEntityTypeConfiguration<AtividadeCronograma>
{
    public void Configure(EntityTypeBuilder<AtividadeCronograma> b)
    {
        b.ToTable("AtividadesCronograma");
        b.Property(x => x.Eap).HasMaxLength(40);
        b.Property(x => x.Nome).HasMaxLength(250);
        b.Property(x => x.Chave).HasMaxLength(80);
        b.Property(x => x.Avanco).HasPrecision(5, 1);
        b.Property(x => x.Custo).HasPrecision(18, 2);
        b.Ignore(x => x.Marco);
        b.Ignore(x => x.Concluida);
        b.HasIndex(x => new { x.VersaoId, x.PaiId, x.Ordem });
        b.HasOne<AtividadeCronograma>().WithMany().HasForeignKey(x => x.PaiId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Composicao>().WithMany().HasForeignKey(x => x.ComposicaoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VinculoAtividadeCfg : IEntityTypeConfiguration<VinculoAtividade>
{
    public void Configure(EntityTypeBuilder<VinculoAtividade> b)
    {
        b.ToTable("VinculosAtividade");
        b.HasIndex(x => x.VersaoId);
        b.HasIndex(x => new { x.PredecessoraId, x.SucessoraId }).IsUnique();
        b.HasOne<AtividadeCronograma>().WithMany().HasForeignKey(x => x.PredecessoraId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AtividadeCronograma>().WithMany().HasForeignKey(x => x.SucessoraId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DesembolsoVersaoCfg : IEntityTypeConfiguration<DesembolsoVersao>
{
    public void Configure(EntityTypeBuilder<DesembolsoVersao> b)
    {
        b.ToTable("DesembolsoVersao");
        b.Property(x => x.Valor).HasPrecision(18, 2);
        b.Property(x => x.ValorBase).HasPrecision(18, 2);
        b.HasIndex(x => new { x.VersaoId, x.Mes }).IsUnique();
    }
}

internal sealed class FeriadoCfg : IEntityTypeConfiguration<Feriado>
{
    public void Configure(EntityTypeBuilder<Feriado> b)
    {
        b.ToTable("Feriados");
        b.Property(x => x.Nome).HasMaxLength(120);
        b.HasIndex(x => new { x.EmpresaId, x.Data });
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RedePlanejamentoCfg : IEntityTypeConfiguration<RedePlanejamento>
{
    public void Configure(EntityTypeBuilder<RedePlanejamento> b)
    {
        b.ToTable("RedesPlanejamento");
        b.Property(x => x.Nome).HasMaxLength(120);
        b.Property(x => x.Cor).HasMaxLength(9);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Locais).WithOne().HasForeignKey(l => l.RedeId);
        b.HasMany(x => x.Pacotes).WithOne().HasForeignKey(p => p.RedeId);
    }
}

internal sealed class RedeLocalCfg : IEntityTypeConfiguration<RedeLocal>
{
    public void Configure(EntityTypeBuilder<RedeLocal> b)
    {
        b.ToTable("RedeLocais");
        b.HasIndex(x => new { x.RedeId, x.LocalId }).IsUnique();
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RedePacoteCfg : IEntityTypeConfiguration<RedePacote>
{
    public void Configure(EntityTypeBuilder<RedePacote> b)
    {
        b.ToTable("RedePacotes");
        b.Property(x => x.Nome).HasMaxLength(150);
        b.HasIndex(x => new { x.RedeId, x.Ordem });
        b.HasOne<Composicao>().WithMany().HasForeignKey(x => x.ComposicaoId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Ajustes).WithOne().HasForeignKey(a => a.PacoteId);
    }
}

internal sealed class RedePacoteLocalCfg : IEntityTypeConfiguration<RedePacoteLocal>
{
    public void Configure(EntityTypeBuilder<RedePacoteLocal> b)
    {
        b.ToTable("RedePacoteLocais");
        b.HasIndex(x => new { x.PacoteId, x.LocalId }).IsUnique();
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VinculoRedeCfg : IEntityTypeConfiguration<VinculoRede>
{
    public void Configure(EntityTypeBuilder<VinculoRede> b)
    {
        b.ToTable("VinculosRede");
        b.HasIndex(x => x.ObraId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RedePacote>().WithMany().HasForeignKey(x => x.PacoteOrigemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RedePacote>().WithMany().HasForeignKey(x => x.PacoteDestinoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RestricaoCfg : IEntityTypeConfiguration<Restricao>
{
    public void Configure(EntityTypeBuilder<Restricao> b)
    {
        b.ToTable("Restricoes");
        b.Property(x => x.AtividadeChave).HasMaxLength(80);
        b.Property(x => x.AtividadeNome).HasMaxLength(250);
        b.Property(x => x.ResponsavelNome).HasMaxLength(150);
        b.Property(x => x.RemovidaPor).HasMaxLength(150);
        b.HasIndex(x => new { x.ObraId, x.Status, x.DataLimite });
        b.HasIndex(x => new { x.ObraId, x.AtividadeChave });
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Sigeo.Domain.Estrutura.Usuario>().WithMany().HasForeignKey(x => x.ResponsavelId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PlanoSemanalCfg : IEntityTypeConfiguration<PlanoSemanal>
{
    public void Configure(EntityTypeBuilder<PlanoSemanal> b)
    {
        b.ToTable("PlanosSemanais");
        b.Property(x => x.FechadoPor).HasMaxLength(150);
        b.Property(x => x.Ppc).HasPrecision(5, 1);
        b.Ignore(x => x.Fim);
        b.Ignore(x => x.Editavel);
        b.HasIndex(x => new { x.ObraId, x.Semana }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Compromissos).WithOne().HasForeignKey(c => c.PlanoId);
    }
}

internal sealed class CompromissoSemanalCfg : IEntityTypeConfiguration<CompromissoSemanal>
{
    public void Configure(EntityTypeBuilder<CompromissoSemanal> b)
    {
        b.ToTable("CompromissosSemanais");
        b.Property(x => x.AtividadeChave).HasMaxLength(80);
        b.Property(x => x.Descricao).HasMaxLength(300).HasAnnotation(SigeoDbContext.TamanhoFixo, true);
        b.Property(x => x.Meta).HasMaxLength(150);
        b.Property(x => x.Responsavel).HasMaxLength(150);
        b.Property(x => x.ObservacaoCausa).HasMaxLength(500);
        b.HasIndex(x => new { x.PlanoId, x.Ordem });
    }
}

internal sealed class MedicaoSemanalCfg : IEntityTypeConfiguration<MedicaoSemanal>
{
    public void Configure(EntityTypeBuilder<MedicaoSemanal> b)
    {
        b.ToTable("MedicoesSemanais");
        b.Property(x => x.AtividadeChave).HasMaxLength(80);
        b.Property(x => x.Servico).HasMaxLength(200);
        b.Property(x => x.MedidoPor).HasMaxLength(150);
        b.Property(x => x.Percentual).HasPrecision(5, 1);
        b.Property(x => x.PercentualAnterior).HasPrecision(5, 1);
        b.HasIndex(x => new { x.ObraId, x.AtividadeChave, x.Semana }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.ObraId, x.Semana });
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DiarioObraCfg : IEntityTypeConfiguration<DiarioObra>
{
    public void Configure(EntityTypeBuilder<DiarioObra> b)
    {
        b.ToTable("DiariosObra");
        b.Property(x => x.AssinadoPorNome).HasMaxLength(150);
        b.Property(x => x.Visitas).HasMaxLength(1000);
        b.Property(x => x.Equipamentos).HasMaxLength(1000);
        b.Ignore(x => x.Editavel); b.Ignore(x => x.TotalProprio); b.Ignore(x => x.TotalTerceiros);
        b.HasIndex(x => new { x.ObraId, x.Data }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.ObraId, x.Numero });
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Efetivo).WithOne().HasForeignKey(x => x.DiarioId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.EquipamentosDia).WithOne().HasForeignKey(x => x.DiarioId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Atividades).WithOne().HasForeignKey(x => x.DiarioId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class DiarioEfetivoCfg : IEntityTypeConfiguration<DiarioEfetivo>
{
    public void Configure(EntityTypeBuilder<DiarioEfetivo> b)
    {
        b.ToTable("DiariosObraEfetivo");
        b.Property(x => x.Nome).HasMaxLength(200);
        b.HasIndex(x => x.DiarioId);
    }
}

internal sealed class DiarioAtividadeCfg : IEntityTypeConfiguration<DiarioAtividade>
{
    public void Configure(EntityTypeBuilder<DiarioAtividade> b)
    {
        b.ToTable("DiariosObraAtividades");
        b.Property(x => x.Descricao).HasMaxLength(1000).HasAnnotation(SigeoDbContext.TamanhoFixo, true);
        b.Property(x => x.AtividadeChave).HasMaxLength(80);
        b.HasIndex(x => x.DiarioId);
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId).OnDelete(DeleteBehavior.Restrict);
    }
}
