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

internal sealed class ContaGerencialCfg : IEntityTypeConfiguration<ContaGerencial>
{
    public void Configure(EntityTypeBuilder<ContaGerencial> b)
    {
        b.ToTable("ContasGerenciais");
        b.Property(x => x.Codigo).HasMaxLength(30);
        b.Property(x => x.Nome).HasMaxLength(150);
        b.Ignore(x => x.Nivel); b.Ignore(x => x.Rotulo);
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class ContaBancariaCfg : IEntityTypeConfiguration<ContaBancaria>
{
    public void Configure(EntityTypeBuilder<ContaBancaria> b)
    {
        b.ToTable("ContasBancarias");
        b.Property(x => x.Nome).HasMaxLength(100);
        b.Property(x => x.Banco).HasMaxLength(100);
        b.Property(x => x.Agencia).HasMaxLength(20);
        b.Property(x => x.Numero).HasMaxLength(30);
        b.Property(x => x.SaldoInicial).HasPrecision(18, 2);
        b.Ignore(x => x.NumeroMascarado); b.Ignore(x => x.Rotulo);
        b.HasOne<Estabelecimento>().WithMany().HasForeignKey(x => x.EstabelecimentoId);
    }
}

internal sealed class CondicaoPagamentoCfg : IEntityTypeConfiguration<CondicaoPagamento>
{
    public void Configure(EntityTypeBuilder<CondicaoPagamento> b)
    {
        b.ToTable("CondicoesPagamento");
        b.Property(x => x.Nome).HasMaxLength(60);
        b.Property(x => x.Prazos).HasMaxLength(200);
        b.Property(x => x.EntradaPerc).HasPrecision(9, 4);
        b.Ignore(x => x.Dias);
        b.HasIndex(x => new { x.EmpresaId, x.Nome }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class TituloCfg : IEntityTypeConfiguration<Titulo>
{
    public void Configure(EntityTypeBuilder<Titulo> b)
    {
        b.ToTable("Titulos");
        b.Property(x => x.DocumentoTipo).HasMaxLength(40);
        b.Property(x => x.DocumentoNumero).HasMaxLength(60);
        b.Property(x => x.Pessoa).HasMaxLength(200);
        b.Property(x => x.Descricao).HasMaxLength(300);
        b.Property(x => x.ChaveOrigem).HasMaxLength(200);
        b.Property(x => x.MotivoCancelamento).HasMaxLength(300);
        foreach (var p in new[] { nameof(Titulo.ValorBruto), nameof(Titulo.RetencaoIss), nameof(Titulo.RetencaoInss), nameof(Titulo.RetencaoIr), nameof(Titulo.RetencaoPcc) })
            b.Property(p).HasPrecision(18, 2);
        b.Ignore(x => x.Retencoes); b.Ignore(x => x.ValorLiquido); b.Ignore(x => x.Status);
        b.HasMany(x => x.Parcelas).WithOne(p => p.Titulo).HasForeignKey(p => p.TituloId);
        b.HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasOne<Comprador>().WithMany().HasForeignKey(x => x.CompradorId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasOne<ContaGerencial>().WithMany().HasForeignKey(x => x.ContaGerencialId);
        b.HasOne<CentroCusto>().WithMany().HasForeignKey(x => x.CentroCustoId);
        b.HasOne<Estabelecimento>().WithMany().HasForeignKey(x => x.EstabelecimentoId);
        b.HasIndex(x => new { x.EmpresaId, x.Tipo, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.EmpresaId, x.ChaveOrigem }).IsUnique().HasFilter("[Excluido] = 0 AND [ChaveOrigem] IS NOT NULL");
        b.HasIndex(x => new { x.Origem, x.OrigemId });
        b.HasIndex(x => x.ObraId);
    }
}

internal sealed class TituloParcelaCfg : IEntityTypeConfiguration<TituloParcela>
{
    public void Configure(EntityTypeBuilder<TituloParcela> b)
    {
        b.ToTable("TituloParcelas");
        foreach (var p in new[] { nameof(TituloParcela.Valor), nameof(TituloParcela.Correcao), nameof(TituloParcela.Juros), nameof(TituloParcela.Multa), nameof(TituloParcela.Desconto), nameof(TituloParcela.ValorPago) })
            b.Property(p).HasPrecision(18, 2);
        b.Property(x => x.Comprovante).HasMaxLength(200);
        b.Ignore(x => x.TotalBaixa);
        b.HasOne<ContaBancaria>().WithMany().HasForeignKey(x => x.ContaBancariaId);
        b.HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorPermutaId);
        b.HasIndex(x => new { x.Status, x.Vencimento });
        b.HasIndex(x => x.DataPagamento);
    }
}

internal sealed class MovimentoBancarioCfg : IEntityTypeConfiguration<MovimentoBancario>
{
    public void Configure(EntityTypeBuilder<MovimentoBancario> b)
    {
        b.ToTable("MovimentosBancarios");
        b.Property(x => x.Valor).HasPrecision(18, 2);
        b.Property(x => x.Historico).HasMaxLength(300);
        b.HasOne<ContaBancaria>().WithMany().HasForeignKey(x => x.ContaBancariaId);
        b.HasOne<TituloParcela>().WithMany().HasForeignKey(x => x.TituloParcelaId);
        b.HasIndex(x => new { x.ContaBancariaId, x.Data });
        b.HasIndex(x => x.TransferenciaId);
    }
}
