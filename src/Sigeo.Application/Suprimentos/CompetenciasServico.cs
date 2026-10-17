using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>
/// Documentação mensal dos contratos de mão de obra: uma competência por mês trabalhado, com os documentos da empresa
/// e os de cada funcionário que trabalhou na obra; conferência item a item (administrativo de obra, engenheiro ou segurança);
/// adiamento justificado aprovado pelo engenheiro da obra; trava a medição do mês até ficar conferida.
/// </summary>
public sealed class CompetenciasServico(IFabricaDb fabrica, IUsuarioAtual usuario, AcessoServico acesso, TimeProvider relogio)
{
    public const string Rota = "suprimentos/documentacao-mensal";
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    private static bool Aplicavel(TipoDocumentoExigido t, Fornecedor f, bool deFuncionario)
    {
        if (t.Frequencia != FrequenciaDocumento.Mensal || !t.Ativo || t.AplicaA != AplicaA.Fornecedor || t.DeFuncionario != deFuncionario) return false;
        if (!deFuncionario && f.Exigencias.Count > 0) return f.Exigencias.Any(x => x.TipoDocumentoExigidoId == t.Id && x.Obrigatorio);
        return MatrizExigencias.Aplica(t, f.TipoFornecimento | TipoFornecimento.ServicoObra, true) && t.Obrigatorio;
    }

    /// <summary>Cria (ou completa, enquanto não conferida) a competência do mês do contrato. Sem documento mensal aplicável: nada.</summary>
    public static async Task<CompetenciaDocumental?> GarantirAsync(ISigeoDb db, Contrato k, Fornecedor f, IReadOnlyList<TipoDocumentoExigido> tipos, DateOnly mes, CancellationToken ct = default)
    {
        var m = new DateOnly(mes.Year, mes.Month, 1);
        var empresa = tipos.Where(t => Aplicavel(t, f, false)).ToList();
        var porFunc = tipos.Where(t => Aplicavel(t, f, true)).ToList();
        if (empresa.Count == 0 && porFunc.Count == 0) return null;
        var c = await db.CompetenciasDocumentais.Include(x => x.Itens).FirstOrDefaultAsync(x => x.ContratoId == k.Id && x.Mes == m, ct);
        if (c is null)
        {
            c = new CompetenciaDocumental { ObraId = k.ObraId, ContratoId = k.Id, FornecedorId = k.FornecedorId, Mes = m, EmpresaId = k.EmpresaId };
            db.CompetenciasDocumentais.Add(c);
        }
        if (c.Situacao == SituacaoCompetencia.Conferida) return c;
        var fimMes = m.AddMonths(1).AddDays(-1);
        var cols = porFunc.Count == 0 ? [] : await db.Colaboradores.AsNoTracking()
            .Where(x => x.FornecedorId == k.FornecedorId && x.ObraId == k.ObraId && (x.ContratoId == null || x.ContratoId == k.Id) && !x.AguardandoValidacao
                        && x.DataInicio <= fimMes && (x.DataSaida == null || x.DataSaida >= m))
            .OrderBy(x => x.Nome).Select(x => new { x.Id, x.Nome }).ToListAsync(ct);
        void Garante(TipoDocumentoExigido t, Guid? col, string desc)
        {
            if (c.Itens.Any(i => i.TipoDocumentoExigidoId == t.Id && i.ColaboradorId == col)) return;
            var it = new ItemCompetencia { CompetenciaId = c.Id, TipoDocumentoExigidoId = t.Id, ColaboradorId = col, Descricao = desc, PodeAdiar = t.PodeAdiar };
            c.Itens.Add(it); db.ItensCompetencia.Add(it);
        }
        foreach (var t in empresa) Garante(t, null, t.Nome);
        foreach (var col in cols) foreach (var t in porFunc) Garante(t, col.Id, $"{t.Nome} · {col.Nome}");
        if (c.Situacao == SituacaoCompetencia.Enviada && c.Itens.Any(i => i.Situacao == SituacaoItemCompetencia.Pendente)) c.Situacao = SituacaoCompetencia.Aberta;
        return c;
    }

