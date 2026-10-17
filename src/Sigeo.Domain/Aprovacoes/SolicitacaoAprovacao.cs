using Sigeo.Domain.Common;

namespace Sigeo.Domain.Aprovacoes;

/// <summary>
/// Motor genérico de aprovação: uma solicitação por objeto (pedido, medição, documento SGQ, avaliação),
/// com etapas geradas a partir das alçadas vigentes no momento do envio (as alçadas ficam "congeladas").
/// </summary>
public class SolicitacaoAprovacao : EntidadeAuditavel, IHistoricoTemporal
{
    public EntidadeAprovavel Entidade { get; set; }
    public Guid EntidadeId { get; set; }
    public Guid? ObraId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    /// <summary>Quem criou/enviou o objeto. Base da segregação de funções: não pode aprovar o próprio pedido.</summary>
    public Guid SolicitanteUsuarioId { get; set; }
    public string SolicitanteNome { get; set; } = string.Empty;
    public StatusAprovacao Status { get; set; } = StatusAprovacao.EmAprovacao;
    public int EtapaAtual { get; set; } = 1;
    public DateTimeOffset EnviadoEm { get; set; }
    public DateTimeOffset? ConcluidoEm { get; set; }
    public ICollection<EtapaAprovacao> Etapas { get; set; } = [];

    /// <summary>Cria a solicitação congelando as alçadas aplicáveis ao valor e à obra no momento do envio.</summary>
    public static SolicitacaoAprovacao Criar(EntidadeAprovavel entidade, Guid entidadeId, Guid empresaId, Guid? obraId,
        string titulo, decimal valor, Guid solicitanteUsuarioId, string solicitanteNome,
        IEnumerable<Estrutura.Alcada> alcadas, DateTimeOffset agora)
    {
        var aplicaveis = alcadas.Where(a => a.Entidade == entidade && a.SeAplica(valor, obraId))
                                .OrderBy(a => a.Ordem).ToList();
        if (aplicaveis.Count == 0)
            throw new RegraDeNegocioException($"Nenhuma alçada configurada para {entidade} no valor de {valor:C}.");

        var s = new SolicitacaoAprovacao
        {
            Entidade = entidade, EntidadeId = entidadeId, EmpresaId = empresaId, ObraId = obraId,
            Titulo = titulo, Valor = valor, EnviadoEm = agora, EtapaAtual = 1,
            SolicitanteUsuarioId = solicitanteUsuarioId, SolicitanteNome = solicitanteNome
        };
        var ordem = 1;
        foreach (var a in aplicaveis)
            s.Etapas.Add(new EtapaAprovacao { Ordem = ordem++, PerfilAprovador = a.PerfilAprovador, UsuarioAprovadorId = a.UsuarioAprovadorId });
        return s;
    }

    /// <summary>
    /// Cadastro mudou: refaz as etapas ainda não decididas a partir das alçadas atuais (já com o aprovador resolvido).
    /// As decididas ficam; as N primeiras alçadas correspondem a elas. Sem mudança (ou sem etapa restante) não mexe.
    /// </summary>
    public (List<EtapaAprovacao> Removidas, List<EtapaAprovacao> Novas) TrocarEtapasPendentes(IReadOnlyList<(PerfilUsuario Perfil, Guid? Usuario)> alcadas)
    {
        if (Status != StatusAprovacao.EmAprovacao) return ([], []);
        var decididas = Etapas.Count(e => e.Decisao != DecisaoAprovacao.Pendente);
        var restantes = alcadas.Skip(decididas).ToList();
        var pendentes = Etapas.Where(e => e.Decisao == DecisaoAprovacao.Pendente).OrderBy(e => e.Ordem).ToList();
        if (restantes.Count == 0 || pendentes.Select(e => (e.PerfilAprovador, e.UsuarioAprovadorId)).SequenceEqual(restantes)) return ([], []);
        foreach (var e in pendentes) Etapas.Remove(e);
        var ordem = decididas + 1;
        var novas = restantes.Select(r => new EtapaAprovacao { SolicitacaoAprovacaoId = Id, Ordem = ordem++, PerfilAprovador = r.Perfil, UsuarioAprovadorId = r.Usuario }).ToList();
        foreach (var e in novas) Etapas.Add(e);
        EtapaAtual = decididas + 1;
        return (pendentes, novas);
    }

    public EtapaAprovacao? Pendente => Etapas.OrderBy(e => e.Ordem).FirstOrDefault(e => e.Decisao == DecisaoAprovacao.Pendente);

    /// <summary>
    /// Requisição: quem solicitou pode aprovar a própria solicitação quando a alçada da etapa é dele (direta, sem delegação)
    /// e o valor está no limite do login. Nos demais documentos vale a segregação de funções.
    /// </summary>
    public bool PermiteSolicitanteAprovar => Entidade == EntidadeAprovavel.Requisicao;

    /// <summary>Ninguém decidiu ainda: a solicitação está na primeira etapa, sem "de acordo".</summary>
    public bool SemDecisoes => Etapas.All(e => e.Decisao == DecisaoAprovacao.Pendente);

