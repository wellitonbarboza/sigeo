using Groundkey.Domain.Common;

namespace Groundkey.Domain.Financeiro;

internal static class Cultura { public static readonly System.Globalization.CultureInfo Br = new("pt-BR"); }

public enum NaturezaConta { Receita = 1, Despesa = 2 }
public enum TipoTitulo { Pagar = 1, Receber = 2 }
public enum StatusTitulo { Aberto = 1, Parcial = 2, Liquidado = 3, Cancelado = 4 }
public enum StatusParcelaTitulo { Aberta = 1, Paga = 2, Cancelada = 3 }
public enum TipoContaBancaria { Corrente = 1, Aplicacao = 2, Caixa = 3 }
public enum FormaPagamento { Pix = 1, Boleto = 2, Transferencia = 3, Deposito = 4, Dinheiro = 5, Cartao = 6, DebitoAutomatico = 7, Permuta = 8, Compensacao = 9, Financiamento = 10 }
public enum OrigemTitulo { Manual = 1, LiberacaoPagamento = 2, ImportacaoErp = 3, VendaUnidade = 4, Comissao = 5, ApuracaoRet = 6, Distrato = 7 }
/// <summary>Tipo da parcela a receber de uma venda (a carteira agrupa por ele).</summary>
public enum TipoParcelaVenda { Avulsa = 0, Entrada = 1, Mensal = 2, Intermediaria = 3, Chaves = 4, Financiamento = 5, Permuta = 6 }

/// <summary>Conta do plano de contas gerencial (hierárquico pelo código: 2 › 2.1 › 2.1.3). Lança-se só na conta analítica (sem filhas).</summary>
public class ContaGerencial : EntidadeAuditavel
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public NaturezaConta Natureza { get; set; } = NaturezaConta.Despesa;
    public bool Ativa { get; set; } = true;

    /// <summary>Código do pai ("2.1.3" → "2.1"; "2" → null).</summary>
    public static string? CodigoPai(string codigo) => codigo.LastIndexOf('.') is var i and > 0 ? codigo[..i] : null;
    public int Nivel => Codigo.Count(c => c == '.') + 1;
    public string Rotulo => $"{Codigo} {Nome}";
}

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

/// <summary>Movimento da conta bancária: baixa de parcela, transferência entre contas ou lançamento avulso (tarifa, rendimento).</summary>
public class MovimentoBancario : EntidadeAuditavel
{
    public Guid ContaBancariaId { get; set; }
    public DateOnly Data { get; set; }
    /// <summary>Entrada positiva, saída negativa.</summary>
    public decimal Valor { get; set; }
    public string Historico { get; set; } = string.Empty;
    public Guid? TituloParcelaId { get; set; }
    public Guid? TransferenciaId { get; set; }
    public Guid? ContaGerencialId { get; set; }
    public Guid? ObraId { get; set; }
    public bool Conciliado { get; set; }
}

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
