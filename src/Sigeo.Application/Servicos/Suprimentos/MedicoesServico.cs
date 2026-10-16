using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Servicos;

/// <summary>Medição de contrato de serviço (T14–T16).</summary>
public sealed class MedicoesServico(IFabricaDb fabrica, AprovacaoServico aprovacoes, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── T14–T16 Medição ─────────

    public async Task<Guid> NovaMedicaoAsync(Guid contratoId, DateOnly inicio, DateOnly fim, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var k = await db.Contratos.Include(x => x.Itens).ThenInclude(i => i.Locais).FirstAsync(x => x.Id == contratoId, ct);
        if (k.Status != StatusContrato.Ativo) throw new RegraDeNegocioException("Contrato não está ativo.");
        if (await db.Medicoes.AnyAsync(m => m.ContratoId == contratoId && m.Status == StatusMedicao.EmElaboracao, ct))
            throw new RegraDeNegocioException("Já existe medição em elaboração para este contrato.");
        var numero = (await db.Medicoes.Where(m => m.ContratoId == contratoId).MaxAsync(m => (int?)m.NumeroMedicao, ct) ?? 0) + 1;
        var m = new Medicao { ContratoId = k.Id, ObraId = k.ObraId, NumeroMedicao = numero, PeriodoInicio = inicio, PeriodoFim = fim };
        // Item contratado por local → uma linha por local (medição por célula serviço × local).
        foreach (var i in k.Itens)
            if (i.Locais.Count == 0) m.Itens.Add(new MedicaoItem { ContratoItemId = i.Id });
            else foreach (var l in i.Locais) m.Itens.Add(new MedicaoItem { ContratoItemId = i.Id, LocalId = l.LocalId });
        db.Medicoes.Add(m);
        await db.SaveChangesAsync(ct);
        return m.Id;
    }

    public async Task SalvarMedicaoAsync(Guid id, IReadOnlyDictionary<Guid, (decimal Qtd, Guid? LocalId, Guid? FvsId)> quantidades,
        decimal? escopo, decimal? material, decimal? servico, decimal? limpeza, decimal? docSeg, DateOnly? reuniao, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var m = await CarregarMedicaoAsync(db, id, ct);
        if (m.Status is not (StatusMedicao.EmElaboracao or StatusMedicao.Reprovada)) throw new RegraDeNegocioException("Medição já enviada.");
        var fvsIds = quantidades.Values.Where(q => q.FvsId != null).Select(q => q.FvsId!.Value).Distinct().ToList();
        var fvsRef = await db.Fvss.AsNoTracking().Where(f => fvsIds.Contains(f.Id) && f.ObraId == m.ObraId)
                               .ToDictionaryAsync(f => f.Id, f => (f.Numero, f.LocalId), ct);
        foreach (var i in m.Itens)
            if (quantidades.TryGetValue(i.Id, out var q))
            {
                if (q.Qtd < 0) throw new RegraDeNegocioException("Quantidade medida não pode ser negativa.");
                i.QtdPeriodo = q.Qtd;
                i.FvsId = q.FvsId is { } fid && fvsRef.ContainsKey(fid) ? fid : null;
                i.FvsRef = i.FvsId is { } f1 ? fvsRef[f1].Numero : null;
                // Linha criada por local (contrato por célula) mantém o local; nas demais, o local vem da tela ou da FVS.
                var porLocal = i.ContratoItem.Locais.Count > 0 && i.LocalId is not null;
                if (!porLocal) i.LocalId = q.LocalId ?? (i.FvsId is { } f2 ? fvsRef[f2].LocalId : null);
            }
        m.NotaEscopo = escopo; m.NotaMaterial = material; m.NotaServico = servico; m.NotaLimpeza = limpeza; m.NotaDocSeg = docSeg; m.DataReuniao = reuniao;
        var anteriores = await db.Medicoes.AsNoTracking().Include(x => x.Itens).Where(x => x.ContratoId == m.ContratoId && x.Id != m.Id).ToListAsync(ct);
        m.Recalcular(anteriores);
        m.CalcularNota(await ParametrosServico.CarregarAsync(db, ct));
        m.AvaliadorUsuarioId = usuario.UsuarioId; m.AvaliadorNome = usuario.Nome; m.AvaliadoEm = relogio.GetUtcNow();
        if (m.Status == StatusMedicao.Reprovada) m.Status = StatusMedicao.EmElaboracao;
        await db.SaveChangesAsync(ct);
    }

    public async Task EnviarMedicaoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var m = await CarregarMedicaoAsync(db, id, ct);
        var fvs = await db.Fvss.AsNoTracking().Where(f => f.ObraId == m.ObraId && f.Status == StatusFvs.Liberada).ToListAsync(ct);
        var locais = await db.Locais.AsNoTracking().Where(l => l.ObraId == m.ObraId).ToDictionaryAsync(l => l.Id, ct);
        var filhos = locais.Values.ToLookup(l => l.LocalPaiId);
        var compIds = m.Contrato.Itens.Where(i => i.ComposicaoId != null).Select(i => i.ComposicaoId!.Value).Distinct().ToList();
        var cels = (await db.ServicosLocais.AsNoTracking().Where(s => s.ObraId == m.ObraId && compIds.Contains(s.ComposicaoId)).Select(s => new { s.ComposicaoId, s.LocalId }).ToListAsync(ct))
                   .ToLookup(c => c.ComposicaoId, c => c.LocalId);
        IReadOnlyCollection<Guid> Exigidos(MedicaoItem i)
        {
            if (i.LocalId is not { } lid || i.ContratoItem.ComposicaoId is not { } cid) return [];
            var sub = MotorOrcamento.Subarvore(lid, filhos);
            return cels[cid].Where(sub.Contains).ToList();
        }
        var tipos = await db.TiposDocumento.AsNoTracking().Where(t => t.AplicaA == AplicaA.Fornecedor).ToListAsync(ct);
        var trava = TravasQualidade.VerificarMedicao(m, fvs, Hoje, locais, Exigidos, tipos);
        m.Bloqueios = trava.Resumo;
        if (!trava.Liberado) { await db.SaveChangesAsync(ct); ValidacaoException.Se(trava.Bloqueios); }
        if (m.PlanoAcaoId is null && RegrasAvaliacao.PlanoPorMedicao(m, m.Contrato.FornecedorId, Hoje, await AvaliacaoFornecedoresServico.NumeroPlanoAsync(db, ct)) is { } plano)
        {
            db.PlanosAcao.Add(plano);
            m.PlanoAcaoId = plano.Id;
        }
        await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Medicao, m.Id, m.ObraId, $"Medição {m.NumeroMedicao:00} – {m.Contrato.Numero}", m.ValorPeriodo, ct, m.Contrato.SpeId);
        m.Status = StatusMedicao.AguardandoAprovacao;
        await db.SaveChangesAsync(ct);
    }

    public static async Task<Medicao> CarregarMedicaoAsync(ISigeoDb db, Guid id, CancellationToken ct) =>
        await db.Medicoes
            .Include(x => x.Itens).ThenInclude(i => i.ContratoItem).ThenInclude(ci => ci.ServicoControlado)
            .Include(x => x.Contrato).ThenInclude(k => k.Itens).ThenInclude(i => i.ServicoControlado)
            .Include(x => x.Contrato).ThenInclude(k => k.Itens).ThenInclude(i => i.Locais)
            .Include(x => x.Contrato).ThenInclude(k => k.Fornecedor).ThenInclude(f => f.Documentos).ThenInclude(d => d.TipoDocumento)
            .Include(x => x.Contrato).ThenInclude(k => k.Fornecedor).ThenInclude(f => f.Exigencias)
            .FirstAsync(x => x.Id == id, ct);
}
