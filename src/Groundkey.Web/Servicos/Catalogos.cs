using Microsoft.EntityFrameworkCore;
using Groundkey.Domain.Common;
using Groundkey.Infrastructure.Servicos;

namespace Groundkey.Web.Servicos;


/// <summary>Produto para a busca (requisição, composição): categoria automática pelo grupo.</summary>
public sealed record ProdutoBusca(Guid Id, string Codigo, string Nome, string Unidade, string? Grupo, string? SubGrupo, bool Controlado, decimal? Preco)
{
    public string Texto => $"{Codigo} – {Nome} ({Unidade})";
    public string Busca { get; } = Rotulos.SemAcento($"{Codigo} {Nome} {Grupo} {SubGrupo}");
}

/// <summary>Listas para os combos das telas (cache curto por circuito — escopo = um usuário).</summary>
public sealed class Catalogos(IFabricaDb fabrica, Groundkey.Web.Seguranca.UsuarioAtualWeb usuario, CacheCompartilhado compartilhado)
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

    /// <summary>Produto salvo/importado: a próxima busca de qualquer usuário da empresa relê o catálogo.</summary>
    public void InvalidarProdutos() { _cache.Clear(); compartilhado.Invalidar(usuario.EmpresaId, "prodbusca"); }

    /// <summary>Nomes dos níveis de local da construtora (Condomínio/Torre/Pavimento/Unidade/Cômodo ou os que ela usa).</summary>
    public Task<Groundkey.Domain.Estrutura.EstruturaLocais> EstruturaAsync() => Cache("estrutura", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        var e = await db.Empresas.AsNoTracking().Where(x => x.Id == usuario.EmpresaId).Select(x => new { x.ModeloEstrutura, x.NomesNiveis }).FirstOrDefaultAsync();
        return Groundkey.Domain.Estrutura.EstruturaLocais.De(e?.ModeloEstrutura, e?.NomesNiveis);
    });

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

    /// <summary>
    /// Locais com o caminho legível (Torre A › Apto 101 › Cozinha), sem condomínio e pavimento: o mesmo "Apto 101" existe em várias torres.
    /// Extra = tipo do local.
    /// </summary>
    public Task<List<Opcao>> LocaisComCaminhoAsync(Guid obraId) => Cache($"locaiscam:{obraId}", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        var ls = await db.Locais.AsNoTracking().Where(l => l.ObraId == obraId).Select(l => new { l.Id, l.Nome, l.Tipo, l.LocalPaiId, l.Codigo }).ToListAsync();
        var porId = ls.ToDictionary(l => l.Id);
        string Caminho(Guid id)
        {
            var partes = new List<string>();
            for (var (atual, n) = (porId.GetValueOrDefault(id), 0); atual is not null && n < 8; atual = atual.LocalPaiId is { } p ? porId.GetValueOrDefault(p) : null, n++)
                if (n == 0 || !(atual.Tipo is "Condomínio" or "Pavimento")) partes.Insert(0, atual.Nome);
            return string.Join(" › ", partes);
        }
        return ls.OrderBy(l => l.Codigo).Select(l => new Opcao(l.Id, Caminho(l.Id), l.Tipo)).ToList();
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

    /// <summary>Catálogo de produtos para a busca: uma cópia por empresa no processo (5 min ou até salvar um produto).</summary>
    public Task<List<ProdutoBusca>> ProdutosBuscaAsync() => compartilhado.ObterAsync(usuario.EmpresaId, "prodbusca", TimeSpan.FromMinutes(5), async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Produtos.AsNoTracking().Where(o => o.Ativo).OrderBy(o => o.Nome)
            .Select(o => new ProdutoBusca(o.Id, o.Codigo, o.Nome, o.Unidade, o.Grupo, o.SubGrupo, o.MaterialControladoId != null, o.PrecoReferencia)).ToListAsync();
    });

    /// <summary>Nomes das marcas ativas (sugestões nos campos de marca).</summary>
    public Task<List<string>> MarcasAsync() => Cache("marcas", async () =>
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Marcas.AsNoTracking().Where(m => m.Ativa).OrderBy(m => m.Nome).Select(m => m.Nome).ToListAsync();
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
