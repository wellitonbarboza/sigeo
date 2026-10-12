using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Infrastructure.Dados;

// ───────────── Estrutura do condomínio, orçamento e mão de obra ─────────────

internal sealed class TipologiaAmbienteCfg : IEntityTypeConfiguration<TipologiaAmbiente>
{
    public void Configure(EntityTypeBuilder<TipologiaAmbiente> b)
    {
        b.ToTable("TipologiaAmbientes");
        b.Property(x => x.Nome).HasMaxLength(80);
        b.Property(x => x.Sigla).HasMaxLength(8);
        b.Property(x => x.Categoria).HasMaxLength(60);
        b.Property(x => x.AreaPiso).HasPrecision(18, 4);
        b.Property(x => x.Perimetro).HasPrecision(18, 4);
        b.Property(x => x.PeDireito).HasPrecision(18, 4);
        b.Property(x => x.AreaVaos).HasPrecision(18, 4);
        b.HasIndex(x => x.TipologiaId);
        b.HasIndex(x => new { x.TipologiaId, x.Sigla }).IsUnique().HasFilter("[Sigla] IS NOT NULL");
    }
}

internal sealed class TipologiaMixCfg : IEntityTypeConfiguration<TipologiaMix>
{
    public void Configure(EntityTypeBuilder<TipologiaMix> b)
    {
        b.ToTable("TipologiaMix");
        b.HasOne<Tipologia>().WithMany().HasForeignKey(x => x.ApartamentoTipologiaId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.TipologiaId);
    }
}

internal sealed class ComposicaoCfg : IEntityTypeConfiguration<Composicao>
{
    public void Configure(EntityTypeBuilder<Composicao> b)
    {
        b.ToTable("Composicoes");
        b.Property(x => x.Codigo).HasMaxLength(30);
        b.Property(x => x.Unidade).HasMaxLength(10);
        b.Property(x => x.Grupo).HasMaxLength(80);
        b.Property(x => x.Fator).HasPrecision(18, 6);
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<ServicoControlado>().WithMany().HasForeignKey(x => x.ServicoControladoId);
        b.HasMany(x => x.Itens).WithOne().HasForeignKey(i => i.ComposicaoId);
        b.Ignore(x => x.CustoUnitario); b.Ignore(x => x.HorasPorUnidade);
    }
}

internal sealed class ComposicaoItemCfg : IEntityTypeConfiguration<ComposicaoItem>
{
    public void Configure(EntityTypeBuilder<ComposicaoItem> b)
    {
        b.ToTable("ComposicaoItens");
        b.Property(x => x.Unidade).HasMaxLength(10);
        b.Property(x => x.Coeficiente).HasPrecision(18, 6);
        b.Property(x => x.PerdaPerc).HasPrecision(9, 4);
        b.Property(x => x.CustoUnitario).HasPrecision(18, 4);
        b.HasOne<Produto>().WithMany().HasForeignKey(x => x.ProdutoId);
        b.HasOne<Domain.Sst.FuncaoObra>().WithMany().HasForeignKey(x => x.FuncaoObraId);
        b.Ignore(x => x.CustoPorUnidade);
    }
}

