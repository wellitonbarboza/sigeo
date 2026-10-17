using Sigeo.Domain.Common;

namespace Sigeo.Domain.Financeiro;

/// <summary>
/// Condição de pagamento estruturada: entrada (%) + prazos em dias ("30/60/90"). Gera as parcelas a partir do total e da data-base.
/// Ex.: "À vista" = prazos "0"; "30/60" = "30;60"; "1+3" = entrada 25% + "30;60;90".
/// </summary>
public class CondicaoPagamento : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    public decimal EntradaPerc { get; set; }
    /// <summary>Prazos em dias separados por ";" (as parcelas depois da entrada dividem o restante igualmente).</summary>
    public string Prazos { get; set; } = "0";
    public bool Ativa { get; set; } = true;

    public IReadOnlyList<int> Dias => Prazos.Split([';', '/', ',', ' '], StringSplitOptions.RemoveEmptyEntries).Select(x => int.TryParse(x, out var d) ? d : -1).Where(d => d >= 0).ToList();

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Nome)) e.Add("Informe o nome da condição.");
        if (EntradaPerc is < 0 or > 100) e.Add("Entrada entre 0 e 100%.");
        if (Dias.Count == 0 && EntradaPerc < 100) e.Add("Informe os prazos em dias (ex.: 30;60;90).");
        return e;
    }

    /// <summary>Parcelas (vencimento, valor) — a última absorve o arredondamento.</summary>
    public IReadOnlyList<(DateOnly Vencimento, decimal Valor)> Gerar(decimal total, DateOnly dataBase)
    {
        var r = new List<(DateOnly, decimal)>();
        var entrada = Math.Round(total * EntradaPerc / 100m, 2);
        if (entrada > 0) r.Add((dataBase, entrada));
        var dias = Dias;
        var resto = total - entrada;
        if (dias.Count == 0) { if (resto != 0) r.Add((dataBase, resto)); return r; }
        var cada = Math.Round(resto / dias.Count, 2);
        for (var i = 0; i < dias.Count; i++)
            r.Add((dataBase.AddDays(dias[i]), i == dias.Count - 1 ? resto - cada * (dias.Count - 1) : cada));
        return r;
    }
}
