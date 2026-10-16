using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Engenharia;

/// <summary>
/// Serviço do orçamento com a sua composição (insumos por unidade de serviço). Ex.: contrapiso convencional (m²) =
/// cimento + areia + aditivo + pedreiro/servente (horas) ou empreitada (R$/m²). Liga-se à ITS (serviço controlado → FVS).
/// </summary>
public class Composicao : EntidadeAuditavel, IHistoricoTemporal
{
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    /// <summary>Unidade do serviço (m², m, m³, un, vb).</summary>
    public string Unidade { get; set; } = "m²";
    /// <summary>Etapa / grupo da EAP (lista "Grupo de serviço").</summary>
    public string? Grupo { get; set; }
    /// <summary>Disciplina (lista configurável: civil, hidráulica, elétrica…) — usada nos kits por apartamento.</summary>
    public string? Disciplina { get; set; }
    public RegraQuantidade Regra { get; set; } = RegraQuantidade.AreaPiso;
    /// <summary>Multiplicador sobre a medida do local (ex.: rodapé = perímetro × 0,9 para descontar portas).</summary>
    public decimal Fator { get; set; } = 1;
    /// <summary>ITS / serviço controlado (puxa o modelo de FVS e a sigla do mapa).</summary>
    public Guid? ServicoControladoId { get; set; }
    public bool Ativa { get; set; } = true;
    public string? Observacao { get; set; }
    public ICollection<ComposicaoItem> Itens { get; set; } = [];

    public decimal CustoUnitario => Math.Round(Itens.Sum(i => i.CustoPorUnidade), 4);
    public decimal Custo(TipoInsumo t) => Math.Round(Itens.Where(i => i.Tipo == t).Sum(i => i.CustoPorUnidade), 4);
    /// <summary>Horas de mão de obra própria por unidade de serviço (base da apropriação).</summary>
    public decimal HorasPorUnidade => Itens.Where(i => i.Tipo == TipoInsumo.MaoObraPropria).Sum(i => i.Coeficiente);

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Codigo)) e.Add("Informe o código do serviço.");
        if (string.IsNullOrWhiteSpace(Descricao)) e.Add("Informe a descrição do serviço.");
        if (string.IsNullOrWhiteSpace(Unidade)) e.Add("Informe a unidade.");
        if (Fator <= 0) e.Add("Fator deve ser maior que zero.");
        foreach (var i in Itens)
        {
            if (i.Coeficiente <= 0) e.Add($"{i.Descricao}: coeficiente deve ser maior que zero.");
            if (i.PerdaPerc is < 0 or > 100) e.Add($"{i.Descricao}: perda entre 0 e 100%.");
            if (i.CustoUnitario < 0) e.Add($"{i.Descricao}: custo não pode ser negativo.");
            if (i.Tipo == TipoInsumo.Material && i.ProdutoId is null) e.Add($"{i.Descricao}: material precisa do produto do cadastro.");
            if (i.Tipo == TipoInsumo.MaoObraPropria && i.FuncaoObraId is null) e.Add($"{i.Descricao}: mão de obra própria precisa da função (base da apropriação em horas).");
            if (string.IsNullOrWhiteSpace(i.Descricao)) e.Add("Insumo sem descrição.");
        }
        return e;
    }
}

/// <summary>Insumo da composição: material (produto do cadastro), mão de obra própria (horas da função), empreitada, equipamento ou terceiro.</summary>
public class ComposicaoItem : Entidade
{
    public Guid ComposicaoId { get; set; }
    public int Ordem { get; set; }
    public TipoInsumo Tipo { get; set; } = TipoInsumo.Material;
    public Guid? ProdutoId { get; set; }
    /// <summary>Mão de obra própria: função (pedreiro, servente…).</summary>
    public Guid? FuncaoObraId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Unidade { get; set; } = "un";
    /// <summary>Quantidade do insumo por unidade do serviço.</summary>
    public decimal Coeficiente { get; set; }
    public decimal PerdaPerc { get; set; }
    public decimal CustoUnitario { get; set; }

    public decimal QuantidadePor(decimal qtdServico) => Math.Round(qtdServico * Coeficiente * (1 + PerdaPerc / 100m), 4);
    public decimal CustoPorUnidade => Math.Round(Coeficiente * (1 + PerdaPerc / 100m) * CustoUnitario, 4);
}

/// <summary>
/// Serviço marcado na tipologia: em que ambiente (cômodo/área comum) ou nível (apartamento, pavimento, torre, condomínio) ele entra.
/// O orçamento explode isso para todos os locais gerados.
/// </summary>
public class ServicoAplicacao : EntidadeAuditavel, IPertenceObra
{
    public Guid ObraId { get; set; }
    public Guid TipologiaId { get; set; }
    public Guid ComposicaoId { get; set; }
    public AlvoServico Alvo { get; set; } = AlvoServico.Ambiente;
    /// <summary>Alvo = Ambiente: o ambiente-modelo da tipologia.</summary>
    public Guid? AmbienteModeloId { get; set; }
    /// <summary>Regra "Fixa": quantidade por local.</summary>
    public decimal? QuantidadeFixa { get; set; }
}

/// <summary>Célula da matriz serviço × local: quantidade e custo orçados. Travada = editada à mão (o gerador não mexe).</summary>
public class ServicoLocal : EntidadeDaObra
{
    public Guid ComposicaoId { get; set; }
    public Guid LocalId { get; set; }
    public Guid? SpeId { get; set; }
    public decimal Quantidade { get; set; }
    /// <summary>Custo unitário da composição na data da geração (fotografia).</summary>
    public decimal CustoUnitario { get; set; }
    public decimal Custo { get; set; }
    public string Origem { get; set; } = OrigemCelula.Tipologia;
    public bool Travado { get; set; }
}

/// <summary>Origem da célula do orçamento: gerada pelas tipologias ou editada à mão.</summary>
public static class OrigemCelula
{
    public const string Tipologia = "Tipologia";
    public const string Manual = "Manual";
}

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
