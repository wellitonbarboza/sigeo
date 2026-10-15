using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Groundkey.Domain.Cadastros;
using Groundkey.Domain.Engenharia;
using Groundkey.Domain.Estrutura;
using Groundkey.Domain.Planejamento;
using Groundkey.Domain.Qualidade;
using Groundkey.Domain.Sst;
using Groundkey.Domain.Suprimentos;

namespace Groundkey.Infrastructure.Dados;

// ───────────── Estoque ─────────────

internal sealed class AlmoxarifadoCfg : IEntityTypeConfiguration<Almoxarifado>
{
    public void Configure(EntityTypeBuilder<Almoxarifado> b)
    {
        b.ToTable("Almoxarifados");
        b.Property(x => x.Codigo).HasMaxLength(30);
        b.Property(x => x.Nome).HasMaxLength(150);
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        // Um almoxarifado por obra; um central por estabelecimento.
        b.HasIndex(x => new { x.EmpresaId, x.ObraId }).IsUnique().HasFilter("[Excluido] = 0 AND [ObraId] IS NOT NULL").HasDatabaseName("UX_Almoxarifados_Obra");
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasOne<Estabelecimento>().WithMany().HasForeignKey(x => x.EstabelecimentoId);
    }
}

internal sealed class SaldoEstoqueCfg : IEntityTypeConfiguration<SaldoEstoque>
{
    public void Configure(EntityTypeBuilder<SaldoEstoque> b)
    {
        b.ToTable("SaldosEstoque");
        b.HasIndex(x => new { x.AlmoxarifadoId, x.ProdutoId }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Almoxarifado>().WithMany().HasForeignKey(x => x.AlmoxarifadoId);
        b.HasOne<Produto>().WithMany().HasForeignKey(x => x.ProdutoId);
        b.Ignore(x => x.Valor); b.Ignore(x => x.AbaixoDoMinimo); b.Ignore(x => x.NoPontoDePedido);
    }
}

internal sealed class MovimentoEstoqueCfg : IEntityTypeConfiguration<MovimentoEstoque>
{
    public void Configure(EntityTypeBuilder<MovimentoEstoque> b)
    {
        b.ToTable("MovimentosEstoque");
        b.Property(x => x.Valor).HasPrecision(18, 2);
        b.Property(x => x.Documento).HasMaxLength(120);
        b.Property(x => x.OrigemTipo).HasMaxLength(40);
        b.Property(x => x.EntreguePara).HasMaxLength(200);
        b.Property(x => x.Lote).HasMaxLength(60);
        b.HasIndex(x => new { x.AlmoxarifadoId, x.ProdutoId, x.Data });
        b.HasIndex(x => new { x.OrigemTipo, x.OrigemId });
        b.HasIndex(x => x.LocalId);
        b.HasOne<Almoxarifado>().WithMany().HasForeignKey(x => x.AlmoxarifadoId);
        b.HasOne<Produto>().WithMany().HasForeignKey(x => x.ProdutoId);
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId);
        b.HasOne<Composicao>().WithMany().HasForeignKey(x => x.ComposicaoId);
        b.HasOne<KitMaterial>().WithMany().HasForeignKey(x => x.KitId);
        b.HasOne<Colaborador>().WithMany().HasForeignKey(x => x.ColaboradorId);
        b.HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId);
    }
}

