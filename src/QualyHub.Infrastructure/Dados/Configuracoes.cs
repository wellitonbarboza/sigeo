using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Importacao;
using QualyHub.Domain.Medicoes;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Sgq;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Infrastructure.Dados;

internal static class Texto
{
    public static PropertyBuilder<T> Longo<T>(this PropertyBuilder<T> p) => p.HasColumnType("nvarchar(max)");
}

internal sealed class EmpresaCfg : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> b)
    {
        b.Property(x => x.Cnpj).HasMaxLength(18);
        b.Property(x => x.NivelSiac).HasMaxLength(2);
        b.HasIndex(x => x.Cnpj).IsUnique();
    }
}

internal sealed class ObraCfg : IEntityTypeConfiguration<Obra>
{
    public void Configure(EntityTypeBuilder<Obra> b)
    {
        b.Property(x => x.Codigo).HasMaxLength(30);
        b.Property(x => x.Uf).HasMaxLength(2);
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasMany(x => x.Locais).WithOne().HasForeignKey(l => l.ObraId);
    }
}

internal sealed class LocalCfg : IEntityTypeConfiguration<Local>
{
    public void Configure(EntityTypeBuilder<Local> b)
    {
        b.HasOne(x => x.LocalPai).WithMany(x => x.Filhos).HasForeignKey(x => x.LocalPaiId);
    }
}

internal sealed class UsuarioCfg : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.HasIndex(x => x.EntraObjectId).IsUnique();
        b.Property(x => x.Email).HasMaxLength(200);
        b.HasMany(x => x.Obras).WithOne(x => x.Usuario).HasForeignKey(x => x.UsuarioId);
        b.HasMany(x => x.LimitesAprovacao).WithOne().HasForeignKey(x => x.UsuarioId);
    }
}

internal sealed class UsuarioObraCfg : IEntityTypeConfiguration<UsuarioObra>
{
    public void Configure(EntityTypeBuilder<UsuarioObra> b) => b.HasIndex(x => new { x.UsuarioId, x.ObraId }).IsUnique();
}

internal sealed class FornecedorCfg : IEntityTypeConfiguration<Fornecedor>
{
    public void Configure(EntityTypeBuilder<Fornecedor> b)
    {
        b.Property(x => x.CpfCnpj).HasMaxLength(18);
        b.Property(x => x.Uf).HasMaxLength(2);
        b.HasIndex(x => new { x.EmpresaId, x.CpfCnpj }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasMany(x => x.Documentos).WithOne(x => x.Fornecedor).HasForeignKey(x => x.FornecedorId);
        b.HasMany(x => x.Avaliacoes).WithOne(x => x.Fornecedor).HasForeignKey(x => x.FornecedorId);
    }
}

internal sealed class FornecedorDocumentoCfg : IEntityTypeConfiguration<FornecedorDocumento>
{
    public void Configure(EntityTypeBuilder<FornecedorDocumento> b)
    {
        b.HasOne(x => x.TipoDocumento).WithMany().HasForeignKey(x => x.TipoDocumentoExigidoId);
        b.Property(x => x.ArquivoHashSha256).HasMaxLength(64);
        b.HasIndex(x => x.DataValidade);
    }
}

internal sealed class FuncionarioCfg : IEntityTypeConfiguration<Funcionario>
{
    public void Configure(EntityTypeBuilder<Funcionario> b)
    {
        b.Property(x => x.CpfHash).HasMaxLength(64);
        b.Property(x => x.CpfMascarado).HasMaxLength(14);
        b.HasIndex(x => new { x.EmpresaId, x.CpfHash }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasMany(x => x.Documentos).WithOne(x => x.Funcionario).HasForeignKey(x => x.FuncionarioId);
        b.HasMany(x => x.Obras).WithOne(x => x.Funcionario).HasForeignKey(x => x.FuncionarioId);
    }
}

internal sealed class FuncionarioDocumentoCfg : IEntityTypeConfiguration<FuncionarioDocumento>
{
    public void Configure(EntityTypeBuilder<FuncionarioDocumento> b)
    {
        b.HasOne(x => x.TipoDocumento).WithMany().HasForeignKey(x => x.TipoDocumentoExigidoId);
        b.Property(x => x.ArquivoHashSha256).HasMaxLength(64);
        b.HasIndex(x => x.DataValidade);
    }
}

internal sealed class ServicoCfg : IEntityTypeConfiguration<ServicoControlado>
{
    public void Configure(EntityTypeBuilder<ServicoControlado> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<DocumentoSgq>().WithMany().HasForeignKey(x => x.DocumentoPeisId);
        b.HasOne<FormModelo>().WithMany().HasForeignKey(x => x.FormModeloFvsId);
    }
}

internal sealed class MaterialCfg : IEntityTypeConfiguration<MaterialControlado>
{
    public void Configure(EntityTypeBuilder<MaterialControlado> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.Property(x => x.CodigosInsumo).Longo();
        b.HasOne<FormModelo>().WithMany().HasForeignKey(x => x.FormModeloFvmId);
    }
}

internal sealed class PedidoCfg : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.Property(x => x.Bloqueios).Longo();
        b.HasMany(x => x.Itens).WithOne(x => x.Pedido).HasForeignKey(x => x.PedidoId);
    }
}

