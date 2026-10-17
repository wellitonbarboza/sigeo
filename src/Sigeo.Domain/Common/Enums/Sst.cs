namespace Sigeo.Domain.Common;

public enum VinculoColaborador { Proprio = 1, Terceiro = 2 }

public enum SituacaoColaborador { Apto = 1, AptoComRestricao = 2, Pendente = 3, Bloqueado = 4, DesligadoDaObra = 5, AguardandoValidacao = 6 }

public enum TipoExigencia { Documento = 1, ExameAso = 2, Treinamento = 3, Epi = 4 }

public enum EfeitoVencimento { BloqueiaPessoa = 1, RestringeAtividade = 2, SoAlerta = 3 }

public enum Obrigatoriedade { Obrigatorio = 1, ConformeAtividade = 2 }

public enum ConclusaoAso { Apto = 1, Inapto = 2 }

[Flags] public enum AtividadeRisco { Nenhuma = 0, Altura = 1, EspacoConfinado = 2, Eletrico = 4, OperacaoEquipamento = 8, AQuente = 16, Icamento = 32, Andaime = 64 }

public enum OrigemDocColaborador { PortalFornecedor = 1, Sst = 2, Rh = 3 }

public enum ValidacaoDoc { Aguardando = 1, Validado = 2, Recusado = 3 }

public enum TipoTreinamento { NR = 1, Integracao = 2, Sgq = 3, Outro = 4 }

public enum MetodoEficaciaTreinamento { Prova = 1, ObservacaoEmCampo = 2, NaoSeAplica = 3 }

public enum StatusTurma { Programada = 1, Concluida = 2, Cancelada = 3 }

public enum ResultadoParticipante { Aprovado = 1, Reprovado = 2, NaoCompareceu = 3 }

public enum MotivoEntregaEpi { PrimeiraEntrega = 1, TrocaDesgaste = 2, Perda = 3, Dano = 4 }

public enum StatusInspecaoSst { EmAndamento = 1, EmTratamento = 2, Concluida = 3 }

public enum TipoPermissaoTrabalho { Altura = 1, AQuente = 2, Eletrico = 3, EspacoConfinado = 4, Icamento = 5 }

public enum StatusPermissaoTrabalho { Rascunho = 1, AguardandoLiberacao = 2, Liberada = 3, Suspensa = 4, Encerrada = 5 }

public enum PapelPt { Executante = 1, Vigia = 2, SupervisorEntrada = 3, Resgate = 4 }

public enum TipoOcorrencia { QuaseAcidente = 1, Incidente = 2, AcidenteSemAfastamento = 3, AcidenteComAfastamento = 4, DanoMaterial = 5 }

public enum StatusOcorrencia { Registrada = 1, EmInvestigacao = 2, Encerrada = 3 }
