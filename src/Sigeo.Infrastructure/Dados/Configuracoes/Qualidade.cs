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

internal sealed class TipoEquipamentoMedicaoCfg : IEntityTypeConfiguration<TipoEquipamentoMedicao>
{
    public void Configure(EntityTypeBuilder<TipoEquipamentoMedicao> b)
    {
        b.ToTable("TiposEquipamentoMedicao");
        b.Property(x => x.Nome).HasMaxLength(100);
        b.Property(x => x.Prefixo).HasMaxLength(10);
        b.Property(x => x.Criterio).HasMaxLength(400);
        b.HasIndex(x => new { x.EmpresaId, x.Nome }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class CautelaFerramentaCfg : IEntityTypeConfiguration<CautelaFerramenta>
{
    public void Configure(EntityTypeBuilder<CautelaFerramenta> b)
    {
        b.ToTable("CautelasFerramenta");
        b.Property(x => x.Para).HasMaxLength(200);
        b.Property(x => x.EntreguePor).HasMaxLength(150);
        b.HasIndex(x => new { x.EquipamentoMedicaoId, x.Data });
        b.HasOne<EquipamentoMedicao>().WithMany().HasForeignKey(x => x.EquipamentoMedicaoId);
        b.HasOne<Colaborador>().WithMany().HasForeignKey(x => x.ColaboradorId);
        b.HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId);
    }
}

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
    public void Configure(EntityTypeBuilder<EquipamentoMedicao> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.Property(x => x.QrToken).HasMaxLength(40);
        b.Property(x => x.ComQuem).HasMaxLength(200);
        b.HasIndex(x => x.QrToken);
        b.HasOne<TipoEquipamentoMedicao>().WithMany().HasForeignKey(x => x.TipoMedicaoId);
        b.HasOne<Domain.Cadastros.Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasOne<Domain.Sst.Colaborador>().WithMany().HasForeignKey(x => x.CautelaColaboradorId);
        b.HasOne<Domain.Cadastros.Fornecedor>().WithMany().HasForeignKey(x => x.CautelaFornecedorId);
    }
}
