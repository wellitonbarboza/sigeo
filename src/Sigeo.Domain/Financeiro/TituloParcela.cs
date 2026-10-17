using Sigeo.Domain.Common;

namespace Sigeo.Domain.Financeiro;

public class TituloParcela : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid TituloId { get; set; }
    public Titulo Titulo { get; set; } = null!;
    public int Numero { get; set; }
    public DateOnly Vencimento { get; set; }
    /// <summary>Valor a pagar/receber (na venda: valor na data-base, antes da correção).</summary>
    public decimal Valor { get; set; }
    public StatusParcelaTitulo Status { get; set; } = StatusParcelaTitulo.Aberta;
    public TipoParcelaVenda TipoVenda { get; set; } = TipoParcelaVenda.Avulsa;
    /// <summary>Permuta: fornecedor que entrega bens/serviços em troca da unidade.</summary>
    public Guid? FornecedorPermutaId { get; set; }

    // Baixa
    public DateOnly? DataPagamento { get; set; }
    public decimal Correcao { get; set; }
    public decimal Juros { get; set; }
    public decimal Multa { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorPago { get; set; }
    public Guid? ContaBancariaId { get; set; }
    public FormaPagamento? Forma { get; set; }
    public string? Comprovante { get; set; }

    public decimal TotalBaixa => Valor + Correcao + Juros + Multa - Desconto;
    public bool Vencida(DateOnly hoje) => Status == StatusParcelaTitulo.Aberta && Vencimento < hoje;
    public int DiasAtraso(DateOnly hoje) => Vencida(hoje) ? hoje.DayNumber - Vencimento.DayNumber : 0;
}
