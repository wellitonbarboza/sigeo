namespace QualyHub.Domain.Common;

// ───────── Acesso e aprovação ─────────
public enum PerfilUsuario { Administrador = 1, Diretoria, Engenharia, Qualidade, Suprimentos, Financeiro, Campo, Terceiro, Almoxarifado, Seguranca }

public enum StatusAprovacao { Rascunho = 0, EmAprovacao = 1, Aprovado = 2, Reprovado = 3, Cancelado = 4 }

public enum DecisaoAprovacao { Pendente = 0, Aprovado = 1, Reprovado = 2, DevolvidoParaAjuste = 3 }

/// <summary>Objetos que passam pelo motor de aprovação (alçadas por tipo × obra × valor).</summary>
public enum EntidadeAprovavel
{
    Pedido = 1, Medicao = 2, DocumentoSgq = 3, AvaliacaoFornecedor = 4,
    Requisicao = 5, Cotacao = 6, Contrato = 7, Pagamento = 8, Aditivo = 9
}

// ───────── Cadastros / fornecedores ─────────
public enum StatusDocumento { PendenteEnvio = 0, Valido = 1, AVencer = 2, Vencido = 3, Reprovado = 4, EmValidacao = 5 }

/// <summary>Status de qualificação exibido no cadastro (T08), derivado da última qualificação (T09).</summary>
public enum StatusQualificacao { EmQualificacao = 1, Qualificado = 2, Vencida = 3, NaoQualificado = 4, Desqualificado = 5 }

[Flags]
public enum TipoFornecimento
{
    Nenhum = 0, Material = 1, ServicoObra = 2, Laboratorio = 4, ServicoEspecializadoEngenharia = 8, Consultoria = 16, Locacao = 32
}

public enum ResultadoQualificacao { EmQualificacao = 1, Qualificado = 2, NaoQualificado = 3 }

public enum SituacaoPsq { NaoAplicavel = 0, Qualificado = 1, NaoConforme = 2, NaoEscopoPsq = 3 }

/// <summary>Enquadramento de laboratório (SiAC Anexo 7): a) acreditado CGCRE … f).</summary>
public enum EnquadramentoLaboratorio { NaoSeAplica = 0, A_Acreditado = 1, B_EmAcreditacao = 2, C_Avaliado = 3, D_InstituicaoPesquisa = 4, E_Outro = 5, F_Outro = 6 }

public enum AplicaA { Fornecedor = 1, Funcionario = 2 }

// ───────── Suprimentos ─────────
public enum TipoRequisicao { Material = 1, Servico = 2 }

