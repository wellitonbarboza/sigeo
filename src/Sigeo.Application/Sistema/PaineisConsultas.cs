using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Abstracoes;
using Sigeo.Contratos;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Regras;

namespace Sigeo.Application.Sistema;

/// <summary>
/// Consultas dos painéis e do mapa da torre, servidas pela API às telas que rodam no navegador (WebAssembly).
/// Só leitura e já agregadas: o navegador recebe números e listas curtas, não as tabelas.
/// A empresa vem do filtro global do <see cref="IFabricaDb"/>; a obra é conferida contra as obras do usuário.
/// </summary>
public sealed class PaineisConsultas(IFabricaDb fabrica, IUsuarioAtual usuario, SstServico sst, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);
    private static readonly string[] Meses = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];

    /// <summary>Obra fora das permitidas ao usuário é recusada (a tela só oferece as permitidas; isso cobre chamada direta à API).</summary>
    public void ConferirObra(Guid? obra)
    {
        if (obra is { } o && !usuario.AcessoTodasObras && !usuario.ObrasPermitidas.Contains(o))
            throw new RegraDeNegocioException("Você não tem acesso a esta obra.");
    }

    /// <summary>Obras ativas que o usuário pode ver (mesma regra do seletor de obra das telas do servidor).</summary>
    public async Task<List<Opcao>> ObrasAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var q = db.Obras.AsNoTracking().Where(o => o.Ativa);
        if (!usuario.AcessoTodasObras)
        {
            var permitidas = usuario.ObrasPermitidas.ToList();
            q = q.Where(o => permitidas.Contains(o.Id));
        }
        return await q.OrderBy(o => o.Nome).Select(o => new Opcao(o.Id, o.Nome, o.Codigo)).ToListAsync(ct);
    }

    public async Task<PainelIndicadoresDto> IndicadoresAsync(int meses, Guid? obra, CancellationToken ct = default)
    {
        ConferirObra(obra);
        meses = Math.Clamp(meses, 1, 24);
        await using var db = fabrica.CriarLeitura();
        var p = await ParametrosServico.CarregarAsync(db, ct: ct);
        var hoje = Hoje;
        var ini = hoje.AddMonths(-meses);
        var iniDt = new DateTimeOffset(ini.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // Só as colunas usadas: antes cada painel trazia as entidades inteiras com itens.
        var peds = await db.Pedidos.AsNoTracking().Where(x => x.CriadoEm >= iniDt && x.RequisicaoId != null && (obra == null || x.ObraId == obra))
            .Select(x => new { RequisicaoId = x.RequisicaoId!.Value, x.CriadoEm }).ToListAsync(ct);
        var reqIds = peds.Select(x => x.RequisicaoId).Distinct().ToList();
        var reqs = await db.Requisicoes.AsNoTracking().Where(r => reqIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.CriadoEm, ct);
        var leads = peds.Where(x => reqs.ContainsKey(x.RequisicaoId)).Select(x => (decimal)(x.CriadoEm - reqs[x.RequisicaoId]).TotalDays).ToList();
        decimal? leadTime = leads.Count == 0 ? null : Math.Round(leads.Average(), 1);

        var limite = p[Parametros.LimiteUmOrcamento];
        var acima = await db.Cotacoes.AsNoTracking().Where(c => c.CriadoEm >= iniDt && c.Status >= StatusCotacao.Aprovado && c.ValorEstimado > limite && (obra == null || c.ObraId == obra))
            .Select(c => new { c.PropostasRecebidas, c.PropostasExigidas }).ToListAsync(ct);
        decimal? pctPropostas = acima.Count == 0 ? null : Math.Round(100m * acima.Count(c => c.PropostasRecebidas >= c.PropostasExigidas) / acima.Count, 0);

        // FvmAprovada é calculada (3 critérios): a consulta traduz a mesma regra em colunas, sem trazer os itens.
        const ResultadoCriterio na = ResultadoCriterio.NaoAvaliado, ap = ResultadoCriterio.Aprovado;
        var aval = await db.Recebimentos.AsNoTracking()
            .Where(r => r.DataRecebimento >= iniDt && r.FvmPrazo != na && r.FvmQualidade != na && r.FvmAtendimento != na && (obra == null || r.ObraId == obra))
            .Select(r => new { r.DataRecebimento, Ok = r.FvmPrazo == ap && r.FvmQualidade == ap && r.FvmAtendimento == ap, r.FornecedorId }).ToListAsync(ct);
        decimal? pctFvm = aval.Count == 0 ? null : Math.Round(100m * aval.Count(r => r.Ok) / aval.Count, 0);

        var meds = await db.Medicoes.AsNoTracking().Where(m => m.NotaFinal != null && m.PeriodoFim >= ini && (obra == null || m.ObraId == obra))
            .Select(m => new { Nota = m.NotaFinal!.Value, m.PeriodoFim, m.Contrato.FornecedorId }).ToListAsync(ct);
        decimal? notaMedia = meds.Count == 0 ? null : Math.Round(meds.Average(m => m.Nota), 2);

        var fvs = await db.Fvss.AsNoTracking().Include(f => f.Itens).Where(f => f.DataInspecao >= iniDt && f.Status != StatusFvs.EmInspecao && (obra == null || f.ObraId == obra)).ToListAsync(ct);
        decimal? pctFvs1a = fvs.Count == 0 ? null : Math.Round(100m * fvs.Count(f => f.Aprovada1aInspecao) / fvs.Count, 0);

        var fvmMes = new List<ValorMes>();
        var notaMes = new List<ValorMes>();
        for (var i = Math.Min(meses, 6) - 1; i >= 0; i--)
        {
            var m = hoje.AddMonths(-i);
            var rm = aval.Where(r => r.DataRecebimento.Month == m.Month && r.DataRecebimento.Year == m.Year).ToList();
            var mm = meds.Where(x => x.PeriodoFim.Month == m.Month && x.PeriodoFim.Year == m.Year).ToList();
            fvmMes.Add(new(Meses[m.Month - 1], rm.Count == 0 ? null : Math.Round(100m * rm.Count(r => r.Ok) / rm.Count, 1)));
            notaMes.Add(new(Meses[m.Month - 1], mm.Count == 0 ? null : Math.Round(mm.Average(x => x.Nota), 2)));
        }

        var planos = await db.PlanosAcao.AsNoTracking().Where(x => x.Status != StatusPlanoAcao.Concluida)
            .Select(x => new { x.FornecedorId, x.Numero, x.Prazo }).ToListAsync(ct);
        var notaPlano = p[Parametros.NotaPlanoPontual];
        var criticos = meds.GroupBy(m => m.FornecedorId).Select(g => (Id: g.Key, Indicador: "Nota média", Valor: g.Average(x => x.Nota), Texto: $"{g.Average(x => x.Nota):0.00}"))
            .Where(x => x.Valor < notaPlano)
            .Concat(aval.GroupBy(r => r.FornecedorId).Select(g => (Id: g.Key, Indicador: "FVM aprovadas", Valor: 100m * g.Count(r => r.Ok) / g.Count(), Texto: "")).Where(x => x.Valor < 95)
                .Select(x => x with { Texto = $"{x.Valor:0}%" }))
            .Take(8).ToList();
        var fids = criticos.Select(c => c.Id).Distinct().ToList();
        var nomes = await db.Fornecedores.AsNoTracking().Where(f => fids.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.NomeExibicao, ct);
        var piores = criticos.Select(c => new FornecedorCritico(nomes.GetValueOrDefault(c.Id, "?"), c.Indicador, c.Texto,
            planos.FirstOrDefault(pl => pl.FornecedorId == c.Id)?.Numero, planos.Any(pl => pl.FornecedorId == c.Id && pl.Prazo < hoje))).ToList();

        var docs = await db.Documentos.AsNoTracking().Where(d => d.Status != StatusDocumentoSgq.Cancelado)
            .Select(d => new { Vigente = d.RevisaoVigenteId != null, Vencida = d.ProximaAnalise != null && d.ProximaAnalise < hoje }).ToListAsync(ct);
        var analiseVencida = docs.Count(d => d.Vencida);
        var vig = docs.Count(d => d.Vigente);
        decimal? pctDocs = vig == 0 ? null : Math.Round(100m * (vig - docs.Count(d => d.Vigente && d.Vencida)) / vig, 1);
        var copias = await db.Copias.AsNoTracking().CountAsync(c => c.Status == StatusCopia.RecolhimentoPendente, ct);
        var leituras = await db.Ciencias.AsNoTracking().CountAsync(c => c.LidoEm == null && c.Prazo < hoje, ct);
        var docForn = await db.FornecedorDocumentos.AsNoTracking().CountAsync(d => d.Bloqueia && d.DataValidade < hoje && d.Status != StatusDocumento.Reprovado, ct);
        List<ItemContagem> documentos =
        [
            new("Documentos vigentes", vig), new("Análise crítica vencida", analiseVencida), new("Cópias obsoletas não recolhidas", copias),
            new("Leituras obrigatórias em atraso", leituras), new("Documentos de fornecedor vencidos que bloqueiam", docForn)
        ];

        var rncs = await db.Rncs.AsNoTracking().Where(r => obra == null || r.ObraId == obra).Select(r => new { r.Status, r.Prazo }).ToListAsync(ct);
        var acsEficacia = await db.AcoesCorretivas.AsNoTracking().CountAsync(a => a.Status == StatusAcaoCorretiva.AguardandoEficacia, ct);
        var eq = (await db.Equipamentos.AsNoTracking().ToListAsync(ct)).Count(e => e.Status(hoje, p.Int(Parametros.AlertaCalibracaoDias)) is StatusEquipamento.Bloqueado or StatusEquipamento.VenceEmBreve);
        List<ItemContagem> qob =
        [
            new("RNC abertas", rncs.Count(r => r.Status is StatusRnc.Aberta or StatusRnc.EmTratamento)),
            new("RNC vencidas", rncs.Count(r => r.Status is StatusRnc.Aberta or StatusRnc.EmTratamento && r.Prazo < hoje)),
            new("Ações corretivas aguardando eficácia", acsEficacia),
            new("Equipamentos bloqueados / a vencer", eq),
            new("FVS reprovadas abertas", fvs.Count(f => f.Status == StatusFvs.Reprovada))
        ];

        return new(leadTime, pctPropostas, pctFvm, notaMedia, pctFvs1a, p[Parametros.MetaFvs1aInspecaoPerc], pctDocs, analiseVencida,
            fvmMes, notaMes, piores, documentos, qob);
    }

    public async Task<PainelSstDto> SstAsync(Guid? obra, CancellationToken ct = default)
    {
        ConferirObra(obra);
        var hoje = Hoje;
        var agora = relogio.GetUtcNow();
        var calc = await sst.RecalcularAsync(null, ct);
        await using var db = fabrica.CriarLeitura();
        var p = await ParametrosServico.CarregarAsync(db, ct);
        var cols = await db.Colaboradores.AsNoTracking().Where(c => (c.DataSaida == null || c.DataSaida > hoje) && (obra == null || c.ObraId == obra))
            .Select(c => new { c.Id, c.Situacao, c.FornecedorId }).ToListAsync(ct);
        var fids = cols.Where(c => c.FornecedorId != null).Select(c => c.FornecedorId!.Value).Distinct().ToList();
        var forn = await db.Fornecedores.AsNoTracking().Where(f => fids.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.NomeExibicao, ct);
        var porEmpresa = cols.Where(c => c.Situacao != SituacaoColaborador.Apto)
            .GroupBy(c => c.FornecedorId is { } f ? forn.GetValueOrDefault(f, "?") : "Própria")
            .Select(g => new ItemContagem(g.Key, g.Sum(c => calc.TryGetValue(c.Id, out var s) ? s.Detalhe.Count(d => !d.Ok) : 1)))
            .OrderByDescending(x => x.Quantidade).Take(8).ToList();
        var vencendo = cols.SelectMany(c => calc.TryGetValue(c.Id, out var s) ? s.Detalhe.Where(d => d.Ok && d.Validade >= hoje && d.Validade <= hoje.AddDays(30)).Select(d => d.Exigencia) : [])
            .GroupBy(x => x).Select(g => new ItemContagem(g.Key, g.Count())).OrderByDescending(x => x.Quantidade).ToList();
        var epiVencendo = await db.EntregasEpi.AsNoTracking().CountAsync(e => e.TrocaPrevista >= hoje && e.TrocaPrevista <= hoje.AddDays(30), ct);
        if (epiVencendo > 0) vencendo.Add(new("Troca de EPI", epiVencendo));

        var acidentes = await db.Ocorrencias.AsNoTracking().Where(o => o.Tipo == TipoOcorrencia.AcidenteComAfastamento && (obra == null || o.ObraId == obra))
            .Select(o => o.DataHora).ToListAsync(ct);
        var umAno = agora.AddYears(-1);
        int? diasSem = acidentes.Count == 0 ? null : (int)(agora - acidentes.Max()).TotalDays;
        var desde90 = agora.AddDays(-90);
        var insp = await db.InspecoesSeguranca.AsNoTracking().Where(i => i.Conformidade != null && (obra == null || i.ObraId == obra) && i.Data >= desde90)
            .Select(i => i.Conformidade!.Value).ToListAsync(ct);

        return new(cols.Count, cols.Count(c => c.Situacao == SituacaoColaborador.Apto), p[Parametros.MetaAptosPerc],
            acidentes.Count(d => d >= umAno), diasSem, cols.Count * 176m * 12,
            insp.Count == 0 ? null : Math.Round(insp.Average(), 0), p[Parametros.MetaConformidadeInspecaoPerc], porEmpresa, vencendo);
    }

    /// <summary>Torres da obra (para o seletor do mapa).</summary>
    public async Task<List<Opcao>> TorresAsync(Guid obra, CancellationToken ct = default)
    {
        ConferirObra(obra);
        await using var db = fabrica.CriarLeitura();
        return await db.Locais.AsNoTracking().Where(l => l.ObraId == obra && l.Tipo == GeradorEmpreendimento.TipoTorre).OrderBy(l => l.Codigo)
            .Select(l => new Opcao(l.Id, l.Codigo + " – " + l.Nome, l.Codigo)).ToListAsync(ct);
    }

    public async Task<MapaTorreDto?> MapaTorreAsync(Guid obra, Guid torre, CancellationToken ct = default)
    {
        ConferirObra(obra);
        await using var db = fabrica.CriarLeitura();
        var locais = await db.Locais.AsNoTracking().Where(l => l.ObraId == obra).ToListAsync(ct);
        var porId = locais.ToDictionary(l => l.Id);
        if (!porId.TryGetValue(torre, out var t)) return null;
        var filhos = locais.ToLookup(l => l.LocalPaiId);

        // Subárvore da torre + ancestrais (FVS aberta num nível acima vale para baixo) + áreas comuns do condomínio.
        var envia = MotorOrcamento.Subarvore(torre, filhos);
        var comunsCond = filhos[t.LocalPaiId].Where(l => l.Tipo == GeradorEmpreendimento.TipoAreaComum).Select(l => l.Id).ToList();
        var celulasEm = new HashSet<Guid>(envia);
        foreach (var c in comunsCond) { envia.Add(c); celulasEm.Add(c); }
        for (var a = t.LocalPaiId; a is { } id && porId.TryGetValue(id, out var pai); a = pai.LocalPaiId) envia.Add(id);

        var comps = await db.Composicoes.AsNoTracking().Where(c => c.ServicoControladoId != null).ToDictionaryAsync(c => c.Id, c => c.ServicoControladoId!.Value, ct);
        var cels = await db.ServicosLocais.AsNoTracking().Where(s => s.ObraId == obra).Select(s => new { s.ComposicaoId, s.LocalId }).ToListAsync(ct);
        var celulas = cels.Where(c => celulasEm.Contains(c.LocalId) && comps.ContainsKey(c.ComposicaoId))
            .Select(c => new CelulaMapa(comps[c.ComposicaoId], c.LocalId)).Distinct().ToList();
        var ids = celulas.Select(c => c.ServicoId).Distinct().ToList();
        var servicos = await db.ServicosControlados.AsNoTracking().Where(s => ids.Contains(s.Id))
            .Select(s => new { s.Id, s.Codigo, s.Sigla, s.Nome }).ToListAsync(ct);

        // Só a FVS mais recente por (serviço, local), e só dos locais enviados.
        var fvs = await db.Fvss.AsNoTracking().Where(f => f.ObraId == obra && f.Status != StatusFvs.Cancelada && f.ServicoControladoId != null)
            .OrderByDescending(f => f.DataInspecao).Select(f => new { f.Id, f.Numero, f.Status, Servico = f.ServicoControladoId!.Value, f.LocalId }).ToListAsync(ct);
        var ultimas = new Dictionary<(Guid, Guid), FvsMapa>();
        foreach (var f in fvs.Where(f => envia.Contains(f.LocalId)))
            ultimas.TryAdd((f.Servico, f.LocalId), new FvsMapa(f.Id, f.Numero, f.Status, f.Servico, f.LocalId));

        return new MapaTorreDto(torre,
            locais.Where(l => envia.Contains(l.Id)).Select(l => new LocalMapa(l.Id, l.LocalPaiId, l.Tipo, l.Nome, l.Codigo, l.Ordem)).ToList(),
            servicos.Select(s => new ServicoMapa(s.Id, s.Codigo, !string.IsNullOrWhiteSpace(s.Sigla) ? s.Sigla! : s.Codigo.Replace("ITS", "").Trim(), s.Nome)).ToList(),
            celulas, ultimas.Values.ToList());
    }
}
