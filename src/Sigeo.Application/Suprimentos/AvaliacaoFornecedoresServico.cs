using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Suprimentos;

/// <summary>Qualificação (T08–T10), avaliação semestral (T17) e numeração dos planos de ação de fornecedor (usada também por recebimento e medição).</summary>
public sealed class AvaliacaoFornecedoresServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── T08–T10 Fornecedor / qualificação ─────────

    public async Task SalvarQualificacaoAsync(FornecedorQualificacao q, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        ValidacaoException.Se(q.Validar());
        q.CalcularValidade(await ParametrosServico.CarregarAsync(db, ct));
        q.ResponsavelUsuarioId ??= usuario.UsuarioId;
        if (await db.Qualificacoes.AnyAsync(x => x.Id == q.Id, ct)) db.Update(q); else db.Qualificacoes.Add(q);
        await db.SaveChangesAsync(ct);
        await RecalcularFornecedoresAsync([q.FornecedorId], ct);
    }

    /// <summary>Recalcula status de qualificação e nota média (6 meses) dos fornecedores.</summary>
    public async Task RecalcularFornecedoresAsync(IReadOnlyCollection<Guid>? ids = null, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var q = db.Fornecedores.Include(f => f.Qualificacoes).AsQueryable();
        if (ids is not null) q = q.Where(f => ids.Contains(f.Id));
        var fs = await q.ToListAsync(ct);
        var desde = Hoje.AddMonths(-6);
        var fids = fs.Select(f => f.Id).ToList();
        var notasMed = await db.Medicoes.AsNoTracking().Where(m => m.NotaFinal != null && m.PeriodoFim >= desde && fids.Contains(m.Contrato.FornecedorId))
            .Select(m => new { m.Contrato.FornecedorId, Nota = m.NotaFinal!.Value }).ToListAsync(ct);
        var desdeDt = new DateTimeOffset(desde.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var recs = await db.Recebimentos.AsNoTracking().Where(r => r.DataRecebimento >= desdeDt && fids.Contains(r.FornecedorId)).ToListAsync(ct);
        foreach (var f in fs)
        {
            f.StatusQualificacao = f.CalcularStatus(Hoje);
            var notas = notasMed.Where(n => n.FornecedorId == f.Id).Select(n => n.Nota)
                .Concat(recs.Where(r => r.FornecedorId == f.Id && r.NotaEntrega is not null).Select(r => r.NotaEntrega!.Value)).ToList();
            f.NotaMedia = notas.Count == 0 ? null : Math.Round(notas.Average(), 2);
        }
        await db.SaveChangesAsync(ct);
        // Qualificação atualizada vale para as cotações em andamento.
        if (await SincronizacaoCadastros.QualificacaoNasCotacoesAsync(db, fids, Hoje, ct) > 0) await db.SaveChangesAsync(ct);
    }

    // ───────── T17 Avaliação semestral ─────────

    public async Task<int> ConsolidarSemestreAsync(string periodo, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var (ini, fim) = AvaliacaoSemestral.Intervalo(periodo);
        var iniDt = new DateTimeOffset(ini.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var fimDt = new DateTimeOffset(fim.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        var recs = await db.Recebimentos.AsNoTracking().Where(r => r.DataRecebimento >= iniDt && r.DataRecebimento <= fimDt).ToListAsync(ct);
        var meds = await db.Medicoes.AsNoTracking().Include(m => m.Contrato).Where(m => m.PeriodoFim >= ini && m.PeriodoFim <= fim).ToListAsync(ct);
        var planos = await db.PlanosAcao.AsNoTracking().ToListAsync(ct);
        var fornecedores = recs.Select(r => r.FornecedorId).Concat(meds.Select(m => m.Contrato.FornecedorId)).Distinct().ToList();
        var existentes = await db.AvaliacoesSemestrais.Where(a => a.Periodo == periodo).ToListAsync(ct);
        foreach (var fid in fornecedores)
        {
            var a = existentes.FirstOrDefault(x => x.FornecedorId == fid);
            if (a is null) { a = new AvaliacaoSemestral { Periodo = periodo, FornecedorId = fid }; db.AvaliacoesSemestrais.Add(a); }
            if (a.Decisao is null)
            {
                a.Consolidar(recs, meds, planos);
                a.QtdRnc = await db.Rncs.CountAsync(r => r.FornecedorId == fid && r.CriadoEm >= iniDt && r.CriadoEm <= fimDt, ct);
            }
        }
        await db.SaveChangesAsync(ct);
        return fornecedores.Count;
    }

    public async Task DecidirSemestreAsync(AvaliacaoSemestral a, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        ValidacaoException.Se(a.ValidarDecisao());
        db.Update(a);
        var f = await db.Fornecedores.FirstAsync(x => x.Id == a.FornecedorId, ct);
        switch (a.Decisao)
        {
            case DecisaoSemestral.Desqualificar:
                f.StatusQualificacao = StatusQualificacao.Desqualificado; f.Ativo = false; break;
            case DecisaoSemestral.ManterComPlanoAcao:
                db.PlanosAcao.Add(new PlanoAcaoFornecedor
                {
                    Numero = await NumeroPlanoAsync(db, ct), FornecedorId = f.Id, Origem = OrigemPlanoAcao.AvaliacaoSemestral, OrigemId = a.Id,
                    TipoPlano = TipoPlanoAcao.Completo, Problema = $"Avaliação {a.Periodo}: nota média {a.NotaMedia:0.00}, FVM aprovadas {a.PercFvmAprovadas:0.#}%.",
                    Acao = "A definir com o fornecedor.", Responsavel = "Suprimentos", Prazo = Hoje.AddDays(30)
                });
                break;
        }
        await db.SaveChangesAsync(ct);
        if (await SincronizacaoCadastros.QualificacaoNasCotacoesAsync(db, [f.Id], Hoje, ct) > 0) await db.SaveChangesAsync(ct);
    }

    public static async Task<string> NumeroPlanoAsync(ISigeoDb db, CancellationToken ct) => $"PA-{await Numerador.ProximoAsync(db, "PlanoAcao", ct):0000}";
}
