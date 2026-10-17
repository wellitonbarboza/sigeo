using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Ficha de estoque (kardex): cada entrada, saída, transferência ou ajuste. Nunca é alterado — corrige-se com outro movimento.</summary>
public class MovimentoEstoque : EntidadeAuditavel
{
    public Guid AlmoxarifadoId { get; set; }
    public Guid ProdutoId { get; set; }
    public DateTimeOffset Data { get; set; }
    public TipoMovimentoEstoque Tipo { get; set; }
    /// <summary>Com sinal: positivo entra, negativo sai.</summary>
    public decimal Quantidade { get; set; }
    /// <summary>Movimento no saldo bloqueado (quarentena).</summary>
    public bool Bloqueado { get; set; }
    public decimal CustoUnitario { get; set; }
    public decimal Valor { get; set; }
    public decimal SaldoApos { get; set; }
    /// <summary>Documento de origem legível (ex.: "Recebimento 219", "Transferência 31").</summary>
    public string Documento { get; set; } = string.Empty;
    public string? OrigemTipo { get; set; }
    public Guid? OrigemId { get; set; }
    /// <summary>Apartamento / local onde foi aplicado (saída).</summary>
    public Guid? LocalId { get; set; }
    /// <summary>Serviço (composição) em que foi aplicado (saída).</summary>
    public Guid? ComposicaoId { get; set; }
    public Guid? KitId { get; set; }
    public Guid? ColaboradorId { get; set; }
    public Guid? FornecedorId { get; set; }
    public string? EntreguePara { get; set; }
    public string? Lote { get; set; }
    public string? Observacao { get; set; }
}
