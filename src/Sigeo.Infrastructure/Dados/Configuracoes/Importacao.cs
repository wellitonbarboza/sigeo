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

internal sealed class VinculoOrigemCfg : IEntityTypeConfiguration<Domain.Importacao.VinculoOrigem>
{
    public void Configure(EntityTypeBuilder<Domain.Importacao.VinculoOrigem> b)
    {
        b.ToTable("VinculosOrigem");
        b.Property(x => x.Sistema).HasMaxLength(40);
        b.Property(x => x.Chave).HasMaxLength(300);
        b.Property(x => x.TextoOrigem).HasMaxLength(400);
        b.Property(x => x.DestinoTexto).HasMaxLength(120);
        b.HasIndex(x => new { x.EmpresaId, x.Sistema, x.Tipo, x.ObraId, x.Chave }).IsUnique().HasFilter("[Excluido] = 0");
    }
}
