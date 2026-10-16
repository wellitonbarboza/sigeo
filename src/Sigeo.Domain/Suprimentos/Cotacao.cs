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

/// <summary>T05 · Fornecedor convidado na cotação (guarda a situação de qualificação na data e a proposta).</summary>
public class CotacaoFornecedor : Entidade
{
    public Guid CotacaoId { get; set; }
    public Cotacao Cotacao { get; set; } = null!;
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    /// <summary>Token do link de resposta do fornecedor.</summary>
    public Guid LinkResposta { get; set; } = Guid.NewGuid();
    public StatusConvite Status { get; set; } = StatusConvite.Convidado;
    public DateOnly? DataResposta { get; set; }
    public decimal? ValorFrete { get; set; }
    public StatusQualificacao QualificacaoNaData { get; set; }
    public DateOnly? PrazoEntregaOfertado { get; set; }
    public string? CondicaoPagamento { get; set; }
    public DateOnly? ValidadeProposta { get; set; }
    public bool TemAnexoProposta { get; set; }
}

/// <summary>T06 · Mapa de cotação (item × fornecedor).</summary>
public class CotacaoItem : Entidade
{
    public Guid CotacaoId { get; set; }
    public Cotacao Cotacao { get; set; } = null!;
    public Guid? RequisicaoItemId { get; set; }
    public RequisicaoItem? RequisicaoItem { get; set; }
    public Guid FornecedorId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Unidade { get; set; } = "UN";
    public decimal Quantidade { get; set; }
    public decimal ValorOfertado { get; set; }
    public decimal? ValorNegociado { get; set; }
    public DateOnly? Entrega { get; set; }
    public bool MaterialControlado { get; set; }
    /// <summary>Confirma que o ofertado atende à especificação técnica (obrigatório em item controlado).</summary>
    public bool? AtendeEspecificacao { get; set; }
    /// <summary>Marca cotada (obrigatória em item de material com preço).</summary>
    public string? Marca { get; set; }

    /// <summary>Último preço unitário pago pelo mesmo produto (pedido anterior) — fotografado ao abrir/salvar.</summary>
    public decimal? PrecoUltimo { get; set; }
    /// <summary>Preço de referência do produto na data da cotação.</summary>
    public decimal? PrecoReferencia { get; set; }

    /// <summary>
    /// Marca do item cotado: obrigatória em material com preço; se a requisição pediu "somente esta marca", tem de ser a mesma.
    /// </summary>
    public string? ErroMarca(bool material, string? marcaRequisitada, bool somenteEssaMarca)
    {
        if (!material || ValorOfertado <= 0) return null;
        if (string.IsNullOrWhiteSpace(Marca)) return $"{Descricao}: informe a marca cotada.";
        if (somenteEssaMarca && !string.IsNullOrWhiteSpace(marcaRequisitada) && Cadastros.Marca.Chave(Marca) != Cadastros.Marca.Chave(marcaRequisitada))
            return $"{Descricao}: a requisição pede somente a marca {marcaRequisitada}.";
        return null;
    }

    public decimal ValorUnitarioFinal => ValorNegociado ?? ValorOfertado;
    public decimal ValorTotal => Math.Round(ValorUnitarioFinal * Quantidade, 2);
    /// <summary>Economia negociada: ofertado × negociado.</summary>
    public decimal Economia => ValorNegociado is { } n ? Math.Round((ValorOfertado - n) * Quantidade, 2) : 0;
    /// <summary>Economia vs. último preço pago (negativo = pagou mais caro que da última vez).</summary>
    public decimal? EconomiaVsUltimo => PrecoUltimo is { } u && ValorUnitarioFinal > 0 ? Math.Round((u - ValorUnitarioFinal) * Quantidade, 2) : null;
    /// <summary>Economia vs. preço de referência do sistema.</summary>
    public decimal? EconomiaVsReferencia => PrecoReferencia is { } r && ValorUnitarioFinal > 0 ? Math.Round((r - ValorUnitarioFinal) * Quantidade, 2) : null;
}

