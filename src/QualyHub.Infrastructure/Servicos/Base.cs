using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Infrastructure.Contexto;
using QualyHub.Infrastructure.Dados;

namespace QualyHub.Infrastructure.Servicos;

/// <summary>
/// Cria um DbContext por operação (padrão recomendado no Blazor Server: o circuito vive muito, o contexto não).
/// As opções são do escopo do circuito — o interceptor de auditoria enxerga o usuário logado.
/// </summary>
public sealed class FabricaDb(DbContextOptions<QualyHubDbContext> opcoes, IUsuarioAtual usuario)
{
    public QualyHubDbContext Criar() => new(opcoes, usuario);
}

/// <summary>Numeração sequencial por empresa/chave, com concorrência otimista (RowVersion) e nova tentativa.</summary>
public static class Numerador
{
    public static async Task<int> ProximoAsync(QualyHubDbContext db, string chave, CancellationToken ct = default)
    {
        var seq = db.Sequencias.Local.FirstOrDefault(s => s.Chave == chave && s.EmpresaId == db.EmpresaAtualId)
                  ?? await db.Sequencias.FirstOrDefaultAsync(s => s.EmpresaId == db.EmpresaAtualId && s.Chave == chave, ct);
        if (seq is null)
        {
            seq = new Sequencia { EmpresaId = db.EmpresaAtualId, Chave = chave };
            db.Sequencias.Add(seq);
        }
        return seq.Proximo();
    }

    /// <summary>Como <see cref="ProximoAsync"/>, mas, se a sequência ainda não existe, começa do maior número já emitido (ex.: último código da categoria).</summary>
    public static async Task<int> ProximoAPartirDeAsync(QualyHubDbContext db, string chave, Func<Task<int>> ultimoExistente, CancellationToken ct = default)
    {
        var seq = db.Sequencias.Local.FirstOrDefault(s => s.Chave == chave && s.EmpresaId == db.EmpresaAtualId)
                  ?? await db.Sequencias.FirstOrDefaultAsync(s => s.EmpresaId == db.EmpresaAtualId && s.Chave == chave, ct);
        if (seq is null)
        {
            seq = new Sequencia { EmpresaId = db.EmpresaAtualId, Chave = chave, Ultimo = await ultimoExistente() };
            db.Sequencias.Add(seq);
        }
        return seq.Proximo();
    }
}

public static class ParametrosServico
{
    public static async Task<ParametrosVigentes> CarregarAsync(QualyHubDbContext db, CancellationToken ct = default) =>
        new(await db.Parametros.AsNoTracking().ToDictionaryAsync(p => p.Chave, p => p.Valor, ct));
}

/// <summary>Arquivo salvo: caminho no armazenamento + SHA-256 (prova de integridade).</summary>
public sealed record ArquivoSalvo(string Blob, string Hash, long Tamanho, string NomeOriginal, string? ContentType);

/// <summary>Armazenamento de arquivos. Produção: Azure Blob Storage (Managed Identity). Desenvolvimento: pasta local.</summary>
public interface IArmazenamento
{
    Task<ArquivoSalvo> SalvarAsync(Stream conteudo, string nomeOriginal, string pasta, string? contentType, CancellationToken ct = default);
    Task<Stream> AbrirAsync(string blob, CancellationToken ct = default);
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

/// <summary>Erros de validação para mostrar na tela (lista legível).</summary>
public sealed class ValidacaoException(IReadOnlyList<string> erros) : Exception(string.Join(" | ", erros))
{
    public IReadOnlyList<string> Erros { get; } = erros;

    public static void Se(IReadOnlyList<string> erros)
    {
        if (erros.Count > 0) throw new ValidacaoException(erros);
    }
}

public static class Token
{
    /// <summary>Token aleatório de 256 bits em base64url; guarde só o hash.</summary>
    public static string Novo() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}

/// <summary>
/// Salva um agregado editado fora do contexto (tela): carrega o atual, copia os valores simples e sincroniza as coleções
/// (inclui novos, atualiza existentes, exclui os removidos). Referências de navegação dos filhos são ignoradas (só as FKs contam).
/// </summary>
public static class Grafo
{
    public static async Task<T> SalvarAsync<T>(QualyHubDbContext db, T novo, params string[] colecoes) where T : Entidade
    {
        IQueryable<T> q = db.Set<T>();
        foreach (var c in colecoes) q = q.Include(c);
        var atual = await q.FirstOrDefaultAsync(e => e.Id == novo.Id);
        if (atual is null)
        {
            foreach (var c in colecoes) foreach (var f in Filhos(novo, c)) LimparReferencias(db, f);
            LimparReferencias(db, novo);
            db.Add(novo);
            return novo;
        }

        var entry = db.Entry(atual);
        var rowVersion = entry.Metadata.FindProperty(nameof(EntidadeAuditavel.RowVersion));
        entry.CurrentValues.SetValues(novo);
        if (rowVersion is not null) entry.Property(rowVersion.Name).OriginalValue = db.Entry(novo).Property(rowVersion.Name).CurrentValue;

        foreach (var c in colecoes)
        {
            var colAtual = entry.Collection(c);
            var itensAtuais = ((System.Collections.IEnumerable)colAtual.CurrentValue!).Cast<Entidade>().ToList();
            var itensNovos = Filhos(novo, c).ToList();
            var lista = colAtual.CurrentValue!;
            var add = lista.GetType().GetMethod("Add")!;
            foreach (var a in itensAtuais.Where(a => itensNovos.All(n => n.Id != a.Id)))
                db.Remove(a);
            foreach (var n in itensNovos)
            {
                var existente = itensAtuais.FirstOrDefault(a => a.Id == n.Id);
                if (existente is not null) db.Entry(existente).CurrentValues.SetValues(n);
                else { LimparReferencias(db, n); add.Invoke(lista, [n]); db.Entry(n).State = EntityState.Added; }
            }
        }
        return atual;
    }

    private static IEnumerable<Entidade> Filhos(object pai, string colecao) =>
        ((System.Collections.IEnumerable?)pai.GetType().GetProperty(colecao)!.GetValue(pai) ?? Array.Empty<Entidade>()).Cast<Entidade>();

    /// <summary>Evita que objetos de referência (Produto, Fornecedor…) carregados na tela sejam inseridos de novo.</summary>
    private static void LimparReferencias(QualyHubDbContext db, object entidade)
    {
        var tipo = db.Model.FindEntityType(entidade.GetType());
        if (tipo is null) return;
        foreach (var nav in tipo.GetNavigations().Where(n => !n.IsCollection && n.IsOnDependent))
            if (nav.PropertyInfo?.CanWrite == true) nav.PropertyInfo.SetValue(entidade, null);
    }
}
