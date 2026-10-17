using System.Globalization;
using System.Text;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Fiscal;

/// <summary>
/// NF-e de entrada (fornecedor → CNPJ da construtora), importada do XML (SIEG ou arquivo).
/// Fluxo: recebida → vinculada ao pedido (regra/IA/pessoa) → recebimento registrado (FVM). Cancelada pela SEFAZ não segue.
/// Não pertence a uma obra na chegada: a obra vem do pedido vinculado.
/// </summary>
public class NotaFiscalEntrada : EntidadeAuditavel
{
    public string Chave { get; set; } = string.Empty;
    public int Numero { get; set; }
    public string Serie { get; set; } = string.Empty;
    public string Modelo { get; set; } = "55";
    public DateTimeOffset Emissao { get; set; }
    public string EmitenteCnpj { get; set; } = string.Empty;
    public string EmitenteNome { get; set; } = string.Empty;
    public string DestinatarioCnpj { get; set; } = string.Empty;
    public string? DestinatarioNome { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal? ValorFrete { get; set; }
    /// <summary>Informações complementares (infCpl): costuma trazer o nº do pedido do cliente.</summary>
    public string? InformacoesComplementares { get; set; }
    /// <summary>Números de pedido citados na nota (xPed dos itens + texto), separados por vírgula.</summary>
    public string? PedidosCitados { get; set; }
    public SituacaoNota Situacao { get; set; } = SituacaoNota.Recebida;
    public OrigemNota Origem { get; set; } = OrigemNota.Manual;
    /// <summary>Caminho do XML no armazenamento (Blob/pasta local).</summary>
    public string? ArquivoXml { get; set; }
    public Guid? FornecedorId { get; set; }
    public Guid? PedidoId { get; set; }
    public Guid? ObraId { get; set; }
    public Guid? RecebimentoId { get; set; }
    public OrigemVinculo? VinculoOrigem { get; set; }
    /// <summary>Por que foi (ou não) vinculada — mostrado na tela para conferência.</summary>
    public string? VinculoDetalhe { get; set; }
    /// <summary>Última análise da IA (texto para o comprador conferir; não altera nada sozinha).</summary>
    public string? AnaliseIa { get; set; }
    public ICollection<NotaFiscalItem> Itens { get; set; } = [];

    public bool PodeVincular => Situacao is SituacaoNota.Recebida or SituacaoNota.Vinculada;

    /// <summary>Liga ao pedido (a obra vem dele). Só nota ainda não recebida nem cancelada/ignorada.</summary>
    public void VincularPedido(Guid pedidoId, Guid obraId, OrigemVinculo origem, string detalhe)
    {
        if (!PodeVincular) throw new InvalidOperationException("Nota já recebida, cancelada ou ignorada.");
        PedidoId = pedidoId; ObraId = obraId; VinculoOrigem = origem; VinculoDetalhe = detalhe;
        Situacao = SituacaoNota.Vinculada;
    }

    public void Desvincular(string motivo)
    {
        if (!PodeVincular) throw new InvalidOperationException("Nota já recebida, cancelada ou ignorada.");
        PedidoId = null; ObraId = null; VinculoOrigem = null; VinculoDetalhe = motivo; Situacao = SituacaoNota.Recebida;
        foreach (var i in Itens) { i.PedidoItemId = null; i.VinculoOrigem = null; i.Confianca = null; }
    }
}