internal sealed class PedidoItemCfg : IEntityTypeConfiguration<PedidoItem>
{
    public void Configure(EntityTypeBuilder<PedidoItem> b)
    {
        b.HasOne(x => x.MaterialControlado).WithMany().HasForeignKey(x => x.MaterialControladoId);
        b.HasIndex(x => x.CodigoInsumo);
    }
}

internal sealed class MedicaoCfg : IEntityTypeConfiguration<Medicao>
{
    public void Configure(EntityTypeBuilder<Medicao> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.Property(x => x.Bloqueios).Longo();
        b.HasMany(x => x.Itens).WithOne(x => x.Medicao).HasForeignKey(x => x.MedicaoId);
    }
}

internal sealed class MedicaoItemCfg : IEntityTypeConfiguration<MedicaoItem>
{
    public void Configure(EntityTypeBuilder<MedicaoItem> b)
    {
        b.HasOne(x => x.ServicoControlado).WithMany().HasForeignKey(x => x.ServicoControladoId);
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId);
    }
}

internal sealed class FormModeloCfg : IEntityTypeConfiguration<FormModelo>
{
    public void Configure(EntityTypeBuilder<FormModelo> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Codigo, x.Versao }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasMany(x => x.Itens).WithOne(x => x.FormModelo).HasForeignKey(x => x.FormModeloId);
        b.HasOne<DocumentoSgq>().WithMany().HasForeignKey(x => x.DocumentoSgqOrigemId);
    }
}

internal sealed class FormItemCfg : IEntityTypeConfiguration<FormItem>
{
    public void Configure(EntityTypeBuilder<FormItem> b)
    {
        b.Property(x => x.ItemVerificacao).HasMaxLength(1000);
        b.Property(x => x.CriterioAceitacao).HasMaxLength(2000);
        b.Property(x => x.MetodoVerificacao).HasMaxLength(1000);
    }
}

