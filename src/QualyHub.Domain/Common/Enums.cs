namespace QualyHub.Domain.Common;

public enum PerfilUsuario { Administrador = 1, Diretoria, Engenharia, Qualidade, Suprimentos, Financeiro, Campo, Terceiro }

public enum StatusAprovacao { Rascunho = 0, EmAprovacao = 1, Aprovado = 2, Reprovado = 3, Cancelado = 4 }

public enum DecisaoAprovacao { Pendente = 0, Aprovado = 1, Reprovado = 2, DevolvidoParaAjuste = 3 }

public enum EntidadeAprovavel { Pedido = 1, Medicao = 2, DocumentoSgq = 3, AvaliacaoFornecedor = 4 }

public enum StatusDocumento { PendenteEnvio = 0, Valido = 1, AVencer = 2, Vencido = 3, Reprovado = 4 }

public enum SituacaoFornecedor { EmQualificacao = 1, Qualificado = 2, QualificadoComRestricao = 3, Bloqueado = 4 }

public enum TipoFicha { FVS = 1, FVM = 2 }

public enum StatusFicha { Rascunho = 0, Enviada = 1, Aprovada = 2, Reprovada = 3, AprovadaAposReinspecao = 4 }

public enum ResultadoItem { NaoAvaliado = 0, Conforme = 1, NaoConforme = 2, NaoAplica = 3 }

public enum TipoResposta { ConformeNaoConforme = 1, Numero = 2, Texto = 3, Data = 4, Foto = 5 }

public enum StatusRnc { Aberta = 1, EmTratativa = 2, AguardandoEficacia = 3, Encerrada = 4 }

public enum OrigemRnc { Ficha = 1, AuditoriaInterna = 2, AuditoriaExterna = 3, Cliente = 4, Fornecedor = 5, Outra = 99 }

public enum StatusDocumentoSgq { EmElaboracao = 1, EmAnalise = 2, Vigente = 3, Obsoleto = 4 }

public enum TipoDocumentoSgq { MQ = 1, PO = 2, PEIS = 3, DS = 4, FORM = 5, PQO = 6, Outro = 99 }

public enum TipoImportacao { Fornecedores = 1, Funcionarios = 2, Pedidos = 3, Medicoes = 4, Servicos = 5, Materiais = 6 }

public enum StatusImportacao { Enviada = 1, EmValidacao = 2, ComErros = 3, AguardandoConfirmacao = 4, Concluida = 5, Cancelada = 6 }

public enum AcaoLinhaImportacao { Inserir = 1, Atualizar = 2, SemAlteracao = 3, Erro = 4 }

public enum AplicaA { Fornecedor = 1, Funcionario = 2 }
