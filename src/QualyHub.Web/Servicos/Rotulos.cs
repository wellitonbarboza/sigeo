using System.Globalization;
using System.Text.RegularExpressions;
using QualyHub.Domain.Common;

namespace QualyHub.Web.Servicos;

/// <summary>Rótulos em português e tom de cor (selo) para os enums do domínio.</summary>
public static partial class Rotulos
{
    public static readonly CultureInfo Br = new("pt-BR");

    private static readonly Dictionary<Enum, string> Fixos = new()
    {
        [StatusRequisicao.AguardandoEngenheiro] = "Aguard. aprovação Engenheiro",
        [StatusRequisicao.AguardandoDiretorTecnico] = "Aguard. aprovação Diretor Técnico",
        [StatusRequisicao.NaoAutorizada] = "Não autorizada",
        [StatusRequisicao.DevolvidaAjuste] = "Devolvida p/ ajuste",
        [StatusRequisicao.EmCotacao] = "Em cotação",
        [StatusRequisicao.PedidoEmitido] = "Pedido/Contrato emitido",
        [StatusCotacao.AguardandoPropostas] = "Aguardando propostas",
        [StatusCotacao.EmAnalise] = "Em análise",
        [StatusCotacao.AguardandoAprovacao] = "Aguard. aprovação",
        [StatusCotacao.ReprovadoVoltaCotacao] = "Reprovado (volta p/ cotação)",
        [StatusCotacao.PedidoGerado] = "Pedido gerado",
        [StatusPedido.AguardandoAprovacaoDiretor] = "Aguard. aprovação Diretor",
        [StatusPedido.EnviadoFornecedor] = "Enviado ao fornecedor",
        [StatusPedido.EntregueParcial] = "Entregue parcial",
        [StatusContrato.EmElaboracao] = "Em elaboração",
        [StatusContrato.AguardandoAprovacaoDiretoria] = "Aguard. aprovação Diretoria",
        [StatusContrato.AguardandoAssinaturas] = "Aguardando assinaturas",
        [StatusMedicao.EmElaboracao] = "Em elaboração",
        [StatusMedicao.AguardandoAprovacao] = "Aguard. aprovação",
        [StatusMedicao.NfRecebida] = "NF recebida",
        [StatusQualificacao.EmQualificacao] = "Em qualificação",
        [StatusQualificacao.NaoQualificado] = "Não qualificado",
        [StatusDocumento.PendenteEnvio] = "Pendente",
        [StatusDocumento.AVencer] = "A vencer",
        [StatusDocumento.EmValidacao] = "Em validação",
        [StatusDocumento.Reprovado] = "Recusado",
        [StatusDocumento.Valido] = "Válido",
        [StatusRevisao.EmElaboracao] = "Em elaboração",
        [StatusRevisao.EmAnaliseCritica] = "Em análise crítica",
        [StatusRevisao.DevolvidaAjuste] = "Devolvida p/ ajuste",
        [StatusRevisao.AguardandoAprovacao] = "Aguardando aprovação",
        [StatusRevisao.AprovadaAguardandoVigencia] = "Aprovada (aguardando vigência)",
        [StatusRevisao.VigenteComRessalvas] = "Vigente c/ ressalvas",
        [StatusDocumentoSgq.EmElaboracao] = "Em elaboração",
        [StatusDocumentoSgq.EmRevisao] = "Em revisão",
        [StatusCopia.EmUso] = "Em uso",
        [StatusCopia.RecolhimentoPendente] = "Recolhimento pendente",
        [TipoCopia.FisicaControlada] = "Física controlada",
        [TipoCopia.EletronicaControlada] = "Eletrônica controlada",
        [TipoCopia.ExternaGrd] = "Externa (GRD)",
        [TipoFornecimento.ServicoObra] = "Serviço de obra",
        [TipoFornecimento.ServicoEspecializadoEngenharia] = "Serviço esp. engenharia",
        [TipoFornecimento.Locacao] = "Locação",
        [TipoFornecimento.Laboratorio] = "Laboratório",
        [SituacaoPsq.NaoAplicavel] = "Não se aplica",
        [SituacaoPsq.NaoConforme] = "Não conforme",
        [SituacaoPsq.NaoEscopoPsq] = "Não é escopo do PSQ",
        [EnquadramentoLaboratorio.NaoSeAplica] = "Não se aplica",
        [EnquadramentoLaboratorio.A_Acreditado] = "a) Acreditado CGCRE",
        [EnquadramentoLaboratorio.B_EmAcreditacao] = "b) Em acreditação",
        [EnquadramentoLaboratorio.C_Avaliado] = "c) Avaliado (Anexo 7)",
        [EnquadramentoLaboratorio.D_InstituicaoPesquisa] = "d) Inst. pesquisa/ensino",
        [EnquadramentoLaboratorio.E_Outro] = "e) Outro",
        [EnquadramentoLaboratorio.F_Outro] = "f) Outro",
        [ResultadoQualificacao.NaoQualificado] = "Não qualificado",
        [ResultadoQualificacao.EmQualificacao] = "Em qualificação",
        [ResultadoCriterio.NaoAvaliado] = "—",
        [DestinoRecebimento.AceitoComRessalva] = "Aceito c/ ressalva",
        [FaixaNota.Nenhuma] = "Nenhuma ação",
        [FaixaNota.PlanoPontual] = "Plano de ação pontual",
        [FaixaNota.ReuniaoPlanoCompleto] = "Reunião + plano completo",
        [DecisaoSemestral.ManterComPlanoAcao] = "Manter c/ plano de ação",
        [StatusPlanoAcao.EmAndamento] = "Em andamento",
        [OrigemPagamento.NotaMaterial] = "NF de material",
        [OrigemPagamento.MedicaoServico] = "Medição de serviço",
        [TipoDocumentoCompra.PedidoCompra] = "Pedido de compra",
        [FormalizacaoContrato.ContratoPadrao] = "Contrato padrão",
        [FormalizacaoContrato.AceiteProposta] = "Aceite de proposta",
        [CriterioSelecao.MenorPreco] = "Menor preço",
        [StatusSolicitacaoCopia.Solicitada] = "Solicitada",
        [DecisaoEtapa.AprovadoComRessalvas] = "Aprovado c/ ressalvas",
        [EtapaRevisaoTipo.AnaliseCritica] = "Análise crítica",
        [EtapaRevisaoTipo.Elaboracao] = "Elaboração",
        [EtapaRevisaoTipo.Aprovacao] = "Aprovação",
        [EtapaRevisaoTipo.Publicacao] = "Publicação",
        [MotivoRevisao.EmissaoInicial] = "Emissão inicial",
        [MotivoRevisao.NaoConformidade] = "Não conformidade / ação corretiva",
        [MotivoRevisao.MudancaRequisito] = "Mudança de requisito (norma, contrato, lei)",
        [MotivoRevisao.AnaliseCriticaPeriodica] = "Análise crítica periódica",
        [NivelAcesso.PublicoInterno] = "Público interno",
        [NivelAcesso.RestritoArea] = "Restrito à área",
        [PerfilUsuario.Almoxarifado] = "Almoxarifado",
        [TipoRequisicao.Servico] = "Serviço",
        [DecisaoAprovacao.DevolvidoParaAjuste] = "Devolvido p/ ajuste",
    };