internal sealed class FichaCfg : IEntityTypeConfiguration<Ficha>
{
    public void Configure(EntityTypeBuilder<Ficha> b)
    {
        // Id vem do dispositivo (Guid v7) → sincronização idempotente.
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.ObraId, x.Tipo, x.Status });
        b.HasIndex(x => new { x.ServicoControladoId, x.LocalId });
        b.HasIndex(x => x.PedidoItemId);
        b.HasOne(x => x.FormModelo).WithMany().HasForeignKey(x => x.FormModeloId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasOne<ServicoControlado>().WithMany().HasForeignKey(x => x.ServicoControladoId);
        b.HasOne<MaterialControlado>().WithMany().HasForeignKey(x => x.MaterialControladoId);
        b.HasOne<Local>().WithMany().HasForeignKey(x => x.LocalId);
        b.HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.PrestadorFornecedorId);
        b.HasOne<Pedido>().WithMany().HasForeignKey(x => x.PedidoId);
        b.HasOne<PedidoItem>().WithMany().HasForeignKey(x => x.PedidoItemId);
        b.HasOne<Ficha>().WithMany().HasForeignKey(x => x.FichaOrigemReinspecaoId);
        b.Property(x => x.Observacao).Longo();
        b.HasMany(x => x.Respostas).WithOne(x => x.Ficha).HasForeignKey(x => x.FichaId);
        b.HasMany(x => x.Evidencias).WithOne(x => x.Ficha).HasForeignKey(x => x.FichaId);
    }
}

internal sealed class FichaRespostaCfg : IEntityTypeConfiguration<FichaResposta>
{
    public void Configure(EntityTypeBuilder<FichaResposta> b)
    {
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasOne<FormItem>().WithMany().HasForeignKey(x => x.FormItemId);
        b.HasIndex(x => new { x.FichaId, x.FormItemId }).IsUnique();
        b.Property(x => x.ValorTexto).HasMaxLength(2000);
        b.Property(x => x.Observacao).HasMaxLength(2000);
    }
}

internal sealed class FichaEvidenciaCfg : IEntityTypeConfiguration<FichaEvidencia>
{
    public void Configure(EntityTypeBuilder<FichaEvidencia> b)
    {
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.ArquivoHashSha256).HasMaxLength(64);
        b.HasOne<FormItem>().WithMany().HasForeignKey(x => x.FormItemId);
    }
}

internal sealed class RncCfg : IEntityTypeConfiguration<Rnc>
{
    public void Configure(EntityTypeBuilder<Rnc> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasOne<Ficha>().WithMany().HasForeignKey(x => x.FichaId);
        b.HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId);
        b.Property(x => x.Descricao).Longo();
        b.Property(x => x.AnaliseCausa).Longo();
        b.Property(x => x.AcaoCorretiva).Longo();
        b.Property(x => x.VerificacaoEficacia).Longo();
    }
}

internal sealed class DocumentoSgqCfg : IEntityTypeConfiguration<DocumentoSgq>
{
    public void Configure(EntityTypeBuilder<DocumentoSgq> b)
    {
        b.Property(x => x.Codigo).HasMaxLength(30);
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasMany(x => x.Versoes).WithOne(x => x.Documento).HasForeignKey(x => x.DocumentoSgqId);
    }
}

internal sealed class VersaoDocumentoSgqCfg : IEntityTypeConfiguration<VersaoDocumentoSgq>
{
    public void Configure(EntityTypeBuilder<VersaoDocumentoSgq> b)
    {
        b.HasIndex(x => new { x.DocumentoSgqId, x.Revisao }).IsUnique();
        b.Property(x => x.DescricaoAlteracao).Longo();
        b.Property(x => x.ArquivoHashSha256).HasMaxLength(64);
        b.HasMany(x => x.Distribuicoes).WithOne().HasForeignKey(x => x.VersaoDocumentoSgqId);
    }
}

internal sealed class SolicitacaoAprovacaoCfg : IEntityTypeConfiguration<SolicitacaoAprovacao>
{
    public void Configure(EntityTypeBuilder<SolicitacaoAprovacao> b)
    {
        b.HasIndex(x => new { x.Entidade, x.EntidadeId });
        b.HasIndex(x => new { x.EmpresaId, x.Status });
        b.Ignore(x => x.Pendente);
        b.Property(x => x.SolicitanteNome).HasMaxLength(150);
        b.HasIndex(x => x.SolicitanteUsuarioId);
        b.HasMany(x => x.Etapas).WithOne(x => x.Solicitacao).HasForeignKey(x => x.SolicitacaoAprovacaoId);
    }
}

