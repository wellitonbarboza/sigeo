using Microsoft.EntityFrameworkCore;
using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Common;
using Groundkey.Domain.Engenharia;
using Groundkey.Domain.Suprimentos;

namespace Groundkey.Application.Servicos;

public sealed record LinhaCustoRealizado(Guid Id, DateOnly Data, TipoCusto Tipo, string Fase, Guid? LocalId, string? Local, string? Fornecedor,
    string Descricao, string? Unidade, decimal Quantidade, decimal Valor, string? Documento, OrigemCusto Origem, bool Historico, string? Observacao, string? Pedido);

public sealed record LinhaOrcadoRealizado(string Fase, decimal Orcado, decimal Realizado)
{
    public decimal Saldo => Orcado - Realizado;
    public decimal? Perc => Orcado == 0 ? null : Math.Round(Realizado / Orcado * 100, 1);
}

public sealed record PainelCusto(decimal Total, decimal Historico, decimal AposCorte, decimal AApropriar, DateOnly? Corte,
    IReadOnlyList<LinhaOrcadoRealizado> PorFase, IReadOnlyDictionary<TipoCusto, decimal> PorTipo, IReadOnlyList<(DateOnly Mes, decimal Valor)> PorMes);

/// <summary>
/// Custo realizado da obra por fase (grupo de serviço) × local. Gerado pelo recebimento (material ao preço do pedido),
/// pela medição aprovada (serviço), pela importação do ERP ou lançado à mão. Base do orçado × realizado.
/// </summary>
public sealed class CustoRealizadoServico(IFabricaDb fabrica)
{
    public async Task<List<LinhaCustoRealizado>> ListarAsync(Guid obraId, DateOnly? de, DateOnly? ate, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var q = db.CustosRealizados.Where(c => c.ObraId == obraId);
        if (de is { } d) q = q.Where(c => c.Data >= d);
        if (ate is { } a) q = q.Where(c => c.Data <= a);
        var lista = await q.OrderByDescending(c => c.Data).ThenBy(c => c.Descricao).ToListAsync(ct);
        var locais = await db.Locais.Where(l => l.ObraId == obraId).ToDictionaryAsync(l => l.Id, l => l.Nome, ct);
        var pedIds = lista.Where(c => c.PedidoId != null).Select(c => c.PedidoId!.Value).Distinct().ToList();
        var peds = await db.Pedidos.IgnoreQueryFilters().Where(p => pedIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.CodigoErp ?? p.Numero.ToString(), ct);
        return lista.Select(c => new LinhaCustoRealizado(c.Id, c.Data, c.TipoCusto, c.Fase, c.LocalId, c.LocalId is { } l ? locais.GetValueOrDefault(l) : null,
            c.FornecedorNome, c.Descricao, c.Unidade, c.Quantidade, c.Valor,
            string.Join(" ", new[] { c.DocumentoTipo, c.DocumentoNumero }.Where(x => !string.IsNullOrWhiteSpace(x))) is { Length: > 0 } doc ? doc : null,
            c.Origem, c.Historico, c.Observacao, c.PedidoId is { } p ? peds.GetValueOrDefault(p) : null)).ToList();
    }

