using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>
/// Encaminhamento automático: cada item da requisição autorizada vai para o comprador da sua categoria.
/// Ordem: subgrupo + obra → grupo + obra → subgrupo → grupo (sem obra) → fila do gestor. Ausente: vai ao substituto;
/// substituto também ausente: fila do gestor.
/// </summary>
public static class EncaminhamentoCompras
{
    public sealed record Destino(Guid? CompradorId, string? CompradorNome, Guid? TitularId, string? TitularNome, string Categoria);

    private static string K(string? s) => (s ?? "").Trim().ToUpperInvariant();

    public static Destino Encaminhar(string? grupo, string? subGrupo, string? categoriaServico, Guid obraId, IReadOnlyCollection<CarteiraComprador> carteiras, DateOnly hoje)
    {
        var ativas = carteiras.Where(c => c.Ativa && !c.Excluido).ToList();
        IEnumerable<(CarteiraComprador C, CarteiraCategoria Cat)> Todas() => ativas.SelectMany(c => c.Categorias.Select(cat => (c, cat)));
        (CarteiraComprador C, CarteiraCategoria Cat)? achado = null;
        string categoria;
        if (!string.IsNullOrWhiteSpace(categoriaServico))
        {
            categoria = $"Serviço › {categoriaServico}";
            achado = Todas().Where(x => K(x.Cat.CategoriaServico) == K(categoriaServico)).OrderBy(x => x.Cat.ObraId == obraId ? 0 : x.Cat.ObraId is null ? 1 : 2)
                .Cast<(CarteiraComprador, CarteiraCategoria)?>().FirstOrDefault(x => x!.Value.Item2.ObraId is null || x.Value.Item2.ObraId == obraId);
        }
        else
        {
            categoria = string.IsNullOrWhiteSpace(subGrupo) ? grupo ?? "Sem grupo" : $"{grupo} › {subGrupo}";
            bool Sub((CarteiraComprador C, CarteiraCategoria Cat) x) => !string.IsNullOrWhiteSpace(x.Cat.SubGrupo) && K(x.Cat.Grupo) == K(grupo) && K(x.Cat.SubGrupo) == K(subGrupo);
            bool Grp((CarteiraComprador C, CarteiraCategoria Cat) x) => string.IsNullOrWhiteSpace(x.Cat.SubGrupo) && string.IsNullOrWhiteSpace(x.Cat.CategoriaServico) && K(x.Cat.Grupo) == K(grupo);
            var l = Todas().ToList();
            achado = l.Where(x => Sub(x) && x.Cat.ObraId == obraId).Cast<(CarteiraComprador, CarteiraCategoria)?>().FirstOrDefault()
                     ?? l.Where(x => Grp(x) && x.Cat.ObraId == obraId).Cast<(CarteiraComprador, CarteiraCategoria)?>().FirstOrDefault()
                     ?? l.Where(x => Sub(x) && x.Cat.ObraId is null).Cast<(CarteiraComprador, CarteiraCategoria)?>().FirstOrDefault()
                     ?? l.Where(x => Grp(x) && x.Cat.ObraId is null).Cast<(CarteiraComprador, CarteiraCategoria)?>().FirstOrDefault();
            if (string.IsNullOrWhiteSpace(grupo)) achado = null;
        }
        if (achado is not { } a) return new Destino(null, null, null, null, categoria);
        var titular = a.Item1;
        if (!titular.AusenteEm(hoje)) return new Destino(titular.UsuarioId, titular.Nome, titular.UsuarioId, titular.Nome, categoria);
        var sub = titular.SubstitutoUsuarioId is { } sid ? ativas.FirstOrDefault(c => c.UsuarioId == sid) : null;
        if (titular.SubstitutoUsuarioId is { } sid2 && (sub is null || !sub.AusenteEm(hoje)))
            return new Destino(sid2, sub?.Nome ?? titular.SubstitutoNome, titular.UsuarioId, titular.Nome, categoria);
        return new Destino(null, null, titular.UsuarioId, titular.Nome, categoria);
    }

    /// <summary>Duas carteiras com a mesma categoria e a mesma obra deixam o encaminhamento ambíguo: bloqueia no cadastro.</summary>
    public static IReadOnlyList<string> Conflitos(CarteiraComprador editada, IEnumerable<CarteiraComprador> outras)
    {
        string Chave(CarteiraCategoria c) => $"{K(c.Grupo)}|{K(c.SubGrupo)}|{K(c.CategoriaServico)}|{c.ObraId}";
        var e = new List<string>();
        foreach (var g in editada.Categorias.GroupBy(Chave).Where(g => g.Count() > 1)) e.Add($"Categoria repetida na carteira: {g.First().Rotulo}.");
        foreach (var o in outras.Where(o => o.Id != editada.Id && o.Ativa && !o.Excluido))
            foreach (var c in editada.Categorias)
                if (o.Categorias.Any(x => Chave(x) == Chave(c)))
                    e.Add($"\"{c.Rotulo}\"{(c.ObraId is null ? "" : " (nesta obra)")} já está na carteira de {o.Nome}. Escolha um dono só, ou limite por obra.");
        return e;
    }

    /// <summary>Data limite de compra do lote: menor (necessidade − lead time) entre os itens.</summary>
    public static DateOnly? DataLimite(IEnumerable<RequisicaoItem> itens, DateOnly dataEntrega) =>
        itens.Select(i => (i.DataNecessidade ?? dataEntrega).AddDays(-Math.Max(0, i.LeadTimeDias))).DefaultIfEmpty().Min() is var d && d == default ? null : d;

    public static string Letra(int indice) => indice < 26 ? ((char)('A' + indice)).ToString() : $"Z{indice - 25}";
}