    /// <summary>Competências do contrato (do início até o mês anterior; e o mês pedido, para a medição em andamento).</summary>
    public static async Task<List<CompetenciaDocumental>> DoContratoAsync(ISigeoDb db, Guid contratoId, DateOnly hoje, DateOnly? mesExtra = null, CancellationToken ct = default)
    {
        var k = await db.Contratos.AsNoTracking().FirstAsync(x => x.Id == contratoId, ct);
        var f = await db.Fornecedores.AsNoTracking().Include(x => x.Exigencias).FirstAsync(x => x.Id == k.FornecedorId, ct);
        var tipos = await db.TiposDocumento.AsNoTracking().Where(t => t.AplicaA == AplicaA.Fornecedor && t.Frequencia == FrequenciaDocumento.Mensal && t.Ativo).ToListAsync(ct);
        var meses = RegrasCompetencia.Meses(k.VigenciaInicio, k.Status is StatusContrato.Encerrado or StatusContrato.Rescindido ? k.VigenciaFim : null, hoje).ToList();
        if (mesExtra is { } me && !meses.Contains(new DateOnly(me.Year, me.Month, 1))) meses.Add(new DateOnly(me.Year, me.Month, 1));
        var r = new List<CompetenciaDocumental>();
        foreach (var m in meses.TakeLast(24))
            if (await GarantirAsync(db, k, f, tipos, m, ct) is { } c) r.Add(c);
        return r;
    }

    /// <summary>Contratos de mão de obra em andamento: competências garantidas em lote (fila de conferência e portal).</summary>
    public static async Task<int> GarantirAtivosAsync(ISigeoDb db, DateOnly hoje, Guid? fornecedorId = null, CancellationToken ct = default)
    {
        var ks = await db.Contratos.AsNoTracking().Where(k => k.Status == StatusContrato.Ativo && (fornecedorId == null || k.FornecedorId == fornecedorId)).Select(k => k.Id).ToListAsync(ct);
        var n = 0;
        foreach (var id in ks) n += (await DoContratoAsync(db, id, hoje, null, ct)).Count;
        return n;
    }

    public async Task<List<LinhaCompetencia>> FilaAsync(bool soPendentes, CancellationToken ct = default)
    {
        await using (var w = fabrica.Criar()) { await GarantirAtivosAsync(w, Hoje, null, ct); await w.SaveChangesAsync(ct); }
        await using var db = fabrica.CriarLeitura();
        var q = db.CompetenciasDocumentais.AsNoTracking().Include(c => c.Itens).AsQueryable();
        if (soPendentes) q = q.Where(c => c.Situacao != SituacaoCompetencia.Conferida || c.Itens.Any(i => i.Situacao == SituacaoItemCompetencia.AdiamentoSolicitado));
        var l = await q.OrderBy(c => c.Mes).ToListAsync(ct);
        return await LinhasAsync(db, l, ct);
    }

    private async Task<List<LinhaCompetencia>> LinhasAsync(ISigeoDb db, List<CompetenciaDocumental> l, CancellationToken ct)
    {
        var p = await ParametrosServico.CarregarAsync(db, ct);
        var dia = p.Int(Parametros.DiaLimiteCompetencia);
        var kIds = l.Select(c => c.ContratoId).Distinct().ToList();
        var ks = await db.Contratos.AsNoTracking().Where(k => kIds.Contains(k.Id)).Select(k => new { k.Id, k.Numero, k.FornecedorId, k.ObraId }).ToDictionaryAsync(k => k.Id, ct);
        var fIds = ks.Values.Select(k => k.FornecedorId).Distinct().ToList();
        var fs = await db.Fornecedores.AsNoTracking().Where(f => fIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.NomeFantasia ?? f.RazaoSocial, ct);
        var obras = await db.Obras.AsNoTracking().ToDictionaryAsync(o => o.Id, o => o.Nome, ct);
        var meds = await db.Medicoes.AsNoTracking().Where(m => kIds.Contains(m.ContratoId)).Select(m => new { m.ContratoId, m.NumeroMedicao, m.PeriodoFim, m.Status }).ToListAsync(ct);
        return l.Select(c =>
        {
            var k = ks[c.ContratoId];
            var med = meds.Where(m => m.ContratoId == c.ContratoId && m.PeriodoFim.Year == c.Mes.Year && m.PeriodoFim.Month == c.Mes.Month).OrderByDescending(m => m.NumeroMedicao).FirstOrDefault();
            return new LinhaCompetencia(c.Id, c.ContratoId, k.Numero, fs.GetValueOrDefault(k.FornecedorId, ""), obras.GetValueOrDefault(k.ObraId, ""), c.Mes, c.Situacao, c.Atrasada(Hoje, dia),
                c.Entregues, c.Itens.Count, c.Itens.Count(i => i.Situacao == SituacaoItemCompetencia.AdiamentoSolicitado),
                med is null ? null : $"Medição {med.NumeroMedicao:00} · {Rotulo(med.Status)}");
        }).ToList();
    }

