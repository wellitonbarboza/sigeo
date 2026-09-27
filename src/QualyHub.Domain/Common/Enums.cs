namespace QualyHub.Domain.Common;

// ───────── Acesso e aprovação ─────────
public enum PerfilUsuario { Administrador = 1, Diretoria, Engenharia, Qualidade, Suprimentos, Financeiro, Campo, Terceiro, Almoxarifado }

public enum StatusAprovacao { Rascunho = 0, EmAprovacao = 1, Aprovado = 2, Reprovado = 3, Cancelado = 4 }

public enum DecisaoAprovacao { Pendente = 0, Aprovado = 1, Reprovado = 2, DevolvidoParaAjuste = 3 }

/// <summary>Objetos que passam pelo motor de aprovação (alçadas por tipo × obra × valor).</summary>
public enum EntidadeAprovavel
{
    Pedido = 1, Medicao = 2, DocumentoSgq = 3, AvaliacaoFornecedor = 4,
    Requisicao = 5, Cotacao = 6, Contrato = 7, Pagamento = 8
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

public enum TipoFrete { CIF = 1, FOB = 2 }

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

// ───────── Qualidade (fichas, RNC) ─────────
public enum TipoFicha { FVS = 1, FVM = 2 }

public enum StatusFicha { Rascunho = 0, Enviada = 1, Aprovada = 2, Reprovada = 3, AprovadaAposReinspecao = 4 }

public enum ResultadoItem { NaoAvaliado = 0, Conforme = 1, NaoConforme = 2, NaoAplica = 3 }

public enum TipoResposta { ConformeNaoConforme = 1, Numero = 2, Texto = 3, Data = 4, Foto = 5 }

public enum StatusRnc { Aberta = 1, EmTratativa = 2, AguardandoEficacia = 3, Encerrada = 4 }

public enum OrigemRnc { Ficha = 1, AuditoriaInterna = 2, AuditoriaExterna = 3, Cliente = 4, Fornecedor = 5, Outra = 99 }

// ───────── Importação ─────────
public enum TipoImportacao { Fornecedores = 1, Funcionarios = 2, Pedidos = 3, Medicoes = 4, Servicos = 5, Materiais = 6, Produtos = 7 }

public enum StatusImportacao { Enviada = 1, EmValidacao = 2, ComErros = 3, AguardandoConfirmacao = 4, Concluida = 5, Cancelada = 6 }

public enum AcaoLinhaImportacao { Inserir = 1, Atualizar = 2, SemAlteracao = 3, Erro = 4 }

/// <summary>Listas configuráveis pelo usuário (tabela ItemLista). Carga inicial genérica, editável na tela.</summary>
public enum TipoLista
{
    Atividade = 1, CategoriaServico = 2, CriterioQualificacao = 3, ProcessoArea = 4, TipoDocumentoSgq = 5,
    EmissorExterno = 6, CondicaoPagamento = 7, Funcao = 8
}
