namespace Sigeo.Domain.Common;

public enum TipoRequisicao { Material = 1, Servico = 2 }

public enum StatusRequisicao
{
    Rascunho = 0, AguardandoEngenheiro = 1, AguardandoDiretorTecnico = 2, Autorizada = 3,
    NaoAutorizada = 4, DevolvidaAjuste = 5, EmCotacao = 6, PedidoEmitido = 7, Concluida = 8, Cancelada = 9,
    ParcialmenteAtendida = 10, Atendida = 11, EncerradaComSaldo = 12
}

public enum StatusItemRequisicao { Aberto = 1, Orcado = 2, Pedido = 3, Recebido = 4, Cancelado = 5 }

public enum TipoFrete { CIF = 1, FOB = 2, NaoSeAplica = 3 }

public enum StatusCotacao { Aberto = 1, AguardandoPropostas = 2, EmAnalise = 3, AguardandoAprovacao = 4, Aprovado = 5, ReprovadoVoltaCotacao = 6, PedidoGerado = 7, Cancelado = 8 }

public enum CriterioSelecao { MenorPreco = 1, Prazo = 2, Tecnica = 3, Historico = 4 }

public enum StatusConvite { Convidado = 1, Respondido = 2, Declinou = 3, Vencedor = 4 }

public enum TipoDocumentoCompra { PedidoCompra = 1, Contrato = 2 }

public enum StatusPedido { Emitido = 1, AguardandoAprovacaoDiretor = 2, Aprovado = 3, EnviadoFornecedor = 4, EntregueParcial = 5, Entregue = 6, Cancelado = 7 }

public enum StatusContrato { EmElaboracao = 1, AguardandoAprovacaoDiretoria = 2, AguardandoAssinaturas = 3, Ativo = 4, Suspenso = 5, Encerrado = 6, Rescindido = 7 }

public enum FormalizacaoContrato { ContratoPadrao = 1, AceiteProposta = 2 }

public enum ResultadoCriterio { NaoAvaliado = 0, Aprovado = 1, Reprovado = 2 }

public enum DestinoRecebimento { Aceito = 1, AceitoComRessalva = 2, Devolvido = 3 }

public enum StatusMedicao { EmElaboracao = 1, AguardandoAprovacao = 2, Aprovada = 3, Reprovada = 4, NfRecebida = 5, Paga = 6 }

public enum FaixaNota { Nenhuma = 0, PlanoPontual = 1, ReuniaoPlanoCompleto = 2 }

public enum OrigemPlanoAcao { Medicao = 1, Fvm = 2, AvaliacaoSemestral = 3, Reclamacao = 4 }

public enum TipoPlanoAcao { Pontual = 1, Completo = 2 }

public enum StatusPlanoAcao { Aberta = 1, EmAndamento = 2, Concluida = 3 }

public enum Eficacia { NaoVerificada = 0, Eficaz = 1, NaoEficaz = 2 }

public enum DecisaoSemestral { ManterQualificado = 1, ManterComPlanoAcao = 2, Desqualificar = 3 }

public enum OrigemPagamento { NotaMaterial = 1, MedicaoServico = 2 }

public enum StatusPagamento { Aguardando = 1, Liberado = 2, Pago = 3, Bloqueado = 4 }

public enum TipoAditivo { Valor = 1, Prazo = 2, Escopo = 3, Combinado = 4 }

public enum StatusAditivo { EmElaboracao = 1, AguardandoAprovacao = 2, AguardandoAssinaturas = 3, Vigente = 4, Reprovado = 5 }

public enum StatusObra { Planejamento = 1, EmExecucao = 2, Concluida = 3, Operacao = 4 }

public enum MomentoDocumento { Cadastro = 1, PorContrato = 2, PorEntrega = 3 }

public enum StatusSolicitacaoDoc { Enviada = 1, Acessada = 2, Parcial = 3, Concluida = 4, Expirada = 5 }

[Flags] public enum CanalNotificacao { Nenhum = 0, Pendencias = 1, Email = 2, WhatsApp = 4 }
