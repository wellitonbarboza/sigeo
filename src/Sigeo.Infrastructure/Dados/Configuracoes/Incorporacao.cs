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

internal sealed class IncorporacaoCfg : IEntityTypeConfiguration<IncorporacaoObra>
{
    public void Configure(EntityTypeBuilder<IncorporacaoObra> b)
    {
        b.ToTable("Incorporacoes");
        b.Property(x => x.Nome).HasMaxLength(150);
        b.Property(x => x.MemorialRegistro).HasMaxLength(120);
        b.Property(x => x.CnpjIncorporacao).HasMaxLength(18);
        b.Property(x => x.IndiceAteChaves).HasMaxLength(20);
        b.Property(x => x.IndiceAposChaves).HasMaxLength(20);
        b.Property(x => x.JurosAposChavesMes).HasPrecision(9, 4);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasIndex(x => x.ObraId).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class AliquotaRegimeCfg : IEntityTypeConfiguration<AliquotaRegime>
{
    public void Configure(EntityTypeBuilder<AliquotaRegime> b)
    {
        b.ToTable("AliquotasRegime");
        foreach (var p in new[] { nameof(AliquotaRegime.Irpj), nameof(AliquotaRegime.Csll), nameof(AliquotaRegime.Pis), nameof(AliquotaRegime.Cofins), nameof(AliquotaRegime.Cbs), nameof(AliquotaRegime.Ibs) })
            b.Property(p).HasPrecision(9, 4);
        b.Property(x => x.CodigoReceita).HasMaxLength(20);
        b.Property(x => x.Observacao).HasMaxLength(300);
        b.Ignore(x => x.Total);
        b.HasIndex(x => new { x.EmpresaId, x.Regime, x.VigenciaInicio }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class IndiceEconomicoCfg : IEntityTypeConfiguration<IndiceEconomico>
{
    public void Configure(EntityTypeBuilder<IndiceEconomico> b)
    {
        b.ToTable("IndicesEconomicos");
        b.Property(x => x.Indice).HasMaxLength(20);
        b.Property(x => x.VariacaoPerc).HasPrecision(9, 4);
        b.HasIndex(x => new { x.EmpresaId, x.Indice, x.Competencia }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class UnidadeComercialCfg : IEntityTypeConfiguration<UnidadeComercial>
{
    public void Configure(EntityTypeBuilder<UnidadeComercial> b)
    {
        b.ToTable("UnidadesComerciais");
        b.Property(x => x.PrecoTabela).HasPrecision(18, 2);
        b.Property(x => x.Vagas).HasMaxLength(60);
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId);
        b.HasIndex(x => x.LocalId).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class CompradorCfg : IEntityTypeConfiguration<Comprador>
{
    public void Configure(EntityTypeBuilder<Comprador> b)
    {
        b.ToTable("Compradores");
        b.Property(x => x.Nome).HasMaxLength(200);
        b.Property(x => x.CpfCnpj).HasMaxLength(18);
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.Telefone).HasMaxLength(60);
        b.Property(x => x.Endereco).HasMaxLength(300);
        b.Property(x => x.Conjuge).HasMaxLength(200);
    }
}

internal sealed class ContratoVendaCfg : IEntityTypeConfiguration<ContratoVenda>
{
    public void Configure(EntityTypeBuilder<ContratoVenda> b)
    {
        b.ToTable("ContratosVenda");
        b.Property(x => x.ValorVenda).HasPrecision(18, 2);
        b.Property(x => x.ValorDevolvido).HasPrecision(18, 2);
        b.Property(x => x.JurosAposChavesMes).HasPrecision(9, 4);
        b.Property(x => x.IndiceAteChaves).HasMaxLength(20);
        b.Property(x => x.IndiceAposChaves).HasMaxLength(20);
        b.HasOne<UnidadeComercial>().WithMany().HasForeignKey(x => x.UnidadeComercialId);
        b.HasOne<Comprador>().WithMany().HasForeignKey(x => x.CompradorId);
        b.HasOne<Titulo>().WithMany().HasForeignKey(x => x.TituloId);
        b.HasMany(x => x.Blocos).WithOne().HasForeignKey(x => x.ContratoVendaId);
        b.HasMany(x => x.Comissoes).WithOne().HasForeignKey(x => x.ContratoVendaId);
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class BlocoCondicaoVendaCfg : IEntityTypeConfiguration<BlocoCondicaoVenda>
{
    public void Configure(EntityTypeBuilder<BlocoCondicaoVenda> b)
    {
        b.ToTable("BlocosCondicaoVenda");
        b.Property(x => x.ValorParcela).HasPrecision(18, 2);
        b.Ignore(x => x.Total);
        b.HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorPermutaId);
    }
}

internal sealed class ComissaoVendaCfg : IEntityTypeConfiguration<ComissaoVenda>
{
    public void Configure(EntityTypeBuilder<ComissaoVenda> b)
    {
        b.ToTable("ComissoesVenda");
        b.Property(x => x.Papel).HasMaxLength(60);
        b.Property(x => x.Percentual).HasPrecision(9, 4);
        b.Property(x => x.Valor).HasPrecision(18, 2);
        b.Property(x => x.PercentualPagoMinimo).HasPrecision(9, 4);
        b.HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasOne<Titulo>().WithMany().HasForeignKey(x => x.TituloId);
    }
}

internal sealed class ApuracaoRetCfg : IEntityTypeConfiguration<ApuracaoRet>
{
    public void Configure(EntityTypeBuilder<ApuracaoRet> b)
    {
        b.ToTable("ApuracoesRet");
        foreach (var p in new[] { nameof(ApuracaoRet.ReceitaRecebida), nameof(ApuracaoRet.Irpj), nameof(ApuracaoRet.Csll), nameof(ApuracaoRet.Pis), nameof(ApuracaoRet.Cofins), nameof(ApuracaoRet.Cbs), nameof(ApuracaoRet.Ibs) })
            b.Property(p).HasPrecision(18, 2);
        b.Property(x => x.AliquotaTotal).HasPrecision(9, 4);
        b.Ignore(x => x.Total);
        b.HasOne<IncorporacaoObra>().WithMany().HasForeignKey(x => x.IncorporacaoId);
        b.HasOne<Titulo>().WithMany().HasForeignKey(x => x.TituloId);
        b.HasIndex(x => new { x.IncorporacaoId, x.Competencia }).IsUnique().HasFilter("[Excluido] = 0");
    }
}
