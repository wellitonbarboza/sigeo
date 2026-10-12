using Microsoft.EntityFrameworkCore;
using QualyHub.Application.Planejamento;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Planejamento;

namespace QualyHub.Application.Servicos;

/// <summary>
/// Planejamento da obra: redes (locais + pacotes do orçamento), geração do cronograma, edição (duração, avanço,
/// predecessoras por EAP, arrastar = folga no vínculo), reprogramações com aprovação (engenheiro → diretor técnico),
/// desembolso mensal pelo custo orçado e importação/exportação MS Project.
/// </summary>
public sealed class PlanejamentoServico(IFabricaDb fabrica, IUsuarioAtual usuario, AprovacaoServico aprovacoes, TimeProvider relogio)
{
    public sealed record AtividadeEdicao(Guid? Id, Guid? PaiId, string Nome, int Duracao, decimal Avanco, string? Predecessoras, DateOnly? Inicio, bool Resumo = false);
    public sealed record PacoteEdicao(Guid? Id, string Nome, Guid? ComposicaoId, int DuracaoPorLocal, int Equipes, IReadOnlyDictionary<Guid, bool>? Ajustes = null);
    public sealed record RedeEdicao(Guid? Id, Guid ObraId, string Nome, TipoRede Tipo, string Cor, int Pulmao, IReadOnlyList<Guid> Locais, IReadOnlyList<PacoteEdicao> Pacotes);

    private Task ExigirAsync(IQualyHubDb db, string acao) =>
        Permissoes.ExigirAsync(db, usuario, acao, PerfilUsuario.Engenharia, PerfilUsuario.Diretoria);

    // ───────── Calendário ─────────

    public static async Task<Calendario> CalendarioAsync(IQualyHubDb db, Guid obraId, CancellationToken ct = default)
    {
        var feriados = await db.Feriados.AsNoTracking().Where(f => f.ObraId == null || f.ObraId == obraId).Select(f => f.Data).ToListAsync(ct);
        return new Calendario(feriados);
    }