    public static string De(Enum e)
    {
        if (Fixos.TryGetValue(e, out var t)) return t;
        var s = e.ToString();
        if (s.Contains(", ")) return string.Join(", ", s.Split(", ").Select(p => De((Enum)Enum.Parse(e.GetType(), p))));
        var palavras = Maiusculas().Replace(s, " $1").Replace("_", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => Acentos.TryGetValue(w, out var a) ? a : Siglas.Contains(w.ToUpperInvariant()) ? w.ToUpperInvariant() : w.ToLower(Br)).ToList();
        var r = string.Join(' ', palavras);
        return char.ToUpper(r[0], Br) + r[1..];
    }

    private static readonly HashSet<string> Siglas = ["FVM", "FVS", "NF", "PSQ", "ITS", "CIF", "FOB", "GRD", "RH"];
    private static readonly Dictionary<string, string> Acentos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Nao"] = "não", ["Servico"] = "serviço", ["Aprovacao"] = "aprovação", ["Revisao"] = "revisão", ["Elaboracao"] = "elaboração",
        ["Qualificacao"] = "qualificação", ["Medicao"] = "medição", ["Concluida"] = "concluída", ["Analise"] = "análise", ["Critica"] = "crítica",
        ["Publicacao"] = "publicação", ["Eficaz"] = "eficaz", ["Obsoleta"] = "obsoleta", ["Pendente"] = "pendente", ["Tecnica"] = "técnica",
        ["Historico"] = "histórico", ["Reclamacao"] = "reclamação", ["Avaliacao"] = "avaliação", ["Semestral"] = "semestral", ["Fisica"] = "física",
        ["Eletronica"] = "eletrônica", ["Recolhida"] = "recolhida", ["Extraviada"] = "extraviada", ["Diretoria"] = "Diretoria", ["Engenharia"] = "Engenharia",
        ["Suprimentos"] = "Suprimentos", ["Qualidade"] = "Qualidade", ["Financeiro"] = "Financeiro", ["Administrador"] = "Administrador", ["Campo"] = "Campo",
        ["Terceiro"] = "Terceiro", ["Consultoria"] = "consultoria", ["Laboratorio"] = "laboratório", ["Locacao"] = "locação", ["Pagina"] = "página",
        ["Transito"] = "trânsito", ["Emissao"] = "emissão", ["Inicial"] = "inicial", ["Recusado"] = "recusado", ["Liberado"] = "liberado",
    };

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex Maiusculas();

    /// <summary>Tom do selo pela semântica do status.</summary>
    public static string Tom(Enum e) => e switch
    {
        StatusRequisicao.Autorizada or StatusRequisicao.PedidoEmitido or StatusRequisicao.Concluida => "ok",
        StatusRequisicao.NaoAutorizada or StatusRequisicao.Cancelada => "erro",
        StatusRequisicao.DevolvidaAjuste or StatusRequisicao.AguardandoEngenheiro or StatusRequisicao.AguardandoDiretorTecnico => "aviso",
        StatusRequisicao.EmCotacao => "info",
        StatusCotacao.Aprovado or StatusCotacao.PedidoGerado => "ok",
        StatusCotacao.AguardandoAprovacao or StatusCotacao.AguardandoPropostas => "aviso",
        StatusCotacao.ReprovadoVoltaCotacao or StatusCotacao.Cancelado => "erro",
        StatusPedido.Aprovado or StatusPedido.EnviadoFornecedor or StatusPedido.Entregue => "ok",
        StatusPedido.AguardandoAprovacaoDiretor or StatusPedido.EntregueParcial => "aviso",
        StatusPedido.Cancelado => "erro",
        StatusContrato.Ativo => "ok",
        StatusContrato.AguardandoAprovacaoDiretoria or StatusContrato.AguardandoAssinaturas or StatusContrato.Suspenso => "aviso",
        StatusContrato.Rescindido => "erro",
        StatusMedicao.Aprovada or StatusMedicao.Paga or StatusMedicao.NfRecebida => "ok",
        StatusMedicao.AguardandoAprovacao => "aviso",
        StatusMedicao.Reprovada => "erro",
        StatusQualificacao.Qualificado => "ok",
        StatusQualificacao.Vencida or StatusQualificacao.NaoQualificado or StatusQualificacao.Desqualificado => "erro",
        StatusQualificacao.EmQualificacao => "aviso",
        ResultadoQualificacao.Qualificado => "ok",
        ResultadoQualificacao.NaoQualificado => "erro",
        StatusDocumento.Valido => "ok",
        StatusDocumento.AVencer or StatusDocumento.EmValidacao or StatusDocumento.PendenteEnvio => "aviso",
        StatusDocumento.Vencido or StatusDocumento.Reprovado => "erro",
        StatusRevisao.Vigente or StatusRevisao.VigenteComRessalvas or StatusRevisao.AprovadaAguardandoVigencia => "ok",
        StatusRevisao.Obsoleta or StatusRevisao.Recusada or StatusRevisao.Cancelada => "erro",
        StatusRevisao.EmElaboracao => "neutro",
        StatusRevisao.DevolvidaAjuste => "aviso",
        StatusRevisao.EmAnaliseCritica or StatusRevisao.AguardandoAprovacao => "info",
        StatusDocumentoSgq.Vigente => "ok",
        StatusDocumentoSgq.EmRevisao => "info",
        StatusDocumentoSgq.Cancelado => "erro",
        StatusCopia.EmUso => "ok",
        StatusCopia.RecolhimentoPendente => "aviso",
        StatusCopia.Extraviada => "erro",
        StatusPlanoAcao.Concluida => "ok",
        StatusPlanoAcao.Aberta => "aviso",
        StatusPlanoAcao.EmAndamento => "info",
        StatusPagamento.Liberado or StatusPagamento.Pago => "ok",
        StatusPagamento.Aguardando => "aviso",
        StatusPagamento.Bloqueado => "erro",
        StatusSolicitacaoCopia.Liberada or StatusSolicitacaoCopia.Entregue or StatusSolicitacaoCopia.Encerrada => "ok",
        StatusSolicitacaoCopia.Solicitada => "aviso",
        StatusSolicitacaoCopia.Recusada => "erro",
        StatusCiencia.Lido => "ok",
        StatusCiencia.Pendente => "aviso",
        StatusCiencia.Atrasado => "erro",
        ResultadoCriterio.Aprovado => "ok",
        ResultadoCriterio.Reprovado => "erro",
        FaixaNota.Nenhuma => "ok",
        FaixaNota.PlanoPontual => "aviso",
        FaixaNota.ReuniaoPlanoCompleto => "erro",
        DecisaoSemestral.ManterQualificado => "ok",
        DecisaoSemestral.ManterComPlanoAcao => "aviso",
        DecisaoSemestral.Desqualificar => "erro",
        SituacaoDocExterno.Vigente => "ok",
        SituacaoDocExterno.Substituida or SituacaoDocExterno.Cancelada => "erro",
        _ => "neutro"
    };

    public static string Moeda(decimal? v) => v is null ? "—" : v.Value.ToString("C2", Br);
    public static string Num(decimal? v, int casas = 2) => v is null ? "—" : v.Value.ToString($"N{casas}", Br);
    public static string Data(DateOnly? d) => d is null ? "—" : d.Value.ToString("dd/MM/yyyy", Br);
    public static string DataCurta(DateOnly? d) => d is null ? "—" : d.Value.ToString("dd/MM/yy", Br);
    public static string DataHora(DateTimeOffset? d) => d is null ? "—" : d.Value.ToLocalTime().ToString("dd/MM/yy HH:mm", Br);
}
