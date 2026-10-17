namespace Sigeo.Domain.Common;

public enum TipoEstabelecimento { Matriz = 1, Filial = 2, Spe = 3, Outro = 4 }

public enum TipoFluxo { Transferencia = 1, Venda = 2, Rateio = 3, TaxaAdministracao = 4, Remessa = 5 }

public enum StatusTransferencia { Rascunho = 1, Enviada = 2, Recebida = 3, Cancelada = 4 }
