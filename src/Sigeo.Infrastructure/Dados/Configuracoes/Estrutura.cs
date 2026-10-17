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

internal sealed class EmpresaCfg : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> b)
    {
        b.Property(x => x.Cnpj).HasMaxLength(18);
        b.Property(x => x.NivelSiac).HasMaxLength(2);
        b.HasIndex(x => x.Cnpj).IsUnique().HasFilter("[Cnpj] <> ''"); // construtora sem CNPJ informado ainda não colide com outra
        b.Property(x => x.ModeloEstrutura).HasMaxLength(30);
        b.Property(x => x.NomesNiveis).HasMaxLength(300);
        b.Property(x => x.TelasBloqueadas).HasMaxLength(2000);
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
        b.Property(x => x.DonoCusto).HasDefaultValue(DonoCustoObra.Spe).HasSentinel((DonoCustoObra)0);
        b.HasOne<Estabelecimento>().WithMany().HasForeignKey(x => x.EstabelecimentoDonoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LocalCfg : IEntityTypeConfiguration<Local>
{
    public void Configure(EntityTypeBuilder<Local> b)
    {
        b.HasOne(x => x.LocalPai).WithMany(x => x.Filhos).HasForeignKey(x => x.LocalPaiId);
        b.Property(x => x.FracaoIdeal).HasPrecision(18, 8);
        b.HasOne<Sigeo.Domain.Engenharia.Spe>().WithMany().HasForeignKey(x => x.SpeId);
        b.HasOne<Sigeo.Domain.Engenharia.Tipologia>().WithMany().HasForeignKey(x => x.TipologiaId);
        b.HasIndex(x => new { x.ObraId, x.SpeId, x.Tipo });
        b.Property(x => x.Categoria).HasMaxLength(60);
        b.Property(x => x.AreaPiso).HasPrecision(18, 4);
        b.Property(x => x.Perimetro).HasPrecision(18, 4);
        b.Property(x => x.PeDireito).HasPrecision(18, 4);
        b.Property(x => x.AreaParede).HasPrecision(18, 4);
        b.HasOne<Domain.Engenharia.TipologiaAmbiente>().WithMany().HasForeignKey(x => x.AmbienteModeloId);
        b.HasIndex(x => x.AmbienteModeloId);
    }
}

internal sealed class CredencialUsuarioCfg : IEntityTypeConfiguration<CredencialUsuario>
{
    public void Configure(EntityTypeBuilder<CredencialUsuario> b)
    {
        b.ToTable("CredenciaisUsuario");
        b.Property(x => x.SenhaHash).HasMaxLength(300);
        b.HasIndex(x => x.UsuarioId).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class UsuarioCfg : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.Property(x => x.Login).HasMaxLength(60);
        b.HasIndex(x => x.Login).IsUnique().HasFilter("[Excluido] = 0 AND [Login] <> ''");
        b.Property(x => x.Email).HasMaxLength(200);
        b.HasMany(x => x.Obras).WithOne(x => x.Usuario).HasForeignKey(x => x.UsuarioId);
        b.HasMany(x => x.LimitesAprovacao).WithOne().HasForeignKey(x => x.UsuarioId);
        b.HasIndex(x => x.ContaId);
        b.Property(x => x.ExcecoesAcesso).HasMaxLength(2000);
        b.Ignore(x => x.VeTodasObras);
        b.HasOne<PerfilAcesso>().WithMany().HasForeignKey(x => x.PerfilAcessoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PerfilAcessoCfg : IEntityTypeConfiguration<PerfilAcesso>
{
    public void Configure(EntityTypeBuilder<PerfilAcesso> b)
    {
        b.ToTable("PerfisAcesso");
        b.Property(x => x.Nome).HasMaxLength(80);
        b.Property(x => x.Regras).Metadata.SetMaxLength(null); // nvarchar(max): uma linha por tela
        b.HasIndex(x => new { x.EmpresaId, x.Nome }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class RegistroPlataformaCfg : IEntityTypeConfiguration<RegistroPlataforma>
{
    public void Configure(EntityTypeBuilder<RegistroPlataforma> b)
    {
        b.ToTable("RegistrosPlataforma");
        b.Property(x => x.Quem).HasMaxLength(150);
        b.Property(x => x.Acao).HasMaxLength(100);
        b.Property(x => x.Empresa).HasMaxLength(200);
        b.Property(x => x.Detalhe).HasMaxLength(1000).HasAnnotation(SigeoDbContext.TamanhoFixo, true);
        b.HasIndex(x => x.Quando);
    }
}

internal sealed class UsuarioObraCfg : IEntityTypeConfiguration<UsuarioObra>
{
    public void Configure(EntityTypeBuilder<UsuarioObra> b) => b.HasIndex(x => new { x.UsuarioId, x.ObraId }).IsUnique();
}

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

internal sealed class CentroCustoCfg : IEntityTypeConfiguration<CentroCusto>
{
    public void Configure(EntityTypeBuilder<CentroCusto> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasOne<Obra>().WithMany().HasForeignKey(x => x.ObraId);
        b.HasOne<CentroCusto>().WithMany().HasForeignKey(x => x.PaiId);
        b.Property(x => x.EstabelecimentosPermitidos).HasMaxLength(2000);
        b.Property(x => x.CodigoErp).HasMaxLength(60);
    }
}

internal sealed class ParametroCfg : IEntityTypeConfiguration<Parametro>
{
    public void Configure(EntityTypeBuilder<Parametro> b)
    {
        b.Property(x => x.Chave).HasMaxLength(60);
        b.HasIndex(x => new { x.EmpresaId, x.Chave }).IsUnique().HasFilter("[Excluido] = 0");
    }
}

internal sealed class ItemListaCfg : IEntityTypeConfiguration<ItemLista>
{
    public void Configure(EntityTypeBuilder<ItemLista> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Tipo, x.Nome }).IsUnique().HasFilter("[Excluido] = 0");
    }
}
