using Sigeo.Domain.Common;
using Sigeo.Domain.Financeiro;

namespace Sigeo.Domain.Incorporacao;

/// <summary>Contrato de compra e venda da unidade: condição em blocos (entrada, mensais, balões, chaves, financiamento, permuta) e comissões.</summary>
public class ContratoVenda : EntidadeDaObra
{
    public int Numero { get; set; }
    public Guid UnidadeComercialId { get; set; }
    public Guid CompradorId { get; set; }
    public DateOnly DataContrato { get; set; }
    /// <summary>Data-base dos valores (a correção conta a partir dela).</summary>
    public DateOnly DataBase { get; set; }
    public decimal ValorVenda { get; set; }
    public string IndiceAteChaves { get; set; } = "INCC";
    public string IndiceAposChaves { get; set; } = "IPCA";
    public decimal JurosAposChavesMes { get; set; }
    public DateOnly? DataChaves { get; set; }
    public StatusContratoVenda Status { get; set; } = StatusContratoVenda.Ativo;
    public Guid? TituloId { get; set; }
    public DateOnly? DataDistrato { get; set; }
    public decimal? ValorDevolvido { get; set; }
    public string? Observacao { get; set; }
    public ICollection<BlocoCondicaoVenda> Blocos { get; set; } = [];
    public ICollection<ComissaoVenda> Comissoes { get; set; } = [];

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (UnidadeComercialId == Guid.Empty) e.Add("Escolha a unidade.");
        if (CompradorId == Guid.Empty) e.Add("Escolha o comprador.");
        if (ValorVenda <= 0) e.Add("Informe o valor da venda.");
        if (DataContrato == default) e.Add("Informe a data do contrato.");
        if (Blocos.Count == 0) e.Add("Monte a condição de pagamento (entrada, mensais, balões, chaves…).");
        foreach (var b in Blocos)
        {
            if (b.Quantidade <= 0 || b.ValorParcela <= 0) e.Add($"{NomesVenda.Bloco(b.Tipo)}: quantidade e valor devem ser maiores que zero.");
            if (b.Tipo == TipoParcelaVenda.Permuta && b.FornecedorPermutaId is null) e.Add("Permuta: escolha o fornecedor que entrega em troca.");
        }
        var soma = Blocos.Sum(b => b.Total);
        if (Blocos.Count > 0 && Math.Abs(soma - ValorVenda) > 0.05m) e.Add($"A condição soma {soma.ToString("N2", Sigeo.Domain.Financeiro.Cultura.Br)}, diferente do valor da venda ({ValorVenda.ToString("N2", Sigeo.Domain.Financeiro.Cultura.Br)}).");
        foreach (var c in Comissoes)
        {
            if (c.FornecedorId == Guid.Empty) e.Add("Comissão: escolha o corretor/imobiliária (cadastro de fornecedores).");
            if (c.Valor <= 0) e.Add("Comissão: informe o percentual ou o valor.");
            if (c.Forma == FormaComissao.Parcelada && c.Parcelas < 2) e.Add("Comissão parcelada: informe 2 parcelas ou mais.");
            if (c.Forma == FormaComissao.AposPercentualPago && c.PercentualPagoMinimo is not (> 0 and <= 100)) e.Add("Comissão após % pago: informe o percentual (1 a 100).");
        }
        return e;
    }
}
