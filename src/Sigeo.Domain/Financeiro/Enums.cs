using Sigeo.Domain.Common;

namespace Sigeo.Domain.Financeiro;

public enum NaturezaConta { Receita = 1, Despesa = 2 }

public enum TipoTitulo { Pagar = 1, Receber = 2 }

public enum StatusTitulo { Aberto = 1, Parcial = 2, Liquidado = 3, Cancelado = 4 }

public enum StatusParcelaTitulo { Aberta = 1, Paga = 2, Cancelada = 3 }

public enum TipoContaBancaria { Corrente = 1, Aplicacao = 2, Caixa = 3 }

public enum FormaPagamento { Pix = 1, Boleto = 2, Transferencia = 3, Deposito = 4, Dinheiro = 5, Cartao = 6, DebitoAutomatico = 7, Permuta = 8, Compensacao = 9, Financiamento = 10 }

public enum OrigemTitulo { Manual = 1, LiberacaoPagamento = 2, ImportacaoErp = 3, VendaUnidade = 4, Comissao = 5, ApuracaoRet = 6, Distrato = 7 }

/// <summary>Tipo da parcela a receber de uma venda (a carteira agrupa por ele).</summary>
public enum TipoParcelaVenda { Avulsa = 0, Entrada = 1, Mensal = 2, Intermediaria = 3, Chaves = 4, Financiamento = 5, Permuta = 6 }
