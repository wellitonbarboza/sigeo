using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>T18 · Liberação de pagamento (3-way: pedido × recebimento/FVM × NF; serviço: medição aprovada).</summary>
public class LiberacaoPagamento : EntidadeAuditavel, IHistoricoTemporal
{
    public int Numero { get; set; }
    public Guid? ObraId { get; set; }
    public OrigemPagamento Origem { get; set; }
    public string ChaveNFe { get; set; } = string.Empty;
    public Guid FornecedorId { get; set; }
    public Guid? PedidoId { get; set; }
    public Guid? ContratoId { get; set; }
    public Guid? MedicaoId { get; set; }
    public decimal ValorNF { get; set; }
    public decimal ValorLiberado { get; set; }
    public string? JustificativaDiferenca { get; set; }
    public bool RecebimentoOk { get; set; }
    public string? EncaminhadoPor { get; set; }
    public DateTimeOffset? EncaminhadoEm { get; set; }
    public Guid? SolicitacaoAprovacaoId { get; set; }
    public string? LiberadoPor { get; set; }
    public DateTimeOffset? LiberadoEm { get; set; }
    public DateOnly Vencimento { get; set; }
    public StatusPagamento Status { get; set; } = StatusPagamento.Aguardando;
    public string? Bloqueios { get; set; }
}