    public async Task SalvarFeriadoAsync(Feriado f, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "cadastrar feriados");
        if (string.IsNullOrWhiteSpace(f.Nome)) throw new ValidacaoException(["Informe o nome do feriado."]);
        if (await db.Feriados.AnyAsync(x => x.Id != f.Id && x.Data == f.Data && x.ObraId == f.ObraId, ct)) throw new RegraDeNegocioException("Já existe feriado nessa data.");
        await Grafo.SalvarAsync(db, f);
        await db.SaveChangesAsync(ct);
        if (f.ObraId is { } o) await RecalcularAbertasAsync(o, ct);
    }

    public async Task ExcluirFeriadoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "cadastrar feriados");
        var f = await db.Feriados.FirstAsync(x => x.Id == id, ct);
        db.Remove(f);
        await db.SaveChangesAsync(ct);
    }

    private async Task RecalcularAbertasAsync(Guid obraId, CancellationToken ct)
    {
        await using var db = fabrica.Criar();
        foreach (var id in await db.VersoesCronograma.Where(v => v.ObraId == obraId && v.Status == StatusVersao.EmElaboracao).Select(v => v.Id).ToListAsync(ct))
        {
            var v = await CarregarAsync(db, id, ct);
            await RecalcularAsync(db, v, ct);
        }
        await db.SaveChangesAsync(ct);
    }

    // ───────── Versões ─────────

    /// <summary>Versão editável da obra; sem nenhuma versão, cria a LB0 começando no início da obra (ou hoje).</summary>
    public async Task<Guid?> VersaoEmElaboracaoAsync(Guid obraId, bool criarSeNaoHouver, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var aberta = await db.VersoesCronograma.Where(v => v.ObraId == obraId && v.Status == StatusVersao.EmElaboracao).Select(v => (Guid?)v.Id).FirstOrDefaultAsync(ct);
        if (aberta is not null || !criarSeNaoHouver) return aberta;
        if (await db.VersoesCronograma.AnyAsync(v => v.ObraId == obraId, ct)) return null;
        await ExigirAsync(db, "criar o cronograma");
        var obra = await db.Obras.AsNoTracking().FirstAsync(o => o.Id == obraId, ct);
        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().DateTime);
        var cal = await CalendarioAsync(db, obraId, ct);
        var v = new VersaoCronograma { ObraId = obraId, Numero = 0, Codigo = "LB0", InicioProjeto = cal.ProximoUtil(obra.DataInicio ?? hoje), DataStatus = hoje, Motivo = "Planejamento inicial" };
        db.VersoesCronograma.Add(v);
        await db.SaveChangesAsync(ct);
        return v.Id;
    }

    /// <summary>Nova reprogramação: copia a versão vigente (atividades, vínculos, avanço e linha de base).</summary>
    public async Task<Guid> NovaReprogramacaoAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "reprogramar o cronograma");
        if (await db.VersoesCronograma.AnyAsync(v => v.ObraId == obraId && (v.Status == StatusVersao.EmElaboracao || v.Status == StatusVersao.EmAprovacao), ct))
            throw new RegraDeNegocioException("Já existe uma versão em elaboração ou em aprovação. Conclua-a antes de abrir outra.");
        var vigente = await db.VersoesCronograma.AsNoTracking().Include(v => v.Atividades).Include(v => v.Vinculos)
            .FirstOrDefaultAsync(v => v.ObraId == obraId && v.Status == StatusVersao.Vigente, ct)
            ?? throw new RegraDeNegocioException("Não há versão vigente para reprogramar. Aprove a linha de base (LB0) primeiro.");
        var numero = await db.VersoesCronograma.Where(v => v.ObraId == obraId).MaxAsync(v => v.Numero, ct) + 1;
        var n = new VersaoCronograma
        {
            ObraId = obraId, Numero = numero, Codigo = VersaoCronograma.CodigoDe(numero), InicioProjeto = vigente.InicioProjeto,
            DataStatus = DateOnly.FromDateTime(relogio.GetLocalNow().DateTime), TerminoBase = vigente.TerminoBase
        };
        var mapa = vigente.Atividades.ToDictionary(a => a.Id, _ => Guid.CreateVersion7());
        foreach (var a in vigente.Atividades)
            n.Atividades.Add(new AtividadeCronograma
            {
                Id = mapa[a.Id], VersaoId = n.Id, PaiId = a.PaiId is { } p ? mapa[p] : null, Ordem = a.Ordem, Eap = a.Eap, Nome = a.Nome, Duracao = a.Duracao,
                Inicio = a.Inicio, Termino = a.Termino, InicioBase = a.InicioBase, TerminoBase = a.TerminoBase, InicioMinimo = a.InicioMinimo,
                Avanco = a.Avanco, Custo = a.Custo, ComposicaoId = a.ComposicaoId, LocalId = a.LocalId, RedeId = a.RedeId, PacoteId = a.PacoteId,
                Chave = a.Chave, Resumo = a.Resumo, Folga = a.Folga, Critica = a.Critica
            });
        foreach (var l in vigente.Vinculos)
            n.Vinculos.Add(new VinculoAtividade { VersaoId = n.Id, PredecessoraId = mapa[l.PredecessoraId], SucessoraId = mapa[l.SucessoraId], Tipo = l.Tipo, Lag = l.Lag });
        n.TerminoPrevisto = vigente.TerminoPrevisto; n.CustoTotal = vigente.CustoTotal;
        db.VersoesCronograma.Add(n);
        await db.SaveChangesAsync(ct);
        return n.Id;
    }

    /// <summary>Descarta a versão em elaboração (exclusão lógica da versão; atividades e vínculos removidos).</summary>
    public async Task DescartarAsync(Guid versaoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "descartar a versão");
        var v = await CarregarAsync(db, versaoId, ct);
        if (!v.Editavel) throw new RegraDeNegocioException("Só a versão em elaboração pode ser descartada.");
        if (v.Numero == 0 && !await db.VersoesCronograma.AnyAsync(x => x.ObraId == v.ObraId && x.Id != v.Id, ct))
        {
            db.VinculosAtividade.RemoveRange(v.Vinculos); db.AtividadesCronograma.RemoveRange(v.Atividades);
            v.Atividades.Clear(); v.Vinculos.Clear(); v.TerminoPrevisto = null; v.CustoTotal = 0;
        }
        else
        {
            db.VinculosAtividade.RemoveRange(v.Vinculos); db.AtividadesCronograma.RemoveRange(v.Atividades);
            db.Remove(v);
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task<VersaoCronograma> CarregarAsync(IQualyHubDb db, Guid versaoId, CancellationToken ct) =>
        await db.VersoesCronograma.Include(v => v.Atividades).Include(v => v.Vinculos).AsSplitQuery().FirstOrDefaultAsync(v => v.Id == versaoId, ct)
        ?? throw new RegraDeNegocioException("Versão do cronograma não encontrada.");

    private async Task<VersaoCronograma> EditavelAsync(IQualyHubDb db, Guid versaoId, CancellationToken ct)
    {
        await ExigirAsync(db, "editar o cronograma");
        var v = await CarregarAsync(db, versaoId, ct);
        if (!v.Editavel) throw new RegraDeNegocioException($"A versão {v.Codigo} está {(v.Status == StatusVersao.EmAprovacao ? "em aprovação" : "fechada")}. Abra uma reprogramação para alterar.");
        return v;
    }

    internal static async Task RecalcularAsync(IQualyHubDb db, VersaoCronograma v, CancellationToken ct)
    {
        var cal = await CalendarioAsync(db, v.ObraId, ct);
        var ats = v.Atividades.ToList();
        MotorCronograma.Renumerar(ats);
        var r = MotorCronograma.Calcular(ats, v.Vinculos.ToList(), v.InicioProjeto, cal);
        v.TerminoPrevisto = ats.Count(a => !a.Resumo) == 0 ? null : r.Termino;
        v.CustoTotal = ats.Where(a => !a.Resumo).Sum(a => a.Custo);
    }

    public async Task AlterarInicioProjetoAsync(Guid versaoId, DateOnly inicio, DateOnly? dataStatus, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var v = await EditavelAsync(db, versaoId, ct);
        var cal = await CalendarioAsync(db, v.ObraId, ct);
        v.InicioProjeto = cal.ProximoUtil(inicio);
        v.DataStatus = dataStatus ?? v.DataStatus;
        await RecalcularAsync(db, v, ct);
        await db.SaveChangesAsync(ct);
    }

    // ───────── Atividades ─────────

    public async Task<Guid> SalvarAtividadeAsync(Guid versaoId, AtividadeEdicao e, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(e.Nome)) throw new ValidacaoException(["Informe o nome da atividade."]);
        if (e.Duracao is < 0 or > 2000) throw new ValidacaoException(["Duração entre 0 e 2000 dias úteis."]);
        if (e.Avanco is < 0 or > 100) throw new ValidacaoException(["Avanço entre 0 e 100%."]);
        await using var db = fabrica.Criar();
        var v = await EditavelAsync(db, versaoId, ct);
        AtividadeCronograma a;
        if (e.Id is { } id)
            a = v.Atividades.FirstOrDefault(x => x.Id == id) ?? throw new RegraDeNegocioException("Atividade não encontrada.");
        else
        {
            if (e.PaiId is { } pai && v.Atividades.All(x => x.Id != pai || !x.Resumo)) throw new RegraDeNegocioException("Agrupador não encontrado.");
            a = new AtividadeCronograma { VersaoId = v.Id, PaiId = e.PaiId, Resumo = e.Resumo, Ordem = v.Atividades.Where(x => x.PaiId == e.PaiId).Select(x => x.Ordem).DefaultIfEmpty(0).Max() + 1, Inicio = v.InicioProjeto, Termino = v.InicioProjeto };
            v.Atividades.Add(a);
            db.AtividadesCronograma.Add(a);
        }
        a.Nome = e.Nome.Trim();
        if (!a.Resumo) { a.Duracao = e.Duracao; a.Avanco = e.Avanco; }
        await RecalcularAsync(db, v, ct); // EAP atualizada para ler as predecessoras

        if (!a.Resumo && e.Predecessoras is not null)
        {
            var porEap = v.Atividades.ToDictionary(x => x.Eap);
            var novas = MotorCronograma.LerPredecessoras(e.Predecessoras).Select(p =>
            {
                if (!porEap.TryGetValue(p.Eap, out var pred)) throw new RegraDeNegocioException($"Predecessora {p.Eap} não existe.");
                if (pred.Resumo) throw new RegraDeNegocioException($"{p.Eap} é um agrupador; vincule às atividades dele.");
                if (pred.Id == a.Id) throw new RegraDeNegocioException("A atividade não pode ser predecessora dela mesma.");
                return (pred, p);
            }).GroupBy(x => x.pred.Id).Select(g => g.Last()).ToList();
            foreach (var velho in v.Vinculos.Where(x => x.SucessoraId == a.Id).ToList()) { v.Vinculos.Remove(velho); db.VinculosAtividade.Remove(velho); }
            foreach (var (pred, p) in novas)
            {
                var l = new VinculoAtividade { VersaoId = v.Id, PredecessoraId = pred.Id, SucessoraId = a.Id, Tipo = p.Tipo, Lag = p.Lag };
                v.Vinculos.Add(l); db.VinculosAtividade.Add(l);
            }
            await RecalcularAsync(db, v, ct);
        }
        if (!a.Resumo && e.Inicio is { } ini && ini != a.Inicio)
        {
            MotorCronograma.Mover(a, ini, v.Atividades.ToList(), v.Vinculos.ToList(), await CalendarioAsync(db, v.ObraId, ct));
            await RecalcularAsync(db, v, ct);
        }
        await db.SaveChangesAsync(ct);
        return a.Id;
    }

    /// <summary>Arrastar a barra (só move: vira folga no vínculo) e/ou puxar a ponta (muda a duração pelo novo término).</summary>
    public async Task MoverAsync(Guid versaoId, Guid atividadeId, DateOnly novoInicio, DateOnly? novoTermino = null, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var v = await EditavelAsync(db, versaoId, ct);
        var a = v.Atividades.FirstOrDefault(x => x.Id == atividadeId) ?? throw new RegraDeNegocioException("Atividade não encontrada.");
        var cal = await CalendarioAsync(db, v.ObraId, ct);
        if (novoTermino is { } fim && !a.Resumo && a.Duracao > 0)
            a.Duracao = Math.Max(1, cal.DiasUteisEntre(cal.ProximoUtil(novoInicio), fim) + (cal.EhUtil(fim) ? 1 : 0));
        if (novoInicio != a.Inicio) MotorCronograma.Mover(a, novoInicio, v.Atividades.ToList(), v.Vinculos.ToList(), cal);
        await RecalcularAsync(db, v, ct);
        await db.SaveChangesAsync(ct);
    }

    // ───────── Ligações (editor por rede) ─────────

    /// <summary>Liga duas atividades (predecessora → sucessora). Recusa repetida, agrupador e ciclo (o motor acusa).</summary>
    public async Task<Guid> LigarAsync(Guid versaoId, Guid predecessoraId, Guid sucessoraId, TipoVinculo tipo, int lag, CancellationToken ct = default)
    {
        if (predecessoraId == sucessoraId) throw new RegraDeNegocioException("A atividade não pode ser predecessora dela mesma.");
        if (lag is < -400 or > 400) throw new ValidacaoException(["Folga entre -400 e 400 dias úteis."]);
        await using var db = fabrica.Criar();
        var v = await EditavelAsync(db, versaoId, ct);
        var p = v.Atividades.FirstOrDefault(a => a.Id == predecessoraId) ?? throw new RegraDeNegocioException("Predecessora não encontrada.");
        var s = v.Atividades.FirstOrDefault(a => a.Id == sucessoraId) ?? throw new RegraDeNegocioException("Sucessora não encontrada.");
        if (p.Resumo || s.Resumo) throw new RegraDeNegocioException("Ligue as atividades, não os agrupadores.");
        if (v.Vinculos.Any(x => x.PredecessoraId == p.Id && x.SucessoraId == s.Id)) throw new RegraDeNegocioException("Essas atividades já estão ligadas.");
        var l = new VinculoAtividade { VersaoId = v.Id, PredecessoraId = p.Id, SucessoraId = s.Id, Tipo = tipo, Lag = lag };
        v.Vinculos.Add(l); db.VinculosAtividade.Add(l);
        await RecalcularAsync(db, v, ct); // ciclo → RegraDeNegocioException do motor, nada é gravado
        await db.SaveChangesAsync(ct);
        return l.Id;
    }

    public async Task AlterarVinculoAsync(Guid versaoId, Guid vinculoId, TipoVinculo tipo, int lag, CancellationToken ct = default)
    {
        if (lag is < -400 or > 400) throw new ValidacaoException(["Folga entre -400 e 400 dias úteis."]);
        await using var db = fabrica.Criar();
        var v = await EditavelAsync(db, versaoId, ct);
        var l = v.Vinculos.FirstOrDefault(x => x.Id == vinculoId) ?? throw new RegraDeNegocioException("Ligação não encontrada.");
        l.Tipo = tipo; l.Lag = lag;
        await RecalcularAsync(db, v, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task ExcluirVinculoAsync(Guid versaoId, Guid vinculoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var v = await EditavelAsync(db, versaoId, ct);
        var l = v.Vinculos.FirstOrDefault(x => x.Id == vinculoId) ?? throw new RegraDeNegocioException("Ligação não encontrada.");
        v.Vinculos.Remove(l); db.VinculosAtividade.Remove(l);
        await RecalcularAsync(db, v, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Muda só a duração (puxar a ponta da barra no editor por rede).</summary>
    public async Task DuracaoAsync(Guid versaoId, Guid atividadeId, int duracao, CancellationToken ct = default)
    {
        if (duracao is < 0 or > 2000) throw new ValidacaoException(["Duração entre 0 e 2000 dias úteis."]);
        await using var db = fabrica.Criar();
        var v = await EditavelAsync(db, versaoId, ct);
        var a = v.Atividades.FirstOrDefault(x => x.Id == atividadeId && !x.Resumo) ?? throw new RegraDeNegocioException("Atividade não encontrada.");
        a.Duracao = duracao;
        await RecalcularAsync(db, v, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Move a atividade N dias úteis (arrastar no editor por rede): vira folga no vínculo que a segura, como no Gantt.</summary>
    public async Task MoverDiasAsync(Guid versaoId, Guid atividadeId, int diasUteis, CancellationToken ct = default)
    {
        if (diasUteis == 0) return;
        await using var db = fabrica.Criar();
        var v = await EditavelAsync(db, versaoId, ct);
        var a = v.Atividades.FirstOrDefault(x => x.Id == atividadeId) ?? throw new RegraDeNegocioException("Atividade não encontrada.");
        var cal = await CalendarioAsync(db, v.ObraId, ct);
        var alvo = cal.Data(Math.Max(cal.Indice(v.InicioProjeto), cal.Indice(cal.ProximoUtil(a.Inicio)) + diasUteis));
        MotorCronograma.Mover(a, alvo, v.Atividades.ToList(), v.Vinculos.ToList(), cal);
        await RecalcularAsync(db, v, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task ExcluirAtividadeAsync(Guid versaoId, Guid atividadeId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var v = await EditavelAsync(db, versaoId, ct);
        var filhos = v.Atividades.ToLookup(x => x.PaiId);
        var remover = new List<AtividadeCronograma>();
        void Juntar(Guid id) { var x = v.Atividades.First(a => a.Id == id); remover.Add(x); foreach (var f in filhos[id]) Juntar(f.Id); }
        Juntar(atividadeId);
        var ids = remover.Select(r => r.Id).ToHashSet();
        foreach (var l in v.Vinculos.Where(l => ids.Contains(l.PredecessoraId) || ids.Contains(l.SucessoraId)).ToList()) { v.Vinculos.Remove(l); db.VinculosAtividade.Remove(l); }
        // filhos antes do pai (FK sem cascata)
        foreach (var r in remover.AsEnumerable().Reverse()) { v.Atividades.Remove(r); db.AtividadesCronograma.Remove(r); }
        await RecalcularAsync(db, v, ct);
        await db.SaveChangesAsync(ct);
    }

    // ───────── Redes ─────────

    public async Task<Guid> SalvarRedeAsync(RedeEdicao e, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "editar as redes");
        var rede = e.Id is { } id
            ? await db.RedesPlanejamento.Include(r => r.Locais).Include(r => r.Pacotes).ThenInclude(p => p.Ajustes).AsSplitQuery().FirstAsync(r => r.Id == id, ct)
            : new RedePlanejamento { ObraId = e.ObraId, Ordem = (await db.RedesPlanejamento.Where(r => r.ObraId == e.ObraId).MaxAsync(r => (int?)r.Ordem, ct) ?? 0) + 1 };
        rede.Nome = e.Nome.Trim(); rede.Tipo = e.Tipo; rede.Cor = string.IsNullOrWhiteSpace(e.Cor) ? "#4f52d6" : e.Cor; rede.Pulmao = e.Pulmao;
        var locaisValidos = await db.Locais.Where(l => l.ObraId == rede.ObraId && e.Locais.Contains(l.Id)).Select(l => l.Id).ToListAsync(ct);
        // locais na ordem informada
        var ordemLocais = e.Locais.Where(locaisValidos.Contains).Distinct().ToList();
        foreach (var x in rede.Locais.Where(x => !ordemLocais.Contains(x.LocalId)).ToList()) { rede.Locais.Remove(x); db.Remove(x); }
        for (var i = 0; i < ordemLocais.Count; i++)
        {
            var x = rede.Locais.FirstOrDefault(l => l.LocalId == ordemLocais[i]);
            if (x is null) { x = new RedeLocal { RedeId = rede.Id, LocalId = ordemLocais[i] }; rede.Locais.Add(x); if (e.Id is not null) db.Add(x); }
            x.Ordem = i + 1;
        }
        var idsPacotes = e.Pacotes.Where(p => p.Id != null).Select(p => p.Id!.Value).ToHashSet();
        foreach (var p in rede.Pacotes.Where(p => !idsPacotes.Contains(p.Id)).ToList())
        {
            if (await db.VinculosRede.AnyAsync(vr => vr.PacoteOrigemId == p.Id || vr.PacoteDestinoId == p.Id, ct))
                throw new RegraDeNegocioException($"O pacote {p.Nome} tem vínculo com outra rede. Exclua o vínculo antes.");
            foreach (var aj in p.Ajustes.ToList()) db.Remove(aj);
            rede.Pacotes.Remove(p); db.Remove(p);
        }
        var ordem = 0;
        foreach (var pe in e.Pacotes)
        {
            var p = pe.Id is { } pid ? rede.Pacotes.FirstOrDefault(x => x.Id == pid) : null;
            if (p is null) { p = new RedePacote { RedeId = rede.Id }; rede.Pacotes.Add(p); if (e.Id is not null) db.Add(p); }
            p.Ordem = ++ordem; p.Nome = pe.Nome.Trim(); p.ComposicaoId = pe.ComposicaoId; p.DuracaoPorLocal = pe.DuracaoPorLocal; p.Equipes = pe.Equipes;
            if (pe.Ajustes is not null)
            {
                foreach (var aj in p.Ajustes.Where(a => !pe.Ajustes.ContainsKey(a.LocalId)).ToList()) { p.Ajustes.Remove(aj); db.Remove(aj); }
                foreach (var (local, incl) in pe.Ajustes)
                {
                    var aj = p.Ajustes.FirstOrDefault(a => a.LocalId == local);
                    if (aj is null) { aj = new RedePacoteLocal { PacoteId = p.Id, LocalId = local }; p.Ajustes.Add(aj); if (e.Id is not null || pe.Id is not null) db.Add(aj); }
                    aj.Incluido = incl;
                }
            }
        }
        ValidacaoException.Se(rede.Validar());
        if (e.Id is null) db.RedesPlanejamento.Add(rede);
        await db.SaveChangesAsync(ct);
        return rede.Id;
    }

    public async Task ExcluirRedeAsync(Guid redeId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "editar as redes");
        var rede = await db.RedesPlanejamento.Include(r => r.Locais).Include(r => r.Pacotes).ThenInclude(p => p.Ajustes).AsSplitQuery().FirstAsync(r => r.Id == redeId, ct);
        var pacotes = rede.Pacotes.Select(p => p.Id).ToList();
        db.VinculosRede.RemoveRange(await db.VinculosRede.Where(v => pacotes.Contains(v.PacoteOrigemId) || pacotes.Contains(v.PacoteDestinoId)).ToListAsync(ct));
        foreach (var p in rede.Pacotes) db.RedePacoteLocais.RemoveRange(p.Ajustes);
        db.RedePacotes.RemoveRange(rede.Pacotes); db.RedeLocais.RemoveRange(rede.Locais);
        db.Remove(rede);
        await db.SaveChangesAsync(ct);
    }

    public async Task SalvarVinculoRedeAsync(VinculoRede v, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "editar as redes");
        if (v.PacoteOrigemId == v.PacoteDestinoId) throw new RegraDeNegocioException("Escolha pacotes diferentes.");
        var redes = await (from p in db.RedePacotes join r in db.RedesPlanejamento on p.RedeId equals r.Id
                           where p.Id == v.PacoteOrigemId || p.Id == v.PacoteDestinoId select new { p.Id, r.ObraId }).ToListAsync(ct);
        if (redes.Count != 2 || redes.Any(r => r.ObraId != v.ObraId)) throw new RegraDeNegocioException("Pacotes não encontrados nesta obra.");
        if (v.Lag is < -60 or > 120 || v.Deslocamento is < -20 or > 20) throw new ValidacaoException(["Folga entre −60 e 120; deslocamento entre −20 e 20 pavimentos."]);
        await Grafo.SalvarAsync(db, v);
        await db.SaveChangesAsync(ct);
    }

    public async Task ExcluirVinculoRedeAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "editar as redes");
        db.Remove(await db.VinculosRede.FirstAsync(x => x.Id == id, ct));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Custo orçado por (serviço, local), somando a subárvore do local (apartamento = seus cômodos).</summary>
    private static async Task<Func<Guid, Guid, decimal>> CustosAsync(IQualyHubDb db, Guid obraId, IReadOnlyCollection<Local> locais, CancellationToken ct)
    {
        var celulas = await db.ServicosLocais.AsNoTracking().Where(s => s.ObraId == obraId).Select(s => new { s.ComposicaoId, s.LocalId, s.Custo }).ToListAsync(ct);
        var porLocal = celulas.GroupBy(c => (c.ComposicaoId, c.LocalId)).ToDictionary(g => g.Key, g => g.Sum(x => x.Custo));
        var filhos = locais.ToLookup(l => l.LocalPaiId);
        var cache = new Dictionary<(Guid, Guid), decimal>();
        decimal Custo(Guid comp, Guid local)
        {
            if (cache.TryGetValue((comp, local), out var c)) return c;
            c = porLocal.GetValueOrDefault((comp, local)) + filhos[local].Sum(f => Custo(comp, f.Id));
            return cache[(comp, local)] = c;
        }
        return Custo;
    }

    /// <summary>Monta as definições das redes da obra para o gerador (locais na ordem, o que está incluso por pacote).</summary>
    public static async Task<(List<GeradorRedes.RedeDef> Redes, List<GeradorRedes.VinculoRedeDef> Vinculos, Func<Guid, Guid, decimal> Custo, List<Local> Locais)> DefinicoesAsync(IQualyHubDb db, Guid obraId, CancellationToken ct = default)
    {
        var locais = await db.Locais.AsNoTracking().Where(l => l.ObraId == obraId).ToListAsync(ct);
        var porId = locais.ToDictionary(l => l.Id);
        var custo = await CustosAsync(db, obraId, locais, ct);
        var redes = await db.RedesPlanejamento.AsNoTracking().Include(r => r.Locais).Include(r => r.Pacotes).ThenInclude(p => p.Ajustes).AsSplitQuery()
            .Where(r => r.ObraId == obraId).OrderBy(r => r.Ordem).ToListAsync(ct);
        int Pavimento(Local l)
        {
            for (var x = l; x is not null; x = x.LocalPaiId is { } p ? porId.GetValueOrDefault(p) : null)
                if (x.Tipo == GeradorEmpreendimento.TipoPavimento) return x.Ordem;
            return 0;
        }
        var defs = redes.Select(r =>
        {
            var ls = r.Locais.OrderBy(x => x.Ordem).Where(x => porId.ContainsKey(x.LocalId)).Select(x => porId[x.LocalId])
                .Select(l => new GeradorRedes.LocalDef(l.Id, l.Nome, Pavimento(l))).ToList();
            var ps = r.Pacotes.OrderBy(p => p.Ordem).Select(p =>
            {
                var aj = p.Ajustes.ToDictionary(a => a.LocalId, a => a.Incluido);
                var incl = ls.Where(l => aj.TryGetValue(l.Id, out var m) ? m : p.ComposicaoId is not { } c || custo(c, l.Id) > 0 || r.Tipo != TipoRede.PorLocal)
                             .Select(l => l.Id).ToHashSet();
                return new GeradorRedes.PacoteDef(p.Id, p.Nome, p.ComposicaoId, p.DuracaoPorLocal, p.Equipes, incl);
            }).ToList();
            return new GeradorRedes.RedeDef(r.Id, r.Nome, r.Tipo, r.Pulmao, ls, ps);
        }).ToList();
        var vinculos = await db.VinculosRede.AsNoTracking().Where(v => v.ObraId == obraId)
            .Select(v => new GeradorRedes.VinculoRedeDef(v.PacoteOrigemId, v.PacoteDestinoId, v.Modo, v.Deslocamento, v.Lag)).ToListAsync(ct);
        return (defs, vinculos, custo, locais);
    }

    /// <summary>Gera (ou regera) o cronograma da versão em elaboração a partir das redes, preservando avanço, base e folgas ajustadas.</summary>
    public async Task<int> GerarPelasRedesAsync(Guid versaoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var v = await EditavelAsync(db, versaoId, ct);
        var (redes, vinculos, custo, _) = await DefinicoesAsync(db, v.ObraId, ct);
        if (redes.Count == 0) throw new RegraDeNegocioException("Cadastre as redes da obra (aba Redes) antes de gerar.");
        var nova = GeradorRedes.Gerar(v.Id, redes, vinculos, custo);
        var m = GeradorRedes.Mesclar(v.Atividades.ToList(), v.Vinculos.ToList(), nova);
        var manuais = m.Atividades.Where(a => a.Chave is null).Select(a => a.Id).ToHashSet();

        db.VinculosAtividade.RemoveRange(v.Vinculos);
        foreach (var a in Profundidade(v.Atividades.Where(a => !manuais.Contains(a.Id)).ToList())) db.AtividadesCronograma.Remove(a);
        await db.SaveChangesAsync(ct);

        v.Atividades.Clear(); v.Vinculos.Clear();
        var existentes = await db.AtividadesCronograma.Where(a => a.VersaoId == v.Id).ToListAsync(ct); // manuais
        foreach (var a in existentes) { var nm = m.Atividades.First(x => x.Id == a.Id); a.PaiId = nm.PaiId; a.Ordem = nm.Ordem; v.Atividades.Add(a); }
        foreach (var a in m.Atividades.Where(a => !manuais.Contains(a.Id))) { a.VersaoId = v.Id; v.Atividades.Add(a); db.AtividadesCronograma.Add(a); }
        foreach (var l in m.Vinculos) { l.VersaoId = v.Id; v.Vinculos.Add(l); db.VinculosAtividade.Add(l); }
        await RecalcularAsync(db, v, ct);
        await db.SaveChangesAsync(ct);
        return v.Atividades.Count(a => !a.Resumo);
    }

    /// <summary>Filhos antes dos pais (para excluir sem cascata).</summary>
    private static List<AtividadeCronograma> Profundidade(List<AtividadeCronograma> ats)
    {
        var nivel = new Dictionary<Guid, int>();
        var porId = ats.ToDictionary(a => a.Id);
        int N(AtividadeCronograma a) => nivel.TryGetValue(a.Id, out var n) ? n : nivel[a.Id] = a.PaiId is { } p && porId.TryGetValue(p, out var pai) ? N(pai) + 1 : 0;
        return [.. ats.OrderByDescending(N)];
    }

    /// <summary>
    /// Sugere redes a partir da estrutura e do orçamento: uma rede por torre com os apartamentos (serviços orçados nos aptos),
    /// uma de estrutura por pavimento (serviços orçados no pavimento/torre) e uma de áreas comuns. Não apaga redes existentes.
    /// </summary>
    public async Task<int> SugerirRedesAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await ExigirAsync(db, "editar as redes");
        if (await db.RedesPlanejamento.AnyAsync(r => r.ObraId == obraId, ct)) throw new RegraDeNegocioException("A obra já tem redes. Edite-as ou exclua antes de sugerir de novo.");
        var locais = await db.Locais.AsNoTracking().Where(l => l.ObraId == obraId).ToListAsync(ct);
        var filhos = locais.ToLookup(l => l.LocalPaiId);
        var celulas = await db.ServicosLocais.AsNoTracking().Where(s => s.ObraId == obraId && s.Custo > 0).Select(s => new { s.ComposicaoId, s.LocalId }).ToListAsync(ct);
        var comps = await db.Composicoes.AsNoTracking().Where(c => c.Ativa).ToDictionaryAsync(c => c.Id, ct);
        var cores = new[] { "#4f52d6", "#64748b", "#0f8a6b", "#d4691e", "#a23ea8" };
        var criadas = 0;
        List<Guid> ServicosEm(IEnumerable<Guid> raizes)
        {
            var sub = raizes.SelectMany(r => MotorOrcamento.Subarvore(r, filhos)).ToHashSet();
            return celulas.Where(c => sub.Contains(c.LocalId) && comps.ContainsKey(c.ComposicaoId)).Select(c => c.ComposicaoId).Distinct()
                .OrderBy(c => comps[c].Grupo ?? "").ThenBy(c => comps[c].Codigo).ToList();
        }
        RedePlanejamento Nova(string nome, TipoRede tipo, IEnumerable<Local> ls, IEnumerable<Guid> servicos, int duracao)
        {
            var r = new RedePlanejamento { ObraId = obraId, Nome = nome, Tipo = tipo, Cor = cores[criadas % cores.Length], Ordem = ++criadas, Pulmao = tipo == TipoRede.PorApartamento ? 1 : 0 };
            var i = 0; foreach (var l in ls) r.Locais.Add(new RedeLocal { RedeId = r.Id, LocalId = l.Id, Ordem = ++i });
            var o = 0; foreach (var s in servicos) r.Pacotes.Add(new RedePacote { RedeId = r.Id, Ordem = ++o, ComposicaoId = s, Nome = comps[s].Descricao, DuracaoPorLocal = duracao, Equipes = 1 });
            return r;
        }
        foreach (var torre in locais.Where(l => l.Tipo == GeradorEmpreendimento.TipoTorre).OrderBy(l => l.Codigo))
        {
            var pavs = filhos[torre.Id].Where(l => l.Tipo == GeradorEmpreendimento.TipoPavimento).OrderBy(l => l.Ordem).ToList();
            var aptos = pavs.SelectMany(p => filhos[p.Id].Where(l => l.Tipo == GeradorEmpreendimento.TipoApartamento).OrderBy(l => l.Ordem)).ToList();
            var servAptos = ServicosEm(aptos.Select(a => a.Id));
            if (aptos.Count > 0 && servAptos.Count > 0) db.RedesPlanejamento.Add(Nova($"{torre.Nome} — apartamentos", TipoRede.PorApartamento, aptos, servAptos, 2));
            var porPav = celulas.Where(c => pavs.Any(p => p.Id == c.LocalId) || c.LocalId == torre.Id).Select(c => c.ComposicaoId).Distinct().Where(comps.ContainsKey).ToList();
            if (pavs.Count > 0 && porPav.Count > 0) db.RedesPlanejamento.Add(Nova($"{torre.Nome} — estrutura e pavimentos", TipoRede.PorPavimento, pavs, porPav, 8));
        }
        var comuns = locais.Where(l => l.Tipo == GeradorEmpreendimento.TipoAreaComum).OrderBy(l => l.Codigo).ToList();
        var servComuns = ServicosEm(comuns.Select(c => c.Id));
        if (comuns.Count > 0 && servComuns.Count > 0) db.RedesPlanejamento.Add(Nova("Área comum", TipoRede.PorLocal, comuns, servComuns, 5));
        if (criadas == 0) throw new RegraDeNegocioException("Não encontrei apartamentos, pavimentos ou áreas comuns com serviço orçado. Gere o orçamento por local primeiro.");
        await db.SaveChangesAsync(ct);
        return criadas;
    }

    // ───────── Aprovação ─────────

    public async Task EnviarParaAprovacaoAsync(Guid versaoId, string motivo, string? descricao, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new ValidacaoException(["Informe o motivo da versão."]);
        await fabrica.EmTransacaoAsync(async db =>
        {
            var v = await EditavelAsync(db, versaoId, ct);
            if (v.Atividades.Count(a => !a.Resumo) == 0) throw new RegraDeNegocioException("A versão não tem atividades.");
            await RecalcularAsync(db, v, ct);
            v.Motivo = motivo.Trim(); v.Descricao = descricao?.Trim(); v.Status = StatusVersao.EmAprovacao; v.MotivoDevolucao = null;
            v.EnviadoEm = relogio.GetUtcNow(); v.EnviadoPor = usuario.Nome;
            var obra = await db.Obras.AsNoTracking().Where(o => o.Id == v.ObraId).Select(o => o.Codigo).FirstAsync(ct);
            await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Cronograma, v.Id, v.ObraId, $"Cronograma {obra} · {v.Codigo} – {v.Motivo}", v.CustoTotal, ct);
            await db.SaveChangesAsync(ct);
        }, ct);
    }

    /// <summary>Reflexo da decisão (chamado pelo motor de aprovação na mesma gravação).</summary>
    public static async Task RefletirDecisaoAsync(IQualyHubDb db, Guid versaoId, bool aprovado, bool voltou, string nome, DateTimeOffset agora, string? justificativa, CancellationToken ct)
    {
        var v = await CarregarAsync(db, versaoId, ct);
        if (voltou) { v.Status = StatusVersao.EmElaboracao; v.MotivoDevolucao = justificativa; return; }
        if (!aprovado) return; // etapa intermediária aprovada: segue em aprovação
        foreach (var antiga in await db.VersoesCronograma.Where(x => x.ObraId == v.ObraId && x.Status == StatusVersao.Vigente && x.Id != v.Id).ToListAsync(ct))
            antiga.Status = StatusVersao.Substituida;
        v.Status = StatusVersao.Vigente; v.AprovadoPor = nome; v.AprovadoEm = agora;
        var folhas = v.Atividades.Where(a => !a.Resumo).ToList();
        if (folhas.All(a => a.InicioBase is null)) // primeira aprovação: congela a linha de base
        {
            foreach (var a in v.Atividades) { a.InicioBase = a.Inicio; a.TerminoBase = a.Termino; }
            v.TerminoBase = v.TerminoPrevisto;
        }
        var cal = await CalendarioAsync(db, v.ObraId, ct);
        var atual = Desembolso.PorMes(folhas, cal);
        var baseM = Desembolso.PorMes(folhas, cal, usarBase: true);
        db.DesembolsosVersao.RemoveRange(await db.DesembolsosVersao.Where(d => d.VersaoId == v.Id).ToListAsync(ct));
        foreach (var mes in atual.Keys.Union(baseM.Keys).Distinct().Order())
            db.DesembolsosVersao.Add(new DesembolsoVersao { VersaoId = v.Id, Mes = mes, Valor = atual.GetValueOrDefault(mes), ValorBase = baseM.GetValueOrDefault(mes) });
    }

    // ───────── MS Project ─────────

    public async Task<int> ImportarMsProjectAsync(Guid versaoId, Stream xml, CancellationToken ct = default)
    {
        var lido = MsProjectXml.Ler(xml);
        await using var db = fabrica.Criar();
        var v = await EditavelAsync(db, versaoId, ct);
        db.VinculosAtividade.RemoveRange(v.Vinculos);
        foreach (var a in Profundidade(v.Atividades.ToList())) db.AtividadesCronograma.Remove(a);
        await db.SaveChangesAsync(ct);
        v.Atividades.Clear(); v.Vinculos.Clear();
        // Feriados do calendário do Project que a obra ainda não tem (o motor de datas usa o calendário da obra).
        if (lido.Feriados is { Count: > 0 } fer)
        {
            var ja = (await db.Feriados.Where(f => f.ObraId == null || f.ObraId == v.ObraId).Select(f => f.Data).ToListAsync(ct)).ToHashSet();
            foreach (var (data, nome) in fer.Where(x => ja.Add(x.Data))) db.Feriados.Add(new Feriado { Data = data, Nome = nome, ObraId = v.ObraId });
            await db.SaveChangesAsync(ct);
        }
        foreach (var a in lido.Atividades) { a.VersaoId = v.Id; v.Atividades.Add(a); db.AtividadesCronograma.Add(a); }
        foreach (var l in lido.Vinculos) { l.VersaoId = v.Id; v.Vinculos.Add(l); db.VinculosAtividade.Add(l); }
        if (lido.Inicio is { } ini) v.InicioProjeto = (await CalendarioAsync(db, v.ObraId, ct)).ProximoUtil(ini);
        await RecalcularAsync(db, v, ct);
        await db.SaveChangesAsync(ct);
        return lido.Atividades.Count(a => !a.Resumo);
    }
}
