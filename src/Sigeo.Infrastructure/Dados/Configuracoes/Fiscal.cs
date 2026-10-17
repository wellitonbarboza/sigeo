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

internal sealed class NotaFiscalEntradaCfg : IEntityTypeConfiguration<NotaFiscalEntrada>
{
    public void Configure(EntityTypeBuilder<NotaFiscalEntrada> b)
    {
        b.ToTable("NotasFiscais");
        b.Property(x => x.Chave).HasMaxLength(44).IsFixedLength();
        b.Property(x => x.Serie).HasMaxLength(5);
        b.Property(x => x.Modelo).HasMaxLength(2);
        b.Property(x => x.EmitenteCnpj).HasMaxLength(14);
        b.Property(x => x.DestinatarioCnpj).HasMaxLength(14);
        b.Property(x => x.EmitenteNome).HasMaxLength(200);
        b.Property(x => x.DestinatarioNome).HasMaxLength(200);
        b.Property(x => x.PedidosCitados).HasMaxLength(200);
        b.Property(x => x.InformacoesComplementares).HasMaxLength(4000);
        b.Property(x => x.VinculoDetalhe).HasMaxLength(500);
        b.Property(x => x.AnaliseIa).HasMaxLength(8000);
        b.Property(x => x.ValorTotal).HasPrecision(18, 2);
        b.Property(x => x.ValorFrete).HasPrecision(18, 2);
        // A mesma nota não entra duas vezes na empresa (SIEG traz de novo, arquivo reenviado).
        b.HasIndex(x => new { x.EmpresaId, x.Chave }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.EmpresaId, x.Situacao, x.Emissao });
        b.HasIndex(x => x.PedidoId);
        b.HasOne<Pedido>().WithMany().HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Recebimento>().WithMany().HasForeignKey(x => x.RecebimentoId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Itens).WithOne().HasForeignKey(i => i.NotaFiscalId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class NotaFiscalItemCfg : IEntityTypeConfiguration<NotaFiscalItem>
{
    public void Configure(EntityTypeBuilder<NotaFiscalItem> b)
    {
        b.ToTable("NotaFiscalItens");
        b.Property(x => x.Codigo).HasMaxLength(60);
        b.Property(x => x.Descricao).HasMaxLength(500);
        b.Property(x => x.Ncm).HasMaxLength(8);
        b.Property(x => x.Cfop).HasMaxLength(4);
        b.Property(x => x.Unidade).HasMaxLength(10);
        b.Property(x => x.PedidoCitado).HasMaxLength(30);
        b.Property(x => x.ItemPedidoCitado).HasMaxLength(10);
        b.Property(x => x.Quantidade).HasPrecision(18, 4);
        b.Property(x => x.ValorUnitario).HasPrecision(18, 6);
        b.Property(x => x.Valor).HasPrecision(18, 2);
        b.Property(x => x.Confianca).HasPrecision(5, 2);
        b.HasIndex(x => x.NotaFiscalId);
        b.HasOne<PedidoItem>().WithMany().HasForeignKey(x => x.PedidoItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ImportacaoNotaCfg : IEntityTypeConfiguration<ImportacaoNota>
{
    public void Configure(EntityTypeBuilder<ImportacaoNota> b)
    {
        b.ToTable("ImportacoesNota");
        b.Property(x => x.Cnpj).HasMaxLength(14);
        b.Property(x => x.Detalhe).HasMaxLength(2000);
        b.HasIndex(x => new { x.EmpresaId, x.CriadoEm });
    }
}

internal sealed class UsoIaCfg : IEntityTypeConfiguration<UsoIa>
{
    public void Configure(EntityTypeBuilder<UsoIa> b)
    {
        b.ToTable("UsosIa");
        b.Property(x => x.Funcao).HasMaxLength(40);
        b.HasIndex(x => new { x.EmpresaId, x.CriadoEm });
    }
}
