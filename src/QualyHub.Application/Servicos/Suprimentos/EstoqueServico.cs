using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Planejamento;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Application.Servicos;

/// <summary>
/// Motor do estoque: garante os almoxarifados e grava movimentos atualizando o saldo no mesmo contexto
/// (quem chama — recebimento, transferência, EPI — grava tudo num SaveChanges só).
/// </summary>
public static class MotorEstoque
{
    /// <summary>Almoxarifado central: o do estabelecimento marcado como almoxarifado central (criado na primeira vez). Null se não houver.</summary>
    public static async Task<Almoxarifado?> CentralAsync(IQualyHubDb db, CancellationToken ct = default)
    {
        var a = db.Almoxarifados.Local.FirstOrDefault(x => x.Tipo == TipoAlmoxarifado.Central && !x.Excluido)
                ?? await db.Almoxarifados.FirstOrDefaultAsync(x => x.Tipo == TipoAlmoxarifado.Central, ct);
        if (a is not null) return a;
        var est = await db.Estabelecimentos.AsNoTracking().FirstOrDefaultAsync(e => e.AlmoxarifadoCentral && e.Ativo, ct);
        if (est is null) return null;
        a = new Almoxarifado { Codigo = "CENTRAL", Nome = "Almoxarifado Central", Tipo = TipoAlmoxarifado.Central, EstabelecimentoId = est.Id, Endereco = est.EnderecoEntrega ?? est.Endereco };
        db.Almoxarifados.Add(a);
        return a;
    }

    /// <summary>Almoxarifado da obra (um por obra, criado na primeira vez).</summary>
    public static async Task<Almoxarifado> DaObraAsync(IQualyHubDb db, Guid obraId, CancellationToken ct = default)
    {
        var a = db.Almoxarifados.Local.FirstOrDefault(x => x.ObraId == obraId && !x.Excluido)
                ?? await db.Almoxarifados.FirstOrDefaultAsync(x => x.ObraId == obraId, ct);
        if (a is not null) return a;
        var obra = await db.Obras.AsNoTracking().Where(o => o.Id == obraId).Select(o => new { o.Codigo, o.Nome }).FirstAsync(ct);
        a = new Almoxarifado { Codigo = $"OB-{obra.Codigo}".Length > 30 ? $"OB-{obra.Codigo}"[..30] : $"OB-{obra.Codigo}", Nome = $"Almoxarifado · {obra.Nome}", Tipo = TipoAlmoxarifado.Obra, ObraId = obraId };
        db.Almoxarifados.Add(a);
        return a;
    }

    public static async Task<SaldoEstoque> SaldoAsync(IQualyHubDb db, Guid almoxarifadoId, Guid produtoId, CancellationToken ct = default)
    {
        var s = db.SaldosEstoque.Local.FirstOrDefault(x => x.AlmoxarifadoId == almoxarifadoId && x.ProdutoId == produtoId && !x.Excluido)
                ?? await db.SaldosEstoque.FirstOrDefaultAsync(x => x.AlmoxarifadoId == almoxarifadoId && x.ProdutoId == produtoId, ct);
        if (s is null) { s = new SaldoEstoque { AlmoxarifadoId = almoxarifadoId, ProdutoId = produtoId }; db.SaldosEstoque.Add(s); }
        return s;
    }

    /// <summary>Aplica o movimento ao saldo (valida saldo, custo médio) e o inclui no contexto.</summary>
    public static async Task<MovimentoEstoque> MovimentarAsync(IQualyHubDb db, MovimentoEstoque m, CancellationToken ct = default)
    {
        if (m.ProdutoId == Guid.Empty) throw new RegraDeNegocioException("Item sem produto cadastrado não entra no estoque: classifique o produto antes.");
        var s = await SaldoAsync(db, m.AlmoxarifadoId, m.ProdutoId, ct);
        RegrasEstoque.Aplicar(s, m);
        db.MovimentosEstoque.Add(m);
        return m;
    }

