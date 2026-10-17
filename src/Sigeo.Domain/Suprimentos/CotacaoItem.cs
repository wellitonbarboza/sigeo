using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

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
    /// <summary>Vencedor deste item (escolha item a item entre os fornecedores).</summary>
    public bool Vencedor { get; set; }
    /// <summary>Quantidade que o vencedor atende, quando menor que a do item (o resto volta para "a pedir").</summary>
    public decimal? QuantidadeVencida { get; set; }
    public decimal QuantidadePedido => QuantidadeVencida ?? Quantidade;
    public decimal ValorTotalVencido => Math.Round(ValorUnitarioFinal * QuantidadePedido, 2);

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
