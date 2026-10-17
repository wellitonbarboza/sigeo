using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Abstracoes;
using Sigeo.Domain.Engenharia;

namespace Sigeo.Application.Comum;

/// <summary>Árvore de locais de uma obra já carregada na tela: situa a quebra por local de qualquer item sem ir ao banco.</summary>
public sealed class ArvoreLocais
{
    private readonly Dictionary<Guid, QuadroLocais.No> _nos;

    public ArvoreLocais(IEnumerable<Domain.Estrutura.Local> locais) =>
        _nos = locais.GroupBy(l => l.Id).ToDictionary(g => g.Key, g => { var l = g.First(); return new QuadroLocais.No(l.Id, l.LocalPaiId, l.Tipo, l.Nome, l.Codigo, l.Ordem); });

    public static readonly ArvoreLocais Vazia = new([]);

    /// <summary>Nome legível de cada local ("QA-T01-P03-302-SAL – Torre 1 › 3º pavimento › Apto 302 › Sala"); ordena pelo código.</summary>
    public Dictionary<Guid, string> Nomes() =>
        _nos.Keys.ToDictionary(id => id, id => Situar([(id, 0m)]) is [var l] ? $"{l.Codigo} – {QuadroLocais.Caminho(l)}" : "");

    public List<LocalQuantidade> Situar(IEnumerable<(Guid LocalId, decimal Quantidade)> quebra) => QuadroLocais.Montar(quebra, _nos);
}
