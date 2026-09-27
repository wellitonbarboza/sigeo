using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Common;
using QualyHub.Infrastructure.Servicos;

namespace QualyHub.Web.Servicos;

public sealed record Opcao(Guid Id, string Texto, string? Extra = null);

/// <summary>Listas para os combos das telas (cache curto por circuito).</summary>
public sealed class Catalogos(FabricaDb fabrica)
{
    private readonly Dictionary<string, (DateTime Em, object Valor)> _cache = [];

    private async Task<T> Cache<T>(string chave, Func<Task<T>> carregar)
    {
        if (_cache.TryGetValue(chave, out var c) && DateTime.UtcNow - c.Em < TimeSpan.FromSeconds(30)) return (T)c.Valor;
        var v = await carregar();
        _cache[chave] = (DateTime.UtcNow, v!);
        return v;
    }

    public void Invalidar() => _cache.Clear();

    public Task<List<string>> ListaAsync(TipoLista tipo) => Cache($"lista:{tipo}", async () =>
    {
        await using var db = fabrica.Criar();
        return await db.Listas.AsNoTracking().Where(l => l.Tipo == tipo && l.Ativo).OrderBy(l => l.Ordem).ThenBy(l => l.Nome).Select(l => l.Nome).ToListAsync();
    });

    public Task<List<Opcao>> ObrasAsync() => Cache("obras", async () =>
    {
        await using var db = fabrica.Criar();
        return await db.Obras.AsNoTracking().Where(o => o.Ativa).OrderBy(o => o.Nome).Select(o => new Opcao(o.Id, o.Nome, o.Codigo)).ToListAsync();
    });

    public Task<List<Opcao>> CentrosCustoAsync() => Cache("cc", async () =>
    {
        await using var db = fabrica.Criar();
        return await db.CentrosCusto.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.Nome).Select(o => new Opcao(o.Id, o.Nome, o.Codigo)).ToListAsync();
    });

    public Task<List<Opcao>> FornecedoresAsync() => Cache("forn", async () =>
    {
        await using var db = fabrica.Criar();
        return await db.Fornecedores.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.RazaoSocial)
            .Select(o => new Opcao(o.Id, o.RazaoSocial, o.StatusQualificacao.ToString())).ToListAsync();
    });

    public Task<List<Opcao>> MateriaisAsync() => Cache("mat", async () =>
    {
        await using var db = fabrica.Criar();
        return await db.MateriaisControlados.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.Codigo).Select(o => new Opcao(o.Id, o.Codigo + " – " + o.Nome, o.Codigo)).ToListAsync();
    });

    public Task<List<Opcao>> ServicosAsync() => Cache("srv", async () =>
    {
        await using var db = fabrica.Criar();
        return await db.ServicosControlados.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.Codigo).Select(o => new Opcao(o.Id, o.Codigo + " – " + o.Nome, o.Codigo)).ToListAsync();
    });

    public Task<List<Opcao>> ProdutosAsync() => Cache("prod", async () =>
    {
        await using var db = fabrica.Criar();
        return await db.Produtos.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.Nome).Select(o => new Opcao(o.Id, o.Nome, o.Unidade)).ToListAsync();
    });

    public Task<List<Opcao>> UsuariosAsync() => Cache("usr", async () =>
    {
        await using var db = fabrica.Criar();
        return await db.Usuarios.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.Nome).Select(o => new Opcao(o.Id, o.Nome, o.Funcao)).ToListAsync();
    });

    public Task<List<Opcao>> PastasAsync() => Cache("pastas", async () =>
    {
        await using var db = fabrica.Criar();
        return await db.Pastas.AsNoTracking().OrderBy(o => o.Ordem).Select(o => new Opcao(o.Id, o.Nome)).ToListAsync();
    });

    public Task<List<Opcao>> TiposDocFornecedorAsync() => Cache("tipodoc", async () =>
    {
        await using var db = fabrica.Criar();
        return await db.TiposDocumento.AsNoTracking().Where(o => o.Ativo && o.AplicaA == AplicaA.Fornecedor).OrderBy(o => o.Nome).Select(o => new Opcao(o.Id, o.Nome)).ToListAsync();
    });
}

/// <summary>Mensagens (toast) exibidas no canto da tela.</summary>
public sealed class Notificacoes
{
    public event Action? Mudou;
    public List<(Guid Id, string Texto, string Tipo)> Itens { get; } = [];

    public void Sucesso(string texto) => Add(texto, "ok");
    public void Erro(string texto) => Add(texto, "erro");
    public void Aviso(string texto) => Add(texto, "aviso");

    private void Add(string t, string tipo)
    {
        var id = Guid.NewGuid();
        Itens.Add((id, t, tipo));
        Mudou?.Invoke();
        _ = Task.Delay(tipo == "erro" ? 9000 : 4500).ContinueWith(_ => { Itens.RemoveAll(i => i.Id == id); Mudou?.Invoke(); });
    }
}
