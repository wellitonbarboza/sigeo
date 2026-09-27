using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Sst;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Infrastructure.Dados;

internal sealed class FvsCfg : IEntityTypeConfiguration<Fvs>
{
    public void Configure(EntityTypeBuilder<Fvs> b)
    {
        b.ToTable("Fvs");
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.LocalId, x.ServicoControladoId, x.Status });
        b.Property(x => x.Numero).HasMaxLength(20);
    }
}

internal sealed class ModeloInspecaoCfg : IEntityTypeConfiguration<ModeloInspecao>
{
    public void Configure(EntityTypeBuilder<ModeloInspecao> b) =>
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
}

internal sealed class RncCfg : IEntityTypeConfiguration<Rnc>
{
    public void Configure(EntityTypeBuilder<Rnc> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.FornecedorId, x.Status });
    }
}

internal sealed class EquipamentoCfg : IEntityTypeConfiguration<EquipamentoMedicao>
{
    public void Configure(EntityTypeBuilder<EquipamentoMedicao> b) =>
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
}

internal sealed class ColaboradorCfg : IEntityTypeConfiguration<Colaborador>
{
    public void Configure(EntityTypeBuilder<Colaborador> b)
    {
        b.Property(x => x.CpfHash).HasMaxLength(64);
        b.Property(x => x.CpfMascarado).HasMaxLength(20);
        b.HasIndex(x => new { x.ObraId, x.CpfHash });
    }
}

internal sealed class AditivoCfg : IEntityTypeConfiguration<Aditivo>
{
    public void Configure(EntityTypeBuilder<Aditivo> b) => b.HasIndex(x => new { x.ContratoId, x.Numero });
}
