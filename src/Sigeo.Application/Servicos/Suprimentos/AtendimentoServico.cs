using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Servicos;

public sealed record LinhaSaldo(Guid ItemId, string Descricao, string Unidade, SaldoItem Saldo, string? Lote, string? Comprador, DateOnly? Necessidade, string? MotivoCancelamento);
public sealed record LinhaLote(Guid Id, string Letra, string Categoria, string? Comprador, string? Titular, bool ComSubstituto, SituacaoLote Situacao, DateOnly? Limite, int Itens, string Detalhe);
public sealed record EventoAtendimento(DateTimeOffset Quando, string Tipo, string Texto, string? Link);
public sealed record AtendimentoDto(Guid RequisicaoId, int Numero, StatusRequisicao Status, IReadOnlyList<LinhaSaldo> Itens, IReadOnlyList<LinhaLote> Lotes,
    IReadOnlyList<EventoAtendimento> Linha, decimal ValorRequisitado, decimal ValorPedido, decimal ValorRecebido, decimal ValorAPedir, decimal ValorCancelado, int Pedidos, int Recebimentos);
public sealed record LinhaFila(Guid LoteId, Guid RequisicaoId, int Requisicao, string Letra, string Obra, string Categoria, int Itens, DateOnly? Limite, int DiasAtraso,
    string Situacao, string Origem, bool Substituicao, Guid? CotacaoAberta, string? Comprador);

