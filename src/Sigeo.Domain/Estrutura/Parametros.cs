using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>Chaves dos parâmetros e valores padrão (carga inicial genérica, editável).</summary>
public static class Parametros
{
    public const string LimiteUmOrcamento = "SUP.LIMITE_1_ORCAMENTO";
    public const string OrcamentosAcimaLimite = "SUP.ORCAMENTOS_ACIMA_LIMITE";
    public const string ValidadeQualifMaterialMeses = "SUP.VALIDADE_QUALIF_MATERIAL_MESES";
    public const string ValidadeQualifServicoMeses = "SUP.VALIDADE_QUALIF_SERVICO_MESES";
    public const string RequalifLaboratorioMeses = "SUP.REQUALIF_LAB_MESES";
    public const string CargaHorariaAvaliadorLab = "SUP.CH_AVALIADOR_LAB_H";
    public const string PesoEscopo = "AVAL.PESO_ESCOPO";
    public const string PesoMaterial = "AVAL.PESO_MATERIAL";
    public const string PesoServico = "AVAL.PESO_SERVICO";
    public const string PesoLimpeza = "AVAL.PESO_LIMPEZA";
    public const string PesoDocSeg = "AVAL.PESO_DOCSEG";
    public const string NotaPlanoCompleto = "AVAL.NOTA_PLANO_COMPLETO";
    public const string NotaPlanoPontual = "AVAL.NOTA_PLANO_PONTUAL";
    public const string PeriodicidadeAvaliacaoMeses = "AVAL.PERIODICIDADE_MESES";
    public const string PesoIdfPrazo = "AVAL.IDF_PESO_PRAZO";
    public const string PesoIdfQualidade = "AVAL.IDF_PESO_QUALIDADE";
    public const string PesoIdfDocumentacao = "AVAL.IDF_PESO_DOCUMENTACAO";
    public const string IdfPreferencial = "AVAL.IDF_PREFERENCIAL";
    public const string IdfMinimo = "AVAL.IDF_MINIMO";
    public const string AlertaVencimentoDocDias = "DOC.ALERTA_VENCIMENTO_DIAS";
    public const string PeriodicidadeAnaliseCriticaMeses = "SGQ.PERIODICIDADE_ANALISE_MESES";
    public const string AlertaAnaliseCriticaDias = "SGQ.ALERTA_ANALISE_DIAS";
    public const string PrazoLeituraDias = "SGQ.PRAZO_LEITURA_DIAS";
    public const string PrazoRecolhimentoDias = "SGQ.PRAZO_RECOLHIMENTO_DIAS";
    public const string PrazoCheckoutDias = "SGQ.PRAZO_CHECKOUT_DIAS";
    public const string VerificacaoExternosMeses = "SGQ.VERIFICACAO_EXTERNOS_MESES";
    public const string RetencaoPadraoPerc = "CTR.RETENCAO_PADRAO_PERC";
    public const string LimiteAditivoPerc = "CTR.LIMITE_ADITIVO_PERC";
    public const string ValidadeLinkPortalDias = "SUP.VALIDADE_LINK_PORTAL_DIAS";
    public const string PrazoValidarDocDias = "SUP.PRAZO_VALIDAR_DOC_DIAS";
    public const string MetaFvs1aInspecaoPerc = "QOB.META_FVS_1A_PERC";
    public const string PrazoCorrecaoFvsDias = "QOB.PRAZO_CORRECAO_FVS_DIAS";
    public const string PrazoTratarRncDias = "QOB.PRAZO_RNC_DIAS";
    public const string PrazoEficaciaAcDias = "QOB.PRAZO_EFICACIA_AC_DIAS";
    public const string AlertaCalibracaoDias = "QOB.ALERTA_CALIBRACAO_DIAS";
    public const string PeriodicidadeCalibracaoMeses = "QOB.PERIODICIDADE_CALIBRACAO_MESES";
    public const string PeriodicidadeVerificacaoMeses = "QOB.PERIODICIDADE_VERIFICACAO_MESES";
    public const string AuditoriasPorAno = "QOB.AUDITORIAS_ANO";
    public const string ReincidenciasAc = "QOB.REINCIDENCIAS_AC";
    // Etiqueta de equipamento (folha A4 de etiquetas adesivas, impressora comum)
    public const string EtiquetaColunas = "ETQ.COLUNAS";
    public const string EtiquetaLinhas = "ETQ.LINHAS";
    public const string EtiquetaLarguraMm = "ETQ.LARGURA_MM";
    public const string EtiquetaAlturaMm = "ETQ.ALTURA_MM";
    public const string EtiquetaMargemSuperiorMm = "ETQ.MARGEM_SUPERIOR_MM";
    public const string EtiquetaMargemEsquerdaMm = "ETQ.MARGEM_ESQUERDA_MM";
    public const string EtiquetaEspacoHorizontalMm = "ETQ.ESPACO_H_MM";
    public const string EtiquetaEspacoVerticalMm = "ETQ.ESPACO_V_MM";
    // Estoque e equipamentos
    public const string ConsumoMinimoDias = "EST.CONSUMO_MINIMO_DIAS";
    public const string AlertaDevolucaoLocacaoDias = "EQP.ALERTA_DEVOLUCAO_DIAS";
    public const string AlertaVencimentoSstDias = "SST.ALERTA_VENCIMENTO_DIAS";
    public const string AlertaCaDias = "SST.ALERTA_CA_DIAS";
    public const string MetaAptosPerc = "SST.META_APTOS_PERC";
    public const string MetaConformidadeInspecaoPerc = "SST.META_CONFORMIDADE_PERC";
    public const string FrequenciaInspecaoDias = "SST.FREQUENCIA_INSPECAO_DIAS";
    // Economia real, histórico de preço e portal
    public const string DesvioEconomiaPerc = "SUP.DESVIO_ECONOMIA_PERC";
    public const string AlertaAumentoPrecoPerc = "SUP.ALERTA_AUMENTO_PRECO_PERC";
    public const string LembreteDocVencidoDias = "DOC.LEMBRETE_VENCIDO_DIAS";
    public const string DiaLimiteCompetencia = "DOC.DIA_LIMITE_COMPETENCIA";

