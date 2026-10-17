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

internal sealed class MetaCfg : IEntityTypeConfiguration<Meta>
{
    public void Configure(EntityTypeBuilder<Meta> b)
    {
        b.ToTable("Metas");
        b.Property(x => x.Nome).HasMaxLength(200);
        b.Property(x => x.Setor).HasMaxLength(80);
        b.Property(x => x.UsuarioNome).HasMaxLength(150);
        b.Property(x => x.EquipeNome).HasMaxLength(150);
        b.Property(x => x.Servico).HasMaxLength(150);
        b.Property(x => x.Unidade).HasMaxLength(20);
        b.HasIndex(x => new { x.EmpresaId, x.Inicio, x.Fim });
        b.HasMany(x => x.Apuracoes).WithOne().HasForeignKey(x => x.MetaId);
        b.Ignore(x => x.DonoNome);
    }
}

internal sealed class MetaApuracaoCfg : IEntityTypeConfiguration<MetaApuracao>
{
    public void Configure(EntityTypeBuilder<MetaApuracao> b)
    {
        b.ToTable("MetasApuracao");
        b.HasIndex(x => new { x.MetaId, x.Mes }).IsUnique();
        b.Property(x => x.ApuradoPor).HasMaxLength(150);
    }
}

internal sealed class NotaCfg : IEntityTypeConfiguration<Nota>
{
    public void Configure(EntityTypeBuilder<Nota> b)
    {
        b.Property(x => x.Entidade).HasMaxLength(60);
        b.HasIndex(x => new { x.Entidade, x.EntidadeId });
    }
}

internal sealed class TarefaCfg : IEntityTypeConfiguration<Tarefa>
{
    public void Configure(EntityTypeBuilder<Tarefa> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.ResponsavelId, x.Status });
        b.HasIndex(x => new { x.OrigemEntidade, x.OrigemId });
        b.Property(x => x.Titulo).HasMaxLength(200);
        b.Property(x => x.Tipo).HasMaxLength(60);
        b.Property(x => x.OrigemEntidade).HasMaxLength(60);
        b.Property(x => x.Status).HasMaxLength(20);
        b.Property(x => x.Prioridade).HasMaxLength(20);
    }
}

internal sealed class TarefaLeituraCfg : IEntityTypeConfiguration<TarefaLeitura>
{
    public void Configure(EntityTypeBuilder<TarefaLeitura> b) => b.HasIndex(x => new { x.TarefaId, x.UsuarioId }).IsUnique();
}

internal sealed class ConversaCfg : IEntityTypeConfiguration<Conversa>
{
    public void Configure(EntityTypeBuilder<Conversa> b)
    {
        b.ToTable("Conversas");
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.RegistroEntidade, x.RegistroId });
        b.Property(x => x.Assunto).HasMaxLength(Conversa.TamanhoAssunto);
        b.Property(x => x.RegistroEntidade).HasMaxLength(60);
        b.Property(x => x.RegistroDescricao).HasMaxLength(200);
        b.Property(x => x.RemetenteNome).HasMaxLength(150);
        b.HasOne<Sigeo.Domain.Estrutura.Obra>().WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Participantes).WithOne().HasForeignKey(p => p.ConversaId);
        b.HasMany(x => x.Mensagens).WithOne().HasForeignKey(m => m.ConversaId);
    }
}

internal sealed class ParticipanteConversaCfg : IEntityTypeConfiguration<ParticipanteConversa>
{
    public void Configure(EntityTypeBuilder<ParticipanteConversa> b)
    {
        b.ToTable("ParticipantesConversa");
        b.Property(x => x.Nome).HasMaxLength(150);
        b.HasIndex(x => new { x.ConversaId, x.UsuarioId }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.UsuarioId, x.Arquivada });
        b.HasOne<Sigeo.Domain.Estrutura.Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MensagemConversaCfg : IEntityTypeConfiguration<MensagemConversa>
{
    public void Configure(EntityTypeBuilder<MensagemConversa> b)
    {
        b.ToTable("MensagensConversa");
        b.Property(x => x.AutorNome).HasMaxLength(150);
        b.HasIndex(x => new { x.ConversaId, x.EnviadaEm });
        b.HasMany(x => x.Leituras).WithOne().HasForeignKey(l => l.MensagemId);
    }
}

internal sealed class LeituraMensagemCfg : IEntityTypeConfiguration<LeituraMensagem>
{
    public void Configure(EntityTypeBuilder<LeituraMensagem> b)
    {
        b.ToTable("LeiturasMensagem");
        b.Property(x => x.Nome).HasMaxLength(150);
        b.HasIndex(x => new { x.MensagemId, x.UsuarioId }).IsUnique();
    }
}

internal sealed class AvisoEmailCfg : IEntityTypeConfiguration<AvisoEmail>
{
    public void Configure(EntityTypeBuilder<AvisoEmail> b)
    {
        b.ToTable("AvisosEmail");
        b.Property(x => x.Evento).HasMaxLength(80);
        b.Property(x => x.Chave).HasMaxLength(200);
        b.Property(x => x.Para).HasMaxLength(200);
        b.Property(x => x.NomeDestinatario).HasMaxLength(150);
        b.Property(x => x.Assunto).HasMaxLength(250);
        b.Property(x => x.Link).HasMaxLength(400);
        b.HasIndex(x => new { x.EmpresaId, x.Chave }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.Situacao, x.ProximaTentativaEm });
    }
}
