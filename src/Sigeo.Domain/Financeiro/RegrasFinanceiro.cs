using Sigeo.Domain.Common;

namespace Sigeo.Domain.Financeiro;

/// <summary>Regras puras do financeiro (aging, fluxo).</summary>
public static class RegrasFinanceiro
{
    public static string Faixa(int dias) => dias switch { <= 0 => "A vencer", <= 30 => "1–30 dias", <= 60 => "31–60 dias", <= 90 => "61–90 dias", _ => "Mais de 90 dias" };
    public static readonly string[] Faixas = ["A vencer", "1–30 dias", "31–60 dias", "61–90 dias", "Mais de 90 dias"];

    public static string NomeForma(FormaPagamento f) => f switch
    {
        FormaPagamento.Pix => "PIX", FormaPagamento.Boleto => "Boleto", FormaPagamento.Transferencia => "TED/transferência", FormaPagamento.Deposito => "Depósito",
        FormaPagamento.Dinheiro => "Dinheiro", FormaPagamento.Cartao => "Cartão", FormaPagamento.DebitoAutomatico => "Débito automático",
        FormaPagamento.Permuta => "Permuta", FormaPagamento.Compensacao => "Compensação", _ => "Financiamento bancário"
    };

    /// <summary>Forma que não passa por conta bancária (não gera movimento).</summary>
    public static bool SemMovimento(FormaPagamento f) => f is FormaPagamento.Permuta or FormaPagamento.Compensacao;
}
