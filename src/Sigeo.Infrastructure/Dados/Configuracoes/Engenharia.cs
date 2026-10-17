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

internal sealed class SpeCfg : IEntityTypeConfiguration<Sigeo.Domain.Engenharia.Spe>
{
    public void Configure(EntityTypeBuilder<Sigeo.Domain.Engenharia.Spe> b)
    {
        b.Property(x => x.Codigo).HasMaxLength(10);
        b.Property(x => x.Cnpj).HasMaxLength(20);
        b.Property(x => x.PatrimonioAfetacao).HasMaxLength(20);
        b.HasIndex(x => new { x.EmpresaId, x.Cnpj }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.ObraId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Sigeo.Domain.Engenharia.Quadra>().WithMany().HasForeignKey(x => x.QuadraId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.QuadraId).IsUnique().HasFilter("[Excluido] = 0 AND [QuadraId] IS NOT NULL");
    }
}

internal sealed class QuadraCfg : IEntityTypeConfiguration<Sigeo.Domain.Engenharia.Quadra>
{
    public void Configure(EntityTypeBuilder<Sigeo.Domain.Engenharia.Quadra> b)
    {
        b.Property(x => x.Codigo).HasMaxLength(6);
        b.Property(x => x.Nome).HasMaxLength(120);
        b.Property(x => x.Tipo).HasMaxLength(40);
        b.Property(x => x.CnpjCondominio).HasMaxLength(20);
        b.Property(x => x.AreaTerreno).HasPrecision(18, 2);
        b.HasIndex(x => new { x.ObraId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Sigeo.Domain.Engenharia.Tipologia>().WithMany().HasForeignKey(x => x.ImplantacaoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class TipologiaCfg : IEntityTypeConfiguration<Sigeo.Domain.Engenharia.Tipologia>
{
    public void Configure(EntityTypeBuilder<Sigeo.Domain.Engenharia.Tipologia> b)
    {
        b.Property(x => x.Codigo).HasMaxLength(20);
        b.HasIndex(x => new { x.ObraId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.Property(x => x.PeDireitoPadrao).HasPrecision(18, 4);
        b.HasMany(x => x.Ambientes).WithOne().HasForeignKey(a => a.TipologiaId);
        b.HasMany(x => x.Mix).WithOne().HasForeignKey(m => m.TipologiaId);
        b.Ignore(x => x.SomaAreaComodos);
    }
}

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
        b.Property(x => x.Descricao).HasMaxLength(120).HasAnnotation(SigeoDbContext.TamanhoFixo, true); // entra no índice único
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

internal sealed class CustoRealizadoCfg : IEntityTypeConfiguration<CustoRealizado>
{
    public void Configure(EntityTypeBuilder<CustoRealizado> b)
    {
        b.ToTable("CustosRealizados");
        b.Property(x => x.Fase).HasMaxLength(120);
        b.Property(x => x.FornecedorNome).HasMaxLength(200);
        b.Property(x => x.Descricao).HasMaxLength(300);
        b.Property(x => x.Unidade).HasMaxLength(10);
        b.Property(x => x.DocumentoTipo).HasMaxLength(40);
        b.Property(x => x.DocumentoNumero).HasMaxLength(60);
        b.Property(x => x.ChaveOrigem).HasMaxLength(200);
        b.Property(x => x.Quantidade).HasPrecision(18, 4);
        b.Property(x => x.Valor).HasPrecision(18, 2);
        b.Ignore(x => x.ValorUnitario);
        b.Ignore(x => x.ValorTravado);
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId);
        b.HasOne<Domain.Cadastros.Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasOne<Domain.Cadastros.Produto>().WithMany().HasForeignKey(x => x.ProdutoId);
        b.HasOne<Domain.Suprimentos.Pedido>().WithMany().HasForeignKey(x => x.PedidoId);
        b.HasIndex(x => new { x.ObraId, x.Data });
        b.HasIndex(x => new { x.ObraId, x.Fase });
        b.HasIndex(x => x.RecebimentoId);
        b.HasIndex(x => x.MedicaoId);
        b.HasIndex(x => new { x.ObraId, x.ChaveOrigem }).IsUnique().HasFilter("[Excluido] = 0 AND [ChaveOrigem] IS NOT NULL");
    }
}