    private static string Rotulo(StatusMedicao s) => s switch
    {
        StatusMedicao.EmElaboracao => "em elaboração", StatusMedicao.AguardandoAprovacao => "aguardando aprovação", StatusMedicao.Aprovada => "aprovada",
        StatusMedicao.Reprovada => "reprovada", StatusMedicao.NfRecebida => "NF recebida", StatusMedicao.Paga => "paga", _ => s.ToString()
    };

    public async Task<CompetenciaDetalhe> DetalheAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var c = await db.CompetenciasDocumentais.AsNoTracking().Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
        return await DetalheAsync(db, c, Hoje, ct);
    }

    public static async Task<CompetenciaDetalhe> DetalheAsync(ISigeoDb db, CompetenciaDocumental c, DateOnly hoje, CancellationToken ct)
    {
        var p = await ParametrosServico.CarregarAsync(db, ct);
        var dia = p.Int(Parametros.DiaLimiteCompetencia);
        var k = await db.Contratos.AsNoTracking().Where(x => x.Id == c.ContratoId).Select(x => new { x.Numero, x.FornecedorId, x.ObraId }).FirstAsync(ct);
        var forn = await db.Fornecedores.AsNoTracking().Where(f => f.Id == k.FornecedorId).Select(f => f.NomeFantasia ?? f.RazaoSocial).FirstAsync(ct);
        var obra = await db.Obras.AsNoTracking().Where(o => o.Id == k.ObraId).Select(o => o.Nome).FirstOrDefaultAsync(ct) ?? "";
        var colIds = c.Itens.Where(i => i.ColaboradorId != null).Select(i => i.ColaboradorId!.Value).Distinct().ToList();
        var cols = await db.Colaboradores.AsNoTracking().Where(x => colIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => (x.Nome, x.DataSaida), ct);
        var fimMes = c.Mes.AddMonths(1).AddDays(-1);
        var dias = (await db.Apropriacoes.AsNoTracking().Where(a => a.ColaboradorId != null && colIds.Contains(a.ColaboradorId.Value) && a.Data >= c.Mes && a.Data <= fimMes)
            .Select(a => new { a.ColaboradorId, a.Data }).ToListAsync(ct)).GroupBy(a => a.ColaboradorId!.Value).ToDictionary(g => g.Key, g => g.Select(x => x.Data).Distinct().Count());
        var itens = c.Itens.OrderBy(i => i.ColaboradorId is null ? 0 : 1).ThenBy(i => i.Descricao).Select(i =>
        {
            (string Nome, DateOnly? Saida)? col = i.ColaboradorId is { } cid && cols.TryGetValue(cid, out var x) ? x : null;
            return new ItemCompetenciaDto(i, col?.Nome, i.ColaboradorId is { } cd && dias.TryGetValue(cd, out var d) ? d : null,
                col?.Saida is { } s && s <= fimMes ? $"desligado em {s:dd/MM}" : null);
        }).ToList();
        var hist = await db.CompetenciasDocumentais.AsNoTracking().Include(x => x.Itens).Where(x => x.ContratoId == c.ContratoId).OrderByDescending(x => x.Mes).ToListAsync(ct);
        var med = await db.Medicoes.AsNoTracking().Where(m => m.ContratoId == c.ContratoId && m.PeriodoFim >= c.Mes && m.PeriodoFim <= fimMes).OrderByDescending(m => m.NumeroMedicao)
            .Select(m => new { m.NumeroMedicao, m.Status, m.ValorPeriodo }).FirstOrDefaultAsync(ct);
        var linhasHist = hist.Select(h => new LinhaCompetencia(h.Id, h.ContratoId, k.Numero, forn, obra, h.Mes, h.Situacao, h.Atrasada(hoje, dia), h.Entregues, h.Itens.Count,
            h.Itens.Count(i => i.Situacao == SituacaoItemCompetencia.AdiamentoSolicitado), null)).ToList();
        return new CompetenciaDetalhe(c, k.Numero, forn, obra, itens, linhasHist,
            med is null ? null : $"Medição {med.NumeroMedicao:00} · {Rotulo(med.Status)} · {med.ValorPeriodo:C2}", c.Atrasada(hoje, dia), c.Limite(dia));
    }

    // ───────── Conferência (construtora) ─────────

    private async Task<(ISigeoDb Db, CompetenciaDocumental C, ItemCompetencia I)> ItemAsync(Guid itemId, CancellationToken ct)
    {
        await acesso.ExigirAsync(Rota, Contratos.AcaoTela.Aprovar, ct);
        var db = fabrica.Criar();
        var i = await db.ItensCompetencia.FirstOrDefaultAsync(x => x.Id == itemId, ct) ?? throw new RegraDeNegocioException("Item não encontrado.");
        var c = await db.CompetenciasDocumentais.Include(x => x.Itens).FirstAsync(x => x.Id == i.CompetenciaId, ct);
        return (db, c, i);
    }

    public async Task ConferirItemAsync(Guid itemId, bool aprovado, string? motivo, CancellationToken ct = default)
    {
        var (db, c, i) = await ItemAsync(itemId, ct);
        await using var _ = db;
        if (c.Situacao == SituacaoCompetencia.Conferida) throw new RegraDeNegocioException("Competência conferida: reabra para alterar.");
        i.Conferir(aprovado, motivo, usuario.Nome, relogio.GetUtcNow());
        if (c.Situacao is SituacaoCompetencia.Aberta or SituacaoCompetencia.Enviada) c.Situacao = SituacaoCompetencia.EmConferencia;
        if (i.FornecedorDocumentoId is { } did && await db.FornecedorDocumentos.FirstOrDefaultAsync(d => d.Id == did, ct) is { } doc)
        {
            doc.Status = aprovado ? StatusDocumento.Valido : StatusDocumento.Reprovado; doc.MotivoRecusa = aprovado ? null : motivo;
            doc.ValidadoPorUsuarioId = usuario.UsuarioId; doc.ValidadoEm = relogio.GetUtcNow();
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Adiamento: só o engenheiro da obra (perfil Engenharia) ou o administrador decide.</summary>
    public async Task DecidirAdiamentoAsync(Guid itemId, bool aprovado, string? motivo, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var perfil = await db.Usuarios.AsNoTracking().Where(u => u.Id == usuario.UsuarioId).Select(u => (PerfilUsuario?)u.Perfil).FirstOrDefaultAsync(ct);
        if (usuario.UsuarioId is not null && perfil is not (PerfilUsuario.Engenharia or PerfilUsuario.Administrador))
            throw new RegraDeNegocioException("Só o engenheiro da obra aprova adiamento de documento.");
        var i = await db.ItensCompetencia.FirstAsync(x => x.Id == itemId, ct);
        var c = await db.CompetenciasDocumentais.FirstAsync(x => x.Id == i.CompetenciaId, ct); // filtro de obra: o engenheiro precisa ter acesso à obra
        i.DecidirAdiamento(aprovado, motivo, usuario.Nome, relogio.GetUtcNow());
        if (!aprovado && c.Situacao == SituacaoCompetencia.Enviada) c.Situacao = SituacaoCompetencia.ComPendencia;
        await db.SaveChangesAsync(ct);
    }

    public async Task ConcluirAsync(Guid id, CancellationToken ct = default)
    {
        await acesso.ExigirAsync(Rota, Contratos.AcaoTela.Aprovar, ct);
        await using var db = fabrica.Criar();
        var c = await db.CompetenciasDocumentais.Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
        c.Concluir(usuario.Nome, relogio.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    public async Task DevolverAsync(Guid id, string? motivo, CancellationToken ct = default)
    {
        await acesso.ExigirAsync(Rota, Contratos.AcaoTela.Aprovar, ct);
        await using var db = fabrica.Criar();
        var c = await db.CompetenciasDocumentais.Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
        c.Devolver(motivo);
        await db.SaveChangesAsync(ct);
    }

    public async Task ReabrirAsync(Guid id, string? motivo, CancellationToken ct = default)
    {
        await acesso.ExigirAsync(Rota, Contratos.AcaoTela.Aprovar, ct);
        await using var db = fabrica.Criar();
        var c = await db.CompetenciasDocumentais.Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
        if (c.Situacao != SituacaoCompetencia.Conferida) throw new RegraDeNegocioException("Só competência conferida pode ser reaberta.");
        c.Reabrir(motivo);
        await db.SaveChangesAsync(ct);
    }
}