    /// <summary>Entrada do recebimento (FVM) no almoxarifado onde foi recebido: lote em quarentena entra bloqueado.</summary>
    public static async Task EntradaRecebimentoAsync(IQualyHubDb db, Recebimento r, IReadOnlyDictionary<Guid, decimal> custos, DateTimeOffset agora, CancellationToken ct = default)
    {
        if (r.AlmoxarifadoId is not { } almox) return;
        if (RegrasEstoque.EntraBloqueado(r.StatusLote) is not { } bloqueado) return;
        foreach (var i in r.Itens.Where(i => i.ProdutoId is not null && i.QtdRecebida > 0))
            await MovimentarAsync(db, new MovimentoEstoque
            {
                AlmoxarifadoId = almox, ProdutoId = i.ProdutoId!.Value, Data = agora, Tipo = TipoMovimentoEstoque.Entrada, Quantidade = i.QtdRecebida, Bloqueado = bloqueado,
                CustoUnitario = i.PedidoItemId is { } pi && custos.TryGetValue(pi, out var c) ? c : 0, Documento = $"Recebimento {r.Numero}" + (bloqueado ? " · quarentena" : ""),
                OrigemTipo = "Recebimento", OrigemId = r.Id, Lote = i.Lote, FornecedorId = r.FornecedorId
            }, ct);
    }

    /// <summary>
    /// Lote do recebimento mudou de situação (ensaio aprovado/reprovado): libera o bloqueado ou baixa como devolvido ao fornecedor.
    /// </summary>
    public static async Task AjustarLoteAsync(IQualyHubDb db, Recebimento r, DateTimeOffset agora, CancellationToken ct = default)
    {
        if (r.AlmoxarifadoId is not { } almox) return;
        var movs = await db.MovimentosEstoque.AsNoTracking().Where(m => m.OrigemTipo == "Recebimento" && m.OrigemId == r.Id).ToListAsync(ct);
        var bloqueadoPorProduto = movs.Where(m => m.Bloqueado || m.Tipo == TipoMovimentoEstoque.Liberacao)
            .GroupBy(m => m.ProdutoId).ToDictionary(g => g.Key, g => g.Where(m => m.Bloqueado).Sum(m => m.Quantidade) - g.Where(m => m.Tipo == TipoMovimentoEstoque.Liberacao).Sum(m => Math.Abs(m.Quantidade)));
        var entra = RegrasEstoque.EntraBloqueado(r.StatusLote);
        foreach (var (produto, q) in bloqueadoPorProduto.Where(x => x.Value > 0))
        {
            if (entra == false)
                await MovimentarAsync(db, new MovimentoEstoque { AlmoxarifadoId = almox, ProdutoId = produto, Data = agora, Tipo = TipoMovimentoEstoque.Liberacao, Quantidade = q,
                    Documento = $"Recebimento {r.Numero} · lote liberado", OrigemTipo = "Recebimento", OrigemId = r.Id }, ct);
            else if (entra is null)
                await MovimentarAsync(db, new MovimentoEstoque { AlmoxarifadoId = almox, ProdutoId = produto, Data = agora, Tipo = TipoMovimentoEstoque.Perda, Quantidade = -q, Bloqueado = true,
                    Documento = $"Recebimento {r.Numero} · lote reprovado devolvido", OrigemTipo = "Recebimento", OrigemId = r.Id, Observacao = "Ensaio reprovado: devolução ao fornecedor" }, ct);
        }
    }

    /// <summary>Remove as restrições do Last Planner abertas pelo documento (transferência conferida, OS verificada).</summary>
    public static async Task RemoverRestricoesAsync(IQualyHubDb db, Guid origemId, string como, string usuario, DateTimeOffset agora, CancellationToken ct = default)
    {
        foreach (var r in await db.Restricoes.Where(r => r.OrigemId == origemId && r.Status == StatusRestricao.Aberta).ToListAsync(ct))
            r.Remover(como, usuario, agora);
    }
}

