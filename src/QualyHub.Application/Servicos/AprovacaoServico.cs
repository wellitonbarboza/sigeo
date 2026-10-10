using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Application.Servicos;

/// <summary>
/// Motor de aprovação ligado ao banco: envia com as alçadas vigentes (congeladas), decide com limite do login,
/// delegação vigente e segregação de funções, e reflete a decisão no objeto (status, "aprovado por/em").
/// </summary>
public sealed class AprovacaoServico(IUsuarioAtual usuario, TimeProvider relogio, IFabricaDb fabrica)
{
    /// <summary>Decide a etapa pendente e grava (decisão + reflexo no documento) numa única gravação — usado pela trilha de autorizações.</summary>
    public async Task DecidirESalvarAsync(Guid solicitacaoId, DecisaoAprovacao decisao, string? justificativa, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await DecidirAsync(db, solicitacaoId, decisao, justificativa, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<SolicitacaoAprovacao> EnviarAsync(IQualyHubDb db, EntidadeAprovavel entidade, Guid entidadeId, Guid? obraId,
        string titulo, decimal valor, CancellationToken ct = default, Guid? speId = null)
    {
        var aberta = await db.SolicitacoesAprovacao.AnyAsync(s => s.Entidade == entidade && s.EntidadeId == entidadeId && s.Status == StatusAprovacao.EmAprovacao, ct);
        if (aberta) throw new RegraDeNegocioException("Já existe uma aprovação em andamento para este registro.");
        var alcadas = await db.Alcadas.AsNoTracking().Where(a => a.Entidade == entidade).ToListAsync(ct);
        await ResolverEngenheiroAsync(db, alcadas, obraId, speId ?? await SpeDoDocumentoAsync(db, entidade, entidadeId, ct), ct);
        await TrocarInativosAsync(db, alcadas, ct);
        var s = SolicitacaoAprovacao.Criar(entidade, entidadeId, usuario.EmpresaId, obraId, titulo, valor,
            usuario.UsuarioId ?? Guid.Empty, usuario.Nome, alcadas, relogio.GetUtcNow());
        db.SolicitacoesAprovacao.Add(s);
        return s;
    }

    /// <summary>
    /// "Engenheiro responsável" (alçada EngenheiroDaObra): o da SPE do documento; obra sem SPE usa o engenheiro da obra.
    /// SPE sem engenheiro barra (o cadastro precisa ser completado).
    /// </summary>
    private static async Task ResolverEngenheiroAsync(IQualyHubDb db, List<Domain.Estrutura.Alcada> alcadas, Guid? obraId, Guid? speId, CancellationToken ct)
    {
        if (!alcadas.Any(a => a.EngenheiroDaObra && a.UsuarioAprovadorId is null)) return;
        Guid? eng = null;
        if (speId is { } sid) eng = await db.Spes.AsNoTracking().Where(s => s.Id == sid).Select(s => s.EngenheiroId).FirstOrDefaultAsync(ct);
        if (eng is null && speId is not null)
            throw new RegraDeNegocioException("Cadastre o engenheiro responsável da SPE (Engenharia › Obras e Empreendimentos › SPE) antes de enviar para aprovação.");
        if (eng is null && obraId is { } oid) eng = await db.Obras.AsNoTracking().Where(o => o.Id == oid).Select(o => o.EngenheiroId).FirstOrDefaultAsync(ct);
        foreach (var a in alcadas.Where(a => a.EngenheiroDaObra && a.UsuarioAprovadorId is null)) a.UsuarioAprovadorId = eng;
    }

    /// <summary>
    /// Aprovador nominal inativo (saiu da empresa, login desativado): a etapa vai para o substituto da alçada, se ativo;
    /// sem substituto, volta a valer o perfil da alçada (qualquer usuário do perfil aprova).
    /// </summary>
    private static async Task TrocarInativosAsync(IQualyHubDb db, List<Domain.Estrutura.Alcada> alcadas, CancellationToken ct)
    {
        var ids = alcadas.SelectMany(a => new[] { a.UsuarioAprovadorId, a.SubstitutoUsuarioId }).Where(x => x is not null).Select(x => x!.Value).Distinct().ToList();
        if (ids.Count == 0) return;
        var ativos = (await db.Usuarios.AsNoTracking().Where(u => ids.Contains(u.Id) && u.Ativo).Select(u => u.Id).ToListAsync(ct)).ToHashSet();
        foreach (var a in alcadas.Where(a => a.UsuarioAprovadorId is { } u && !ativos.Contains(u)))
            a.UsuarioAprovadorId = a.SubstitutoUsuarioId is { } s && ativos.Contains(s) ? s : null;
    }

    /// <summary>SPE (destino do custo) do documento em aprovação — base do engenheiro responsável.</summary>
    public static async Task<Guid?> SpeDoDocumentoAsync(IQualyHubDb db, EntidadeAprovavel entidade, Guid id, CancellationToken ct = default) => entidade switch
    {
        EntidadeAprovavel.Requisicao => await db.Requisicoes.Where(x => x.Id == id).Select(x => x.SpeId).FirstOrDefaultAsync(ct),
        EntidadeAprovavel.Cotacao => await (from c in db.Cotacoes join r in db.Requisicoes on c.RequisicaoId equals r.Id where c.Id == id select r.SpeId).FirstOrDefaultAsync(ct),
        EntidadeAprovavel.Pedido => await db.Pedidos.Where(x => x.Id == id).Select(x => x.SpeId).FirstOrDefaultAsync(ct),
        EntidadeAprovavel.Contrato => await db.Contratos.Where(x => x.Id == id).Select(x => x.SpeId).FirstOrDefaultAsync(ct),
        EntidadeAprovavel.Aditivo => await (from a in db.Aditivos join k in db.Contratos on a.ContratoId equals k.Id where a.Id == id select k.SpeId).FirstOrDefaultAsync(ct),
        EntidadeAprovavel.Medicao => await (from m in db.Medicoes join k in db.Contratos on m.ContratoId equals k.Id where m.Id == id select k.SpeId).FirstOrDefaultAsync(ct),
        _ => null
    };

    /// <summary>
    /// Cadastro mudou (alçada, engenheiro da SPE ou da obra): as aprovações em andamento refazem as etapas que ainda
    /// não foram decididas, com as alçadas e o engenheiro atuais. Etapas já decididas ficam como estão (evidência).
    /// Chame depois de gravar o cadastro (lê o banco). Retorna quantas solicitações mudaram.
    /// </summary>
    public static async Task<int> RecalcularPendentesAsync(IQualyHubDb db, System.Linq.Expressions.Expression<Func<SolicitacaoAprovacao, bool>> filtro, CancellationToken ct = default)
    {
        var abertas = await db.SolicitacoesAprovacao.Include(s => s.Etapas).Where(s => s.Status == StatusAprovacao.EmAprovacao).Where(filtro).ToListAsync(ct);
        if (abertas.Count == 0) return 0;
        var todas = await db.Alcadas.AsNoTracking().ToListAsync(ct);
        var mudaram = 0;
        foreach (var s in abertas)
        {
            var alcadas = todas.Where(a => a.Entidade == s.Entidade && a.SeAplica(s.Valor, s.ObraId)).OrderBy(a => a.Ordem)
                               .Select(a => new Domain.Estrutura.Alcada { Entidade = a.Entidade, Ordem = a.Ordem, PerfilAprovador = a.PerfilAprovador, UsuarioAprovadorId = a.UsuarioAprovadorId, SubstitutoUsuarioId = a.SubstitutoUsuarioId, EngenheiroDaObra = a.EngenheiroDaObra })
                               .ToList();
            try { await ResolverEngenheiroAsync(db, alcadas, s.ObraId, await SpeDoDocumentoAsync(db, s.Entidade, s.EntidadeId, ct), ct); }
            catch (RegraDeNegocioException) { continue; } // SPE ficou sem engenheiro: mantém o aprovador atual
            await TrocarInativosAsync(db, alcadas, ct);
            var (removidas, novas) = s.TrocarEtapasPendentes(alcadas.Select(a => (a.PerfilAprovador, a.UsuarioAprovadorId)).ToList());
            if (novas.Count == 0) continue;
            db.EtapasAprovacao.RemoveRange(removidas);
            db.EtapasAprovacao.AddRange(novas);
            mudaram++;
        }
        return mudaram;
    }

    /// <summary>Monta o aprovador logado (perfil + limites) e as delegações vigentes recebidas por ele.</summary>
    public async Task<(Aprovador Ator, List<DelegacaoRecebida> Delegacoes)> AtorAsync(IQualyHubDb db, CancellationToken ct = default)
    {
        var uid = usuario.UsuarioId ?? throw new RegraDeNegocioException("Usuário não identificado.");
        var u = await db.Usuarios.AsNoTracking().Include(x => x.LimitesAprovacao).FirstAsync(x => x.Id == uid, ct);
        var agora = relogio.GetUtcNow();
        var dels = await db.DelegacoesAprovacao.AsNoTracking()
            .Where(d => d.SubstitutoUsuarioId == uid && !d.Revogada && d.Inicio <= agora && d.Fim > agora).ToListAsync(ct);
        var titularesIds = dels.Select(d => d.TitularUsuarioId).Distinct().ToList();
        var titulares = await db.Usuarios.AsNoTracking().Include(x => x.LimitesAprovacao).Where(x => titularesIds.Contains(x.Id)).ToListAsync(ct);
        var recebidas = dels.Select(d =>
        {
            var t = titulares.First(x => x.Id == d.TitularUsuarioId);
            return new DelegacaoRecebida(d, new Aprovador(t.Id, t.Nome, t.Perfil, t.LimitesAprovacao.ToList()));
        }).ToList();
        return (new Aprovador(u.Id, u.Nome, u.Perfil, u.LimitesAprovacao.ToList()), recebidas);
    }

    private (Contratos.MapaAcesso Mapa, DateTimeOffset Em)? _mapa;

    /// <summary>O usuário logado pode decidir a etapa pendente? (para habilitar os botões na tela).</summary>
    public async Task<bool> PodeDecidirAsync(IQualyHubDb db, SolicitacaoAprovacao s, CancellationToken ct = default)
    {
        if (s.Status != StatusAprovacao.EmAprovacao || s.Pendente is not { } etapa || usuario.UsuarioId is not { } uid) return false;
        if (_mapa is not { } m || m.Em < relogio.GetUtcNow().AddSeconds(-30)) _mapa = m = (await AcessoServico.CalcularAsync(db, uid, ct), relogio.GetUtcNow());
        if (!m.Mapa.Pode(TelaDe(s.Entidade), Contratos.AcaoTela.Aprovar)) return false;
        var (ator, dels) = await AtorAsync(db, ct);
        return etapa.Autoriza(ator) || dels.Any(d => d.Delegacao.Vigente(relogio.GetUtcNow(), s.Entidade, s.ObraId) && etapa.Autoriza(d.Titular));
    }

    public async Task<SolicitacaoAprovacao> DecidirAsync(IQualyHubDb db, Guid solicitacaoId, DecisaoAprovacao decisao, string? justificativa, CancellationToken ct = default)
    {
        var s = await db.SolicitacoesAprovacao.Include(x => x.Etapas).FirstAsync(x => x.Id == solicitacaoId, ct);
        // Aprovar = permissão "Aprovar" na tela do documento (perfil de acesso) E a alçada de valor (abaixo).
        await Permissoes.ExigirTelaAsync(db, usuario, TelaDe(s.Entidade), Contratos.AcaoTela.Aprovar, ct);
        var (ator, dels) = await AtorAsync(db, ct);
        var agora = relogio.GetUtcNow();
        var etapa = s.Pendente;
        s.Decidir(ator, decisao, justificativa, agora, dels);
        await RefletirAsync(db, s, etapa!, ator.Nome, agora, justificativa, ct);
        return s;
    }

    /// <summary>Tela do documento em aprovação (onde vale a permissão "Aprovar").</summary>
    public static string TelaDe(EntidadeAprovavel e) => e switch
    {
        EntidadeAprovavel.Pedido => "pedidos", EntidadeAprovavel.Medicao => "medicoes", EntidadeAprovavel.DocumentoSgq => "sgq/documentos",
        EntidadeAprovavel.AvaliacaoFornecedor => "fornecedores/avaliacao", EntidadeAprovavel.Requisicao => "requisicoes", EntidadeAprovavel.Cotacao => "cotacoes",
        EntidadeAprovavel.Contrato or EntidadeAprovavel.Aditivo => "contratos", EntidadeAprovavel.Pagamento => "pagamentos",
        EntidadeAprovavel.Cronograma => "engenharia/planejamento", _ => throw new ArgumentOutOfRangeException(nameof(e))
    };

    public static async Task<SolicitacaoAprovacao?> UltimaAsync(IQualyHubDb db, EntidadeAprovavel e, Guid id, CancellationToken ct = default) =>
        await db.SolicitacoesAprovacao.AsNoTracking().Include(x => x.Etapas).Where(x => x.Entidade == e && x.EntidadeId == id)
                .OrderByDescending(x => x.EnviadoEm).FirstOrDefaultAsync(ct);

    private static async Task RefletirAsync(IQualyHubDb db, SolicitacaoAprovacao s, EtapaAprovacao etapa, string nome, DateTimeOffset agora, string? just, CancellationToken ct)
    {
        var aprovado = s.Status == StatusAprovacao.Aprovado;
        var reprovado = s.Status == StatusAprovacao.Reprovado;
        var devolvido = s.Status == StatusAprovacao.Rascunho;
        switch (s.Entidade)
        {
            case EntidadeAprovavel.Requisicao:
                var r = await db.Requisicoes.FirstAsync(x => x.Id == s.EntidadeId, ct);
                if (etapa.Decisao == DecisaoAprovacao.Aprovado)
                {
                    if (etapa.Ordem == 1) { r.AprovadoEngenheiroPor = nome; r.AprovadoEngenheiroEm = agora; }
                    else { r.AprovadoDiretorPor = nome; r.AprovadoDiretorEm = agora; }
                }
                r.Status = aprovado ? StatusRequisicao.Autorizada : reprovado ? StatusRequisicao.NaoAutorizada
                         : devolvido ? StatusRequisicao.DevolvidaAjuste : StatusRequisicao.AguardandoDiretorTecnico;
                if (reprovado || devolvido) r.MotivoReprovacao = just;
                break;
            case EntidadeAprovavel.Cotacao:
                var c = await db.Cotacoes.FirstAsync(x => x.Id == s.EntidadeId, ct);
                if (aprovado) { c.AprovadoPor = nome; c.AprovadoEm = agora; c.Status = StatusCotacao.Aprovado; }
                else if (reprovado || devolvido) c.Status = StatusCotacao.ReprovadoVoltaCotacao;
                break;
            case EntidadeAprovavel.Pedido:
                var p = await db.Pedidos.FirstAsync(x => x.Id == s.EntidadeId, ct);
                if (aprovado) { p.AprovadoDiretorPor = nome; p.AprovadoDiretorEm = agora; p.Status = StatusPedido.Aprovado; }
                else if (reprovado) p.Status = StatusPedido.Cancelado;
                else if (devolvido) p.Status = StatusPedido.Emitido;
                break;
            case EntidadeAprovavel.Contrato:
                var k = await db.Contratos.FirstAsync(x => x.Id == s.EntidadeId, ct);
                if (aprovado) { k.AprovadoDiretoriaPor = nome; k.AprovadoDiretoriaEm = agora; k.Status = StatusContrato.AguardandoAssinaturas; }
                else if (reprovado || devolvido) k.Status = StatusContrato.EmElaboracao;
                break;
            case EntidadeAprovavel.Medicao:
                var m = await db.Medicoes.FirstAsync(x => x.Id == s.EntidadeId, ct);
                if (aprovado) { m.AprovadoDiretorPor = nome; m.AprovadoDiretorEm = agora; m.Status = StatusMedicao.Aprovada; }
                else if (reprovado || devolvido) { m.Status = StatusMedicao.Reprovada; m.MotivoReprovacao = just; }
                break;
            case EntidadeAprovavel.Aditivo:
                var ad = await db.Aditivos.FirstAsync(x => x.Id == s.EntidadeId, ct);
                if (aprovado) { ad.AprovadoPor = nome; ad.AprovadoEm = agora; ad.Status = StatusAditivo.AguardandoAssinaturas; }
                else if (reprovado) ad.Status = StatusAditivo.Reprovado;
                else if (devolvido) ad.Status = StatusAditivo.EmElaboracao;
                break;
            case EntidadeAprovavel.Cronograma:
                await PlanejamentoServico.RefletirDecisaoAsync(db, s.EntidadeId, aprovado, reprovado || devolvido, nome, agora, just, ct);
                break;
            case EntidadeAprovavel.Pagamento:
                var l = await db.Pagamentos.FirstAsync(x => x.Id == s.EntidadeId, ct);
                if (aprovado) { l.LiberadoPor = nome; l.LiberadoEm = agora; l.Status = StatusPagamento.Liberado; }
                else if (reprovado || devolvido) l.Status = StatusPagamento.Bloqueado;
                break;
        }
    }
}
