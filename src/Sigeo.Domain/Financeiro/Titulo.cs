using Sigeo.Domain.Common;

namespace Sigeo.Domain.Financeiro;

/// <summary>
/// Título a pagar ou a receber (nota, boleto, fatura, folha, DARF, parcela de venda…), com as parcelas.
/// Pode nascer da liberação de pagamento, da venda de unidade, da comissão, da apuração do RET, da importação do ERP ou à mão.
/// </summary>
public class Titulo : EntidadeAuditavel, IHistoricoTemporal
{
    public TipoTitulo Tipo { get; set; } = TipoTitulo.Pagar;
    public int Numero { get; set; }
    public string? DocumentoTipo { get; set; }
    public string? DocumentoNumero { get; set; }
    public Guid? FornecedorId { get; set; }
    /// <summary>Quem paga/recebe quando não é fornecedor (comprador da unidade, órgão público…).</summary>
    public string? Pessoa { get; set; }
    public Guid? CompradorId { get; set; }
    public Guid? EstabelecimentoId { get; set; }
    public Guid? ObraId { get; set; }
    public Guid? CentroCustoId { get; set; }
    public Guid? ContaGerencialId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public DateOnly Emissao { get; set; }
    public DateOnly Competencia { get; set; }
    public decimal ValorBruto { get; set; }
    public decimal RetencaoIss { get; set; }
    public decimal RetencaoInss { get; set; }
    public decimal RetencaoIr { get; set; }
    /// <summary>PIS/COFINS/CSLL retidos (4,65%).</summary>
    public decimal RetencaoPcc { get; set; }
    public OrigemTitulo Origem { get; set; } = OrigemTitulo.Manual;
    public Guid? OrigemId { get; set; }
    public string? ChaveOrigem { get; set; }
    /// <summary>Lançado antes da data de corte (importado do ERP).</summary>
    public bool Historico { get; set; }
    public bool Cancelado { get; set; }
    public string? MotivoCancelamento { get; set; }
    public string? Observacao { get; set; }
    public ICollection<TituloParcela> Parcelas { get; set; } = [];

    public decimal Retencoes => RetencaoIss + RetencaoInss + RetencaoIr + RetencaoPcc;
    public decimal ValorLiquido => ValorBruto - Retencoes;

    public StatusTitulo Status
    {
        get
        {
            if (Cancelado) return StatusTitulo.Cancelado;
            var vivas = Parcelas.Where(p => p.Status != StatusParcelaTitulo.Cancelada).ToList();
            if (vivas.Count > 0 && vivas.All(p => p.Status == StatusParcelaTitulo.Paga)) return StatusTitulo.Liquidado;
            return vivas.Any(p => p.Status == StatusParcelaTitulo.Paga) ? StatusTitulo.Parcial : StatusTitulo.Aberto;
        }
    }

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Descricao)) e.Add("Informe a descrição.");
        if (ValorBruto <= 0) e.Add("Valor deve ser maior que zero.");
        if (Retencoes > ValorBruto) e.Add("Retenções maiores que o valor bruto.");
        if (Emissao == default) e.Add("Informe a emissão.");
        if (Tipo == TipoTitulo.Pagar && FornecedorId is null && string.IsNullOrWhiteSpace(Pessoa)) e.Add("Informe o fornecedor (ou a quem pagar).");
        if (Tipo == TipoTitulo.Receber && CompradorId is null && string.IsNullOrWhiteSpace(Pessoa)) e.Add("Informe de quem receber.");
        var vivas = Parcelas.Where(p => p.Status != StatusParcelaTitulo.Cancelada).ToList();
        if (vivas.Count == 0) e.Add("Informe ao menos uma parcela.");
        else if (Math.Abs(vivas.Sum(p => p.Valor) - ValorLiquido) > 0.05m) e.Add($"Soma das parcelas ({vivas.Sum(p => p.Valor).ToString("N2", Cultura.Br)}) diferente do valor líquido ({ValorLiquido.ToString("N2", Cultura.Br)}).");
        if (vivas.Any(p => p.Valor <= 0)) e.Add("Parcela com valor zerado ou negativo.");
        return e;
    }
}
