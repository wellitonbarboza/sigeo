using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Common;
using QualyHub.Infrastructure.Servicos;

namespace QualyHub.Web.Servicos;

public sealed record Opcao(Guid Id, string Texto, string? Extra = null);

/// <summary>Produto para a busca (requisição, composição): categoria automática pelo grupo.</summary>
public sealed record ProdutoBusca(Guid Id, string Codigo, string Nome, string Unidade, string? Grupo, string? SubGrupo, bool Controlado, decimal? Preco)
{
    public string Texto => $"{Codigo} – {Nome} ({Unidade})";
    public string Busca { get; } = Rotulos.SemAcento($"{Codigo} {Nome} {Grupo} {SubGrupo}");
}

/// <summary>Listas para os combos das telas (cache curto por circuito — escopo = um usuário).</summary>
public sealed class Catalogos(IFabricaDb fabrica, QualyHub.Web.Seguranca.UsuarioAtualWeb usuario)
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
        await using var db = fabrica.CriarLeitura();
        return await db.Listas.AsNoTracking().Where(l => l.Tipo == tipo && l.Ativo).OrderBy(l => l.Ordem).ThenBy(l => l.Nome).Select(l => l.Nome).ToListAsync();
    });

    /// <summary>Obras ativas que o usuário pode acessar (todas, ou só as permitidas no cadastro dele).</summary>
    public Task<List<Opcao>> ObrasAsync() => Cache($"obras:{usuario.UsuarioId}", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        var q = db.Obras.AsNoTracking().Where(o => o.Ativa);
        if (!usuario.AcessoTodasObras)
        {
            var permitidas = usuario.ObrasPermitidas.ToList();
            q = q.Where(o => permitidas.Contains(o.Id));
        }
        return await q.OrderBy(o => o.Nome).Select(o => new Opcao(o.Id, o.Nome, o.Codigo)).ToListAsync();
    });

    /// <summary>SPEs ativas; Extra = ObraId (filtrar pela obra da tela).</summary>
    public Task<List<Opcao>> SpesAsync() => Cache("spes", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return (await db.Spes.AsNoTracking().Where(s => s.Ativa).OrderBy(s => s.Codigo).ToListAsync())
            .Select(s => new Opcao(s.Id, $"{s.Codigo} – {s.NomeExibicao} ({s.Cnpj})", s.ObraId.ToString())).ToList();
    });

    public Task<List<Opcao>> CentrosCustoAsync() => Cache("cc", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.CentrosCusto.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.Nome).Select(o => new Opcao(o.Id, o.Nome, o.Codigo)).ToListAsync();
    });

    public Task<List<Opcao>> FornecedoresAsync() => Cache("forn", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Fornecedores.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.RazaoSocial)
            .Select(o => new Opcao(o.Id, o.RazaoSocial, o.StatusQualificacao.ToString())).ToListAsync();
    });

    public Task<List<Opcao>> MateriaisAsync() => Cache("mat", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.MateriaisControlados.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.Codigo).Select(o => new Opcao(o.Id, o.Codigo + " – " + o.Nome, o.Codigo)).ToListAsync();
    });

    public Task<List<Opcao>> ServicosAsync() => Cache("srv", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.ServicosControlados.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.Codigo).Select(o => new Opcao(o.Id, o.Codigo + " – " + o.Nome, o.Codigo)).ToListAsync();
    });

    public Task<List<Opcao>> ProdutosAsync() => Cache("prod", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Produtos.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.Nome).Select(o => new Opcao(o.Id, o.Nome, o.Unidade)).ToListAsync();
    });

    public Task<List<Opcao>> UsuariosAsync() => Cache("usr", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Usuarios.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.Nome).Select(o => new Opcao(o.Id, o.Nome, o.Funcao)).ToListAsync();
    });

    public Task<List<Opcao>> PastasAsync() => Cache("pastas", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Pastas.AsNoTracking().OrderBy(o => o.Ordem).Select(o => new Opcao(o.Id, o.Nome)).ToListAsync();
    });

    public Task<List<Opcao>> LocaisAsync(Guid obraId) => Cache($"locais:{obraId}", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Locais.AsNoTracking().Where(l => l.ObraId == obraId).OrderBy(l => l.Codigo).Select(l => new Opcao(l.Id, l.Nome, l.Tipo)).ToListAsync();
    });

    public Task<List<Opcao>> ModelosAsync(TipoModelo tipo, bool soVigentes = true) => Cache($"modelos:{tipo}:{soVigentes}", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.ModelosInspecao.AsNoTracking().Where(m => m.Tipo == tipo && (!soVigentes || m.Situacao == SituacaoModelo.Vigente))
            .OrderBy(m => m.Codigo).Select(m => new Opcao(m.Id, m.Codigo + " – " + m.Titulo, m.ServicoControladoId.ToString())).ToListAsync();
    });

    public Task<List<Opcao>> FuncoesAsync() => Cache("funcoes", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Funcoes.AsNoTracking().Where(f => f.Ativa).OrderBy(f => f.Descricao).Select(f => new Opcao(f.Id, f.Descricao)).ToListAsync();
    });

    public Task<List<Opcao>> ColaboradoresAsync() => Cache("colab", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Colaboradores.AsNoTracking().Where(c => c.DataSaida == null).OrderBy(c => c.Nome).Select(c => new Opcao(c.Id, c.Nome, c.Situacao.ToString())).ToListAsync();
    });

    public Task<List<Opcao>> ContratosAtivosAsync() => Cache("ctr", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Contratos.AsNoTracking().Where(k => k.Status == StatusContrato.Ativo).OrderBy(k => k.Numero)
            .Select(k => new Opcao(k.Id, k.Numero + " – " + k.Fornecedor.RazaoSocial, k.FornecedorId.ToString())).ToListAsync();
    });

    public Task<List<Opcao>> DocumentosSgqAsync() => Cache("docsgq", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Documentos.AsNoTracking().OrderBy(d => d.Codigo).Select(d => new Opcao(d.Id, d.Codigo + " – " + d.Titulo)).ToListAsync();
    });

    public Task<List<ProdutoBusca>> ProdutosBuscaAsync() => Cache("prodbusca", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Produtos.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.Nome)
            .Select(o => new ProdutoBusca(o.Id, o.Codigo, o.Nome, o.Unidade, o.Grupo, o.SubGrupo, o.MaterialControladoId != null, o.PrecoReferencia)).ToListAsync();
    });

    /// <summary>Estabelecimentos ativos; Extra = tipo.</summary>
    public Task<List<Opcao>> EstabelecimentosAsync() => Cache("estab", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return (await db.Estabelecimentos.AsNoTracking().Where(e => e.Ativo).OrderBy(e => e.Tipo).ThenBy(e => e.Codigo).ToListAsync())
            .Select(e => new Opcao(e.Id, $"{e.Codigo} – {e.NomeExibicao} ({e.Cnpj})", e.Tipo.ToString())).ToList();
    });

    /// <summary>Serviços do orçamento (composições ativas); Extra = unidade.</summary>
    public Task<List<Opcao>> ComposicoesAsync() => Cache("comp", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Composicoes.AsNoTracking().Where(c => c.Ativa).OrderBy(c => c.Grupo).ThenBy(c => c.Codigo)
            .Select(c => new Opcao(c.Id, c.Codigo + " – " + c.Descricao, c.Unidade)).ToListAsync();
    });

    /// <summary>Grupos (nível 1) do catálogo de produtos.</summary>
    public Task<List<Opcao>> GruposProdutoAsync() => Cache("grupos", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.CategoriasProduto.AsNoTracking().Where(c => c.PaiId == null && c.Ativo).OrderBy(c => c.Nome).Select(c => new Opcao(c.Id, c.Nome, c.Sigla)).ToListAsync();
    });

    public Task<List<Opcao>> TiposDocFornecedorAsync() => Cache("tipodoc", async () =>
    {
        await using var db = fabrica.CriarLeitura();
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
