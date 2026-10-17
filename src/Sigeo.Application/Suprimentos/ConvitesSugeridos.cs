using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Fornecedores para convidar e os sugeridos (sugeridos primeiro na lista).</summary>
public sealed record FornecedoresSugeridos(List<Opcao> Lista, HashSet<Guid> Sugeridos);

/// <summary>
/// Fornecedores para convidar numa cotação: ativos do tipo de fornecimento da requisição, com os "sugeridos"
/// (atendem os grupos do catálogo dos itens ou as ITS dos serviços) primeiro. Usado ao gerar e ao completar a cotação.
/// </summary>
internal static class ConvitesSugeridos
{
    public static async Task<FornecedoresSugeridos> ParaAsync(ISigeoDb db, FornecedoresServico forn, TipoRequisicao tipoReq,
        IEnumerable<RequisicaoItem> itens, CancellationToken ct = default)
    {
        var tipo = tipoReq == TipoRequisicao.Material ? TipoFornecimento.Material
            : TipoFornecimento.ServicoObra | TipoFornecimento.Laboratorio | TipoFornecimento.Consultoria | TipoFornecimento.ServicoEspecializadoEngenharia | TipoFornecimento.Locacao;
        var lista = itens.ToList();
        var prods = lista.Where(i => i.ProdutoId != null).Select(i => i.ProdutoId!.Value).ToList();
        var cats = await db.CategoriasProduto.AsNoTracking().ToListAsync(ct);
        var grupos = (await db.Produtos.AsNoTracking().Where(p => prods.Contains(p.Id)).Select(p => new { p.Grupo, p.SubGrupo }).ToListAsync(ct))
            .Select(p => (G: cats.FirstOrDefault(c => c.PaiId == null && c.Nome == p.Grupo), p.SubGrupo)).Where(x => x.G is not null)
            .Select(x => (x.G!.Id, cats.FirstOrDefault(c => c.PaiId == x.G.Id && c.Nome == x.SubGrupo)?.Id)).Distinct().ToList();
        var its = lista.Where(i => i.ServicoControladoId != null).Select(i => i.ServicoControladoId!.Value).Distinct().ToList();
        var sugeridos = await forn.QueAtendemAsync(grupos, its, ct);
        var fs = (await db.Fornecedores.AsNoTracking().Where(f => f.Ativo).OrderBy(f => f.RazaoSocial).ToListAsync(ct))
            .Where(f => (f.TipoFornecimento & tipo) != 0 || sugeridos.Contains(f.Id)).OrderByDescending(f => sugeridos.Contains(f.Id)).ThenBy(f => f.RazaoSocial)
            .Select(f => new Opcao(f.Id, f.RazaoSocial, f.StatusQualificacao.ToString())).ToList();
        return new(fs, sugeridos);
    }
}