    public async Task<CustoRealizado?> ObterAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.CustosRealizados.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    /// <summary>
    /// Lançamento manual (novo ou edição). Lançamento vindo de recebimento/medição/ERP só muda fase, local, tipo e observação:
    /// valor e quantidade pertencem ao documento de origem.
    /// </summary>
    public async Task SalvarAsync(CustoRealizado c, CancellationToken ct = default)
    {
        c.Descricao = (c.Descricao ?? "").Trim();
        c.Fase = string.IsNullOrWhiteSpace(c.Fase) ? CustoRealizado.FaseAApropriar : c.Fase.Trim();
        await using var db = fabrica.Criar();
        var atual = await db.CustosRealizados.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (atual is not null && atual.ValorTravado)
        {
            atual.Fase = c.Fase; atual.LocalId = c.LocalId; atual.TipoCusto = c.TipoCusto; atual.Observacao = c.Observacao;
            await db.SaveChangesAsync(ct);
            return;
        }
        ValidacaoException.Se(c.Validar());
        var corte = await db.Obras.Where(o => o.Id == c.ObraId).Select(o => o.DataCorteSistema).FirstOrDefaultAsync(ct);
        c.Historico = corte is { } dc && c.Data < dc;
        c.Origem = OrigemCusto.Manual;
        if (c.FornecedorId is { } fid)
            c.FornecedorNome = await db.Fornecedores.Where(f => f.Id == fid).Select(f => f.NomeFantasia ?? f.RazaoSocial).FirstOrDefaultAsync(ct);
        if (atual is null) db.CustosRealizados.Add(c);
        else
        {
            atual.Data = c.Data; atual.TipoCusto = c.TipoCusto; atual.Fase = c.Fase; atual.LocalId = c.LocalId; atual.FornecedorId = c.FornecedorId;
            atual.FornecedorNome = c.FornecedorNome; atual.ProdutoId = c.ProdutoId; atual.Descricao = c.Descricao; atual.Unidade = c.Unidade;
            atual.Quantidade = c.Quantidade; atual.Valor = c.Valor; atual.DocumentoTipo = c.DocumentoTipo; atual.DocumentoNumero = c.DocumentoNumero;
            atual.Observacao = c.Observacao; atual.Historico = c.Historico;
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Reclassifica vários lançamentos de uma vez (fase e/ou local). <paramref name="semLocal"/> = passa para "obra toda".</summary>
    public async Task<int> ReclassificarAsync(IReadOnlyCollection<Guid> ids, string? fase, Guid? localId, bool semLocal, CancellationToken ct = default)
    {
        if (ids.Count == 0) return 0;
        if (string.IsNullOrWhiteSpace(fase) && localId is null && !semLocal) throw new RegraDeNegocioException("Escolha a fase e/ou o local para reclassificar.");
        await using var db = fabrica.Criar();
        var lista = await db.CustosRealizados.Where(c => ids.Contains(c.Id)).ToListAsync(ct);
        foreach (var c in lista)
        {
            if (!string.IsNullOrWhiteSpace(fase)) c.Fase = fase.Trim();
            if (semLocal) c.LocalId = null;
            else if (localId is not null) c.LocalId = localId;
        }
        await db.SaveChangesAsync(ct);
        return lista.Count;
    }

    /// <summary>Orçado (matriz serviço × local, pelo grupo da composição) × realizado, por fase; totais por tipo e por mês.</summary>
    public async Task<PainelCusto> PainelAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var corte = await db.Obras.Where(o => o.Id == obraId).Select(o => o.DataCorteSistema).FirstOrDefaultAsync(ct);
        var real = await db.CustosRealizados.Where(c => c.ObraId == obraId)
            .Select(c => new { c.Fase, c.TipoCusto, c.Valor, c.Data, c.Historico }).ToListAsync(ct);
        var orc = await (from s in db.ServicosLocais
                         join k in db.Composicoes on s.ComposicaoId equals k.Id
                         where s.ObraId == obraId
                         group s by k.Grupo into g
                         select new { Fase = g.Key, Valor = g.Sum(x => x.Custo) }).ToListAsync(ct);
        var fases = orc.Select(o => o.Fase ?? "Sem grupo").Union(real.Select(r => r.Fase)).Distinct();
        var porFase = fases.Select(f => new LinhaOrcadoRealizado(f,
                orc.Where(o => (o.Fase ?? "Sem grupo") == f).Sum(o => o.Valor), real.Where(r => r.Fase == f).Sum(r => r.Valor)))
            .OrderByDescending(l => Math.Max(l.Orcado, l.Realizado)).ToList();
        return new PainelCusto(real.Sum(r => r.Valor), real.Where(r => r.Historico).Sum(r => r.Valor), real.Where(r => !r.Historico).Sum(r => r.Valor),
            real.Where(r => r.Fase == CustoRealizado.FaseAApropriar).Sum(r => r.Valor), corte, porFase,
            real.GroupBy(r => r.TipoCusto).ToDictionary(g => g.Key, g => g.Sum(x => x.Valor)),
            real.GroupBy(r => new DateOnly(r.Data.Year, r.Data.Month, 1)).OrderBy(g => g.Key).Select(g => (g.Key, g.Sum(x => x.Valor))).ToList());
    }

    /// <summary>Grava a data de corte da obra (antes dela = histórico do ERP).</summary>
    public async Task DefinirCorteAsync(Guid obraId, DateOnly? corte, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var o = await db.Obras.FirstAsync(x => x.Id == obraId, ct);
        o.DataCorteSistema = corte;
        if (corte is { } dc)
            await db.CustosRealizados.Where(c => c.ObraId == obraId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Historico, c => c.Data < dc), ct);
        await db.SaveChangesAsync(ct);
    }

    // ───────────── Geração automática ─────────────

