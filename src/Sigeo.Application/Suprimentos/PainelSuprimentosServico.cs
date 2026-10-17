using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Common;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Painel do gestor de suprimentos e os indicadores automáticos das metas (tempo de compra, entrega no prazo, necessidade…).</summary>
public sealed class PainelSuprimentosServico(IFabricaDb fabrica, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);
    private static DateTimeOffset Dt(DateOnly d) => new(d.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    // ───────── Indicadores (também usados pelas metas) ─────────

    /// <summary>Tempo de compra (dias corridos da autorização da requisição até o pedido), por comprador do lote.</summary>
    public static async Task<List<(Guid? Comprador, decimal Dias)>> TemposCompraAsync(ISigeoDb db, DateOnly inicio, DateOnly fim, Guid? obra, CancellationToken ct)
    {
        var l = await (from p in db.Pedidos.AsNoTracking()
                       join r in db.Requisicoes.AsNoTracking() on p.RequisicaoId equals r.Id
                       where p.CriadoEm >= Dt(inicio) && p.CriadoEm < Dt(fim.AddDays(1)) && p.Status != StatusPedido.Cancelado && r.AprovadoDiretorEm != null && (obra == null || p.ObraId == obra)
                       select new { p.Id, p.CriadoEm, Aut = r.AprovadoDiretorEm!.Value }).ToListAsync(ct);
        var ids = l.Select(x => x.Id).ToList();
        var comp = await (from pi in db.PedidoItens.AsNoTracking()
                          join ri in db.RequisicaoItens.AsNoTracking() on pi.RequisicaoItemId equals ri.Id
                          join lo in db.LotesCompra.AsNoTracking() on ri.LoteCompraId equals lo.Id
                          where ids.Contains(pi.PedidoId)
                          select new { pi.PedidoId, lo.CompradorUsuarioId }).ToListAsync(ct);
        var porPed = comp.GroupBy(x => x.PedidoId).ToDictionary(g => g.Key, g => g.First().CompradorUsuarioId);
        return l.Select(x => (porPed.GetValueOrDefault(x.Id), (decimal)Math.Max(0, (x.CriadoEm - x.Aut).TotalDays))).ToList();
    }

    /// <summary>Pedido no prazo e completo: tudo recebido até a data de entrega prometida. Conta os pedidos com entrega no período.</summary>
    public static async Task<List<(Guid FornecedorId, Guid? Comprador, bool NoPrazo)>> EntregasAsync(ISigeoDb db, DateOnly inicio, DateOnly fim, DateOnly hoje, Guid? obra, CancellationToken ct)
    {
        var ate = fim < hoje ? fim : hoje;
        var peds = await db.Pedidos.AsNoTracking().Include(p => p.Itens)
            .Where(p => p.Entrega >= inicio && p.Entrega <= ate && p.Status != StatusPedido.Cancelado && p.Status != StatusPedido.Emitido && p.Status != StatusPedido.AguardandoAprovacaoDiretor && (obra == null || p.ObraId == obra))
            .ToListAsync(ct);
        var ids = peds.Select(p => p.Id).ToList();
        var recs = await (from ri in db.Set<RecebimentoItem>().AsNoTracking()
                          join r in db.Recebimentos.AsNoTracking() on ri.RecebimentoId equals r.Id
                          where r.PedidoId != null && ids.Contains(r.PedidoId.Value) && r.Destino != DestinoRecebimento.Devolvido && ri.PedidoItemId != null
                          select new { ri.PedidoItemId, ri.QtdRecebida, r.DataRecebimento }).ToListAsync(ct);
        var riIds = peds.SelectMany(p => p.Itens).Where(i => i.RequisicaoItemId != null).Select(i => i.RequisicaoItemId!.Value).ToList();
        var lotes = await (from ri in db.RequisicaoItens.AsNoTracking() join lo in db.LotesCompra.AsNoTracking() on ri.LoteCompraId equals lo.Id
                           where riIds.Contains(ri.Id) select new { ri.Id, lo.CompradorUsuarioId }).ToDictionaryAsync(x => x.Id, x => x.CompradorUsuarioId, ct);
        return peds.Select(p =>
        {
            var limite = p.Entrega.AddDays(1);
            var ok = p.Itens.All(i => recs.Where(r => r.PedidoItemId == i.Id && DateOnly.FromDateTime(r.DataRecebimento.ToLocalTime().Date) < limite).Sum(r => r.QtdRecebida) >= i.Quantidade);
            var comp = p.Itens.Select(i => i.RequisicaoItemId is { } rid ? lotes.GetValueOrDefault(rid) : null).FirstOrDefault(x => x != null);
            return (p.FornecedorId, comp, ok);
        }).ToList();
    }

    /// <summary>Itens com necessidade no período: recebidos (net de cancelado) até a data de necessidade.</summary>
    public static async Task<List<(string? Grupo, int LeadCadastrado, decimal? DiasReais, bool AteNecessidade, bool DentroLead, Guid? Comprador)>> NecessidadesAsync(
        ISigeoDb db, DateOnly inicio, DateOnly fim, DateOnly hoje, Guid? obra, CancellationToken ct)
    {
        var ate = fim < hoje ? fim : hoje;
        var itens = await (from i in db.RequisicaoItens.AsNoTracking()
                           join r in db.Requisicoes.AsNoTracking() on i.RequisicaoId equals r.Id
                           where r.AprovadoDiretorEm != null && r.Tipo == TipoRequisicao.Material && (obra == null || r.ObraId == obra)
                                 && (i.DataNecessidade ?? r.DataEntrega) >= inicio && (i.DataNecessidade ?? r.DataEntrega) <= ate
                                 && !(i.Status == StatusItemRequisicao.Cancelado && i.QtdCancelada == 0)
                           select new { i.Id, i.ProdutoId, i.Quantidade, i.QtdCancelada, i.LeadTimeDias, i.LoteCompraId, Nec = i.DataNecessidade ?? r.DataEntrega, Aut = r.AprovadoDiretorEm!.Value }).ToListAsync(ct);
        if (itens.Count == 0) return [];
        var ids = itens.Select(i => i.Id).ToList();
        var recs = await (from pi in db.PedidoItens.AsNoTracking()
                          join ri in db.Set<RecebimentoItem>().AsNoTracking() on pi.Id equals ri.PedidoItemId
                          join r in db.Recebimentos.AsNoTracking() on ri.RecebimentoId equals r.Id
                          where pi.RequisicaoItemId != null && ids.Contains(pi.RequisicaoItemId.Value) && r.Destino != DestinoRecebimento.Devolvido
                          select new { Item = pi.RequisicaoItemId!.Value, ri.QtdRecebida, r.DataRecebimento }).ToListAsync(ct);
        var prodIds = itens.Where(i => i.ProdutoId != null).Select(i => i.ProdutoId!.Value).Distinct().ToList();
        var grupos = await db.Produtos.AsNoTracking().Where(p => prodIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Grupo, ct);
        var loteIds = itens.Where(i => i.LoteCompraId != null).Select(i => i.LoteCompraId!.Value).Distinct().ToList();
        var comp = await db.LotesCompra.AsNoTracking().Where(l => loteIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.CompradorUsuarioId, ct);
        return itens.Select(i =>
        {
            var precisa = i.Quantidade - i.QtdCancelada;
            DateTimeOffset? completo = null; decimal acum = 0;
            foreach (var r in recs.Where(r => r.Item == i.Id).OrderBy(r => r.DataRecebimento)) { acum += r.QtdRecebida; if (acum >= precisa) { completo = r.DataRecebimento; break; } }
            decimal? dias = completo is { } c ? (decimal)Math.Max(0, (c - i.Aut).TotalDays) : null;
            var ateNec = completo is { } c2 && DateOnly.FromDateTime(c2.ToLocalTime().Date) <= i.Nec;
            var dentro = dias is { } d && i.LeadTimeDias > 0 && d <= i.LeadTimeDias;
            return (i.ProdutoId is { } p ? grupos.GetValueOrDefault(p) : null, i.LeadTimeDias, dias, ateNec, dentro, i.LoteCompraId is { } l ? comp.GetValueOrDefault(l) : null);
        }).ToList();
    }

    public static async Task<List<(Guid? Comprador, bool Competitiva)>> CotacoesCompetitivasAsync(ISigeoDb db, DateOnly inicio, DateOnly fim, Guid? obra, CancellationToken ct)
    {
        var l = await (from c in db.Cotacoes.AsNoTracking()
                       join lo in db.LotesCompra.AsNoTracking() on c.LoteCompraId equals lo.Id into lg
                       from lo in lg.DefaultIfEmpty()
                       where c.CriadoEm >= Dt(inicio) && c.CriadoEm < Dt(fim.AddDays(1)) && (c.Status == StatusCotacao.Aprovado || c.Status == StatusCotacao.PedidoGerado || c.Status == StatusCotacao.AguardandoAprovacao)
                             && (obra == null || c.ObraId == obra)
                       select new { Comp = lo != null ? lo.CompradorUsuarioId : null, c.PropostasRecebidas }).ToListAsync(ct);
        return l.Select(x => (x.Comp, x.PropostasRecebidas >= 3)).ToList();
    }

    private static decimal? Perc(int a, int t) => t == 0 ? null : Math.Round(100m * a / t, 0);
    private static decimal? Media(IEnumerable<decimal> v) { var l = v.ToList(); return l.Count == 0 ? null : Math.Round(l.Average(), 1); }

    // ───────── Painel ─────────

    public async Task<PainelSuprimentosDto> CarregarAsync(DateOnly inicio, DateOnly fim, Guid? obra, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var hoje = Hoje;
        var economia = await EconomiaServico.AvaliarPeriodoAsync(db, inicio, fim, obra, ct);
        var tempos = await TemposCompraAsync(db, inicio, fim, obra, ct);
        var entregas = await EntregasAsync(db, inicio, fim, hoje, obra, ct);
        var nec = await NecessidadesAsync(db, inicio, fim, hoje, obra, ct);
        var metas = await db.Metas.AsNoTracking().Where(m => m.Ativa && m.Inicio <= fim && m.Fim >= inicio).ToListAsync(ct);
        var carteiras = await db.CarteirasComprador.AsNoTracking().Include(c => c.Ausencias).Where(c => c.Ativa).OrderBy(c => c.Nome).ToListAsync(ct);
        var lotes = await db.LotesCompra.AsNoTracking().Where(l => l.Situacao == SituacaoLote.EmCompra || l.Situacao == SituacaoLote.EmRecebimento).Where(l => obra == null || l.ObraId == obra).ToListAsync(ct);
        decimal? MetaDe(IndicadorMeta ind, Guid? usuario) =>
            metas.Where(m => m.Indicador == ind && m.Dono == DonoMeta.Pessoa && m.UsuarioId == usuario && usuario != null).Select(m => (decimal?)m.Alvo).FirstOrDefault()
            ?? metas.Where(m => m.Indicador == ind && m.Dono == DonoMeta.Setor).Select(m => (decimal?)m.Alvo).FirstOrDefault();
        MetaComprador Linha(Guid? u, string nome, bool setor)
        {
            var ec = EconomiaReal.Resumir(economia.Where(a => setor || a.Compra.CompradorId == u));
            var t = tempos.Where(x => setor || x.Comprador == u).Select(x => x.Dias);
            var e = entregas.Where(x => setor || x.Comprador == u).ToList();
            var ls = lotes.Where(l => setor || l.CompradorUsuarioId == u).ToList();
            var atr = ls.Count(l => l.Situacao == SituacaoLote.EmCompra && l.DataLimiteCompra is { } d && d < hoje);
            var aus = !setor && carteiras.FirstOrDefault(c => c.UsuarioId == u)?.AusenteEm(hoje) == true;
            return new MetaComprador(u, nome, ec.BaseTotal == 0 ? null : ec.Percentual, setor ? MetaDe(IndicadorMeta.EconomiaReal, null) : MetaDe(IndicadorMeta.EconomiaReal, u),
                Media(t), setor ? MetaDe(IndicadorMeta.TempoCompra, null) : MetaDe(IndicadorMeta.TempoCompra, u),
                Perc(e.Count(x => x.NoPrazo), e.Count), setor ? MetaDe(IndicadorMeta.EntregaNoPrazo, null) : MetaDe(IndicadorMeta.EntregaNoPrazo, u), ls.Count, atr, aus);
        }
        var compradores = carteiras.Select(c => Linha(c.UsuarioId, c.Nome, false)).ToList();
        if (lotes.Any(l => l.CompradorUsuarioId is null)) compradores.Add(Linha(null, "Fila do gestor", false) with { Economia = null, TempoCompra = null, NoPrazo = null });

        // Volume de requisições autorizadas no período.
        var reqs = await db.Requisicoes.AsNoTracking().Where(r => r.AprovadoDiretorEm >= Dt(inicio) && r.AprovadoDiretorEm < Dt(fim.AddDays(1)) && (obra == null || r.ObraId == obra))
            .Select(r => new { r.Id, r.Status }).ToListAsync(ct);
        var reqIds = reqs.Select(r => r.Id).ToList();
        var valores = await (from pi in db.PedidoItens.AsNoTracking() join p in db.Pedidos.AsNoTracking() on pi.PedidoId equals p.Id
                             where p.RequisicaoId != null && reqIds.Contains(p.RequisicaoId.Value) && p.Status != StatusPedido.Cancelado
                             select new { p.RequisicaoId, Rec = pi.QuantidadeRecebidaAceita * pi.ValorUnitario, Saldo = (pi.Quantidade - pi.QuantidadeRecebidaAceita) * pi.ValorUnitario }).ToListAsync(ct);
        var atendidas = reqs.Where(r => r.Status is StatusRequisicao.Atendida or StatusRequisicao.Concluida).Select(r => r.Id).ToHashSet();
        var parciais = reqs.Where(r => r.Status == StatusRequisicao.ParcialmenteAtendida).Select(r => r.Id).ToHashSet();
        var volume = new VolumeRequisicoes(reqs.Count, atendidas.Count, parciais.Count, reqs.Count(r => r.Status == StatusRequisicao.EncerradaComSaldo),
            reqs.Count(r => r.Status is StatusRequisicao.Autorizada or StatusRequisicao.EmCotacao or StatusRequisicao.PedidoEmitido),
            Math.Round(valores.Where(v => atendidas.Contains(v.RequisicaoId!.Value)).Sum(v => v.Rec), 2),
            Math.Round(valores.Where(v => parciais.Contains(v.RequisicaoId!.Value)).Sum(v => Math.Max(0, v.Saldo)), 2));

        var lead = nec.GroupBy(n => n.Grupo ?? "Sem grupo").Select(g => new LeadCategoria(g.Key,
                Media(g.Where(x => x.LeadCadastrado > 0).Select(x => (decimal)x.LeadCadastrado)), Media(g.Where(x => x.DiasReais != null).Select(x => x.DiasReais!.Value)),
                Perc(g.Count(x => x.DentroLead), g.Count(x => x.LeadCadastrado > 0)), Perc(g.Count(x => x.AteNecessidade), g.Count()), g.Count()))
            .OrderByDescending(x => x.Itens).ToList();

        // Necessidades dos próximos 7 dias com saldo a pedir.
        var prox = await (from i in db.RequisicaoItens.AsNoTracking()
                          join r in db.Requisicoes.AsNoTracking() on i.RequisicaoId equals r.Id
                          where (r.Status == StatusRequisicao.Autorizada || r.Status == StatusRequisicao.EmCotacao || r.Status == StatusRequisicao.PedidoEmitido || r.Status == StatusRequisicao.ParcialmenteAtendida)
                                && (i.DataNecessidade ?? r.DataEntrega) >= hoje && (i.DataNecessidade ?? r.DataEntrega) <= hoje.AddDays(7) && (obra == null || r.ObraId == obra)
                                && i.Status != StatusItemRequisicao.Cancelado
                          select new { i.Id, Id2 = r.Id, r.Numero, i.Descricao, i.Unidade, Nec = i.DataNecessidade ?? r.DataEntrega, i.LoteCompraId }).ToListAsync(ct);
        var proxIds = prox.Select(x => x.Id2).Distinct().ToList();
        var saldos = new Dictionary<Guid, SaldoItem>();
        foreach (var rid in proxIds) foreach (var s in await AtendimentoServico.SaldosAsync(db, rid, ct)) saldos[s.ItemId] = s;
        var nomesLote = lotes.ToDictionary(l => l.Id, l => l.CompradorNome);
        var semPedido = prox.Where(x => saldos.TryGetValue(x.Id, out var s) && s.APedir > 0 && s.Pedido == 0)
            .Select(x => new NecessidadeSemPedido(x.Id2, x.Numero, x.Descricao, x.Nec, saldos[x.Id].APedir, x.Unidade, x.LoteCompraId is { } l ? nomesLote.GetValueOrDefault(l) : null))
            .OrderBy(x => x.Necessidade).Take(30).ToList();

        var fIds = entregas.Select(e => e.FornecedorId).Distinct().ToList();
        var nomes = await db.Fornecedores.AsNoTracking().Where(f => fIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.NomeFantasia ?? f.RazaoSocial, ct);
        var porForn = entregas.GroupBy(e => e.FornecedorId).Select(g => new EntregaFornecedor(g.Key, nomes.GetValueOrDefault(g.Key, ""), g.Count(), g.Count(x => x.NoPrazo), Perc(g.Count(x => x.NoPrazo), g.Count()) ?? 0))
            .OrderBy(x => x.Perc).ThenByDescending(x => x.Pedidos).Take(15).ToList();

        var p0 = await ParametrosServico.CarregarAsync(db, ct: ct);
        var alertas = await AlertasFornecedoresAsync(db, hoje, obra, p0[Domain.Estrutura.Parametros.IdfMinimo], ct);
        var ncs = await NaoConformidadesAsync(db, hoje.AddDays(-30), obra, ct);
        var setor = Linha(null, "Setor", true);
        return new PainelSuprimentosDto(compradores, setor, volume, Perc(nec.Count(n => n.AteNecessidade), nec.Count), MetaDe(IndicadorMeta.AtendimentoNecessidade, null),
            lead, semPedido, porForn, alertas, ncs);
    }

    /// <summary>Fornecedores com pedido/contrato aberto e problema de avaliação: IDF abaixo do mínimo, qualificação, documento vencido ou vencendo.</summary>
    public static async Task<List<AlertaFornecedor>> AlertasFornecedoresAsync(ISigeoDb db, DateOnly hoje, Guid? obra, decimal idfMinimo, CancellationToken ct)
    {
        var abertos = new[] { StatusPedido.Aprovado, StatusPedido.EnviadoFornecedor, StatusPedido.EntregueParcial, StatusPedido.Emitido, StatusPedido.AguardandoAprovacaoDiretor };
        var peds = await db.Pedidos.AsNoTracking().Include(p => p.Itens).Where(p => abertos.Contains(p.Status) && (obra == null || p.ObraId == obra)).ToListAsync(ct);
        var contratos = await db.Contratos.AsNoTracking().Where(k => k.Status == StatusContrato.Ativo && (obra == null || k.ObraId == obra)).Select(k => k.FornecedorId).ToListAsync(ct);
        var ids = peds.Select(p => p.FornecedorId).Concat(contratos).Distinct().ToList();
        if (ids.Count == 0) return [];
        var fs = await db.Fornecedores.AsNoTracking().Include(f => f.Qualificacoes).Include(f => f.Documentos).ThenInclude(d => d.TipoDocumento).Where(f => ids.Contains(f.Id)).ToListAsync(ct);
        var desde = new DateTimeOffset(hoje.AddMonths(-6).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var idf = (await db.Recebimentos.AsNoTracking().Where(r => ids.Contains(r.FornecedorId) && r.Idf != null && r.DataRecebimento >= desde).Select(r => new { r.FornecedorId, r.Idf }).ToListAsync(ct))
            .GroupBy(x => x.FornecedorId).ToDictionary(g => g.Key, g => Math.Round(g.Average(x => x.Idf!.Value), 1));
        var r = new List<AlertaFornecedor>();
        foreach (var f in fs)
        {
            var dele = peds.Where(p => p.FornecedorId == f.Id).ToList();
            var valor = dele.Sum(p => p.Itens.Sum(i => (i.Quantidade - i.QuantidadeRecebidaAceita) * i.ValorUnitario));
            void A(string tipo, string det, string tom) => r.Add(new AlertaFornecedor(f.Id, f.NomeExibicao, tipo, det, tom, dele.Count, Math.Round(valor, 2)));
            var st = f.CalcularStatus(hoje);
            if (st is StatusQualificacao.Desqualificado or StatusQualificacao.NaoQualificado) A("Bloqueado", $"Qualificação: {(st == StatusQualificacao.Desqualificado ? "desqualificado" : "não qualificado")}", "erro");
            else if (st is StatusQualificacao.Vencida or StatusQualificacao.EmQualificacao) A(st == StatusQualificacao.Vencida ? "Avaliação vencida" : "Em qualificação", "Qualificação a renovar/concluir", "aviso");
            if (idf.TryGetValue(f.Id, out var n) && n < idfMinimo) A("IDF abaixo do mínimo", $"IDF {n:0.0} (mínimo {idfMinimo:0.#}) nos últimos 6 meses", "erro");
            foreach (var b in TravasQualidade.DocumentosBloqueantes(f, hoje)) A("Documento", b, "erro");
            foreach (var d in f.Documentos.Where(d => !d.Excluido && d.DataValidade is { } v && v >= hoje && v <= hoje.AddDays(15) && d.TipoDocumento is { PorEntrega: false })
                         .GroupBy(d => d.TipoDocumentoExigidoId).Select(g => g.OrderByDescending(x => x.DataValidade).First()))
                A("Documento vencendo", $"{d.TipoDocumento.Nome} vence em {d.DataValidade:dd/MM/yyyy}", "info");
        }
        return r.OrderBy(a => a.Tom == "erro" ? 0 : a.Tom == "aviso" ? 1 : 2).ThenByDescending(a => a.ValorAberto).ToList();
    }

    public static async Task<List<NcRecebimento>> NaoConformidadesAsync(ISigeoDb db, DateOnly desde, Guid? obra, CancellationToken ct)
    {
        var d0 = Dt(desde);
        var l = await db.Recebimentos.AsNoTracking().Include(r => r.Itens)
            .Where(r => r.DataRecebimento >= d0 && (obra == null || r.ObraId == obra))
            .Where(r => r.FvmPrazo == ResultadoCriterio.Reprovado || r.FvmQualidade == ResultadoCriterio.Reprovado || r.FvmAtendimento == ResultadoCriterio.Reprovado
                        || r.Destino != DestinoRecebimento.Aceito)
            .OrderByDescending(r => r.DataRecebimento).Take(60).ToListAsync(ct);
        var fIds = l.Select(r => r.FornecedorId).Distinct().ToList();
        var nomes = await db.Fornecedores.AsNoTracking().Where(f => fIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.NomeFantasia ?? f.RazaoSocial, ct);
        var obras = await db.Obras.AsNoTracking().ToDictionaryAsync(o => o.Id, o => o.Nome, ct);
        return l.Select(r => new NcRecebimento(r.Id, DateOnly.FromDateTime(r.DataRecebimento.ToLocalTime().Date), nomes.GetValueOrDefault(r.FornecedorId, ""),
            string.Join(", ", r.Itens.Select(i => i.Descricao).Distinct().Take(3)), obras.GetValueOrDefault(r.ObraId, ""),
            r.Destino == DestinoRecebimento.Devolvido ? "Devolução" : r.FvmQualidade == ResultadoCriterio.Reprovado ? "FVM reprovada (qualidade)"
                : r.FvmPrazo == ResultadoCriterio.Reprovado ? "Fora do prazo" : r.FvmAtendimento == ResultadoCriterio.Reprovado ? "Atendimento/documentação" : "Aceito com ressalva",
            r.Ocorrencia ?? (r.Destino == DestinoRecebimento.Devolvido ? "Devolvido" : "—"))).ToList();
    }
}
