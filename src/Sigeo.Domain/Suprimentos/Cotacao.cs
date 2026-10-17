using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>
/// T04 · Orçamento / cotação. Regra de alçada: até o limite → 1 proposta; acima → N propostas (parâmetros).
/// Vencedor só entre fornecedores qualificados; aprovação pelo motor (Diretor Técnico; serviço: Diretoria + Suprimentos).
/// </summary>
public class Cotacao : EntidadeDaObra, IHistoricoTemporal
{
    public int Numero { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid? RequisicaoId { get; set; }
    public Requisicao? Requisicao { get; set; }
    /// <summary>Lote da requisição (comprador) que esta cotação atende; nulo = requisição inteira (cotações antigas).</summary>
    public Guid? LoteCompraId { get; set; }
    /// <summary>Prazo de resposta dos fornecedores: depois dele o portal não aceita mais envio nem edição.</summary>
    public DateTimeOffset? PrazoResposta { get; set; }
    public TipoRequisicao Tipo { get; set; }
    public TipoFrete Frete { get; set; } = TipoFrete.CIF;
    public string? EnderecoEntrega { get; set; }
    public decimal ValorEstimado { get; set; }
    public int PropostasExigidas { get; set; } = 1;
    public int PropostasRecebidas { get; set; }
    public string? JustificativaPropostas { get; set; }
    public Guid? FornecedorVencedorId { get; set; }
    public Fornecedor? FornecedorVencedor { get; set; }
    public CriterioSelecao? CriterioSelecao { get; set; }
    public Guid? SolicitacaoAprovacaoId { get; set; }
    public string? AprovadoPor { get; set; }
    public DateTimeOffset? AprovadoEm { get; set; }
    public bool Concluido { get; set; }
    public bool SemOrcamento { get; set; }
    public string? ChaveNFe { get; set; }
    /// <summary>Análise do mapa por IA (pedida no botão do mapa): texto para o comprador conferir, não decide o vencedor.</summary>
    public string? AnaliseIa { get; set; }
    public DateTimeOffset? AnaliseIaEm { get; set; }
    public StatusCotacao Status { get; set; } = StatusCotacao.Aberto;

    public ICollection<CotacaoFornecedor> Fornecedores { get; set; } = [];
    public ICollection<CotacaoItem> Itens { get; set; } = [];

    /// <summary>Há vencedor escolhido item a item (senão vale o vencedor geral para todos os itens).</summary>
    public bool VencedorPorItem => Itens.Any(i => i.Vencedor);

    /// <summary>Itens vencedores: os marcados item a item ou, sem marcação, todos os do vencedor geral.</summary>
    public IReadOnlyList<CotacaoItem> ItensVencedores() => VencedorPorItem
        ? Itens.Where(i => i.Vencedor && i.ValorUnitarioFinal > 0).ToList()
        : Itens.Where(i => i.FornecedorId == FornecedorVencedorId && FornecedorVencedorId != null).ToList();

    /// <summary>Fornecedores com algum item vencido (um pedido para cada).</summary>
    public IReadOnlyList<Guid> FornecedoresVencedores() => ItensVencedores().Select(i => i.FornecedorId).Distinct().ToList();

    /// <summary>
    /// Marca o vencedor de um item: só um fornecedor por item (o outro perde a marca). Quantidade vencida opcional:
    /// menor que a do item quando o fornecedor só atende parte (o resto volta para "a pedir").
    /// </summary>
    public void DefinirVencedorItem(Guid? requisicaoItemId, string descricao, Guid? fornecedorId, decimal? quantidade = null)
    {
        var linhas = Itens.Where(i => i.RequisicaoItemId == requisicaoItemId && (requisicaoItemId != null || i.Descricao == descricao)).ToList();
        foreach (var l in linhas) { l.Vencedor = false; l.QuantidadeVencida = null; }
        if (fornecedorId is null) return;
        var alvo = linhas.FirstOrDefault(l => l.FornecedorId == fornecedorId) ?? throw new Aprovacoes.RegraDeNegocioException("Fornecedor não cotou este item.");
        if (alvo.ValorUnitarioFinal <= 0) throw new Aprovacoes.RegraDeNegocioException($"{alvo.Descricao}: o fornecedor não deu preço para este item.");
        if (quantidade is { } q && (q <= 0 || q > alvo.Quantidade)) throw new Aprovacoes.RegraDeNegocioException($"{alvo.Descricao}: quantidade vencida entre 0 e {alvo.Quantidade:0.###}.");
        alvo.Vencedor = true;
        alvo.QuantidadeVencida = quantidade is { } q2 && q2 < alvo.Quantidade ? q2 : null;
    }

    /// <summary>Vencedor geral = fornecedor com maior valor entre os itens vencidos (listas e relatórios).</summary>
    public void AtualizarVencedorGeral()
    {
        if (!VencedorPorItem) return;
        FornecedorVencedorId = ItensVencedores().GroupBy(i => i.FornecedorId).OrderByDescending(g => g.Sum(i => i.ValorTotalVencido)).Select(g => (Guid?)g.Key).FirstOrDefault();
    }

    public string FaixaAlcada(ParametrosVigentes p) =>
        ValorEstimado <= p[Parametros.LimiteUmOrcamento] ? $"≤ {p[Parametros.LimiteUmOrcamento]:C0}" : $"> {p[Parametros.LimiteUmOrcamento]:C0}";

    /// <summary>Valor estimado = soma do menor valor ofertado por item; propostas exigidas conforme a faixa.</summary>
    public void Recalcular(ParametrosVigentes p)
    {
        ValorEstimado = Itens.GroupBy(i => i.RequisicaoItemId)
                             .Sum(g => g.Where(i => i.ValorTotal > 0).Select(i => i.ValorTotal).DefaultIfEmpty(0).Min());
        PropostasExigidas = ValorEstimado <= p[Parametros.LimiteUmOrcamento] ? 1 : p.Int(Parametros.OrcamentosAcimaLimite);
        PropostasRecebidas = Fornecedores.Count(f => f.Status is StatusConvite.Respondido or StatusConvite.Vencedor);
    }
}