    /// <summary>
    /// O solicitante retira a solicitação para voltar a editar o documento — só enquanto ninguém decidiu (1ª etapa).
    /// A solicitação fica cancelada (histórico); um novo envio gera outra.
    /// </summary>
    public void RetirarPeloSolicitante(Guid usuarioId, DateTimeOffset agora)
    {
        if (Status != StatusAprovacao.EmAprovacao) throw new RegraDeNegocioException("Solicitação não está em aprovação.");
        if (usuarioId != SolicitanteUsuarioId) throw new RegraDeNegocioException("Só quem enviou pode voltar o documento para edição.");
        if (!SemDecisoes) throw new RegraDeNegocioException("A primeira aprovação já foi dada: não é mais possível voltar para edição.");
        Status = StatusAprovacao.Cancelado;
        ConcluidoEm = agora;
    }

    /// <summary>
    /// Registra a decisão da etapa atual e avança/encerra o fluxo. Regras aplicadas, nesta ordem:
    /// <list type="number">
    /// <item>Alçada: o ator (ou o titular que delegou a ele, com delegação vigente) precisa ser o aprovador da etapa.</item>
    /// <item>Segregação de funções: quem solicitou não aprova (exceto requisição, com alçada própria); a mesma pessoa não decide duas etapas.</item>
    /// <item>Limite do login: na etapa que conclui a aprovação, o valor não pode passar do limite do titular.</item>
    /// </list>
    /// </summary>
    public void Decidir(Aprovador ator, DecisaoAprovacao decisao, string? justificativa, DateTimeOffset agora,
        IEnumerable<DelegacaoRecebida>? delegacoes = null)
    {
        if (Status != StatusAprovacao.EmAprovacao)
            throw new RegraDeNegocioException("Solicitação não está em aprovação.");
        if (decisao == DecisaoAprovacao.Pendente)
            throw new RegraDeNegocioException("Decisão inválida.");

        var etapa = Pendente ?? throw new RegraDeNegocioException("Não há etapa pendente.");

        // 1) Alçada — direta ou por delegação vigente.
        Aprovador titular;
        DelegacaoAprovacao? viaDelegacao = null;
        if (etapa.Autoriza(ator))
            titular = ator;
        else
        {
            var d = (delegacoes ?? []).FirstOrDefault(x =>
                x.Delegacao.SubstitutoUsuarioId == ator.UsuarioId &&
                x.Delegacao.TitularUsuarioId == x.Titular.UsuarioId &&
                x.Delegacao.Vigente(agora, Entidade, ObraId) &&
                etapa.Autoriza(x.Titular));
            if (d is null)
                throw new RegraDeNegocioException("Usuário sem alçada para esta etapa.");
            titular = d.Titular;
            viaDelegacao = d.Delegacao;
        }

        // 2) Segregação de funções. Na requisição, o solicitante que tem a alçada pode aprovar a própria, e o substituto
        //    que ele delegou pode aprovar em nome dele; o solicitante nunca aprova como substituto de outro titular.
        var envolveSolicitante = ator.UsuarioId == SolicitanteUsuarioId || titular.UsuarioId == SolicitanteUsuarioId;
        if (envolveSolicitante && !PermiteSolicitanteAprovar
            || ator.UsuarioId == SolicitanteUsuarioId && viaDelegacao is not null)
            throw new RegraDeNegocioException("Segregação de funções: quem solicitou não pode aprovar a própria solicitação.");
        var jaDecidiu = Etapas.Where(e => e.Decisao != DecisaoAprovacao.Pendente)
                              .SelectMany(e => new[] { e.DecididoPorUsuarioId, e.EmNomeDeUsuarioId })
                              .Where(id => id is not null).ToHashSet();
        if (jaDecidiu.Contains(ator.UsuarioId) || jaDecidiu.Contains(titular.UsuarioId))
            throw new RegraDeNegocioException("Segregação de funções: a mesma pessoa não pode decidir duas etapas da mesma solicitação.");

        if (decisao is DecisaoAprovacao.Reprovado or DecisaoAprovacao.DevolvidoParaAjuste && string.IsNullOrWhiteSpace(justificativa))
            throw new RegraDeNegocioException("Justificativa obrigatória para reprovar ou devolver.");

        // 3) Limite do login — só pesa na etapa que conclui a aprovação (as anteriores são "de acordo").
        var concluiAprovacao = decisao == DecisaoAprovacao.Aprovado &&
                               !Etapas.Any(e => e != etapa && e.Decisao == DecisaoAprovacao.Pendente);
        if (concluiAprovacao && titular.LimitePara(Entidade, ObraId) is { } limite && Valor > limite)
            throw new RegraDeNegocioException($"Valor de {Valor:C} acima do limite de aprovação de {titular.Nome} ({limite:C}).");

        etapa.Decisao = decisao;
        etapa.DecididoPorUsuarioId = ator.UsuarioId;
        etapa.DecididoPorNome = ator.Nome;
        etapa.DecididoEm = agora;
        etapa.Justificativa = justificativa;
        if (viaDelegacao is not null)
        {
            etapa.EmNomeDeUsuarioId = titular.UsuarioId;
            etapa.EmNomeDeNome = titular.Nome;
            etapa.DelegacaoAprovacaoId = viaDelegacao.Id;
        }

        switch (decisao)
        {
            case DecisaoAprovacao.Aprovado when Pendente is null:
                Status = StatusAprovacao.Aprovado; ConcluidoEm = agora; break;
            case DecisaoAprovacao.Aprovado:
                EtapaAtual = Pendente!.Ordem; break;
            case DecisaoAprovacao.Reprovado:
                Status = StatusAprovacao.Reprovado; ConcluidoEm = agora; break;
            case DecisaoAprovacao.DevolvidoParaAjuste:
                Status = StatusAprovacao.Rascunho; ConcluidoEm = agora; break;
        }
    }
}
