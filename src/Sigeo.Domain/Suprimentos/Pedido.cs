using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

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
