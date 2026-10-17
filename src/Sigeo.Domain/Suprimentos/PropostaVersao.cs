using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Cada envio da proposta pelo portal guarda uma versão (a cotação mostra a última; o histórico fica para conferência).</summary>
public class PropostaVersao : Entidade
{
    public Guid CotacaoFornecedorId { get; set; }
    public int Versao { get; set; }
    public DateTimeOffset EnviadaEm { get; set; }
    public string? EnviadaPor { get; set; }
    public decimal ValorItens { get; set; }
    public decimal? ValorFrete { get; set; }
    public DateOnly? PrazoEntrega { get; set; }
    public string? CondicaoPagamento { get; set; }
    public DateOnly? Validade { get; set; }
    /// <summary>Itens da versão: descrição, quantidade, unitário e marca (texto JSON, só para consulta).</summary>
    public string? ItensJson { get; set; }
}