/// <summary>Resumo das 3 economias do vencedor (padrão ExpertData): valor e % sobre a base.</summary>
public sealed record EconomiaCotacao(decimal Negociada, decimal NegociadaPerc, decimal? VsUltimo, decimal? VsUltimoPerc, decimal? VsReferencia, decimal? VsReferenciaPerc, decimal Total)
{
    public static EconomiaCotacao Calcular(IEnumerable<CotacaoItem> itensVencedor)
    {
        var l = itensVencedor.Where(i => i.ValorUnitarioFinal > 0).ToList();
        var total = l.Sum(i => i.ValorTotal);
        static decimal Perc(decimal econ, decimal baseV) => baseV == 0 ? 0 : Math.Round(econ / baseV * 100, 1);
        var ofertado = l.Sum(i => Math.Round(i.ValorOfertado * i.Quantidade, 2));
        var neg = l.Sum(i => i.Economia);
        var comUlt = l.Where(i => i.EconomiaVsUltimo is not null).ToList();
        var comRef = l.Where(i => i.EconomiaVsReferencia is not null).ToList();
        decimal? ult = comUlt.Count == 0 ? null : comUlt.Sum(i => i.EconomiaVsUltimo!.Value);
        decimal? rf = comRef.Count == 0 ? null : comRef.Sum(i => i.EconomiaVsReferencia!.Value);
        var baseUlt = comUlt.Sum(i => Math.Round(i.PrecoUltimo!.Value * i.Quantidade, 2));
        var baseRef = comRef.Sum(i => Math.Round(i.PrecoReferencia!.Value * i.Quantidade, 2));
        return new EconomiaCotacao(neg, Perc(neg, ofertado), ult, ult is null ? null : Perc(ult.Value, baseUlt), rf, rf is null ? null : Perc(rf.Value, baseRef), total);
    }
}

/// <summary>T07 · Pedido de compra (ou referência a contrato). 2ª aprovação do Diretor antes da compra; registra envio e cópias.</summary>
public class Pedido : EntidadeDaObra, IOrigemErp, IHistoricoTemporal
{
    public int Numero { get; set; }
    public Guid? CotacaoId { get; set; }
    public Cotacao? Cotacao { get; set; }
    public Guid? RequisicaoId { get; set; }
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    /// <summary>SPE faturada (CNPJ que deve constar na NF).</summary>
    /// <summary>SPE de destino (beneficiária) — impressa no pedido e pedida na observação da NF.</summary>
    public Guid? SpeId { get; set; }
    /// <summary>Faturar para (CNPJ comprador).</summary>
    public Guid? EstabelecimentoFaturamentoId { get; set; }
    /// <summary>Entrega no almoxarifado central (FVM lá e depois transferência para a obra/SPE).</summary>
    public bool EntregaAlmoxarifadoCentral { get; set; }
    public string? CondicaoPagamento { get; set; }
    public TipoFrete Frete { get; set; } = TipoFrete.CIF;
    public string? Transportadora { get; set; }
    public decimal? ValorFrete { get; set; }
    public DateOnly Entrega { get; set; }
    public string? EnderecoEntrega { get; set; }
    public TipoDocumentoCompra TipoDocumento { get; set; } = TipoDocumentoCompra.PedidoCompra;
    public Guid? ContratoId { get; set; }
    public bool EspecificacoesIncluidas { get; set; }
    public Guid? SolicitacaoAprovacaoId { get; set; }
    public string? AprovadoDiretorPor { get; set; }
    public DateTimeOffset? AprovadoDiretorEm { get; set; }
    public DateTimeOffset? EnviadoFornecedorEm { get; set; }
    public string? EmailsCopia { get; set; }
    public StatusPedido Status { get; set; } = StatusPedido.Emitido;
    public bool SemOrcamento { get; set; }
    public string? ChaveNFe { get; set; }
    /// <summary>Motivos de bloqueio calculados pelas travas (fornecedor não qualificado, documento vencido…).</summary>
    public string? Bloqueios { get; set; }
    public string? CodigoErp { get; set; }
    public Guid? UltimaImportacaoId { get; set; }

    public ICollection<PedidoItem> Itens { get; set; } = [];

    public decimal Valor => Itens.Sum(i => i.ValorTotal) + (ValorFrete ?? 0);
}

public class PedidoItem : Entidade
{
    public Guid PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;
    public Guid? RequisicaoItemId { get; set; }
    public Guid? ProdutoId { get; set; }
    public string CodigoInsumo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Unidade { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    public Guid? MaterialControladoId { get; set; }
    public MaterialControlado? MaterialControlado { get; set; }
    public string? EspecificacaoTecnica { get; set; }
    public string? DocumentosExigidos { get; set; }
    /// <summary>Marca comprada (vem da cotação vencedora).</summary>
    public string? Marca { get; set; }
    public decimal QuantidadeRecebidaAceita { get; set; }
}
