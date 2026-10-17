using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

public enum SituacaoLote { EmCompra = 1, EmRecebimento = 2, Atendido = 3, Encerrado = 4 }

// ═══════════════════════ Contrato recorrente: parcelas (locação, serviço contínuo, manutenção) ═══════════════════════

/// <summary>Medição = medição com avaliação (como sempre foi). As demais geram parcelas automáticas.</summary>
public enum FormaCobranca { Medicao = 0, ValorFixo = 1, Diaria = 2, Uso = 3 }

public enum Periodicidade { Mensal = 1, Quinzenal = 2, Semanal = 3, DatasCombinadas = 4 }

public enum StatusParcela { Prevista = 1, Conferida = 2, Aprovada = 3, Liberada = 4, Paga = 5, Cancelada = 6 }

public enum BaseEconomia { ComBase = 1, SemBase = 2, Emergencial = 3, Desvio = 4 }

// ═══════════════════════ Equipamentos (patrimônio próprio e locados) e manutenção ═══════════════════════

public enum PropriedadeEquipamento { Propria = 1, Locada = 2, Terceiro = 3 }

public enum Criticidade { A = 1, B = 2, C = 3 }

public enum SituacaoEquipamento { Disponivel = 1, EmUso = 2, EmManutencao = 3, Parado = 4, Devolvido = 5, Baixado = 6 }

public enum TipoMovimentoEquipamento { Chegada = 1, Transferencia = 2, Devolucao = 3, EnvioManutencao = 4, RetornoManutencao = 5, Baixa = 6 }

public enum GatilhoManutencao { Tempo = 1, Uso = 2, Evento = 3 }

public enum TipoOrdemServico { Preventiva = 1, Corretiva = 2, Inspecao = 3 }

public enum StatusOrdemServico { Aberta = 1, EmExecucao = 2, AguardandoPeca = 3, Concluida = 4, Verificada = 5, Cancelada = 6 }

// ═══════════════════════ Estoque físico e gerencial (almoxarifado central + almoxarifados das obras) ═══════════════════════

public enum TipoAlmoxarifado { Central = 1, Obra = 2 }

public enum TipoMovimentoEstoque
{
    /// <summary>Entrada pelo recebimento com FVM (disponível ou bloqueada, conforme o lote).</summary>
    Entrada = 1,
    /// <summary>Saída para aplicação (apartamento/local + serviço), avulsa ou por kit.</summary>
    Saida = 2,
    TransferenciaSaida = 3,
    TransferenciaEntrada = 4,
    /// <summary>Sobra devolvida pela frente de serviço.</summary>
    Devolucao = 5,
    AjusteInventario = 6,
    Perda = 7,
    /// <summary>Lote liberado após o ensaio: sai do bloqueado e entra no disponível.</summary>
    Liberacao = 8,
    /// <summary>Saldo inicial (inventário de implantação).</summary>
    Implantacao = 9,
    /// <summary>Entrega de EPI ao colaborador.</summary>
    EntregaEpi = 10,
}

public enum TipoInventario { Implantacao = 1, Ciclico = 2, Geral = 3 }

public enum StatusInventario { EmContagem = 1, AguardandoAprovacao = 2, Aprovado = 3, Cancelado = 4 }

public enum OrigemKit { Automatico = 1, Manual = 2 }

public enum SituacaoProposta
{
    ConviteRecebido = 1, EmPreenchimento = 2, PropostaEnviada = 3, Declinada = 4, PrazoEncerrado = 5,
    EmAnalise = 6, Vencedora = 7, VencedoraParcial = 8, NaoVencedora = 9, Cancelada = 10
}

public enum SituacaoCompetencia { Aberta = 1, Enviada = 2, EmConferencia = 3, ComPendencia = 4, Conferida = 5 }

public enum SituacaoItemCompetencia { Pendente = 1, Enviado = 2, Aprovado = 3, Recusado = 4, AdiamentoSolicitado = 5, Adiado = 6 }