internal sealed class InventarioEstoqueCfg : IEntityTypeConfiguration<InventarioEstoque>
{
    public void Configure(EntityTypeBuilder<InventarioEstoque> b)
    {
        b.ToTable("InventariosEstoque");
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.Property(x => x.AprovadoPor).HasMaxLength(150);
        b.HasOne<Almoxarifado>().WithMany().HasForeignKey(x => x.AlmoxarifadoId);
        b.HasMany(x => x.Itens).WithOne().HasForeignKey(i => i.InventarioId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class InventarioItemCfg : IEntityTypeConfiguration<InventarioItem>
{
    public void Configure(EntityTypeBuilder<InventarioItem> b)
    {
        b.ToTable("InventarioItens");
        b.HasOne<Produto>().WithMany().HasForeignKey(x => x.ProdutoId);
        b.Ignore(x => x.Diferenca);
    }
}

internal sealed class KitMaterialCfg : IEntityTypeConfiguration<KitMaterial>
{
    public void Configure(EntityTypeBuilder<KitMaterial> b)
    {
        b.ToTable("KitsMaterial");
        b.Property(x => x.Codigo).HasMaxLength(30);
        b.Property(x => x.Nome).HasMaxLength(150);
        b.Property(x => x.Etapa).HasMaxLength(150);
        b.Property(x => x.Disciplina).HasMaxLength(100);
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasMany(x => x.Itens).WithOne().HasForeignKey(i => i.KitId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class KitMaterialItemCfg : IEntityTypeConfiguration<KitMaterialItem>
{
    public void Configure(EntityTypeBuilder<KitMaterialItem> b)
    {
        b.ToTable("KitMaterialItens");
        b.HasOne<Produto>().WithMany().HasForeignKey(x => x.ProdutoId);
    }
}

// ───────────── Equipamentos e manutenção ─────────────

internal sealed class FamiliaEquipamentoCfg : IEntityTypeConfiguration<FamiliaEquipamento>
{
    public void Configure(EntityTypeBuilder<FamiliaEquipamento> b)
    {
        b.ToTable("FamiliasEquipamento");
        b.Property(x => x.Nome).HasMaxLength(100);
        b.HasIndex(x => new { x.EmpresaId, x.Nome }).IsUnique().HasFilter("[Excluido] = 0");
        b.Ignore(x => x.Campos);
    }
}

internal sealed class EquipamentoPatrimonioCfg : IEntityTypeConfiguration<Equipamento>
{
    public void Configure(EntityTypeBuilder<Equipamento> b)
    {
        b.ToTable("EquipamentosPatrimonio");
        b.Property(x => x.Patrimonio).HasMaxLength(40);
        b.Property(x => x.Nome).HasMaxLength(150);
        b.Property(x => x.QrToken).HasMaxLength(40);
        b.Property(x => x.CamposExtras).HasMaxLength(1000);
        b.HasIndex(x => new { x.EmpresaId, x.Patrimonio }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => x.QrToken).IsUnique();
        b.HasOne<FamiliaEquipamento>().WithMany().HasForeignKey(x => x.FamiliaId);
        b.HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasOne<Contrato>().WithMany().HasForeignKey(x => x.ContratoId);
        b.HasOne<ContratoItem>().WithMany().HasForeignKey(x => x.ContratoItemId);
        b.HasOne<Almoxarifado>().WithMany().HasForeignKey(x => x.AlmoxarifadoId);
        b.HasMany(x => x.Documentos).WithOne().HasForeignKey(d => d.EquipamentoId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class EquipamentoDocumentoCfg : IEntityTypeConfiguration<EquipamentoDocumento>
{
    public void Configure(EntityTypeBuilder<EquipamentoDocumento> b)
    {
        b.ToTable("EquipamentoDocumentos");
        b.Property(x => x.Nome).HasMaxLength(150);
    }
}

internal sealed class MovimentoEquipamentoCfg : IEntityTypeConfiguration<MovimentoEquipamento>
{
    public void Configure(EntityTypeBuilder<MovimentoEquipamento> b)
    {
        b.ToTable("MovimentosEquipamento");
        b.Property(x => x.Checklist).HasMaxLength(2000);
        b.HasIndex(x => new { x.EquipamentoId, x.Data });
        b.HasOne<Equipamento>().WithMany().HasForeignKey(x => x.EquipamentoId);
        b.HasOne<Almoxarifado>().WithMany().HasForeignKey(x => x.DeAlmoxarifadoId);
        b.HasOne<Almoxarifado>().WithMany().HasForeignKey(x => x.ParaAlmoxarifadoId);
    }
}

internal sealed class PlanoManutencaoCfg : IEntityTypeConfiguration<PlanoManutencao>
{
    public void Configure(EntityTypeBuilder<PlanoManutencao> b)
    {
        b.ToTable("PlanosManutencao");
        b.Property(x => x.Nome).HasMaxLength(150);
        b.Property(x => x.Executante).HasMaxLength(150);
        b.Property(x => x.Checklist).HasMaxLength(4000);
        b.HasOne<Equipamento>().WithMany().HasForeignKey(x => x.EquipamentoId);
        b.HasOne<Contrato>().WithMany().HasForeignKey(x => x.ContratoId);
        b.Ignore(x => x.ProximaData); b.Ignore(x => x.ProximaLeitura); b.Ignore(x => x.ItensChecklist);
    }
}

internal sealed class OrdemServicoCfg : IEntityTypeConfiguration<OrdemServico>
{
    public void Configure(EntityTypeBuilder<OrdemServico> b)
    {
        b.ToTable("OrdensServico");
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.EquipamentoId, x.Status });
        b.Property(x => x.AbertaPor).HasMaxLength(150);
        b.Property(x => x.ExecutadoPor).HasMaxLength(150);
        b.Property(x => x.VerificadoPor).HasMaxLength(150);
        b.Property(x => x.ModoFalha).HasMaxLength(100);
        b.Property(x => x.Causa).HasMaxLength(1000);
        b.Property(x => x.ChecklistResultado).HasMaxLength(4000);
        b.Property(x => x.CustoMaoObra).HasPrecision(18, 2); b.Property(x => x.CustoPecas).HasPrecision(18, 2); b.Property(x => x.CustoTerceiros).HasPrecision(18, 2);
        b.HasOne<Equipamento>().WithMany().HasForeignKey(x => x.EquipamentoId);
        b.HasOne<PlanoManutencao>().WithMany().HasForeignKey(x => x.PlanoId);
        b.HasOne<Contrato>().WithMany().HasForeignKey(x => x.ContratoId);
        b.HasOne<Requisicao>().WithMany().HasForeignKey(x => x.RequisicaoId);
        b.Ignore(x => x.CustoTotal); b.Ignore(x => x.EmAberto);
    }
}

// ───────────── Contrato recorrente ─────────────

internal sealed class ParcelaContratoCfg : IEntityTypeConfiguration<ParcelaContrato>
{
    public void Configure(EntityTypeBuilder<ParcelaContrato> b)
    {
        b.ToTable("ParcelasContrato");
        b.HasIndex(x => new { x.ContratoId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.Property(x => x.ValorPrevisto).HasPrecision(18, 2);
        b.Property(x => x.NotaValor).HasPrecision(18, 2);
        b.Property(x => x.NotaNumero).HasMaxLength(40);
        b.Property(x => x.NotaOrigem).HasMaxLength(60);
        b.Property(x => x.Composicao).Metadata.SetMaxLength(null);
        b.Property(x => x.ConferidoPor).HasMaxLength(150); b.Property(x => x.AprovadoPor).HasMaxLength(150); b.Property(x => x.LiberadoPor).HasMaxLength(150);
        b.HasOne<Contrato>().WithMany().HasForeignKey(x => x.ContratoId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasOne<Domain.Fiscal.NotaFiscalEntrada>().WithMany().HasForeignKey(x => x.NotaFiscalId);
        b.HasOne<OrdemServico>().WithMany().HasForeignKey(x => x.OrdemServicoId);
        b.Ignore(x => x.Diferenca);
    }
}

internal sealed class UsoContratoCfg : IEntityTypeConfiguration<UsoContrato>
{
    public void Configure(EntityTypeBuilder<UsoContrato> b)
    {
        b.ToTable("UsosContrato");
        b.Property(x => x.Documento).HasMaxLength(60);
        b.HasIndex(x => new { x.ContratoId, x.Data });
        b.HasOne<Contrato>().WithMany().HasForeignKey(x => x.ContratoId);
        b.HasOne<ContratoItem>().WithMany().HasForeignKey(x => x.ContratoItemId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
    }
}

// ───────────── Equipamentos de medição: tipos e cautela ─────────────

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

internal sealed class DiarioEquipamentoCfg : IEntityTypeConfiguration<DiarioEquipamento>
{
    public void Configure(EntityTypeBuilder<DiarioEquipamento> b)
    {
        b.ToTable("DiariosObraEquipamentos");
        b.Property(x => x.Horas).HasPrecision(9, 2);
        b.HasIndex(x => x.DiarioId);
        b.HasOne<Equipamento>().WithMany().HasForeignKey(x => x.EquipamentoId);
    }
}