    /// <summary>Padrão genérico SiAC/PBQP-H (sem dados de empresa). Chave → (valor, unidade, descrição, grupo).</summary>
    public static readonly IReadOnlyDictionary<string, (decimal Valor, string Unidade, string Descricao, string Grupo)> Padrao =
        new Dictionary<string, (decimal, string, string, string)>
        {
            [LimiteUmOrcamento] = (1000m, "R$", "Até este valor: 1 orçamento; acima: exige mais propostas", "Suprimentos"),
            [OrcamentosAcimaLimite] = (3m, "un", "Orçamentos exigidos acima do limite", "Suprimentos"),
            [ValidadeQualifMaterialMeses] = (3m, "meses", "Validade da consulta de qualificação – material", "Suprimentos"),
            [ValidadeQualifServicoMeses] = (12m, "meses", "Validade da qualificação – serviços e demais tipos", "Suprimentos"),
            [RequalifLaboratorioMeses] = (12m, "meses", "Requalificação de laboratório não acreditado (c a f)", "Suprimentos"),
            [CargaHorariaAvaliadorLab] = (16m, "h", "Carga horária mínima do avaliador de laboratório (NBR ISO/IEC 17025)", "Suprimentos"),
            [PesoEscopo] = (0.25m, "", "Peso – Cumprimento do escopo do contrato", "Avaliação do prestador"),
            [PesoMaterial] = (0.25m, "", "Peso – Qualidade do material (se aplicável)", "Avaliação do prestador"),
            [PesoServico] = (0.30m, "", "Peso – Qualidade do serviço", "Avaliação do prestador"),
            [PesoLimpeza] = (0.10m, "", "Peso – Limpeza e organização", "Avaliação do prestador"),
            [PesoDocSeg] = (0.10m, "", "Peso – Documentação e segurança do trabalho", "Avaliação do prestador"),
            [NotaPlanoCompleto] = (7m, "", "Nota abaixo disto → reunião + plano de ação completo", "Avaliação do prestador"),
            [NotaPlanoPontual] = (9m, "", "Nota abaixo disto → plano de ação pontual", "Avaliação do prestador"),
            [PeriodicidadeAvaliacaoMeses] = (6m, "meses", "Periodicidade da avaliação consolidada de fornecedores (3 = trimestral, 6 = semestral)", "Avaliação do prestador"),
            [PesoIdfPrazo] = (0.4m, "", "IDF da entrega – peso do prazo (nota 0 a 10)", "Avaliação do prestador"),
            [PesoIdfQualidade] = (0.4m, "", "IDF da entrega – peso da qualidade (nota 0 a 10)", "Avaliação do prestador"),
            [PesoIdfDocumentacao] = (0.2m, "", "IDF da entrega – peso da documentação/atendimento (nota 0 a 10)", "Avaliação do prestador"),
            [IdfPreferencial] = (8m, "", "IDF a partir do qual o fornecedor é preferencial", "Avaliação do prestador"),
            [IdfMinimo] = (6m, "", "IDF abaixo disto: critério reprovado na entrega e fornecedor desqualificado na consolidação", "Avaliação do prestador"),
            [AlertaVencimentoDocDias] = (30m, "dias", "Alerta de vencimento de documento de fornecedor", "Suprimentos"),
            [RetencaoPadraoPerc] = (5m, "%", "Retenção contratual padrão", "Contratos"),
            [PeriodicidadeAnaliseCriticaMeses] = (12m, "meses", "Periodicidade padrão de análise crítica de documento", "Documentos do SGQ"),
            [AlertaAnaliseCriticaDias] = (30m, "dias", "Alerta antes do vencimento da análise crítica", "Documentos do SGQ"),
            [PrazoLeituraDias] = (7m, "dias", "Prazo para confirmação de leitura", "Documentos do SGQ"),
            [PrazoRecolhimentoDias] = (3m, "dias", "Prazo para recolher cópia obsoleta", "Documentos do SGQ"),
            [PrazoCheckoutDias] = (5m, "dias", "Prazo máximo de check-out", "Documentos do SGQ"),
            [VerificacaoExternosMeses] = (6m, "meses", "Verificação de atualização de documentos externos", "Documentos do SGQ"),
            [LimiteAditivoPerc] = (25m, "%", "Alerta quando aditivos acumulados passam deste % do contrato", "Contratos"),
            [DesvioEconomiaPerc] = (40m, "%", "Economia real: compra com diferença acima deste % do último preço fica fora da base", "Suprimentos"),
            [AlertaAumentoPrecoPerc] = (10m, "%", "Aumento de preço acima deste % pede motivo e entra na lista do gestor", "Suprimentos"),
            [LembreteDocVencidoDias] = (3m, "dias", "Documento vencido: lembrete ao fornecedor a cada N dias até regularizar", "Suprimentos"),
            [DiaLimiteCompetencia] = (20m, "dia", "Documentação mensal: dia limite do mês seguinte ao trabalhado", "Suprimentos"),
            [ValidadeLinkPortalDias] = (15m, "dias", "Validade do link do portal do fornecedor", "Suprimentos"),
            [PrazoValidarDocDias] = (2m, "dias úteis", "Prazo para validar documento enviado pelo portal", "Suprimentos"),
            [MetaFvs1aInspecaoPerc] = (85m, "%", "Meta de FVS aprovadas na 1ª inspeção", "Qualidade da obra"),
            [PrazoCorrecaoFvsDias] = (3m, "dias", "Prazo padrão para correção de FVS reprovada", "Qualidade da obra"),
            [PrazoTratarRncDias] = (7m, "dias", "Prazo padrão para tratar RNC", "Qualidade da obra"),
            [PrazoEficaciaAcDias] = (30m, "dias", "Prazo para verificar eficácia da ação corretiva", "Qualidade da obra"),
            [AlertaCalibracaoDias] = (30m, "dias", "Alerta de calibração a vencer", "Qualidade da obra"),
            [PeriodicidadeCalibracaoMeses] = (12m, "meses", "Periodicidade padrão – calibração externa", "Qualidade da obra"),
            [PeriodicidadeVerificacaoMeses] = (6m, "meses", "Periodicidade padrão – verificação interna", "Qualidade da obra"),
            [AuditoriasPorAno] = (1m, "un", "Auditorias internas por ano (mínimo)", "Qualidade da obra"),
            [ReincidenciasAc] = (2m, "RNC", "Reincidências (mesmo serviço/prestador em 90 dias) que obrigam ação corretiva", "Qualidade da obra"),
            [AlertaVencimentoSstDias] = (30m, "dias", "Alerta de vencimento de documento/ASO/treinamento", "Segurança e pessoas"),
            [AlertaCaDias] = (60m, "dias", "Alerta de validade de CA de EPI", "Segurança e pessoas"),
            [MetaAptosPerc] = (95m, "%", "Meta de colaboradores aptos", "Segurança e pessoas"),
            [MetaConformidadeInspecaoPerc] = (90m, "%", "Meta de conformidade das inspeções de segurança", "Segurança e pessoas"),
            [FrequenciaInspecaoDias] = (7m, "dias", "Frequência mínima de inspeção geral do canteiro", "Segurança e pessoas"),
            [EtiquetaColunas] = (3m, "un", "Etiquetas por linha na folha A4", "Etiquetas"),
            [EtiquetaLinhas] = (8m, "un", "Linhas de etiquetas na folha A4", "Etiquetas"),
            [EtiquetaLarguraMm] = (63.5m, "mm", "Largura da etiqueta", "Etiquetas"),
            [EtiquetaAlturaMm] = (33.9m, "mm", "Altura da etiqueta", "Etiquetas"),
            [EtiquetaMargemSuperiorMm] = (12.9m, "mm", "Margem superior da folha", "Etiquetas"),
            [EtiquetaMargemEsquerdaMm] = (7.2m, "mm", "Margem esquerda da folha", "Etiquetas"),
            [EtiquetaEspacoHorizontalMm] = (2.5m, "mm", "Espaço entre etiquetas (horizontal)", "Etiquetas"),
            [EtiquetaEspacoVerticalMm] = (0m, "mm", "Espaço entre etiquetas (vertical)", "Etiquetas"),
            [ConsumoMinimoDias] = (60m, "dias", "Período de consumo usado na sugestão de mínimos", "Estoque"),
            [AlertaDevolucaoLocacaoDias] = (5m, "dias", "Alerta antes da data de devolução do equipamento locado", "Estoque"),
        };
}
