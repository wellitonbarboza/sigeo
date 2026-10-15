using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Groundkey.Application.Abstracoes;
using Groundkey.Infrastructure.Dados;

namespace Groundkey.Infrastructure.Servicos;

/// <summary>
/// Cria um DbContext por operação (padrão recomendado no Blazor Server: o circuito vive muito, o contexto não).
/// As opções são do escopo do circuito — o interceptor de auditoria enxerga o usuário logado.
/// </summary>
public sealed class FabricaDb(DbContextOptions<GroundkeyDbContext> opcoes, IUsuarioAtual usuario) : IFabricaDb
{
    public IGroundkeyDb Criar() => new GroundkeyDbContext(opcoes, usuario);

    /// <summary>
    /// Contexto só de leitura: nada é rastreado (sem o snapshot de cada linha que o EF guarda para detectar alteração).
    /// Use em listagens e consultas de tela; para gravar, use <see cref="Criar"/>.
    /// </summary>
    public IGroundkeyDb CriarLeitura()
    {
        var db = new GroundkeyDbContext(opcoes, usuario);
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        return db;
    }
    public IUsuarioAtual Usuario => usuario;

    /// <summary>
    /// Executa várias gravações numa transação, compatível com a estratégia de nova tentativa do Azure SQL:
    /// cada tentativa usa um contexto novo (nada fica meio salvo) e só confirma se tudo der certo.
    /// </summary>
    public async Task EmTransacaoAsync(Func<IGroundkeyDb, Task> acao, CancellationToken ct = default)
    {
        await using var estrategia = Criar();
        await estrategia.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var db = Criar();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await acao(db);
            await tx.CommitAsync(ct);
        });
    }
}

public sealed class ArmazenamentoLocal(string raiz) : IArmazenamento
{
    public async Task<ArquivoSalvo> SalvarAsync(Stream conteudo, string nomeOriginal, string pasta, string? contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(nomeOriginal);
        var blob = Path.Combine(pasta, DateTime.UtcNow.ToString("yyyy/MM"), $"{Guid.CreateVersion7():N}{ext}").Replace('\\', '/');
        var destino = Path.Combine(raiz, blob);
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        await using var arq = File.Create(destino);
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long total = 0;
        int lidos;
        while ((lidos = await conteudo.ReadAsync(buffer, ct)) > 0)
        {
            sha.AppendData(buffer, 0, lidos);
            await arq.WriteAsync(buffer.AsMemory(0, lidos), ct);
            total += lidos;
        }
        return new ArquivoSalvo(blob, Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant(), total, nomeOriginal, contentType);
    }

    public Task<Stream> AbrirAsync(string blob, CancellationToken ct = default) =>
        Task.FromResult<Stream>(File.OpenRead(Path.Combine(raiz, blob)));
}

