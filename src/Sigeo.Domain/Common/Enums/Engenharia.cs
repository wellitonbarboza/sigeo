namespace Sigeo.Domain.Common;

/// <summary>Tipologia de apartamento (cômodos), de torre (pavimentos, mix e áreas comuns) ou implantação do condomínio (áreas comuns).</summary>
public enum TipoTipologia { Apartamento = 1, Torre = 2, Implantacao = 3 }

/// <summary>Onde o ambiente-modelo é criado: cômodo do apto, área comum do pavimento, da torre ou do condomínio.</summary>
public enum EscopoAmbiente { Comodo = 1, AreaComumPavimento = 2, AreaComumTorre = 3, AreaComumCondominio = 4 }

/// <summary>Como a quantidade do serviço sai da geometria do local.</summary>
public enum RegraQuantidade { AreaPiso = 1, AreaParede = 2, Perimetro = 3, AreaTeto = 4, Unidade = 5, Fixa = 6 }

/// <summary>Em que nível o serviço é aplicado dentro da tipologia.</summary>
public enum AlvoServico { Ambiente = 1, Apartamento = 2, Pavimento = 3, Torre = 4, Condominio = 5 }

public enum TipoInsumo { Material = 1, MaoObraPropria = 2, MaoObraEmpreitada = 3, Equipamento = 4, ServicoTerceiro = 5 }

/// <summary>Natureza do custo (curva ABC, custo por tipo e, depois, plano de contas).</summary>
public enum TipoCusto { Material = 1, Equipamento = 2, MaoObra = 3, Servico = 4, Frete = 5, Outros = 6 }

/// <summary>De onde veio o lançamento de custo realizado.</summary>
public enum OrigemCusto { Manual = 1, ImportacaoErp = 2, Recebimento = 3, Medicao = 4 }

/// <summary>De-para de importação: o que está sendo ligado (o texto do sistema de origem → o cadastro do SIGEO).</summary>
public enum TipoVinculoOrigem { Produto = 1, Fornecedor = 2, Fase = 3, Local = 4, ContaGerencial = 5, ContaBancaria = 6 }