internal sealed class ServicoAplicacaoCfg : IEntityTypeConfiguration<ServicoAplicacao>
{
    public void Configure(EntityTypeBuilder<ServicoAplicacao> b)
    {
        b.ToTable("ServicoAplicacoes");
        b.Property(x => x.QuantidadeFixa).HasPrecision(18, 4);
        b.HasOne<Tipologia>().WithMany().HasForeignKey(x => x.TipologiaId);
        b.HasOne<Composicao>().WithMany().HasForeignKey(x => x.ComposicaoId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasOne<TipologiaAmbiente>().WithMany().HasForeignKey(x => x.AmbienteModeloId);
        b.HasIndex(x => new { x.TipologiaId, x.ComposicaoId, x.Alvo, x.AmbienteModeloId }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class ServicoLocalCfg : IEntityTypeConfiguration<ServicoLocal>
{
    public void Configure(EntityTypeBuilder<ServicoLocal> b)
    {
        b.ToTable("ServicosLocais");
        b.Property(x => x.Quantidade).HasPrecision(18, 4);
        b.Property(x => x.CustoUnitario).HasPrecision(18, 4);
        b.Property(x => x.Custo).HasPrecision(18, 2);
        b.Property(x => x.Origem).HasMaxLength(20);
        b.HasOne<Composicao>().WithMany().HasForeignKey(x => x.ComposicaoId);
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId);
        b.HasOne<Spe>().WithMany().HasForeignKey(x => x.SpeId);
        b.HasIndex(x => new { x.ComposicaoId, x.LocalId }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.ObraId, x.LocalId });
    }
}

internal sealed class EquipeCfg : IEntityTypeConfiguration<Equipe>
{
    public void Configure(EntityTypeBuilder<Equipe> b)
    {
        b.ToTable("Equipes");
        b.Property(x => x.Codigo).HasMaxLength(20);
        b.Property(x => x.Nome).HasMaxLength(120);
        b.HasIndex(x => new { x.ObraId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasMany(x => x.Membros).WithOne().HasForeignKey(m => m.EquipeId);
    }
}

internal sealed class EquipeMembroCfg : IEntityTypeConfiguration<EquipeMembro>
{
    public void Configure(EntityTypeBuilder<EquipeMembro> b)
    {
        b.ToTable("EquipeMembros");
        b.HasOne<Domain.Sst.Colaborador>().WithMany().HasForeignKey(x => x.ColaboradorId);
        b.HasIndex(x => new { x.EquipeId, x.ColaboradorId }).IsUnique();
    }
}

internal sealed class CustoMaoObraMesCfg : IEntityTypeConfiguration<CustoMaoObraMes>
{
    public void Configure(EntityTypeBuilder<CustoMaoObraMes> b)
    {
        b.ToTable("CustosMaoObra");
        b.Property(x => x.Salario).HasPrecision(18, 2);
        b.Property(x => x.Beneficios).HasPrecision(18, 2);
        b.Property(x => x.Outros).HasPrecision(18, 2);
        b.Property(x => x.EncargosPerc).HasPrecision(9, 4);
        b.Property(x => x.HorasMes).HasPrecision(9, 2);
        b.HasOne<Equipe>().WithMany().HasForeignKey(x => x.EquipeId);
        b.HasOne<Domain.Sst.Colaborador>().WithMany().HasForeignKey(x => x.ColaboradorId);
        b.HasOne<Domain.Sst.FuncaoObra>().WithMany().HasForeignKey(x => x.FuncaoObraId);
        b.Property(x => x.Descricao).HasMaxLength(120).HasAnnotation(QualyHubDbContext.TamanhoFixo, true); // entra no índice único
        b.HasIndex(x => new { x.ObraId, x.Competencia });
        // Uma linha por equipe/colaborador/função/descrição no mês (NULL conta como igual no índice único do SQL Server).
        b.HasIndex(x => new { x.ObraId, x.Competencia, x.EquipeId, x.ColaboradorId, x.FuncaoObraId, x.Descricao }).IsUnique().HasFilter("[Excluido] = 0")
         .HasDatabaseName("UX_CustosMaoObra_Linha");
        b.Ignore(x => x.Total); b.Ignore(x => x.CustoHora);
    }
}

internal sealed class ApropriacaoCfg : IEntityTypeConfiguration<Apropriacao>
{
    public void Configure(EntityTypeBuilder<Apropriacao> b)
    {
        b.ToTable("Apropriacoes");
        b.Property(x => x.Horas).HasPrecision(18, 2);
        b.Property(x => x.QuantidadeExecutada).HasPrecision(18, 4);
        b.HasOne<Equipe>().WithMany().HasForeignKey(x => x.EquipeId);
        b.HasOne<Domain.Sst.Colaborador>().WithMany().HasForeignKey(x => x.ColaboradorId);
        b.HasOne<Composicao>().WithMany().HasForeignKey(x => x.ComposicaoId);
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId);
        b.HasIndex(x => new { x.ObraId, x.Data });
        b.HasIndex(x => x.Lancamento);
    }
}

// ───────────── Estabelecimentos ─────────────

internal sealed class EstabelecimentoCfg : IEntityTypeConfiguration<Estabelecimento>
{
    public void Configure(EntityTypeBuilder<Estabelecimento> b)
    {
        b.ToTable("Estabelecimentos");
        b.Property(x => x.Codigo).HasMaxLength(20);
        b.Property(x => x.Cnpj).HasMaxLength(20);
        b.Property(x => x.Raiz).HasMaxLength(8);
        b.Property(x => x.RazaoSocial).HasMaxLength(200);
        b.HasIndex(x => new { x.EmpresaId, x.Cnpj }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Spe>().WithMany().HasForeignKey(x => x.SpeId);
        // Regras "só um": uma SPE por estabelecimento, uma fábrica que fatura materiais e um almoxarifado central por empresa.
        b.HasIndex(x => x.SpeId).IsUnique().HasFilter("[Excluido] = 0 AND [SpeId] IS NOT NULL").HasDatabaseName("UX_Estabelecimentos_Spe");
        b.HasIndex(x => new { x.EmpresaId, x.FaturaMateriais }).IsUnique().HasFilter("[Excluido] = 0 AND [FaturaMateriais] = 1").HasDatabaseName("UX_Estabelecimentos_FaturaMateriais");
        b.HasIndex(x => new { x.EmpresaId, x.AlmoxarifadoCentral }).IsUnique().HasFilter("[Excluido] = 0 AND [AlmoxarifadoCentral] = 1").HasDatabaseName("UX_Estabelecimentos_Almoxarifado");
    }
}

internal sealed class FluxoEstabelecimentoCfg : IEntityTypeConfiguration<FluxoEstabelecimento>
{
    public void Configure(EntityTypeBuilder<FluxoEstabelecimento> b)
    {
        b.ToTable("FluxosEstabelecimento");
        b.Property(x => x.Base).HasMaxLength(80);
        b.Property(x => x.Situacao).HasMaxLength(30);
        b.HasOne<Estabelecimento>().WithMany().HasForeignKey(x => x.OrigemId);
        b.HasOne<Estabelecimento>().WithMany().HasForeignKey(x => x.DestinoId);
    }
}

// ───────────── Fornecedores ─────────────

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

// ───────────── Suprimentos: quebra por local e transferência ─────────────

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

// ───────────── Área do Fornecedor ─────────────

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
