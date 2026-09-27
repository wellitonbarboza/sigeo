using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;

namespace QualyHub.Domain.Suprimentos;

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

    public decimal ValorUnitarioFinal => ValorNegociado ?? ValorOfertado;
    public decimal ValorTotal => Math.Round(ValorUnitarioFinal * Quantidade, 2);
    public decimal Economia => ValorNegociado is { } n ? Math.Round((ValorOfertado - n) * Quantidade, 2) : 0;
}

/// <summary>T07 · Pedido de compra (ou referência a contrato). 2ª aprovação do Diretor antes da compra; registra envio e cópias.</summary>
public class Pedido : EntidadeDaObra, IOrigemObraPrima, IHistoricoTemporal
{
    public int Numero { get; set; }
    public Guid? CotacaoId { get; set; }
    public Cotacao? Cotacao { get; set; }
    public Guid? RequisicaoId { get; set; }
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
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
    public string? CodigoObraPrima { get; set; }
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
    public decimal QuantidadeRecebidaAceita { get; set; }
}
