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
        // "Engenheiro responsável" (T21/T24): o da SPE do documento; obra sem SPE usa o engenheiro da obra. Resolvido no envio.
        if (alcadas.Any(a => a.EngenheiroDaObra))
        {
            Guid? eng = null;
            if (speId is { } sid) eng = await db.Spes.AsNoTracking().Where(s => s.Id == sid).Select(s => s.EngenheiroId).FirstOrDefaultAsync(ct);
            if (eng is null && speId is not null)
                throw new RegraDeNegocioException("Cadastre o engenheiro responsável da SPE (Engenharia › Obras e Empreendimentos › SPE) antes de enviar para aprovação.");
            if (eng is null && obraId is { } oid) eng = await db.Obras.AsNoTracking().Where(o => o.Id == oid).Select(o => o.EngenheiroId).FirstOrDefaultAsync(ct);
            foreach (var a in alcadas.Where(a => a.EngenheiroDaObra && a.UsuarioAprovadorId is null)) a.UsuarioAprovadorId = eng;
        }
        var s = SolicitacaoAprovacao.Criar(entidade, entidadeId, usuario.EmpresaId, obraId, titulo, valor,
            usuario.UsuarioId ?? Guid.Empty, usuario.Nome, alcadas, relogio.GetUtcNow());
        db.SolicitacoesAprovacao.Add(s);
        return s;
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

    /// <summary>O usuário logado pode decidir a etapa pendente? (para habilitar os botões na tela).</summary>
    public async Task<bool> PodeDecidirAsync(IQualyHubDb db, SolicitacaoAprovacao s, CancellationToken ct = default)
    {
        if (s.Status != StatusAprovacao.EmAprovacao || s.Pendente is not { } etapa || usuario.UsuarioId is null) return false;
        var (ator, dels) = await AtorAsync(db, ct);
        return etapa.Autoriza(ator) || dels.Any(d => d.Delegacao.Vigente(relogio.GetUtcNow(), s.Entidade, s.ObraId) && etapa.Autoriza(d.Titular));
    }

    public async Task<SolicitacaoAprovacao> DecidirAsync(IQualyHubDb db, Guid solicitacaoId, DecisaoAprovacao decisao, string? justificativa, CancellationToken ct = default)
    {
        var s = await db.SolicitacoesAprovacao.Include(x => x.Etapas).FirstAsync(x => x.Id == solicitacaoId, ct);
        var (ator, dels) = await AtorAsync(db, ct);
        var agora = relogio.GetUtcNow();
        var etapa = s.Pendente;
        s.Decidir(ator, decisao, justificativa, agora, dels);
        await RefletirAsync(db, s, etapa!, ator.Nome, agora, justificativa, ct);
        return s;
    }

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
            case EntidadeAprovavel.Pagamento:
                var l = await db.Pagamentos.FirstAsync(x => x.Id == s.EntidadeId, ct);
                if (aprovado) { l.LiberadoPor = nome; l.LiberadoEm = agora; l.Status = StatusPagamento.Liberado; }
                else if (reprovado || devolvido) l.Status = StatusPagamento.Bloqueado;
                break;
        }
    }
}
