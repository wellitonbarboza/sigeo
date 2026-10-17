using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Planejamento;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>
/// Motor do estoque: garante os almoxarifados e grava movimentos atualizando o saldo no mesmo contexto
/// (quem chama — recebimento, transferência, EPI — grava tudo num SaveChanges só).
/// </summary>
public static class MotorEstoque
{
    /// <summary>Almoxarifado central: o do estabelecimento marcado como almoxarifado central (criado na primeira vez). Null se não houver.</summary>
    public static async Task<Almoxarifado?> CentralAsync(ISigeoDb db, CancellationToken ct = default)
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
    public static async Task<Almoxarifado> DaObraAsync(ISigeoDb db, Guid obraId, CancellationToken ct = default)
    {
        var a = db.Almoxarifados.Local.FirstOrDefault(x => x.ObraId == obraId && !x.Excluido)
                ?? await db.Almoxarifados.FirstOrDefaultAsync(x => x.ObraId == obraId, ct);
        if (a is not null) return a;
        var obra = await db.Obras.AsNoTracking().Where(o => o.Id == obraId).Select(o => new { o.Codigo, o.Nome }).FirstAsync(ct);
        a = new Almoxarifado { Codigo = $"OB-{obra.Codigo}".Length > 30 ? $"OB-{obra.Codigo}"[..30] : $"OB-{obra.Codigo}", Nome = $"Almoxarifado · {obra.Nome}", Tipo = TipoAlmoxarifado.Obra, ObraId = obraId };
        db.Almoxarifados.Add(a);
        return a;
    }

    public static async Task<SaldoEstoque> SaldoAsync(ISigeoDb db, Guid almoxarifadoId, Guid produtoId, CancellationToken ct = default)
    {
        var s = db.SaldosEstoque.Local.FirstOrDefault(x => x.AlmoxarifadoId == almoxarifadoId && x.ProdutoId == produtoId && !x.Excluido)
                ?? await db.SaldosEstoque.FirstOrDefaultAsync(x => x.AlmoxarifadoId == almoxarifadoId && x.ProdutoId == produtoId, ct);
        if (s is null) { s = new SaldoEstoque { AlmoxarifadoId = almoxarifadoId, ProdutoId = produtoId }; db.SaldosEstoque.Add(s); }
        return s;
    }

    /// <summary>Aplica o movimento ao saldo (valida saldo, custo médio) e o inclui no contexto.</summary>
    public static async Task<MovimentoEstoque> MovimentarAsync(ISigeoDb db, MovimentoEstoque m, CancellationToken ct = default)
    {
        if (m.ProdutoId == Guid.Empty) throw new RegraDeNegocioException("Item sem produto cadastrado não entra no estoque: classifique o produto antes.");
        var s = await SaldoAsync(db, m.AlmoxarifadoId, m.ProdutoId, ct);
        RegrasEstoque.Aplicar(s, m);
        db.MovimentosEstoque.Add(m);
        return m;
    }

    /// <summary>Entrada do recebimento (FVM) no almoxarifado onde foi recebido: lote em quarentena entra bloqueado.</summary>
    public static async Task EntradaRecebimentoAsync(ISigeoDb db, Recebimento r, IReadOnlyDictionary<Guid, decimal> custos, DateTimeOffset agora, CancellationToken ct = default)
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
    public static async Task AjustarLoteAsync(ISigeoDb db, Recebimento r, DateTimeOffset agora, CancellationToken ct = default)
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
    public static async Task RemoverRestricoesAsync(ISigeoDb db, Guid origemId, string como, string usuario, DateTimeOffset agora, CancellationToken ct = default)
    {
        foreach (var r in await db.Restricoes.Where(r => r.OrigemId == origemId && r.Status == StatusRestricao.Aberta).ToListAsync(ct))
            r.Remover(como, usuario, agora);
    }
}