/// <summary>Estoque: saldos, saídas, mínimos, inventário, kits, solicitações de transferência e restrições.</summary>
public sealed class EstoqueServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── Almoxarifados ─────────

    /// <summary>Garante o central (se houver estabelecimento marcado) e o almoxarifado de cada obra ativa.</summary>
    public async Task<List<Almoxarifado>> AlmoxarifadosAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        await MotorEstoque.CentralAsync(db, ct);
        var obras = await db.Obras.AsNoTracking().Select(o => o.Id).ToListAsync(ct);
        foreach (var o in obras) await MotorEstoque.DaObraAsync(db, o, ct);
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(ct);
        return await db.Almoxarifados.AsNoTracking().OrderBy(a => a.Tipo).ThenBy(a => a.Nome).ToListAsync(ct);
    }

    public async Task SalvarAlmoxarifadoAsync(Almoxarifado a, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var atual = await db.Almoxarifados.FirstAsync(x => x.Id == a.Id, ct);
        if (string.IsNullOrWhiteSpace(a.Nome)) throw new RegraDeNegocioException("Informe o nome.");
        atual.Nome = a.Nome.Trim(); atual.Endereco = a.Endereco; atual.Ativo = a.Ativo;
        await db.SaveChangesAsync(ct);
    }

    // ───────── Saídas, devoluções e perdas ─────────

    public sealed record SaidaInfo(Guid AlmoxarifadoId, Guid ProdutoId, decimal Quantidade, TipoMovimentoEstoque Tipo, Guid? LocalId, Guid? ComposicaoId,
        Guid? ColaboradorId, Guid? FornecedorId, string? EntreguePara, string? Observacao);

    public async Task<Guid> LancarAsync(SaidaInfo s, CancellationToken ct = default)
    {
        if (s.Quantidade <= 0) throw new RegraDeNegocioException("Informe a quantidade.");
        if (s.Tipo is not (TipoMovimentoEstoque.Saida or TipoMovimentoEstoque.Devolucao or TipoMovimentoEstoque.Perda))
            throw new RegraDeNegocioException("Lançamento manual aceita saída, devolução ou perda.");
        if (s.Tipo == TipoMovimentoEstoque.Perda && string.IsNullOrWhiteSpace(s.Observacao)) throw new RegraDeNegocioException("Perda/avaria: descreva o motivo.");
        if (s.Tipo == TipoMovimentoEstoque.Saida && s.LocalId is null && s.ColaboradorId is null && string.IsNullOrWhiteSpace(s.EntreguePara))
            throw new RegraDeNegocioException("Saída: informe o local de aplicação ou para quem foi entregue.");
        await using var db = fabrica.Criar();
        var m = await MotorEstoque.MovimentarAsync(db, new MovimentoEstoque
        {
            AlmoxarifadoId = s.AlmoxarifadoId, ProdutoId = s.ProdutoId, Data = relogio.GetUtcNow(), Tipo = s.Tipo,
            Quantidade = s.Tipo == TipoMovimentoEstoque.Devolucao ? s.Quantidade : -s.Quantidade, LocalId = s.LocalId, ComposicaoId = s.ComposicaoId,
            ColaboradorId = s.ColaboradorId, FornecedorId = s.FornecedorId, EntreguePara = s.EntreguePara, Observacao = s.Observacao,
            Documento = s.Tipo switch { TipoMovimentoEstoque.Devolucao => "Devolução de sobra", TipoMovimentoEstoque.Perda => "Perda / avaria", _ => "Saída avulsa" }
        }, ct);
        await db.SaveChangesAsync(ct);
        return m.Id;
    }

    /// <summary>
    /// Estorna um lançamento manual (saída, devolução ou perda) com o movimento inverso — o kardex não apaga nada.
    /// Cada lançamento só pode ser estornado uma vez.
    /// </summary>
    public async Task EstornarAsync(Guid movimentoId, string motivo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new RegraDeNegocioException("Informe o motivo do estorno.");
        await using var db = fabrica.Criar();
        var m = await db.MovimentosEstoque.AsNoTracking().FirstAsync(x => x.Id == movimentoId, ct);
        if (m.Tipo is not (TipoMovimentoEstoque.Saida or TipoMovimentoEstoque.Devolucao or TipoMovimentoEstoque.Perda or TipoMovimentoEstoque.EntregaEpi))
            throw new RegraDeNegocioException("Só saída, devolução, perda ou entrega de EPI são estornadas aqui. Recebimento, transferência e inventário têm fluxo próprio (cancelar/ajustar).");
        if (m.OrigemTipo == "Estorno") throw new RegraDeNegocioException("Este movimento já é um estorno.");
        if (await db.MovimentosEstoque.AnyAsync(x => x.OrigemTipo == "Estorno" && x.OrigemId == m.Id, ct)) throw new RegraDeNegocioException("Lançamento já estornado.");
        await MotorEstoque.MovimentarAsync(db, new MovimentoEstoque
        {
            AlmoxarifadoId = m.AlmoxarifadoId, ProdutoId = m.ProdutoId, Data = relogio.GetUtcNow(), Tipo = m.Quantidade < 0 ? TipoMovimentoEstoque.Devolucao : TipoMovimentoEstoque.Saida,
            Quantidade = -m.Quantidade, CustoUnitario = m.CustoUnitario, LocalId = m.LocalId, ComposicaoId = m.ComposicaoId, KitId = m.KitId,
            ColaboradorId = m.ColaboradorId, FornecedorId = m.FornecedorId, EntreguePara = m.EntreguePara,
            Documento = $"Estorno · {m.Documento}", OrigemTipo = "Estorno", OrigemId = m.Id, Observacao = motivo.Trim()
        }, ct);
        await db.SaveChangesAsync(ct);
    }

    // ───────── Mínimos ─────────

    public async Task SalvarMinimosAsync(Guid almoxarifadoId, Guid produtoId, decimal minimo, decimal pontoPedido, decimal maximo, CancellationToken ct = default)
    {
        if (minimo < 0 || pontoPedido < 0 || maximo < 0) throw new RegraDeNegocioException("Mínimo, ponto de pedido e máximo não podem ser negativos.");
        if (pontoPedido > 0 && pontoPedido < minimo) throw new RegraDeNegocioException("O ponto de pedido deve ser maior ou igual ao mínimo.");
        if (maximo > 0 && maximo < pontoPedido) throw new RegraDeNegocioException("O máximo deve ser maior ou igual ao ponto de pedido.");
        await using var db = fabrica.Criar();
        var s = await MotorEstoque.SaldoAsync(db, almoxarifadoId, produtoId, ct);
        s.Minimo = minimo; s.PontoPedido = pontoPedido; s.Maximo = maximo;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Sugere mínimo/ponto de pedido/máximo pelo consumo dos últimos 60 dias × prazo de entrega do produto (só preenche quem está zerado).</summary>
    public async Task<int> SugerirMinimosAsync(Guid almoxarifadoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var desde = relogio.GetUtcNow().AddDays(-60);
        var consumo = await db.MovimentosEstoque.AsNoTracking()
            .Where(m => m.AlmoxarifadoId == almoxarifadoId && m.Data >= desde && m.Quantidade < 0 &&
                        (m.Tipo == TipoMovimentoEstoque.Saida || m.Tipo == TipoMovimentoEstoque.EntregaEpi || m.Tipo == TipoMovimentoEstoque.TransferenciaSaida))
            .GroupBy(m => m.ProdutoId).Select(g => new { g.Key, Q = -g.Sum(m => m.Quantidade) }).ToListAsync(ct);
        var ids = consumo.Select(c => c.Key).ToList();
        var lead = await db.Produtos.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.LeadTimeDias, ct);
        var n = 0;
        foreach (var c in consumo)
        {
            var s = await MotorEstoque.SaldoAsync(db, almoxarifadoId, c.Key, ct);
            if (s.Minimo > 0 || s.PontoPedido > 0) continue;
            (s.Minimo, s.PontoPedido, s.Maximo) = RegrasEstoque.Sugerir(c.Q, 60, lead.GetValueOrDefault(c.Key, 7));
            n++;
        }
        await db.SaveChangesAsync(ct);
        return n;
    }

    // ───────── Solicitação de transferência (reposição, kit, requisição atendida pelo estoque) ─────────

    public sealed record ItemSolicitado(Guid ProdutoId, decimal Quantidade);

    /// <summary>Cria a transferência (rascunho = solicitada) entre dois almoxarifados quaisquer.</summary>
    public async Task<Guid> SolicitarTransferenciaAsync(Guid origemId, Guid destinoId, IReadOnlyList<ItemSolicitado> itens, string? observacao, CancellationToken ct = default)
    {
        if (origemId == destinoId) throw new RegraDeNegocioException("Origem e destino devem ser diferentes.");
        if (itens.Count == 0 || itens.Any(i => i.Quantidade <= 0)) throw new RegraDeNegocioException("Informe os itens e quantidades.");
        await using var db = fabrica.Criar();
        var alms = await db.Almoxarifados.AsNoTracking().Where(a => a.Id == origemId || a.Id == destinoId).ToDictionaryAsync(a => a.Id, ct);
        if (alms.Count != 2) throw new RegraDeNegocioException("Almoxarifado não encontrado.");
        var obraId = alms[destinoId].ObraId ?? alms[origemId].ObraId ?? throw new RegraDeNegocioException("Transferência entre centrais não é suportada.");
        var ids = itens.Select(i => i.ProdutoId).ToList();
        var prods = await db.Produtos.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var saldos = await db.SaldosEstoque.AsNoTracking().Where(s => s.AlmoxarifadoId == origemId && ids.Contains(s.ProdutoId)).ToDictionaryAsync(s => s.ProdutoId, ct);
        var t = new TransferenciaMaterial
        {
            ObraId = obraId, OrigemAlmoxarifadoId = origemId, DestinoAlmoxarifadoId = destinoId, Data = Hoje,
            OrigemEstabelecimentoId = alms[origemId].EstabelecimentoId, Numero = await Numerador.ProximoAsync(db, "Transferencia", ct),
            SpeId = alms[destinoId].ObraId is { } od ? await db.Spes.AsNoTracking().Where(s => s.ObraId == od).Select(s => (Guid?)s.Id).FirstOrDefaultAsync(ct) : null,
            Observacao = observacao
        };
        foreach (var i in itens)
        {
            var p = prods[i.ProdutoId];
            t.Itens.Add(new TransferenciaItem { TransferenciaId = t.Id, ProdutoId = p.Id, Descricao = p.Nome, Unidade = p.Unidade, Quantidade = i.Quantidade,
                CustoUnitario = saldos.TryGetValue(p.Id, out var s) ? s.CustoMedio : p.PrecoReferencia ?? 0 });
        }
        db.Transferencias.Add(t);
        await db.SaveChangesAsync(ct);
        return t.Id;
    }

    /// <summary>Restrição de material no Last Planner até a transferência ser conferida (removida sozinha na conferência).</summary>
    public async Task<int> CriarRestricaoAsync(Guid obraId, Guid origemId, string atividade, string descricao, DateOnly dataLimite, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var r = new Restricao
        {
            ObraId = obraId, Tipo = TipoRestricao.Material, Descricao = descricao, AtividadeChave = $"EST:{origemId:N}", AtividadeNome = atividade,
            ResponsavelId = usuario.UsuarioId, ResponsavelNome = string.IsNullOrWhiteSpace(usuario.Nome) ? "Almoxarifado" : usuario.Nome, DataLimite = dataLimite, OrigemId = origemId,
            Numero = await Numerador.ProximoAsync(db, "Restricao", ct)
        };
        ValidacaoException.Se(r.Validar());
        db.Restricoes.Add(r);
        await db.SaveChangesAsync(ct);
        return r.Numero;
    }

    // ───────── Inventário ─────────

    /// <summary>Abre o inventário. Implantação começa vazio (o almoxarife inclui o que encontrar); cíclico/geral traz os itens com saldo.</summary>
    public async Task<Guid> AbrirInventarioAsync(Guid almoxarifadoId, TipoInventario tipo, IReadOnlyList<Guid>? produtos, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (await db.Inventarios.AnyAsync(i => i.AlmoxarifadoId == almoxarifadoId && (i.Status == StatusInventario.EmContagem || i.Status == StatusInventario.AguardandoAprovacao), ct))
            throw new RegraDeNegocioException("Já há um inventário aberto neste almoxarifado.");
        var inv = new InventarioEstoque { AlmoxarifadoId = almoxarifadoId, Tipo = tipo, Data = Hoje, Numero = await Numerador.ProximoAsync(db, "Inventario", ct) };
        if (tipo != TipoInventario.Implantacao)
        {
            var q = db.SaldosEstoque.AsNoTracking().Where(s => s.AlmoxarifadoId == almoxarifadoId && (s.Disponivel != 0 || s.Bloqueado != 0));
            if (produtos is { Count: > 0 }) q = q.Where(s => produtos.Contains(s.ProdutoId));
            foreach (var s in await q.ToListAsync(ct)) inv.Itens.Add(new InventarioItem { InventarioId = inv.Id, ProdutoId = s.ProdutoId, SaldoSistema = s.Disponivel });
        }
        db.Inventarios.Add(inv);
        await db.SaveChangesAsync(ct);
        return inv.Id;
    }

    public sealed record Contagem(Guid ProdutoId, decimal? Contado, decimal? CustoUnitario, string? Observacao);

    public async Task SalvarContagemAsync(Guid inventarioId, IReadOnlyList<Contagem> itens, bool enviarParaAprovacao, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var inv = await db.Inventarios.Include(i => i.Itens).FirstAsync(i => i.Id == inventarioId, ct);
        if (inv.Status != StatusInventario.EmContagem) throw new RegraDeNegocioException("Inventário não está em contagem.");
        foreach (var c in itens)
        {
            if (c.Contado < 0) throw new RegraDeNegocioException("Quantidade contada não pode ser negativa.");
            var it = inv.Itens.FirstOrDefault(i => i.ProdutoId == c.ProdutoId);
            if (it is null)
            {
                var saldo = await db.SaldosEstoque.AsNoTracking().Where(s => s.AlmoxarifadoId == inv.AlmoxarifadoId && s.ProdutoId == c.ProdutoId).Select(s => (decimal?)s.Disponivel).FirstOrDefaultAsync(ct) ?? 0;
                it = new InventarioItem { InventarioId = inv.Id, ProdutoId = c.ProdutoId, SaldoSistema = saldo };
                inv.Itens.Add(it); db.InventarioItens.Add(it);
            }
            it.Contado = c.Contado; it.CustoUnitario = c.CustoUnitario; it.Observacao = c.Observacao;
        }
        if (enviarParaAprovacao)
        {
            if (inv.Itens.Count == 0 || inv.Itens.Any(i => i.Contado is null)) throw new RegraDeNegocioException("Conte todos os itens antes de enviar.");
            if (inv.Tipo == TipoInventario.Implantacao && inv.Itens.Any(i => i.Contado > 0 && i.CustoUnitario is not > 0))
                throw new RegraDeNegocioException("Implantação: informe o custo unitário de cada item (vira o custo médio inicial).");
            if (inv.Itens.Any(i => i.Diferenca != 0 && string.IsNullOrWhiteSpace(i.Observacao) && inv.Tipo != TipoInventario.Implantacao))
                throw new RegraDeNegocioException("Diferença entre o contado e o sistema: descreva o motivo em cada item.");
            inv.Status = StatusInventario.AguardandoAprovacao;
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Tira um item da contagem (incluído por engano) enquanto o inventário está em contagem.</summary>
    public async Task RemoverItemInventarioAsync(Guid inventarioId, Guid produtoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var inv = await db.Inventarios.Include(i => i.Itens).FirstAsync(i => i.Id == inventarioId, ct);
        if (inv.Status != StatusInventario.EmContagem) throw new RegraDeNegocioException("Inventário não está em contagem.");
        foreach (var it in inv.Itens.Where(i => i.ProdutoId == produtoId).ToList()) { inv.Itens.Remove(it); db.InventarioItens.Remove(it); }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Aprova: cada diferença vira ajuste (ou implantação) no estoque.</summary>
    public async Task AprovarInventarioAsync(Guid inventarioId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var inv = await db.Inventarios.Include(i => i.Itens).FirstAsync(i => i.Id == inventarioId, ct);
        if (inv.Status != StatusInventario.AguardandoAprovacao) throw new RegraDeNegocioException("Inventário não está aguardando aprovação.");
        var agora = relogio.GetUtcNow();
        foreach (var it in inv.Itens)
        {
            var s = await MotorEstoque.SaldoAsync(db, inv.AlmoxarifadoId, it.ProdutoId, ct);
            var dif = (it.Contado ?? s.Disponivel) - s.Disponivel; // compara com o saldo de agora (pode ter havido movimento durante a contagem)
            if (dif == 0) continue;
            await MotorEstoque.MovimentarAsync(db, new MovimentoEstoque
            {
                AlmoxarifadoId = inv.AlmoxarifadoId, ProdutoId = it.ProdutoId, Data = agora, Quantidade = dif,
                Tipo = inv.Tipo == TipoInventario.Implantacao ? TipoMovimentoEstoque.Implantacao : TipoMovimentoEstoque.AjusteInventario,
                CustoUnitario = it.CustoUnitario ?? 0, Documento = $"Inventário {inv.Numero}", OrigemTipo = "Inventario", OrigemId = inv.Id, Observacao = it.Observacao
            }, ct);
        }
        inv.Status = StatusInventario.Aprovado; inv.AprovadoPor = usuario.Nome; inv.AprovadoEm = agora;
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelarInventarioAsync(Guid inventarioId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var inv = await db.Inventarios.FirstAsync(i => i.Id == inventarioId, ct);
        if (inv.Status is StatusInventario.Aprovado or StatusInventario.Cancelado) throw new RegraDeNegocioException("Inventário já encerrado.");
        inv.Status = StatusInventario.Cancelado;
        await db.SaveChangesAsync(ct);
    }

    // ───────── Kits ─────────

    public async Task<Guid> SalvarKitAsync(KitMaterial k, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (string.IsNullOrWhiteSpace(k.Codigo)) k.Codigo = $"KIT-{await Numerador.ProximoAsync(db, "Kit", ct):000}";
        foreach (var i in k.Itens) i.KitId = k.Id;
        if (k.Origem == OrigemKit.Automatico) k.Itens.Clear();
        ValidacaoException.Se(k.Validar());
        if (await db.KitsMaterial.AnyAsync(x => x.Codigo == k.Codigo && x.Id != k.Id, ct)) throw new RegraDeNegocioException($"Código {k.Codigo} já existe.");
        await Grafo.SalvarAsync(db, k, nameof(KitMaterial.Itens));
        await db.SaveChangesAsync(ct);
        return k.Id;
    }

    /// <summary>Apartamento + cômodos (os serviços do orçamento são lançados nos dois níveis).</summary>
    private static async Task<List<Guid>> LocaisDoAptoAsync(IQualyHubDb db, Guid localId, CancellationToken ct)
    {
        var ids = new List<Guid> { localId };
        var nivel = new List<Guid> { localId };
        for (var i = 0; i < 3 && nivel.Count > 0; i++)
        {
            nivel = await db.Locais.AsNoTracking().Where(l => l.LocalPaiId != null && nivel.Contains(l.LocalPaiId.Value)).Select(l => l.Id).ToListAsync(ct);
            ids.AddRange(nivel);
        }
        return ids;
    }

    /// <summary>
    /// Material do kit para um apartamento. Automático: insumos de material (com produto) dos serviços da etapa/disciplina
    /// lançados no Orçamento por Local do apartamento e dos cômodos. Manual: a lista do kit.
    /// </summary>
    public static async Task<List<NecessidadeMaterial>> NecessidadeAsync(IQualyHubDb db, KitMaterial k, Guid localId, CancellationToken ct = default)
    {
        if (k.Origem == OrigemKit.Manual)
        {
            var ids = k.Itens.Select(i => i.ProdutoId).ToList();
            var precos = await db.Produtos.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.PrecoReferencia ?? 0, ct);
            return [.. k.Itens.Select(i => new NecessidadeMaterial(i.ProdutoId, null, i.Quantidade, Math.Round(i.Quantidade * precos.GetValueOrDefault(i.ProdutoId), 2)))];
        }
        var locais = await LocaisDoAptoAsync(db, localId, ct);
        var q = from sl in db.ServicosLocais.AsNoTracking()
                join c in db.Composicoes.AsNoTracking() on sl.ComposicaoId equals c.Id
                where locais.Contains(sl.LocalId) && sl.Quantidade > 0
                select new { sl.ComposicaoId, sl.Quantidade, c.Grupo, c.Disciplina };
        if (!string.IsNullOrWhiteSpace(k.Etapa)) q = q.Where(x => x.Grupo == k.Etapa);
        if (!string.IsNullOrWhiteSpace(k.Disciplina)) q = q.Where(x => x.Disciplina == k.Disciplina);
        var servicos = await q.ToListAsync(ct);
        var compIds = servicos.Select(s => s.ComposicaoId).Distinct().ToList();
        var itens = await db.Set<ComposicaoItem>().AsNoTracking().Where(i => compIds.Contains(i.ComposicaoId) && i.Tipo == TipoInsumo.Material && i.ProdutoId != null).ToListAsync(ct);
        var r = new List<NecessidadeMaterial>();
        foreach (var s in servicos)
            foreach (var i in itens.Where(i => i.ComposicaoId == s.ComposicaoId))
            {
                var qtd = i.QuantidadePor(s.Quantidade);
                if (qtd > 0) r.Add(new NecessidadeMaterial(i.ProdutoId!.Value, s.ComposicaoId, qtd, Math.Round(qtd * i.CustoUnitario, 2)));
            }
        // Junta o mesmo produto do mesmo serviço (vários cômodos).
        return [.. r.GroupBy(x => (x.ProdutoId, x.ComposicaoId)).Select(g => new NecessidadeMaterial(g.Key.ProdutoId, g.Key.ComposicaoId, Math.Round(g.Sum(x => x.Quantidade), 4), g.Sum(x => x.CustoOrcado)))];
    }

    public async Task<List<NecessidadeMaterial>> NecessidadeAsync(Guid kitId, IReadOnlyList<Guid> locais, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var k = await db.KitsMaterial.AsNoTracking().Include(x => x.Itens).FirstAsync(x => x.Id == kitId, ct);
        var r = new List<NecessidadeMaterial>();
        foreach (var l in locais) r.AddRange(await NecessidadeAsync(db, k, l, ct));
        return r;
    }

    /// <summary>
    /// Entrega o kit para o apartamento: baixa cada insumo no almoxarifado com o apartamento e o serviço (consumo real × orçado).
    /// Quantidades podem ser ajustadas na entrega (ex.: entrega parcial).
    /// </summary>
    public async Task<int> EntregarKitAsync(Guid kitId, Guid almoxarifadoId, Guid localId, string entreguePara, Guid? fornecedorId,
        IReadOnlyDictionary<Guid, decimal>? ajustes, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entreguePara)) throw new RegraDeNegocioException("Informe quem retirou o kit.");
        await using var db = fabrica.Criar();
        var k = await db.KitsMaterial.AsNoTracking().Include(x => x.Itens).FirstAsync(x => x.Id == kitId, ct);
        var nec = await NecessidadeAsync(db, k, localId, ct);
        if (nec.Count == 0) throw new RegraDeNegocioException("O kit não tem material para este apartamento (confira o orçamento por local e os produtos dos insumos).");
        var local = await db.Locais.AsNoTracking().Where(l => l.Id == localId).Select(l => l.Nome).FirstAsync(ct);
        var agora = relogio.GetUtcNow();
        var totalPorProduto = nec.GroupBy(n => n.ProdutoId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantidade));
        var n = 0;
        foreach (var x in nec)
        {
            var q = x.Quantidade;
            if (ajustes is not null && ajustes.TryGetValue(x.ProdutoId, out var aj)) q = totalPorProduto[x.ProdutoId] == 0 ? 0 : Math.Round(aj * x.Quantidade / totalPorProduto[x.ProdutoId], 4);
            if (q <= 0) continue;
            await MotorEstoque.MovimentarAsync(db, new MovimentoEstoque
            {
                AlmoxarifadoId = almoxarifadoId, ProdutoId = x.ProdutoId, Data = agora, Tipo = TipoMovimentoEstoque.Saida, Quantidade = -q, LocalId = localId,
                ComposicaoId = x.ComposicaoId, KitId = k.Id, FornecedorId = fornecedorId, EntreguePara = entreguePara.Trim(), Documento = $"Kit {k.Codigo} · {local}",
                OrigemTipo = "Kit", OrigemId = k.Id
            }, ct);
            n++;
        }
        await db.SaveChangesAsync(ct);
        return n;
    }

    public sealed record ConsumoLocal(Guid LocalId, string Local, decimal Orcado, decimal Real, decimal DiferencaPerc);

    /// <summary>Consumo real (saídas com o apartamento, menos devoluções) × orçado (material do kit no orçamento) por apartamento.</summary>
    public async Task<List<ConsumoLocal>> ConsumoAsync(Guid kitId, IReadOnlyList<Guid> locais, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var k = await db.KitsMaterial.AsNoTracking().Include(x => x.Itens).FirstAsync(x => x.Id == kitId, ct);
        var nomes = await db.Locais.AsNoTracking().Where(l => locais.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.Nome, ct);
        var r = new List<ConsumoLocal>();
        foreach (var l in locais)
        {
            var nec = await NecessidadeAsync(db, k, l, ct);
            var produtos = nec.Select(x => x.ProdutoId).Distinct().ToList();
            var real = -await db.MovimentosEstoque.AsNoTracking()
                .Where(m => m.LocalId == l && produtos.Contains(m.ProdutoId) && (m.Tipo == TipoMovimentoEstoque.Saida || m.Tipo == TipoMovimentoEstoque.Devolucao))
                .SumAsync(m => (decimal?)m.Valor, ct) ?? 0;
            var orc = nec.Sum(x => x.CustoOrcado);
            r.Add(new ConsumoLocal(l, nomes.GetValueOrDefault(l, ""), orc, real, orc == 0 ? 0 : Math.Round((real / orc - 1) * 100, 1)));
        }
        return r;
    }
}
