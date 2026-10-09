using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualyHub.Domain.Colaboracao;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Sst;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Infrastructure.Dados;

internal sealed class FvsCfg : IEntityTypeConfiguration<Fvs>
{
    public void Configure(EntityTypeBuilder<Fvs> b)
    {
        b.ToTable("Fvs");
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.LocalId, x.ServicoControladoId, x.Status });
        b.Property(x => x.Numero).HasMaxLength(20);
    }
}

internal sealed class ModeloInspecaoCfg : IEntityTypeConfiguration<ModeloInspecao>
{
    public void Configure(EntityTypeBuilder<ModeloInspecao> b) =>
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
}

internal sealed class RncCfg : IEntityTypeConfiguration<Rnc>
{
    public void Configure(EntityTypeBuilder<Rnc> b)
    {
        b.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique().HasFilter("[Excluido] = 0");
        b.HasIndex(x => new { x.FornecedorId, x.Status });
    }
}

internal sealed class EquipamentoCfg : IEntityTypeConfiguration<EquipamentoMedicao>
{
    public void Configure(EntityTypeBuilder<EquipamentoMedicao> b) =>
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique().HasFilter("[Excluido] = 0");
}

internal sealed class ColaboradorCfg : IEntityTypeConfiguration<Colaborador>
{
    public void Configure(EntityTypeBuilder<Colaborador> b)
    {
        b.Property(x => x.CpfHash).HasMaxLength(64);
        b.Property(x => x.CpfMascarado).HasMaxLength(20);
        b.HasIndex(x => new { x.ObraId, x.CpfHash });
    }
}

internal sealed class AditivoCfg : IEntityTypeConfiguration<Aditivo>
{
    public void Configure(EntityTypeBuilder<Aditivo> b) => b.HasIndex(x => new { x.ContratoId, x.Numero });
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

// ───────────── Mensagens internas e fila de avisos por e-mail ─────────────

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
        b.HasOne<QualyHub.Domain.Estrutura.Obra>().WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Restrict);
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
        b.HasOne<QualyHub.Domain.Estrutura.Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
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
