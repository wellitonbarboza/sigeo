using System.Globalization;
using System.Text;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Fiscal;

public enum SituacaoNota { Recebida = 1, Vinculada = 2, Recebimento = 3, Cancelada = 4, Ignorada = 5 }

public enum OrigemNota { Manual = 1, Sieg = 2 }

/// <summary>Quem ligou a nota/item ao pedido: regra automática (CNPJ, nº do pedido, descrição), IA (sugestão aceita) ou pessoa.</summary>
public enum OrigemVinculo { Regra = 1, Ia = 2, Manual = 3 }
