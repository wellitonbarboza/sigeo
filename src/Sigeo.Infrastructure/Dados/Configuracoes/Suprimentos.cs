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

internal sealed class RequisicaoItemLocalCfg : IEntityTypeConfiguration<RequisicaoItemLocal>
{
    public void Configure(EntityTypeBuilder<RequisicaoItemLocal> b)
    {
        b.ToTable("RequisicaoItemLocais");
        b.Property(x => x.Quantidade).HasPrecision(18, 4);
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId);
        b.HasOne<Composicao>().WithMany().HasForeignKey(x => x.ComposicaoId);
        b.HasIndex(x => x.LocalId);
        b.HasIndex(x => new { x.RequisicaoItemId, x.LocalId, x.ComposicaoId }).IsUnique();
    }
}

internal sealed class ContratoItemLocalCfg : IEntityTypeConfiguration<ContratoItemLocal>
{
    public void Configure(EntityTypeBuilder<ContratoItemLocal> b)
    {
        b.ToTable("ContratoItemLocais");
        b.Property(x => x.Quantidade).HasPrecision(18, 4);
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId);
        b.HasIndex(x => new { x.ContratoItemId, x.LocalId }).IsUnique();
    }
}

internal sealed class TransferenciaCfg : IEntityTypeConfiguration<TransferenciaMaterial>
{
    public void Configure(EntityTypeBuilder<TransferenciaMaterial> b)
    {
        b.ToTable("Transferencias");
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasOne<Recebimento>().WithMany().HasForeignKey(x => x.RecebimentoId);
        b.HasOne<Spe>().WithMany().HasForeignKey(x => x.SpeId);
        b.HasOne<Pedido>().WithMany().HasForeignKey(x => x.PedidoId);
        b.HasOne<Estabelecimento>().WithMany().HasForeignKey(x => x.OrigemEstabelecimentoId);
        b.HasOne<Almoxarifado>().WithMany().HasForeignKey(x => x.OrigemAlmoxarifadoId);
        b.HasOne<Almoxarifado>().WithMany().HasForeignKey(x => x.DestinoAlmoxarifadoId);
        // Um recebimento gera no máximo uma transferência ativa.
        b.HasIndex(x => x.RecebimentoId).IsUnique().HasFilter("[Excluido] = 0 AND [RecebimentoId] IS NOT NULL AND [Status] <> N'Cancelada'")
         .HasDatabaseName("UX_Transferencias_Recebimento");
        b.HasMany(x => x.Itens).WithOne().HasForeignKey(i => i.TransferenciaId);
        b.Ignore(x => x.Valor);
    }
}

internal sealed class TransferenciaItemCfg : IEntityTypeConfiguration<TransferenciaItem>
{
    public void Configure(EntityTypeBuilder<TransferenciaItem> b)
    {
        b.ToTable("TransferenciaItens");
        b.Property(x => x.Quantidade).HasPrecision(18, 4);
        b.Property(x => x.QuantidadeRecebida).HasPrecision(18, 4);
        b.Property(x => x.CustoUnitario).HasPrecision(18, 4);
        b.Property(x => x.Lote).HasMaxLength(60);
        b.HasOne<Produto>().WithMany().HasForeignKey(x => x.ProdutoId);
        b.HasOne<RecebimentoItem>().WithMany().HasForeignKey(x => x.RecebimentoItemId);
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId);
        b.Ignore(x => x.Valor);
    }
}

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
        b.Property(x => x.RascunhoJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.PendenciaNaData).HasMaxLength(1000);
        b.Property(x => x.MotivoDeclinio).HasMaxLength(300).HasAnnotation(SigeoDbContext.TamanhoFixo, true);
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
        b.HasOne<Almoxarifado>().WithMany().HasForeignKey(x => x.AlmoxarifadoId);
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
        b.HasOne<Produto>().WithMany().HasForeignKey(x => x.ProdutoId);
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

internal sealed class AditivoCfg : IEntityTypeConfiguration<Aditivo>
{
    public void Configure(EntityTypeBuilder<Aditivo> b) => b.HasIndex(x => new { x.ContratoId, x.Numero });
}
