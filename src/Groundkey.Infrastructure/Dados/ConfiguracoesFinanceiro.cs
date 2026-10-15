using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Groundkey.Domain.Cadastros;
using Groundkey.Domain.Estrutura;
using Groundkey.Domain.Financeiro;
using Groundkey.Domain.Incorporacao;

namespace Groundkey.Infrastructure.Dados;

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