public enum StatusRequisicao
{
    Rascunho = 0, AguardandoEngenheiro = 1, AguardandoDiretorTecnico = 2, Autorizada = 3,
    NaoAutorizada = 4, DevolvidaAjuste = 5, EmCotacao = 6, PedidoEmitido = 7, Concluida = 8, Cancelada = 9
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

// ───────── Documentos do SGQ ─────────
public enum OrigemDocumento { Interno = 1, Externo = 2 }

public enum AbrangenciaDocumento { Corporativo = 1, PorObra = 2 }

public enum StatusDocumentoSgq { EmElaboracao = 1, Vigente = 2, EmRevisao = 3, Cancelado = 4 }

public enum StatusRevisao
{
    EmElaboracao = 1, EmAnaliseCritica = 2, DevolvidaAjuste = 3, AguardandoAprovacao = 4,
    AprovadaAguardandoVigencia = 5, Vigente = 6, VigenteComRessalvas = 7, Recusada = 8, Obsoleta = 9, Cancelada = 10
}

public enum MotivoRevisao { EmissaoInicial = 1, Melhoria = 2, NaoConformidade = 3, Auditoria = 4, MudancaRequisito = 5, AnaliseCriticaPeriodica = 6 }

public enum EtapaRevisaoTipo { Elaboracao = 1, AnaliseCritica = 2, Aprovacao = 3, Publicacao = 4 }

public enum DecisaoEtapa { Pendente = 0, Aprovado = 1, AprovadoComRessalvas = 2, Devolvido = 3, Recusado = 4 }

public enum NivelAcesso { PublicoInterno = 1, RestritoArea = 2, Confidencial = 3 }

public enum TipoCopia { FisicaControlada = 1, EletronicaControlada = 2, ExternaGrd = 3 }

public enum StatusCopia { EmUso = 1, RecolhimentoPendente = 2, Recolhida = 3, Extraviada = 4 }

public enum DestinoObsoleta { Destruida = 1, ArquivadaObsoleta = 2 }

public enum StatusSolicitacaoCopia { Solicitada = 1, Liberada = 2, Impressa = 3, Entregue = 4, Encerrada = 5, Recusada = 6 }

public enum StatusCiencia { Pendente = 1, Lido = 2, Atrasado = 3 }

public enum EscopoPermissao { Pasta = 1, Documento = 2 }

public enum SituacaoDocExterno { Vigente = 1, Substituida = 2, Cancelada = 3 }

public enum EventoDocumentoTipo
{
    Criacao = 1, NovaRevisao, AlteracaoMetadado, AlteracaoNomenclatura, AlteracaoStatus, AprovacaoDevolucao, Publicacao, Obsolescencia,
    Visualizacao, Download, ImpressaoControlada, ImpressaoNaoControlada, CheckoutCheckin, LiberacaoForcadaCheckout,
    CopiaDistribuida, CopiaRecolhida, ArquivoMovido, PermissaoAlterada, LeituraConfirmada, ExclusaoLogica
}

// ───────── Qualidade da obra (Q01–Q15) ─────────
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

// ───────── Segurança e pessoas (S01–S13) ─────────
public enum VinculoColaborador { Proprio = 1, Terceiro = 2 }
public enum SituacaoColaborador { Apto = 1, AptoComRestricao = 2, Pendente = 3, Bloqueado = 4, DesligadoDaObra = 5 }
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

// ───────── Contratos: aditivo e notificações (T28, T27) ─────────
public enum TipoAditivo { Valor = 1, Prazo = 2, Escopo = 3, Combinado = 4 }
public enum StatusAditivo { EmElaboracao = 1, AguardandoAprovacao = 2, AguardandoAssinaturas = 3, Vigente = 4, Reprovado = 5 }
public enum StatusObra { Planejamento = 1, EmExecucao = 2, Concluida = 3, Operacao = 4 }
public enum MomentoDocumento { Cadastro = 1, PorContrato = 2, PorEntrega = 3 }
public enum StatusSolicitacaoDoc { Enviada = 1, Acessada = 2, Parcial = 3, Concluida = 4, Expirada = 5 }
[Flags] public enum CanalNotificacao { Nenhum = 0, Pendencias = 1, Email = 2, WhatsApp = 4 }

// ───────── Importação ─────────
public enum TipoImportacao { Fornecedores = 1, Funcionarios = 2, Pedidos = 3, Medicoes = 4, Servicos = 5, Materiais = 6, Produtos = 7 }

public enum StatusImportacao { Enviada = 1, EmValidacao = 2, ComErros = 3, AguardandoConfirmacao = 4, Concluida = 5, Cancelada = 6 }

public enum AcaoLinhaImportacao { Inserir = 1, Atualizar = 2, SemAlteracao = 3, Erro = 4 }

/// <summary>Listas configuráveis pelo usuário (tabela ItemLista). Carga inicial genérica, editável na tela.</summary>
public enum TipoLista
{
    Atividade = 1, CategoriaServico = 2, CriterioQualificacao = 3, ProcessoArea = 4, TipoDocumentoSgq = 5,
    EmissorExterno = 6, CondicaoPagamento = 7, Funcao = 8, TipoTarefa = 9, TipoAgrupamento = 10,
    TipoComodo = 11, GrupoServico = 12, CategoriaDocumento = 13
}

public enum PrioridadeTarefa { Baixa, Normal, Alta, Urgente }
public enum StatusTarefa { Aberta, EmAndamento, Concluida, Cancelada }

// ───────── Engenharia: estrutura do condomínio, serviços e orçamento ─────────
/// <summary>Tipologia de apartamento (cômodos), de torre (pavimentos, mix e áreas comuns) ou implantação do condomínio (áreas comuns).</summary>
public enum TipoTipologia { Apartamento = 1, Torre = 2, Implantacao = 3 }
/// <summary>Onde o ambiente-modelo é criado: cômodo do apto, área comum do pavimento, da torre ou do condomínio.</summary>
public enum EscopoAmbiente { Comodo = 1, AreaComumPavimento = 2, AreaComumTorre = 3, AreaComumCondominio = 4 }
/// <summary>Como a quantidade do serviço sai da geometria do local.</summary>
public enum RegraQuantidade { AreaPiso = 1, AreaParede = 2, Perimetro = 3, AreaTeto = 4, Unidade = 5, Fixa = 6 }
/// <summary>Em que nível o serviço é aplicado dentro da tipologia.</summary>
public enum AlvoServico { Ambiente = 1, Apartamento = 2, Pavimento = 3, Torre = 4, Condominio = 5 }
public enum TipoInsumo { Material = 1, MaoObraPropria = 2, MaoObraEmpreitada = 3, Equipamento = 4, ServicoTerceiro = 5 }

// ───────── Fornecedores: matriz de documentos ─────────
public enum FrequenciaDocumento { NoCadastro = 1, Validade = 2, Mensal = 3, PorContrato = 4, PorMedicao = 5, PorEntrega = 6, Anual = 7 }

// ───────── Estabelecimentos e logística ─────────
public enum TipoEstabelecimento { Matriz = 1, Filial = 2, Spe = 3, Outro = 4 }
public enum TipoFluxo { Transferencia = 1, Venda = 2, Rateio = 3, TaxaAdministracao = 4, Remessa = 5 }
public enum StatusTransferencia { Rascunho = 1, Enviada = 2, Recebida = 3, Cancelada = 4 }
