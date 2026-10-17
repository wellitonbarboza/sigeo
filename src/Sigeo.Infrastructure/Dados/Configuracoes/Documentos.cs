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

internal sealed class PastaCfg : IEntityTypeConfiguration<Pasta>
{
    public void Configure(EntityTypeBuilder<Pasta> b) => b.HasOne<Pasta>().WithMany().HasForeignKey(x => x.PastaPaiId);
}

internal sealed class DocumentoCfg : IEntityTypeConfiguration<Documento>
{
    public void Configure(EntityTypeBuilder<Documento> b)
    {
        b.Property(x => x.Codigo).HasMaxLength(40);
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne(x => x.Pasta).WithMany().HasForeignKey(x => x.PastaId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasOne<RegraRetencao>().WithMany().HasForeignKey(x => x.RegraRetencaoId);
        b.HasOne<DocumentoRevisao>().WithMany().HasForeignKey(x => x.RevisaoVigenteId);
        b.Ignore(x => x.RevisaoVigente);
        b.HasMany(x => x.Revisoes).WithOne(x => x.Documento).HasForeignKey(x => x.DocumentoId);
        b.HasMany(x => x.Relacionados).WithOne().HasForeignKey(x => x.DocumentoId);
        b.HasIndex(x => x.ProximaAnalise);
    }
}

internal sealed class DocumentoRelacionadoCfg : IEntityTypeConfiguration<DocumentoRelacionado>
{
    public void Configure(EntityTypeBuilder<DocumentoRelacionado> b)
    {
        b.HasOne<Documento>().WithMany().HasForeignKey(x => x.RelacionadoId);
        b.HasIndex(x => new { x.DocumentoId, x.RelacionadoId }).IsUnique();
    }
}

internal sealed class DocumentoRevisaoCfg : IEntityTypeConfiguration<DocumentoRevisao>
{
    public void Configure(EntityTypeBuilder<DocumentoRevisao> b)
    {
        b.HasIndex(x => new { x.DocumentoId, x.Revisao }).IsUnique().HasFilter("[Excluido] = 0");
        b.Property(x => x.HashSha256).HasMaxLength(64);
        b.Ignore(x => x.Rotulo); b.Ignore(x => x.EtapaAtual);
        b.HasMany(x => x.Etapas).WithOne(x => x.Revisao).HasForeignKey(x => x.DocumentoRevisaoId);
        b.HasMany(x => x.Copias).WithOne(x => x.Revisao).HasForeignKey(x => x.DocumentoRevisaoId);
    }
}

internal sealed class CopiaControladaCfg : IEntityTypeConfiguration<CopiaControlada>
{
    public void Configure(EntityTypeBuilder<CopiaControlada> b)
    {
        b.Property(x => x.NumeroCopia).HasMaxLength(60);
        b.HasIndex(x => new { x.EmpresaId, x.NumeroCopia }).IsUnique().HasFilter("[Excluido] = 0");
        b.Property(x => x.QrToken).HasMaxLength(32);
        b.HasIndex(x => x.QrToken).IsUnique();
        b.HasOne<SolicitacaoCopia>().WithMany().HasForeignKey(x => x.SolicitacaoCopiaId);
        b.HasOne<Grd>().WithMany().HasForeignKey(x => x.GrdId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
    }
}

internal sealed class SolicitacaoCopiaCfg : IEntityTypeConfiguration<SolicitacaoCopia>
{
    public void Configure(EntityTypeBuilder<SolicitacaoCopia> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.Ignore(x => x.QtdTotal);
        b.HasMany(x => x.Itens).WithOne().HasForeignKey(x => x.SolicitacaoCopiaId);
    }
}

internal sealed class SolicitacaoCopiaItemCfg : IEntityTypeConfiguration<SolicitacaoCopiaItem>
{
    public void Configure(EntityTypeBuilder<SolicitacaoCopiaItem> b) =>
        b.HasOne(x => x.Revisao).WithMany().HasForeignKey(x => x.DocumentoRevisaoId);
}

internal sealed class CienciaCfg : IEntityTypeConfiguration<Ciencia>
{
    public void Configure(EntityTypeBuilder<Ciencia> b)
    {
        b.HasOne(x => x.Revisao).WithMany().HasForeignKey(x => x.DocumentoRevisaoId);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId);
        b.HasIndex(x => new { x.DocumentoRevisaoId, x.UsuarioId }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class PermissaoDocumentoCfg : IEntityTypeConfiguration<PermissaoDocumento>
{
    public void Configure(EntityTypeBuilder<PermissaoDocumento> b)
    {
        b.HasIndex(x => new { x.Escopo, x.AlvoId });
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId);
    }
}

internal sealed class CheckoutCfg : IEntityTypeConfiguration<CheckoutDocumento>
{
    public void Configure(EntityTypeBuilder<CheckoutDocumento> b)
    {
        b.HasOne<DocumentoRevisao>().WithMany().HasForeignKey(x => x.DocumentoRevisaoId);
        b.Ignore(x => x.Ativo);
        // Só um check-out aberto por revisão (garantia no banco, além da regra no domínio).
        b.HasIndex(x => x.DocumentoRevisaoId).IsUnique().HasFilter("[CheckinEm] IS NULL AND [LiberadoForcadoPor] IS NULL AND [Excluido] = 0");
    }
}

internal sealed class EventoDocumentoCfg : IEntityTypeConfiguration<EventoDocumento>
{
    public void Configure(EntityTypeBuilder<EventoDocumento> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.DataHora });
        b.HasIndex(x => x.DocumentoId);
    }
}

internal sealed class DocumentoExternoCfg : IEntityTypeConfiguration<DocumentoExterno>
{
    public void Configure(EntityTypeBuilder<DocumentoExterno> b) =>
        b.HasIndex(x => new { x.EmpresaId, x.Identificacao, x.Edicao }).IsUnique().HasFilter("[Excluido] = 0");
}

internal sealed class GrdCfg : IEntityTypeConfiguration<Grd>
{
    public void Configure(EntityTypeBuilder<Grd> b)
    {
        b.Property(x => x.Numero).HasMaxLength(30);
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => x.TokenConfirmacao).IsUnique();
        b.HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasOne<Contrato>().WithMany().HasForeignKey(x => x.ContratoId);
        b.HasOne<Pedido>().WithMany().HasForeignKey(x => x.PedidoId);
        b.HasMany(x => x.Itens).WithOne().HasForeignKey(x => x.GrdId);
    }
}

internal sealed class GrdItemCfg : IEntityTypeConfiguration<GrdItem>
{
    public void Configure(EntityTypeBuilder<GrdItem> b) => b.HasOne(x => x.Revisao).WithMany().HasForeignKey(x => x.DocumentoRevisaoId);
}
