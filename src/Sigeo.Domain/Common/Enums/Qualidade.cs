namespace Sigeo.Domain.Common;

public enum TipoModelo { FVS = 1, FVM = 2, SST = 3 }

public enum SituacaoModelo { Rascunho = 0, Vigente = 1, EmRevisao = 2, Obsoleto = 3 }

public enum ResultadoItem { NaoAvaliado = 0, Conforme = 1, NaoConforme = 2, NaoAplica = 3 }

public enum FotoItem { Opcional = 0, Obrigatoria = 1, SoEmNaoConformidade = 2 }

public enum StatusFvs { EmInspecao = 1, Aprovada = 2, Reprovada = 3, Reinspecao = 4, Liberada = 5, Cancelada = 6 }

public enum OrigemRegistro { Web = 1, CelularOnline = 2, CelularOffline = 3 }

public enum OrigemRnc { Fvs = 1, FvmRecebimento = 2, AuditoriaInterna = 3, AuditoriaExterna = 4, ReclamacaoCliente = 5, Fornecedor = 6, ProcessoInterno = 7, InspecaoSeguranca = 8 }

public enum ClassificacaoRnc { Servico = 1, Material = 2, Projeto = 3, Sistema = 4, ReclamacaoCliente = 5, Seguranca = 6 }

public enum Gravidade { Baixa = 1, Media = 2, Alta = 3, RiscoGraveIminente = 4 }

public enum StatusRnc { Aberta = 1, EmTratamento = 2, AguardandoEficacia = 3, Encerrada = 4, Cancelada = 5 }

public enum MetodoAnalise { CincoPorques = 1, Ishikawa = 2, Outro = 3 }

public enum StatusAcaoCorretiva { EmAnalise = 1, EmExecucao = 2, AguardandoEficacia = 3, Encerrada = 4 }

public enum StatusItemPlano { Pendente = 1, Feito = 2, Cancelado = 3 }

public enum TipoEquipamento { Trena = 1, Nivel = 2, Prumo = 3, Esquadro = 4, Balanca = 5, Termometro = 6, Outro = 99 }

public enum TipoControleEquipamento { CalibracaoExterna = 1, VerificacaoInterna = 2, Padrao = 3 }

public enum StatusEquipamento { EmUso = 1, VenceEmBreve = 2, Bloqueado = 3, EmCalibracao = 4, Baixado = 5 }

public enum ResultadoCalibracao { Aprovado = 1, Reprovado = 2 }

public enum StatusAuditoria { Planejada = 1, EmExecucao = 2, RelatorioEmitido = 3, Encerrada = 4 }

public enum TipoConstatacao { NaoAvaliado = 0, Conforme = 1, NaoConformidade = 2, OportunidadeMelhoria = 3, Observacao = 4 }

public enum MomentoEnsaio { NaChegada = 1, Posterior = 2 }

public enum ExecutanteEnsaio { Obra = 1, LaboratorioExterno = 2 }

public enum AcaoReprovacaoEnsaio { RecusarCarga = 1, AbrirRnc = 2, RncComLocais = 3 }

public enum SituacaoEnsaio { Aguardando = 1, Aprovado = 2, Reprovado = 3 }

public enum StatusLote { EmVerificacao = 1, EmQuarentena = 2, Liberado = 3, Devolvido = 4, AceitoSobConcessao = 5 }
