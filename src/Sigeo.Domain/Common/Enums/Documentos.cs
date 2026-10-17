namespace Sigeo.Domain.Common;

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
