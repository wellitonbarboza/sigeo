using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;

namespace QualyHub.Application.Comum;

/// <summary>Checagem de perfil no serviço (a tela esconde o botão; o serviço garante a regra).</summary>
public static class Permissoes
{
    public static async Task ExigirAsync(IQualyHubDb db, IUsuarioAtual usuario, string acao, params Domain.Common.PerfilUsuario[] perfis)
    {
        if (usuario.UsuarioId is not { } id) return; // rotinas de sistema/importação
        var p = await db.Usuarios.AsNoTracking().Where(u => u.Id == id).Select(u => (Domain.Common.PerfilUsuario?)u.Perfil).FirstOrDefaultAsync();
        if (p is Domain.Common.PerfilUsuario.Administrador || (p is { } x && perfis.Contains(x))) return;
        throw new Domain.Aprovacoes.RegraDeNegocioException($"Seu perfil não permite {acao}.");
    }

    /// <summary>Confere a ação na tela pelo perfil de acesso do usuário (telas liberadas para a construtora + perfil + exceções).</summary>
    public static async Task ExigirTelaAsync(IQualyHubDb db, IUsuarioAtual usuario, string rota, char acao, CancellationToken ct = default)
    {
        if (usuario.UsuarioId is not { } id) return;
        Servicos.AcessoServico.Conferir(await Servicos.AcessoServico.CalcularAsync(db, id, ct), rota, acao);
    }
}

/// <summary>Numeração sequencial por empresa/chave, com concorrência otimista (RowVersion) e nova tentativa.</summary>
public static class Numerador
{
    public static async Task<int> ProximoAsync(IQualyHubDb db, string chave, CancellationToken ct = default)
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
    public static async Task<int> ProximoAPartirDeAsync(IQualyHubDb db, string chave, Func<Task<int>> ultimoExistente, CancellationToken ct = default)
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
    public static async Task<ParametrosVigentes> CarregarAsync(IQualyHubDb db, CancellationToken ct = default) =>
        new(await db.Parametros.AsNoTracking().ToDictionaryAsync(p => p.Chave, p => p.Valor, ct));
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
    public static async Task<T> SalvarAsync<T>(IQualyHubDb db, T novo, params string[] colecoes) where T : Entidade
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

        var filhosMudaram = false;
        foreach (var c in colecoes)
        {
            var colAtual = entry.Collection(c);
            var itensAtuais = ((System.Collections.IEnumerable)colAtual.CurrentValue!).Cast<Entidade>().ToList();
            var itensNovos = Filhos(novo, c).ToList();
            var lista = colAtual.CurrentValue!;
            var add = lista.GetType().GetMethod("Add")!;
            var novosPorId = itensNovos.ToDictionary(n => n.Id);
            foreach (var a in itensAtuais.Where(a => !novosPorId.ContainsKey(a.Id)))
            { db.Remove(a); filhosMudaram = true; }
            var atuaisPorId = itensAtuais.ToDictionary(a => a.Id);
            foreach (var n in itensNovos)
            {
                if (atuaisPorId.TryGetValue(n.Id, out var existente))
                {
                    var e = db.Entry(existente);
                    e.CurrentValues.SetValues(n);
                    filhosMudaram |= e.State == EntityState.Modified;
                }
                else { LimparReferencias(db, n); add.Invoke(lista, [n]); db.Entry(n).State = EntityState.Added; filhosMudaram = true; }
            }
        }
        // Só os itens mudaram: marca o agregado como alterado para o RowVersion ser conferido (dois usuários editando os itens
        // do mesmo documento não se sobrescrevem em silêncio) e a auditoria registrar quem alterou.
        if (filhosMudaram && rowVersion is not null && entry.State == EntityState.Unchanged) entry.State = EntityState.Modified;
        return atual;
    }

    private static IEnumerable<Entidade> Filhos(object pai, string colecao) =>
        ((System.Collections.IEnumerable?)pai.GetType().GetProperty(colecao)!.GetValue(pai) ?? Array.Empty<Entidade>()).Cast<Entidade>();

    /// <summary>Evita que objetos de referência (Produto, Fornecedor…) carregados na tela sejam inseridos de novo.</summary>
    private static void LimparReferencias(IQualyHubDb db, object entidade)
    {
        var tipo = db.Model.FindEntityType(entidade.GetType());
        if (tipo is null) return;
        foreach (var nav in tipo.GetNavigations().Where(n => !n.IsCollection && n.IsOnDependent))
            if (nav.PropertyInfo?.CanWrite == true) nav.PropertyInfo.SetValue(entidade, null);
    }
}