    /// <summary>
    /// Material recebido vira custo realizado ao preço do pedido. Fase = grupo da composição da requisição, senão a fase padrão do grupo
    /// do produto no catálogo, senão "A apropriar";
    /// locais = quebra por local da requisição (rateio pela quantidade). Idempotente (chave por item × local).
    /// </summary>
    public static async Task GerarDoRecebimentoAsync(IGroundkeyDb db, Recebimento r, CancellationToken ct = default)
    {
        if (r.PedidoId is not { } pedidoId || r.Destino == DestinoRecebimento.Devolvido) return;
        var ped = await db.Pedidos.AsNoTracking().Include(p => p.Itens).Include(p => p.Fornecedor).FirstAsync(p => p.Id == pedidoId, ct);
        var reqIds = ped.Itens.Where(i => i.RequisicaoItemId != null).Select(i => i.RequisicaoItemId!.Value).ToList();
        var reqItens = await db.Set<RequisicaoItem>().AsNoTracking().Include(i => i.Locais).Where(i => reqIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);
        var compIds = reqItens.Values.SelectMany(i => i.Locais.Select(l => l.ComposicaoId).Append(i.ComposicaoId)).Where(x => x != null).Select(x => x!.Value).Distinct().ToList();
        var grupos = await db.Composicoes.AsNoTracking().Where(k => compIds.Contains(k.Id)).ToDictionaryAsync(k => k.Id, k => k.Grupo, ct);
        var prodIds = ped.Itens.Where(i => i.ProdutoId != null).Select(i => i.ProdutoId!.Value).ToList();
        var prods = await db.Produtos.AsNoTracking().Where(p => prodIds.Contains(p.Id)).Select(p => new { p.Id, p.TipoCusto, p.Grupo }).ToListAsync(ct);
        // Padrão do grupo do catálogo: tipo de custo e fase do orçamento quando a compra não veio de composição.
        var nomesGrupo = prods.Where(p => p.Grupo != null).Select(p => p.Grupo!).Distinct().ToList();
        var padroes = (await db.CategoriasProduto.AsNoTracking().Where(c => c.PaiId == null && nomesGrupo.Contains(c.Nome))
            .Select(c => new { c.Nome, c.TipoCustoPadrao, c.FasePadrao }).ToListAsync(ct)).GroupBy(c => c.Nome.ToUpperInvariant()).ToDictionary(g => g.Key, g => g.First());
        var tipos = prods.ToDictionary(p => p.Id, p => p.TipoCusto ?? (p.Grupo is { } gn ? padroes.GetValueOrDefault(gn.ToUpperInvariant())?.TipoCustoPadrao : null));
        var fasesGrupo = prods.ToDictionary(p => p.Id, p => p.Grupo is { } gn ? padroes.GetValueOrDefault(gn.ToUpperInvariant())?.FasePadrao : null);
        var corte = await db.Obras.Where(o => o.Id == r.ObraId).Select(o => o.DataCorteSistema).FirstOrDefaultAsync(ct);
        var data = DateOnly.FromDateTime(r.DataRecebimento.Date);

        foreach (var ri in r.Itens.Where(i => i.PedidoItemId != null && i.QtdRecebida > 0))
        {
            var pi = ped.Itens.FirstOrDefault(i => i.Id == ri.PedidoItemId);
            if (pi is null) continue;
            var valor = Math.Round(ri.QtdRecebida * pi.ValorUnitario, 2);
            if (valor == 0) continue;
            var req = pi.RequisicaoItemId is { } rid ? reqItens.GetValueOrDefault(rid) : null;
            var compId = req?.ComposicaoId ?? req?.Locais.FirstOrDefault(l => l.ComposicaoId != null)?.ComposicaoId;
            var fase = compId is { } c && grupos.GetValueOrDefault(c) is { Length: > 0 } g ? g
                : pi.ProdutoId is { } pf && fasesGrupo.GetValueOrDefault(pf) is { Length: > 0 } fg ? fg : CustoRealizado.FaseAApropriar;
            var partes = RateioCusto.Ratear(ri.QtdRecebida, valor, req?.Locais.Select(l => (l.LocalId, l.Quantidade)).ToList() ?? []);
            foreach (var p in partes)
            {
                var chave = $"REC:{ri.Id:N}:{p.LocalId?.ToString("N") ?? "obra"}";
                if (await db.CustosRealizados.AnyAsync(x => x.ObraId == r.ObraId && x.ChaveOrigem == chave, ct)) continue;
                db.CustosRealizados.Add(new CustoRealizado
                {
                    ObraId = r.ObraId, Data = data, Historico = corte is { } dc && data < dc,
                    TipoCusto = pi.ProdutoId is { } prod && tipos.GetValueOrDefault(prod) is { } t ? t : TipoCusto.Material,
                    Fase = fase, LocalId = p.LocalId, FornecedorId = ped.FornecedorId, FornecedorNome = ped.Fornecedor.NomeFantasia ?? ped.Fornecedor.RazaoSocial,
                    ProdutoId = pi.ProdutoId, Descricao = pi.Descricao, Unidade = pi.Unidade, Quantidade = p.Quantidade, Valor = p.Valor,
                    DocumentoTipo = "NF", DocumentoNumero = r.NumeroNotaFiscal, PedidoId = ped.Id, RecebimentoId = r.Id,
                    Origem = OrigemCusto.Recebimento, ChaveOrigem = chave
                });
            }
        }
        // Frete do pedido: entra uma vez, no primeiro recebimento (fase do primeiro item).
        if (ped.ValorFrete is > 0 && !await db.CustosRealizados.AnyAsync(x => x.ObraId == r.ObraId && x.ChaveOrigem == $"FRETE:{ped.Id:N}", ct)
            && !db.CustosRealizados.Local.Any(x => x.ChaveOrigem == $"FRETE:{ped.Id:N}"))
        {
            var fase = db.CustosRealizados.Local.FirstOrDefault(x => x.RecebimentoId == r.Id)?.Fase ?? CustoRealizado.FaseAApropriar;
            db.CustosRealizados.Add(new CustoRealizado
            {
                ObraId = r.ObraId, Data = data, Historico = corte is { } dc2 && data < dc2, TipoCusto = TipoCusto.Frete, Fase = fase,
                FornecedorId = ped.FornecedorId, FornecedorNome = ped.Transportadora ?? ped.Fornecedor.NomeFantasia ?? ped.Fornecedor.RazaoSocial,
                Descricao = $"Frete do pedido {ped.Numero}", Quantidade = 1, Valor = ped.ValorFrete.Value, DocumentoTipo = "NF", DocumentoNumero = r.NumeroNotaFiscal,
                PedidoId = ped.Id, RecebimentoId = r.Id, Origem = OrigemCusto.Recebimento, ChaveOrigem = $"FRETE:{ped.Id:N}"
            });
        }
    }

