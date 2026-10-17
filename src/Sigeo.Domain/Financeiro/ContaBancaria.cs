using Sigeo.Domain.Common;

namespace Sigeo.Domain.Financeiro;

/// <summary>Conta bancária ou caixa da construtora/SPE. O número fica só para identificação e aparece mascarado nas telas.</summary>
public class ContaBancaria : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    public TipoContaBancaria Tipo { get; set; } = TipoContaBancaria.Corrente;
    public string? Banco { get; set; }
    public string? Agencia { get; set; }
    public string? Numero { get; set; }
    /// <summary>Titular (Matriz/Filial/SPE).</summary>
    public Guid? EstabelecimentoId { get; set; }
    public decimal SaldoInicial { get; set; }
    public DateOnly DataSaldoInicial { get; set; }
    public bool Ativa { get; set; } = true;

    public string NumeroMascarado => string.IsNullOrWhiteSpace(Numero) ? "" : "•••" + new string(Numero.Where(char.IsLetterOrDigit).TakeLast(3).ToArray());
    public string Rotulo => string.Join(" · ", new[] { Nome, Banco, NumeroMascarado }.Where(x => !string.IsNullOrWhiteSpace(x)));
}