/// <summary>
/// Atendimento parcial da requisição (várias cotações, pedidos e recebimentos até o saldo zerar) e encaminhamento
/// automático para o comprador da categoria, com substituto na ausência.
/// </summary>
public sealed class AtendimentoServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── Saldos e situação (usados por cotação, pedido e recebimento) ─────────

    public static async Task<List<SaldoItem>> SaldosAsync(ISigeoDb db, Guid requisicaoId, CancellationToken ct = default)
    {
        var itens = await db.RequisicaoItens.Where(i => i.RequisicaoId == requisicaoId).ToListAsync(ct);
        return await SaldosAsync(db, requisicaoId, itens, ct);
    }

    private static async Task<List<SaldoItem>> SaldosAsync(ISigeoDb db, Guid requisicaoId, List<RequisicaoItem> itens, CancellationToken ct)
    {
        var ids = itens.Select(i => i.Id).ToList();
        var andamento = SincronizacaoCadastros.CotacoesEmAndamento;
        var cot = await (from ci in db.Set<CotacaoItem>().AsNoTracking()
                         join c in db.Cotacoes.AsNoTracking() on ci.CotacaoId equals c.Id
                         where ci.RequisicaoItemId != null && ids.Contains(ci.RequisicaoItemId.Value) && andamento.Contains(c.Status)
                         select new AtendimentoRequisicao.LinhaCotacao(c.Id, ci.RequisicaoItemId!.Value, ci.Quantidade)).ToListAsync(ct);
        var ped = await (from pi in db.PedidoItens.AsNoTracking()
                         join p in db.Pedidos.AsNoTracking() on pi.PedidoId equals p.Id
                         where pi.RequisicaoItemId != null && ids.Contains(pi.RequisicaoItemId.Value) && p.Status != StatusPedido.Cancelado
                         select new AtendimentoRequisicao.LinhaPedido(pi.RequisicaoItemId!.Value, pi.Quantidade, pi.QuantidadeRecebidaAceita)).ToListAsync(ct);
        // Serviço: o contrato gerado atende os itens vencidos (não há pedido nem recebimento de material).
        var contratados = await (from ci in db.Set<CotacaoItem>().AsNoTracking()
                                 join c in db.Cotacoes.AsNoTracking() on ci.CotacaoId equals c.Id
                                 where ci.RequisicaoItemId != null && ids.Contains(ci.RequisicaoItemId.Value) && c.Status == StatusCotacao.PedidoGerado && c.Tipo == TipoRequisicao.Servico
                                       && (ci.Vencedor || ci.FornecedorId == c.FornecedorVencedorId)
                                 select new { RequisicaoItemId = ci.RequisicaoItemId!.Value, ci.Vencedor, ci.QuantidadeVencida, ci.Quantidade, c.Id, Geral = ci.FornecedorId == c.FornecedorVencedorId }).ToListAsync(ct);
        var porCot = contratados.GroupBy(x => x.Id).SelectMany(g => g.Any(x => x.Vencedor) ? g.Where(x => x.Vencedor) : g.Where(x => x.Geral));
        ped.AddRange(porCot.Select(x => new AtendimentoRequisicao.LinhaPedido(x.RequisicaoItemId, x.QuantidadeVencida ?? x.Quantidade, x.QuantidadeVencida ?? x.Quantidade)));
        return AtendimentoRequisicao.Calcular(itens, cot, ped);
    }

    /// <summary>Recalcula a situação da requisição, dos itens e dos lotes. Grava com o próximo SaveChanges.</summary>
    public static async Task AtualizarAsync(ISigeoDb db, Guid? requisicaoId, CancellationToken ct = default)
    {
        if (requisicaoId is not { } id) return;
        var r = await db.Requisicoes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return;
        var itens = await db.RequisicaoItens.Where(i => i.RequisicaoId == id).ToListAsync(ct);
        var saldos = await SaldosAsync(db, id, itens, ct);
        var porId = saldos.ToDictionary(s => s.ItemId);
        foreach (var i in itens)
        {
            var s = porId[i.Id];
            if (i.Status == StatusItemRequisicao.Cancelado && i.QtdCancelada == 0) continue;
            i.Status = s.Cancelado >= s.Requisitado && s.Requisitado > 0 ? StatusItemRequisicao.Cancelado
                : s.Atendido ? StatusItemRequisicao.Recebido : s.Pedido > 0 ? StatusItemRequisicao.Pedido : s.EmCotacao > 0 ? StatusItemRequisicao.Orcado : StatusItemRequisicao.Aberto;
        }
        r.Status = AtendimentoRequisicao.Situacao(r.Status == StatusRequisicao.PedidoEmitido ? StatusRequisicao.EmCotacao : r.Status, saldos);
        foreach (var l in await db.LotesCompra.Where(x => x.RequisicaoId == id).ToListAsync(ct))
            l.Situacao = AtendimentoRequisicao.SituacaoLote(itens.Where(i => i.LoteCompraId == l.Id).Select(i => porId[i.Id]).ToList());
    }

    // ───────── Encaminhamento (na autorização) ─────────

    /// <summary>Divide a requisição autorizada em lotes por comprador (categoria dos itens). Idempotente: itens já com lote ficam.</summary>
    public static async Task EncaminharAsync(ISigeoDb db, Requisicao r, DateOnly hoje, DateTimeOffset agora, CancellationToken ct = default)
    {
        var itens = await db.RequisicaoItens.Where(i => i.RequisicaoId == r.Id && i.LoteCompraId == null && i.Status != StatusItemRequisicao.Cancelado).ToListAsync(ct);
        if (itens.Count == 0) return;
        var carteiras = await db.CarteirasComprador.AsNoTracking().Include(c => c.Categorias).Include(c => c.Ausencias).Where(c => c.Ativa).ToListAsync(ct);
        var prodIds = itens.Where(i => i.ProdutoId != null).Select(i => i.ProdutoId!.Value).Distinct().ToList();
        var prods = await db.Produtos.AsNoTracking().Where(p => prodIds.Contains(p.Id)).Select(p => new { p.Id, p.Grupo, p.SubGrupo }).ToDictionaryAsync(p => p.Id, ct);
        var existentes = await db.LotesCompra.Where(l => l.RequisicaoId == r.Id).ToListAsync(ct);
        var grupos = itens.Select(i =>
        {
            var p = i.ProdutoId is { } pid && prods.TryGetValue(pid, out var x) ? x : null;
            var d = r.Tipo == TipoRequisicao.Servico
                ? EncaminhamentoCompras.Encaminhar(null, null, r.CategoriaServico ?? "Serviço", r.ObraId, carteiras, hoje)
                : EncaminhamentoCompras.Encaminhar(p?.Grupo, p?.SubGrupo, null, r.ObraId, carteiras, hoje);
            return (Item: i, Destino: d);
        }).GroupBy(x => (x.Destino.CompradorId, x.Destino.TitularId)).ToList();
        foreach (var g in grupos)
        {
            var d = g.First().Destino;
            var lote = existentes.FirstOrDefault(l => l.CompradorUsuarioId == d.CompradorId && l.TitularUsuarioId == d.TitularId && l.Situacao == SituacaoLote.EmCompra);
            if (lote is null)
            {
                var categorias = g.Select(x => x.Destino.Categoria).Distinct().ToList();
                lote = new LoteCompra
                {
                    ObraId = r.ObraId, RequisicaoId = r.Id, Letra = EncaminhamentoCompras.Letra(existentes.Count),
                    Categoria = categorias.Count == 1 ? categorias[0] : string.Join(", ", categorias.Take(3)) + (categorias.Count > 3 ? "…" : ""),
                    CompradorUsuarioId = d.CompradorId, CompradorNome = d.CompradorNome, TitularUsuarioId = d.TitularId, TitularNome = d.TitularNome, AtribuidoEm = agora
                };
                existentes.Add(lote);
                db.LotesCompra.Add(lote);
            }
            foreach (var x in g) x.Item.LoteCompraId = lote.Id;
            lote.DataLimiteCompra = EncaminhamentoCompras.DataLimite(itens.Where(i => i.LoteCompraId == lote.Id), r.DataEntrega);
        }
    }

    /// <summary>
    /// Ausências: lote não iniciado (sem cotação) de quem está ausente vai para o substituto; quando o titular volta,
    /// o que o substituto não começou volta para ele (o que começou fica com o substituto).
    /// </summary>
    public static async Task<int> RevisarAusenciasAsync(ISigeoDb db, DateOnly hoje, DateTimeOffset agora, CancellationToken ct = default)
    {
        var carteiras = await db.CarteirasComprador.AsNoTracking().Include(c => c.Ausencias).Where(c => c.Ativa).ToListAsync(ct);
        if (carteiras.Count == 0) return 0;
        var porUsuario = carteiras.ToDictionary(c => c.UsuarioId);
        var lotes = await db.LotesCompra.Where(l => l.Situacao == SituacaoLote.EmCompra && l.TitularUsuarioId != null).ToListAsync(ct);
        var comCotacao = (await db.Cotacoes.AsNoTracking().Where(c => c.LoteCompraId != null).Select(c => c.LoteCompraId!.Value).Distinct().ToListAsync(ct)).ToHashSet();
        var n = 0;
        foreach (var l in lotes.Where(l => !comCotacao.Contains(l.Id)))
        {
            if (!porUsuario.TryGetValue(l.TitularUsuarioId!.Value, out var tit)) continue;
            var aus = tit.Ausencias.FirstOrDefault(a => a.Inicio <= hoje && a.Fim >= hoje);
            if (aus is null && l.CompradorUsuarioId != tit.UsuarioId)
            { l.CompradorUsuarioId = tit.UsuarioId; l.CompradorNome = tit.Nome; l.AtribuidoEm = agora; l.MotivoReatribuicao = "Titular voltou da ausência"; n++; }
            else if (aus is { TransferirAbertos: true } && l.CompradorUsuarioId == tit.UsuarioId)
            {
                var sub = tit.SubstitutoUsuarioId is { } sid ? porUsuario.GetValueOrDefault(sid) : null;
                var livre = tit.SubstitutoUsuarioId is not null && (sub is null || !sub.AusenteEm(hoje));
                l.CompradorUsuarioId = livre ? tit.SubstitutoUsuarioId : null;
                l.CompradorNome = livre ? sub?.Nome ?? tit.SubstitutoNome : null;
                l.AtribuidoEm = agora; l.MotivoReatribuicao = $"Ausência de {tit.Nome} até {aus.Fim:dd/MM}"; n++;
            }
        }
        return n;
    }

    // ───────── Tela da requisição › aba Atendimento ─────────

    public async Task<AtendimentoDto> AtendimentoAsync(Guid requisicaoId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var r = await db.Requisicoes.AsNoTracking().FirstAsync(x => x.Id == requisicaoId, ct);
        var itens = await db.RequisicaoItens.AsNoTracking().Where(i => i.RequisicaoId == requisicaoId).OrderBy(i => i.Descricao).ToListAsync(ct);
        var saldos = (await SaldosAsync(db, requisicaoId, itens, ct)).ToDictionary(s => s.ItemId);
        var lotes = await db.LotesCompra.AsNoTracking().Where(l => l.RequisicaoId == requisicaoId).OrderBy(l => l.Letra).ToListAsync(ct);
        var lotePorId = lotes.ToDictionary(l => l.Id);
        var linhas = itens.Select(i =>
        {
            var l = i.LoteCompraId is { } lid ? lotePorId.GetValueOrDefault(lid) : null;
            return new LinhaSaldo(i.Id, i.Descricao, i.Unidade, saldos[i.Id], l?.Letra, l?.CompradorNome, i.DataNecessidade ?? r.DataEntrega, i.MotivoCancelamento);
        }).ToList();
        var precos = itens.ToDictionary(i => i.Id, i => i.ValorReferencia ?? 0);
        var cots = await db.Cotacoes.AsNoTracking().Include(c => c.Fornecedores).Include(c => c.Itens).Where(c => c.RequisicaoId == requisicaoId).ToListAsync(ct);
        var peds = await db.Pedidos.AsNoTracking().Include(p => p.Itens).Include(p => p.Fornecedor).Where(p => p.RequisicaoId == requisicaoId && p.Status != StatusPedido.Cancelado).ToListAsync(ct);
        var pedIds = peds.Select(p => p.Id).ToList();
        var recs = await db.Recebimentos.AsNoTracking().Include(x => x.Itens).Where(x => x.PedidoId != null && pedIds.Contains(x.PedidoId.Value)).ToListAsync(ct);
        var forn = await db.Fornecedores.AsNoTracking().Where(f => cots.SelectMany(c => c.Fornecedores).Select(x => x.FornecedorId).Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.NomeExibicao, ct);
        var notas = await db.Notas.AsNoTracking().Where(n => n.Entidade == nameof(Requisicao) && n.EntidadeId == requisicaoId).ToListAsync(ct);
        var ev = new List<EventoAtendimento>();
        if (r.AprovadoDiretorEm is { } au) ev.Add(new(au, "Autorização", $"Autorizada por {r.AprovadoEngenheiroPor} e {r.AprovadoDiretorPor}; {lotes.Count} lote(s): " +
            string.Join(", ", lotes.Select(l => $"{l.Letra} para {l.CompradorNome ?? "fila do gestor"}{(l.ComSubstituto ? $" (substituindo {l.TitularNome})" : "")}")), null));
        foreach (var c in cots)
        {
            var lote = c.LoteCompraId is { } lc && lotePorId.TryGetValue(lc, out var lt) ? $"lote {lt.Letra}, " : "";
            ev.Add(new(c.CriadoEm, "Cotação", $"Cotação {c.Numero} ({lote}{c.Itens.Select(i => i.RequisicaoItemId).Distinct().Count()} itens, {c.Fornecedores.Count} fornecedores) · {Rotulo(c.Status)}", $"cotacoes?id={c.Id}"));
        }
        foreach (var p in peds)
            ev.Add(new(p.CriadoEm, "Pedido", $"Pedido {p.Numero} · {p.Fornecedor.NomeExibicao}: {string.Join(", ", p.Itens.Select(i => $"{i.Descricao} {i.Quantidade:0.###} {i.Unidade}"))}", $"pedidos?id={p.Id}"));
        foreach (var x in recs)
        {
            var ped = peds.First(p => p.Id == x.PedidoId);
            ev.Add(new(x.DataRecebimento, "Recebimento", $"Recebimento {x.Numero} do pedido {ped.Numero}: " +
                string.Join(", ", x.Itens.Where(i => i.PedidoItemId != null).Select(i => $"{ped.Itens.FirstOrDefault(pi => pi.Id == i.PedidoItemId)?.Descricao} {i.QtdRecebida:0.###}")) +
                (x.Destino == DestinoRecebimento.Devolvido ? " · devolvido" : ""), $"recebimentos?id={x.Id}"));
        }
        foreach (var n in notas) ev.Add(new(n.CriadoEm, "Nota", n.Texto, null));
        var loteLinhas = lotes.Select(l =>
        {
            var doLote = itens.Where(i => i.LoteCompraId == l.Id).ToList();
            var pedLote = peds.Where(p => p.Itens.Any(pi => doLote.Any(i => i.Id == pi.RequisicaoItemId))).Select(p => p.Numero).ToList();
            var cotAb = cots.Where(c => c.LoteCompraId == l.Id && SincronizacaoCadastros.CotacoesEmAndamento.Contains(c.Status)).Select(c => c.Numero).ToList();
            var det = $"{doLote.Count} itens" + (pedLote.Count > 0 ? $" · pedido(s) {string.Join(", ", pedLote)}" : "") + (cotAb.Count > 0 ? $" · cotação {string.Join(", ", cotAb)} aberta" : "");
            return new LinhaLote(l.Id, l.Letra, l.Categoria, l.CompradorNome, l.TitularNome, l.ComSubstituto, l.Situacao, l.DataLimiteCompra, doLote.Count, det);
        }).ToList();
        decimal V(Func<SaldoItem, decimal> f) => Math.Round(itens.Sum(i => f(saldos[i.Id]) * precos[i.Id]), 2);
        var valorPedido = peds.Sum(p => p.Itens.Sum(i => i.ValorTotal));
        var valorRecebido = peds.Sum(p => p.Itens.Sum(i => Math.Round(i.QuantidadeRecebidaAceita * i.ValorUnitario, 2)));
        return new AtendimentoDto(r.Id, r.Numero, r.Status, linhas, loteLinhas, ev.OrderBy(e => e.Quando).ToList(),
            V(s => s.Requisitado), valorPedido, valorRecebido, V(s => s.APedir), V(s => s.Cancelado), peds.Count, recs.Count);
    }

    private static string Rotulo(StatusCotacao s) => s switch
    {
        StatusCotacao.AguardandoPropostas => "aguardando propostas", StatusCotacao.EmAnalise => "em análise", StatusCotacao.AguardandoAprovacao => "aguardando aprovação",
        StatusCotacao.Aprovado => "aprovada", StatusCotacao.PedidoGerado => "pedido gerado", StatusCotacao.Cancelado => "cancelada",
        StatusCotacao.ReprovadoVoltaCotacao => "reprovada (volta para cotação)", _ => "aberta"
    };

    /// <summary>Encerra o saldo a pedir dos itens (todos, se nenhum indicado), com justificativa; o solicitante é avisado pelas notas.</summary>
    public async Task<int> EncerrarSaldoAsync(Guid requisicaoId, IReadOnlyCollection<Guid>? itensIds, string? motivo, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var r = await db.Requisicoes.FirstAsync(x => x.Id == requisicaoId, ct);
        if (r.Status is not (StatusRequisicao.Autorizada or StatusRequisicao.EmCotacao or StatusRequisicao.PedidoEmitido or StatusRequisicao.ParcialmenteAtendida))
            throw new RegraDeNegocioException("Só dá para encerrar saldo de requisição autorizada e ainda não atendida.");
        var itens = await db.RequisicaoItens.Where(i => i.RequisicaoId == requisicaoId).ToListAsync(ct);
        var saldos = (await SaldosAsync(db, requisicaoId, itens, ct)).ToDictionary(s => s.ItemId);
        var alvo = itens.Where(i => (itensIds is null || itensIds.Count == 0 || itensIds.Contains(i.Id)) && saldos[i.Id].APedir > 0).ToList();
        if (alvo.Count == 0) throw new RegraDeNegocioException("Não há saldo a pedir para encerrar.");
        var textos = new List<string>();
        foreach (var i in alvo)
        {
            var q = AtendimentoRequisicao.EncerrarSaldo(i, saldos[i.Id], motivo);
            textos.Add($"{q:0.###} {i.Unidade} de {i.Descricao}");
        }
        db.Notas.Add(Domain.Colaboracao.Nota.Criar(nameof(Requisicao), r.Id, $"Saldo encerrado por {usuario.Nome}: {string.Join("; ", textos)}. Motivo: {motivo!.Trim()}"));
        await db.SaveChangesAsync(ct);
        await AtualizarAsync(db, r.Id, ct);
        await db.SaveChangesAsync(ct);
        return alvo.Count;
    }

    // ───────── Fila do comprador ─────────

    public async Task<List<LinhaFila>> FilaAsync(Guid? compradorId, bool gestor, CancellationToken ct = default)
    {
        await using (var w = fabrica.Criar())
        {
            if (await RevisarAusenciasAsync(w, Hoje, relogio.GetUtcNow(), ct) > 0) await w.SaveChangesAsync(ct);
        }
        await using var db = fabrica.CriarLeitura();
        var q = db.LotesCompra.AsNoTracking().Where(l => l.Situacao == SituacaoLote.EmCompra || l.Situacao == SituacaoLote.EmRecebimento);
        q = gestor ? q.Where(l => compradorId == null ? true : l.CompradorUsuarioId == compradorId || l.CompradorUsuarioId == null)
                   : q.Where(l => l.CompradorUsuarioId == compradorId);
        var lotes = await q.ToListAsync(ct);
        var reqIds = lotes.Select(l => l.RequisicaoId).Distinct().ToList();
        var reqs = await db.Requisicoes.AsNoTracking().Where(r => reqIds.Contains(r.Id)).Select(r => new { r.Id, r.Numero, r.ObraId }).ToDictionaryAsync(r => r.Id, ct);
        var obras = await db.Obras.AsNoTracking().ToDictionaryAsync(o => o.Id, o => o.Nome, ct);
        var loteIds = lotes.Select(l => l.Id).ToList();
        var itens = await db.RequisicaoItens.AsNoTracking().Where(i => i.LoteCompraId != null && loteIds.Contains(i.LoteCompraId.Value)).ToListAsync(ct);
        var cots = await db.Cotacoes.AsNoTracking().Include(c => c.Fornecedores).Where(c => c.LoteCompraId != null && loteIds.Contains(c.LoteCompraId.Value)).ToListAsync(ct);
        var saldos = new Dictionary<Guid, SaldoItem>();
        foreach (var rid in reqIds)
            foreach (var s in await SaldosAsync(db, rid, itens.Where(i => i.RequisicaoId == rid).ToList(), ct)) saldos[s.ItemId] = s;
        return lotes.Select(l =>
        {
            var doLote = itens.Where(i => i.LoteCompraId == l.Id).ToList();
            var aberta = cots.Where(c => c.LoteCompraId == l.Id && SincronizacaoCadastros.CotacoesEmAndamento.Contains(c.Status)).OrderByDescending(c => c.Numero).FirstOrDefault();
            var aPedir = doLote.Count(i => saldos.TryGetValue(i.Id, out var s) && s.DisponivelCotacao > 0);
            var situacao = aberta is not null
                ? $"Em cotação {aberta.Numero} · {aberta.Fornecedores.Count(f => f.Status is StatusConvite.Respondido or StatusConvite.Vencedor)} de {aberta.Fornecedores.Count} respostas"
                : l.Situacao == SituacaoLote.EmRecebimento ? "Aguardando entrega"
                : cots.Any(c => c.LoteCompraId == l.Id) ? $"Saldo a pedir ({aPedir} itens)" : "Aguardando cotação";
            var atraso = l.DataLimiteCompra is { } d && d < Hoje && aberta is null && l.Situacao == SituacaoLote.EmCompra ? Hoje.DayNumber - d.DayNumber : 0;
            var r = reqs[l.RequisicaoId];
            return new LinhaFila(l.Id, l.RequisicaoId, r.Numero, l.Letra, obras.GetValueOrDefault(r.ObraId, ""), l.Categoria, doLote.Count, l.DataLimiteCompra, atraso, situacao,
                l.NaFilaDoGestor ? "Sem comprador na carteira" : l.ComSubstituto ? $"Substituindo {l.TitularNome}" : "Minha carteira", l.ComSubstituto, aberta?.Id, l.CompradorNome);
        }).OrderBy(x => x.Limite ?? DateOnly.MaxValue).ToList();
    }

    /// <summary>Gestor (ou o próprio comprador) reatribui um lote; opcionalmente inclui a categoria na carteira do novo comprador.</summary>
    public async Task ReatribuirAsync(Guid loteId, Guid? novoCompradorId, string? motivo, bool incluirNaCarteira, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var l = await db.LotesCompra.FirstAsync(x => x.Id == loteId, ct);
        if (l.Situacao is SituacaoLote.Atendido or SituacaoLote.Encerrado) throw new RegraDeNegocioException("Lote já atendido.");
        string? nome = null;
        if (novoCompradorId is { } nid)
            nome = await db.Usuarios.Where(u => u.Id == nid && u.Ativo).Select(u => u.Nome).FirstOrDefaultAsync(ct) ?? throw new RegraDeNegocioException("Usuário não encontrado ou inativo.");
        l.CompradorUsuarioId = novoCompradorId; l.CompradorNome = nome;
        l.TitularUsuarioId ??= novoCompradorId; l.TitularNome ??= nome;
        if (incluirNaCarteira && novoCompradorId is { } uid) { l.TitularUsuarioId = uid; l.TitularNome = nome; }
        l.AtribuidoEm = relogio.GetUtcNow();
        l.MotivoReatribuicao = string.IsNullOrWhiteSpace(motivo) ? $"Reatribuído por {usuario.Nome}" : $"{motivo.Trim()} ({usuario.Nome})";
        if (incluirNaCarteira && novoCompradorId is { } u)
        {
            var c = await db.CarteirasComprador.Include(x => x.Categorias).FirstOrDefaultAsync(x => x.UsuarioId == u, ct);
            if (c is null) { c = new CarteiraComprador { UsuarioId = u, Nome = nome! }; db.CarteirasComprador.Add(c); }
            var item = await db.RequisicaoItens.AsNoTracking().Where(i => i.LoteCompraId == l.Id && i.ProdutoId != null).Select(i => i.ProdutoId).FirstOrDefaultAsync(ct);
            var req = await db.Requisicoes.AsNoTracking().FirstAsync(x => x.Id == l.RequisicaoId, ct);
            if (req.Tipo == TipoRequisicao.Servico)
            {
                if (!c.Categorias.Any(x => x.CategoriaServico == req.CategoriaServico)) c.Categorias.Add(new CarteiraCategoria { CarteiraId = c.Id, CategoriaServico = req.CategoriaServico });
            }
            else if (item is { } pid && await db.Produtos.AsNoTracking().Where(p => p.Id == pid).Select(p => new { p.Grupo, p.SubGrupo }).FirstOrDefaultAsync(ct) is { Grupo: not null } g)
            {
                if (!c.Categorias.Any(x => x.Grupo == g.Grupo && (x.SubGrupo == null || x.SubGrupo == g.SubGrupo)))
                    c.Categorias.Add(new CarteiraCategoria { CarteiraId = c.Id, Grupo = g.Grupo, SubGrupo = g.SubGrupo });
            }
            var outras = await db.CarteirasComprador.AsNoTracking().Include(x => x.Categorias).Where(x => x.Id != c.Id).ToListAsync(ct);
            ValidacaoException.Se(EncaminhamentoCompras.Conflitos(c, outras));
        }
        await db.SaveChangesAsync(ct);
    }

    // ───────── Carteiras ─────────

    public async Task<List<CarteiraComprador>> CarteirasAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.CarteirasComprador.AsNoTracking().Include(c => c.Categorias).Include(c => c.Ausencias).OrderBy(c => c.Nome).ToListAsync(ct);
    }

    public async Task<Guid> SalvarCarteiraAsync(CarteiraComprador c, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var e = c.Validar().ToList();
        if (c.UsuarioId != Guid.Empty)
        {
            var u = await db.Usuarios.AsNoTracking().Where(x => x.Id == c.UsuarioId).Select(x => new { x.Nome, x.Ativo }).FirstOrDefaultAsync(ct);
            if (u is null || !u.Ativo) e.Add("Usuário comprador inativo ou não encontrado.");
            else c.Nome = u.Nome;
            if (await db.CarteirasComprador.AnyAsync(x => x.UsuarioId == c.UsuarioId && x.Id != c.Id, ct)) e.Add("Este usuário já tem carteira de compras.");
        }
        if (c.SubstitutoUsuarioId is { } sid) c.SubstitutoNome = await db.Usuarios.AsNoTracking().Where(x => x.Id == sid).Select(x => x.Nome).FirstOrDefaultAsync(ct);
        else c.SubstitutoNome = null;
        var outras = await db.CarteirasComprador.AsNoTracking().Include(x => x.Categorias).Where(x => x.Id != c.Id).ToListAsync(ct);
        e.AddRange(EncaminhamentoCompras.Conflitos(c, outras));
        ValidacaoException.Se(e);
        foreach (var x in c.Categorias) { x.CarteiraId = c.Id; x.Grupo = Limpo(x.Grupo); x.SubGrupo = Limpo(x.SubGrupo); x.CategoriaServico = Limpo(x.CategoriaServico); }
        foreach (var a in c.Ausencias) a.CarteiraId = c.Id;
        await Grafo.SalvarAsync(db, c, nameof(CarteiraComprador.Categorias), nameof(CarteiraComprador.Ausencias));
        await db.SaveChangesAsync(ct);
        if (await RevisarAusenciasAsync(db, Hoje, relogio.GetUtcNow(), ct) > 0) await db.SaveChangesAsync(ct);
        return c.Id;
    }

    private static string? Limpo(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

/// <summary>Lead time padrão por categoria do catálogo: produto sem lead time herda o do subgrupo, senão o do grupo.</summary>
public static class LeadTimeCategorias
{
    public static async Task<Dictionary<(string Grupo, string Sub), int>> MapaAsync(ISigeoDb db, CancellationToken ct = default)
    {
        var cats = await db.CategoriasProduto.AsNoTracking().Where(c => c.Ativo).Select(c => new { c.Id, c.PaiId, c.Nome, c.LeadTimeDias }).ToListAsync(ct);
        var porId = cats.ToDictionary(c => c.Id);
        var r = new Dictionary<(string, string), int>();
        foreach (var c in cats.Where(c => c.LeadTimeDias is > 0))
        {
            var chave = c.PaiId is { } pid && porId.TryGetValue(pid, out var pai) ? (K(pai.Nome), K(c.Nome)) : (K(c.Nome), "");
            r[chave] = c.LeadTimeDias!.Value;
        }
        return r;
    }

    public static int De(Dictionary<(string Grupo, string Sub), int> mapa, string? grupo, string? subGrupo) =>
        mapa.TryGetValue((K(grupo), K(subGrupo)), out var s) ? s : mapa.TryGetValue((K(grupo), ""), out var g) ? g : 0;

    private static string K(string? s) => (s ?? "").Trim().ToUpperInvariant();

    /// <summary>Completa o lead time dos itens que vieram sem (produto sem lead time próprio).</summary>
    public static async Task CompletarAsync(ISigeoDb db, IEnumerable<RequisicaoItem> itens, CancellationToken ct = default)
    {
        var sem = itens.Where(i => i.LeadTimeDias <= 0 && i.ProdutoId != null).ToList();
        if (sem.Count == 0) return;
        var mapa = await MapaAsync(db, ct);
        if (mapa.Count == 0) return;
        var ids = sem.Select(i => i.ProdutoId!.Value).Distinct().ToList();
        var prods = await db.Produtos.AsNoTracking().Where(p => ids.Contains(p.Id)).Select(p => new { p.Id, p.Grupo, p.SubGrupo }).ToDictionaryAsync(p => p.Id, ct);
        foreach (var i in sem)
            if (prods.TryGetValue(i.ProdutoId!.Value, out var p)) i.LeadTimeDias = De(mapa, p.Grupo, p.SubGrupo);
    }
}
