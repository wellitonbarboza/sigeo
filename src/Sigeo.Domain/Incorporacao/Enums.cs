using Sigeo.Domain.Common;
using Sigeo.Domain.Financeiro;

namespace Sigeo.Domain.Incorporacao;

public enum RegimeTributario { RetGeral = 1, RetSocial = 2, LucroPresumido = 3, LucroReal = 4 }

public enum SituacaoUnidade { Disponivel = 1, Reservada = 2, Proposta = 3, Vendida = 4, Permuta = 5, Bloqueada = 6 }

public enum StatusContratoVenda { Ativo = 1, Quitado = 2, Distratado = 3 }

public enum FormaComissao { AVista = 1, Parcelada = 2, AposPercentualPago = 3 }