internal sealed class EtapaAprovacaoCfg : IEntityTypeConfiguration<EtapaAprovacao>
{
    public void Configure(EntityTypeBuilder<EtapaAprovacao> b)
    {
        b.Property(x => x.Justificativa).HasMaxLength(2000);
        b.Property(x => x.EmNomeDeNome).HasMaxLength(150);
        b.HasIndex(x => new { x.PerfilAprovador, x.Decisao });
        b.HasIndex(x => x.UsuarioAprovadorId);
        b.HasOne<DelegacaoAprovacao>().WithMany().HasForeignKey(x => x.DelegacaoAprovacaoId);
    }
}

internal sealed class LimiteAprovacaoCfg : IEntityTypeConfiguration<LimiteAprovacao>
{
    public void Configure(EntityTypeBuilder<LimiteAprovacao> b)
    {
        // Um limite ativo por usuário × tipo × obra (obra nula = geral).
        b.HasIndex(x => new { x.UsuarioId, x.Entidade, x.ObraId }).IsUnique().HasFilter("[Excluido] = 0 AND [Ativo] = 1");
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
    }
}

internal sealed class DelegacaoAprovacaoCfg : IEntityTypeConfiguration<DelegacaoAprovacao>
{
    public void Configure(EntityTypeBuilder<DelegacaoAprovacao> b)
    {
        b.Property(x => x.Motivo).HasMaxLength(500);
        b.HasIndex(x => new { x.SubstitutoUsuarioId, x.Inicio, x.Fim });
        b.HasIndex(x => x.TitularUsuarioId);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.TitularUsuarioId);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.SubstitutoUsuarioId);
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.ToTable(t => t.HasCheckConstraint("CK_Delegacao_Pessoas", "[TitularUsuarioId] <> [SubstitutoUsuarioId]"));
        b.ToTable(t => t.HasCheckConstraint("CK_Delegacao_Periodo", "[Fim] > [Inicio]"));
    }
}

internal sealed class LayoutImportacaoCfg : IEntityTypeConfiguration<LayoutImportacao>
{
    public void Configure(EntityTypeBuilder<LayoutImportacao> b)
    {
        b.Property(x => x.MapeamentoJson).Longo();
        b.Property(x => x.Delimitador).HasMaxLength(3);
    }
}

internal sealed class LoteImportacaoCfg : IEntityTypeConfiguration<LoteImportacao>
{
    public void Configure(EntityTypeBuilder<LoteImportacao> b)
    {
        b.Property(x => x.ArquivoHashSha256).HasMaxLength(64);
        b.HasIndex(x => new { x.EmpresaId, x.ArquivoHashSha256 });   // detecta o mesmo arquivo importado duas vezes
        b.HasMany(x => x.Linhas).WithOne().HasForeignKey(x => x.LoteImportacaoId);
    }
}

internal sealed class LinhaImportacaoCfg : IEntityTypeConfiguration<LinhaImportacao>
{
    public void Configure(EntityTypeBuilder<LinhaImportacao> b)
    {
        b.Property(x => x.DadosJson).Longo();
        b.Property(x => x.Erros).Longo();
        b.Property(x => x.ChaveNatural).HasMaxLength(100);
    }
}

internal sealed class RegistroAuditoriaCfg : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> b)
    {
        b.ToTable("Auditoria");
        b.Property(x => x.ValoresAnterioresJson).Longo();
        b.Property(x => x.ValoresNovosJson).Longo();
        b.Property(x => x.CamposAlterados).Longo();
        b.Property(x => x.Entidade).HasMaxLength(100);
        b.Property(x => x.EntidadeId).HasMaxLength(50);
        b.Property(x => x.Operacao).HasMaxLength(20);
        b.HasIndex(x => new { x.Entidade, x.EntidadeId });
        b.HasIndex(x => x.Quando);
    }
}
