using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;

namespace Sigeo.Application.Cadastros;

/// <summary>Leituras da tela de cadastro de fornecedores (Cadastros/Fornecedores).</summary>
public sealed class FornecedoresCadastroConsultas(IFabricaDb fabrica)
{
    public async Task<List<Fornecedor>> ListarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Fornecedores.AsNoTracking().OrderBy(f => f.RazaoSocial).ToListAsync(ct);
    }

    /// <summary>Listas de apoio do formulário: grupos (categorias ativas) e tipos de documento de fornecedor.</summary>
    public async Task<FornecedorCadastroApoio> ApoioAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var cats = await db.CategoriasProduto.AsNoTracking().Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(ct);
        var tipos = await db.TiposDocumento.AsNoTracking().Where(t => t.Ativo && t.AplicaA == AplicaA.Fornecedor).OrderBy(t => t.Categoria).ThenBy(t => t.Ordem).ThenBy(t => t.Nome).ToListAsync(ct);
        return new(cats, tipos);
    }

    /// <summary>Fornecedor com contatos, grupos, serviços e exigências (para edição).</summary>
    public async Task<Fornecedor> ObterAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Fornecedores.AsNoTracking().Include(x => x.Contatos).Include(x => x.Grupos).Include(x => x.Servicos).Include(x => x.Exigencias).AsSplitQuery().FirstAsync(x => x.Id == id, ct);
    }
}
