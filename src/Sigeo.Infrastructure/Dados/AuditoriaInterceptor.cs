using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sigeo.Domain.Common;

namespace Sigeo.Infrastructure.Dados;

/// <summary>
/// Preenche campos de auditoria, converte exclusão física em exclusão lógica e grava o log append-only.
/// Roda em toda gravação (Web, Campo, Importação, Functions) — a aplicação não tem como pular.
/// </summary>
public sealed class AuditoriaInterceptor(IUsuarioAtual usuario, TimeProvider relogio) : SaveChangesInterceptor
{
    // SenhaHash nunca vai para o log de auditoria (só o fato de ter mudado aparece pela data de alteração).
    private static readonly HashSet<string> Ignorados = [nameof(EntidadeAuditavel.RowVersion), "SenhaHash"];
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData e, InterceptionResult<int> r, CancellationToken ct = default)
    {
        if (e.Context is not null) Processar(e.Context);
        return base.SavingChangesAsync(e, r, ct);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData e, InterceptionResult<int> r)
    {
        if (e.Context is not null) Processar(e.Context);
        return base.SavingChanges(e, r);
    }

    private void Processar(DbContext ctx)
    {
        var agora = relogio.GetUtcNow();
        var registros = new List<RegistroAuditoria>();

        foreach (var entry in ctx.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is RegistroAuditoria || entry.State is EntityState.Detached or EntityState.Unchanged) continue;

            // Exclusão física → lógica (registro de qualidade não pode desaparecer).
            if (entry.State == EntityState.Deleted && entry.Entity is IExclusaoLogica soft)
            {
                entry.State = EntityState.Modified;
                soft.Excluido = true;
                soft.ExcluidoEm = agora;
                soft.ExcluidoPor = usuario.Nome;
            }

            if (entry.Entity is IAuditavel aud)
            {
                if (entry.State == EntityState.Added)
                {
                    aud.CriadoEm = agora;
                    aud.CriadoPor = usuario.Nome;
                }
                else if (entry.State == EntityState.Modified)
                {
                    aud.AlteradoEm = agora;
                    aud.AlteradoPor = usuario.Nome;
                    entry.Property(nameof(IAuditavel.CriadoEm)).IsModified = false;
                    entry.Property(nameof(IAuditavel.CriadoPor)).IsModified = false;
                }
            }

            if (entry.Entity is IPertenceEmpresa emp && entry.State == EntityState.Added && emp.EmpresaId == Guid.Empty)
                emp.EmpresaId = usuario.EmpresaId;

            registros.Add(Montar(entry, agora));
        }

        if (registros.Count > 0) ctx.Set<RegistroAuditoria>().AddRange(registros);
    }

    private RegistroAuditoria Montar(EntityEntry entry, DateTimeOffset agora)
    {
        var excluido = entry.Entity is IExclusaoLogica { Excluido: true } && entry.Property(nameof(IExclusaoLogica.Excluido)).IsModified;
        var operacao = entry.State switch
        {
            EntityState.Added => "Inclusao",
            EntityState.Deleted => "Exclusao",
            _ when excluido => "Exclusao",
            _ => "Alteracao"
        };

        var props = entry.Properties.Where(p => !Ignorados.Contains(p.Metadata.Name)).ToList();
        var alteradas = entry.State == EntityState.Modified ? props.Where(p => p.IsModified).ToList() : props;

        return new RegistroAuditoria
        {
            EmpresaId = (entry.Entity as IPertenceEmpresa)?.EmpresaId ?? usuario.EmpresaId,
            Quando = agora,
            Usuario = usuario.Nome,
            UsuarioId = usuario.UsuarioId?.ToString(),
            Entidade = entry.Metadata.ClrType.Name,
            EntidadeId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "",
            Operacao = operacao,
            ValoresAnterioresJson = entry.State == EntityState.Added ? null
                : JsonSerializer.Serialize(alteradas.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue), Json),
            ValoresNovosJson = entry.State == EntityState.Deleted ? null
                : JsonSerializer.Serialize(alteradas.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue), Json),
            CamposAlterados = entry.State == EntityState.Modified ? string.Join(",", alteradas.Select(p => p.Metadata.Name)) : null,
            Origem = usuario.Origem,
            CorrelationId = usuario.CorrelationId
        };
    }
}