    /// <summary>Medição aprovada vira custo de serviço por item × local (fase = grupo da composição contratada). Idempotente.</summary>
    public static async Task GerarDaMedicaoAsync(IGroundkeyDb db, Guid medicaoId, CancellationToken ct = default)
    {
        var m = await db.Medicoes.AsNoTracking().Include(x => x.Contrato).ThenInclude(k => k.Fornecedor)
            .Include(x => x.Itens).ThenInclude(i => i.ContratoItem).FirstAsync(x => x.Id == medicaoId, ct);
        var compIds = m.Itens.Where(i => i.ContratoItem.ComposicaoId != null).Select(i => i.ContratoItem.ComposicaoId!.Value).Distinct().ToList();
        var grupos = await db.Composicoes.AsNoTracking().Where(k => compIds.Contains(k.Id)).ToDictionaryAsync(k => k.Id, k => k.Grupo, ct);
        var corte = await db.Obras.Where(o => o.Id == m.ObraId).Select(o => o.DataCorteSistema).FirstOrDefaultAsync(ct);
        foreach (var i in m.Itens.Where(i => i.ValorPeriodo != 0))
        {
            var chave = $"MED:{i.Id:N}";
            if (await db.CustosRealizados.AnyAsync(x => x.ObraId == m.ObraId && x.ChaveOrigem == chave, ct)) continue;
            var compId = i.ContratoItem.ComposicaoId;
            db.CustosRealizados.Add(new CustoRealizado
            {
                ObraId = m.ObraId, Data = m.PeriodoFim, Historico = corte is { } dc && m.PeriodoFim < dc,
                TipoCusto = m.Contrato.Recorrente ? TipoCusto.Equipamento : TipoCusto.Servico,
                Fase = compId is { } c && grupos.GetValueOrDefault(c) is { Length: > 0 } g ? g : CustoRealizado.FaseAApropriar,
                LocalId = i.LocalId, FornecedorId = m.Contrato.FornecedorId, FornecedorNome = m.Contrato.Fornecedor.NomeFantasia ?? m.Contrato.Fornecedor.RazaoSocial,
                Descricao = $"{i.ContratoItem.Item} {i.ContratoItem.Descricao}".Trim(), Unidade = i.ContratoItem.Unidade,
                Quantidade = i.QtdPeriodo, Valor = i.ValorPeriodo, DocumentoTipo = "Medição", DocumentoNumero = $"{m.Contrato.Numero}/{m.NumeroMedicao}",
                MedicaoId = m.Id, Origem = OrigemCusto.Medicao, ChaveOrigem = chave
            });
        }
    }
}
