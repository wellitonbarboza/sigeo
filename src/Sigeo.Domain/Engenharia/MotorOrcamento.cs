using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Engenharia;

/// <summary>Motor do orçamento: mede os locais e explode os serviços das tipologias em células (função pura, testável).</summary>
public static class MotorOrcamento
{
    public sealed record Celula(Guid ComposicaoId, Guid LocalId, Guid? SpeId, decimal Quantidade);

    /// <summary>Medida do local pela regra; níveis agregados (apto, pavimento, torre) somam os filhos quando não têm medida própria.</summary>
    public static decimal Medir(Local l, RegraQuantidade regra, ILookup<Guid?, Local> filhos, decimal? fixa = null)
    {
        switch (regra)
        {
            case RegraQuantidade.Unidade: return 1;
            case RegraQuantidade.Fixa: return fixa ?? 1;
        }
        decimal? propria = regra switch
        {
            RegraQuantidade.AreaPiso or RegraQuantidade.AreaTeto => l.AreaPiso ?? (l.Tipo == GeradorEmpreendimento.TipoApartamento && !filhos[l.Id].Any() ? l.AreaPrivativa : null),
            RegraQuantidade.AreaParede => l.AreaParede,
            RegraQuantidade.Perimetro => l.Perimetro,
            _ => null
        };
        return propria ?? filhos[l.Id].Sum(f => Medir(f, regra, filhos));
    }

    public static List<Celula> Gerar(IReadOnlyCollection<Local> locais, IEnumerable<ServicoAplicacao> aplicacoes, IReadOnlyDictionary<Guid, Composicao> composicoes)
    {
        var filhos = locais.ToLookup(l => l.LocalPaiId);
        // Índices: evita varrer todos os locais para cada aplicação.
        var porAmbiente = locais.Where(l => l.AmbienteModeloId != null).ToLookup(l => l.AmbienteModeloId!.Value);
        var porTipoTipologia = locais.Where(l => l.TipologiaId != null).ToLookup(l => (l.Tipo, l.TipologiaId!.Value));
        var pavimentosDaTorre = locais.Where(l => l.Tipo == GeradorEmpreendimento.TipoPavimento && l.LocalPaiId != null).ToLookup(l => l.LocalPaiId!.Value);
        var celulas = new Dictionary<(Guid, Guid), Celula>();
        foreach (var ap in aplicacoes)
        {
            if (!composicoes.TryGetValue(ap.ComposicaoId, out var comp) || !comp.Ativa) continue;
            IEnumerable<Local> alvo = ap.Alvo switch
            {
                AlvoServico.Ambiente => ap.AmbienteModeloId is { } am ? porAmbiente[am] : [],
                AlvoServico.Apartamento => porTipoTipologia[(GeradorEmpreendimento.TipoApartamento, ap.TipologiaId)],
                AlvoServico.Torre => porTipoTipologia[(GeradorEmpreendimento.TipoTorre, ap.TipologiaId)],
                AlvoServico.Condominio => porTipoTipologia[(GeradorEmpreendimento.TipoCondominio, ap.TipologiaId)],
                AlvoServico.Pavimento => porTipoTipologia[(GeradorEmpreendimento.TipoTorre, ap.TipologiaId)].SelectMany(t => pavimentosDaTorre[t.Id]),
                _ => []
            };
            foreach (var l in alvo)
            {
                var q = Math.Round(Medir(l, comp.Regra, filhos, ap.QuantidadeFixa) * comp.Fator, 4);
                if (q <= 0) continue;
                var k = (comp.Id, l.Id);
                celulas[k] = celulas.TryGetValue(k, out var c) ? c with { Quantidade = c.Quantidade + q } : new Celula(comp.Id, l.Id, l.SpeId, q);
            }
        }
        return [.. celulas.Values];
    }

    /// <summary>Locais da subárvore (o próprio + descendentes).</summary>
    public static HashSet<Guid> Subarvore(Guid raiz, ILookup<Guid?, Local> filhos)
    {
        var r = new HashSet<Guid> { raiz };
        var pilha = new Stack<Guid>([raiz]);
        while (pilha.Count > 0) foreach (var f in filhos[pilha.Pop()]) if (r.Add(f.Id)) pilha.Push(f.Id);
        return r;
    }
}
