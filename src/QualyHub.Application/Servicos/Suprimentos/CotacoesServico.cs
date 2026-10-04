using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Regras;
using QualyHub.Domain.Suprimentos;
using QualyHub.Domain.Qualidade;

namespace QualyHub.Application.Servicos;

/// <summary>Cotação (T04–T06): convite, mapa de preços, aprovação e fechamento — a cotação aprovada gera o pedido (material) ou a minuta de contrato (serviço), por isso a geração fica aqui.</summary>
public sealed class CotacoesServico(IFabricaDb fabrica, AprovacaoServico aprovacoes, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── T04–T06 Cotação ─────────

    public async Task<Guid> GerarCotacaoAsync(Guid requisicaoId, IReadOnlyCollection<Guid> fornecedores, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var r = await db.Requisicoes.Include(x => x.Itens).FirstAsync(x => x.Id == requisicaoId, ct);
        if (r.Status != StatusRequisicao.Autorizada) throw new RegraDeNegocioException("A requisição precisa estar autorizada (Engenheiro + Diretor Técnico).");
        if (fornecedores.Count == 0) throw new RegraDeNegocioException("Convide ao menos um fornecedor.");
        var fs = await db.Fornecedores.Include(f => f.Qualificacoes).Where(f => fornecedores.Contains(f.Id)).ToListAsync(ct);
        var c = new Cotacao
        {
            ObraId = r.ObraId, RequisicaoId = r.Id, Numero = await Numerador.ProximoAsync(db, "Cotacao", ct), Tipo = r.Tipo,
            Nome = $"SC {r.Numero} - {r.Atividade}".ToUpperInvariant(), EnderecoEntrega = r.EnderecoEntrega, Status = StatusCotacao.AguardandoPropostas
        };
        foreach (var f in fs)
        {
            c.Fornecedores.Add(new CotacaoFornecedor { FornecedorId = f.Id, QualificacaoNaData = f.CalcularStatus(Hoje) });
            foreach (var i in r.Itens.Where(i => i.Status != StatusItemRequisicao.Cancelado))
                c.Itens.Add(new CotacaoItem
                {
                    FornecedorId = f.Id, RequisicaoItemId = i.Id, Descricao = i.Descricao, Unidade = i.Unidade, Quantidade = i.Quantidade,
                    Entrega = i.DataNecessidade ?? r.DataEntrega, MaterialControlado = i.MaterialControlado
                });
        }
        foreach (var i in r.Itens) i.Status = StatusItemRequisicao.Orcado;
        r.Status = StatusRequisicao.EmCotacao;
        db.Cotacoes.Add(c);
        await db.SaveChangesAsync(ct);
        return c.Id;
    }

    public async Task SalvarCotacaoAsync(Cotacao c, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var p = await ParametrosServico.CarregarAsync(db, ct);
        foreach (var f in c.Fornecedores)
            if (f.Status == StatusConvite.Convidado && c.Itens.Any(i => i.FornecedorId == f.FornecedorId && i.ValorOfertado > 0))
            { f.Status = StatusConvite.Respondido; f.DataResposta ??= Hoje; }
        c.Recalcular(p);
        await PrecosReferenciaAsync(db, c, ct);
        await Grafo.SalvarAsync(db, c, nameof(Cotacao.Fornecedores), nameof(Cotacao.Itens));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Fotografa, por item, o último preço pago pelo mesmo produto (pedido de outra cotação) e o preço de referência do cadastro.
    /// Base das 3 economias do mapa (vs. ofertado, vs. último, vs. referência).
    /// </summary>
    public static async Task PrecosReferenciaAsync(IQualyHubDb db, Cotacao c, CancellationToken ct = default)
    {
        var reqIds = c.Itens.Where(i => i.RequisicaoItemId != null).Select(i => i.RequisicaoItemId!.Value).Distinct().ToList();
        if (reqIds.Count == 0) return;
        var prodPorReq = await db.Set<RequisicaoItem>().AsNoTracking().Where(r => reqIds.Contains(r.Id) && r.ProdutoId != null)
                                 .ToDictionaryAsync(r => r.Id, r => r.ProdutoId!.Value, ct);
        var prodIds = prodPorReq.Values.Distinct().ToList();
        if (prodIds.Count == 0) return;
        var refs = await db.Produtos.AsNoTracking().Where(x => prodIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.PrecoReferencia, ct);
        var ultimos = await (from pi in db.PedidoItens.AsNoTracking()
                             join pe in db.Pedidos.AsNoTracking() on pi.PedidoId equals pe.Id
                             where pi.ProdutoId != null && prodIds.Contains(pi.ProdutoId.Value) && pe.CotacaoId != c.Id && pi.ValorUnitario > 0
                             orderby pe.CriadoEm descending
                             select new { Prod = pi.ProdutoId!.Value, pi.ValorUnitario }).ToListAsync(ct);
        var ultimo = ultimos.GroupBy(x => x.Prod).ToDictionary(g => g.Key, g => g.First().ValorUnitario);
        foreach (var i in c.Itens)
        {
            if (i.RequisicaoItemId is not { } rid || !prodPorReq.TryGetValue(rid, out var pid)) continue;
            i.PrecoUltimo = ultimo.TryGetValue(pid, out var u) ? u : null;
            i.PrecoReferencia = refs.GetValueOrDefault(pid);
        }
    }

    public async Task EnviarCotacaoAprovacaoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var c = await db.Cotacoes.Include(x => x.Itens).ThenInclude(i => i.RequisicaoItem).Include(x => x.Fornecedores)
                        .Include(x => x.FornecedorVencedor!).ThenInclude(f => f.Qualificacoes)
                        .Include(x => x.FornecedorVencedor!).ThenInclude(f => f.Documentos).ThenInclude(d => d.TipoDocumento)
                        .FirstAsync(x => x.Id == id, ct);
        var p = await ParametrosServico.CarregarAsync(db, ct);
        var trava = TravasQualidade.VerificarCotacao(c, p, Hoje);
        ValidacaoException.Se(trava.Bloqueios);
        foreach (var f in c.Fornecedores) if (f.FornecedorId == c.FornecedorVencedorId) f.Status = StatusConvite.Vencedor;
        await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Cotacao, c.Id, c.ObraId, c.Nome, c.ValorEstimado, ct);
        c.Status = StatusCotacao.AguardandoAprovacao;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Monta pedido (material) a partir da cotação aprovada — leva especificação e documentos exigidos dos itens controlados.</summary>
    public async Task<Guid> GerarPedidoAsync(Guid cotacaoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var c = await db.Cotacoes.Include(x => x.Itens).ThenInclude(i => i.RequisicaoItem).Include(x => x.Requisicao).FirstAsync(x => x.Id == cotacaoId, ct);
        if (c.Status != StatusCotacao.Aprovado) throw new RegraDeNegocioException("Cotação ainda não aprovada.");
        var itens = c.Itens.Where(i => i.FornecedorId == c.FornecedorVencedorId).ToList();
        var p = new Pedido
        {
            ObraId = c.ObraId, Numero = await Numerador.ProximoAsync(db, "Pedido", ct), CotacaoId = c.Id, RequisicaoId = c.RequisicaoId,
            SpeId = c.Requisicao?.SpeId, EstabelecimentoFaturamentoId = c.Requisicao?.EstabelecimentoFaturamentoId,
            EntregaAlmoxarifadoCentral = c.Requisicao?.Tipo == TipoRequisicao.Material && await db.Estabelecimentos.AnyAsync(e => e.AlmoxarifadoCentral && e.Ativo, ct),
            FornecedorId = c.FornecedorVencedorId!.Value, Frete = c.Frete, EnderecoEntrega = c.EnderecoEntrega,
            Entrega = itens.Select(i => i.Entrega).Max() ?? Hoje.AddDays(7), SemOrcamento = c.SemOrcamento,
            CondicaoPagamento = c.Fornecedores.FirstOrDefault(f => f.FornecedorId == c.FornecedorVencedorId)?.CondicaoPagamento
        };
        foreach (var i in itens)
            p.Itens.Add(new PedidoItem
            {
                RequisicaoItemId = i.RequisicaoItemId, ProdutoId = i.RequisicaoItem?.ProdutoId, Descricao = i.Descricao, Unidade = i.Unidade,
                Quantidade = i.Quantidade, ValorUnitario = i.ValorUnitarioFinal, ValorTotal = i.ValorTotal,
                MaterialControladoId = i.RequisicaoItem?.MaterialControladoId, EspecificacaoTecnica = i.RequisicaoItem?.EspecificacaoTecnica,
                DocumentosExigidos = i.RequisicaoItem?.DocumentosExigidos
            });
        p.EspecificacoesIncluidas = p.Itens.Where(i => i.MaterialControladoId is not null).All(i => !string.IsNullOrWhiteSpace(i.EspecificacaoTecnica));
        c.Status = StatusCotacao.PedidoGerado; c.Concluido = true;
        if (c.Requisicao is { } r) r.Status = StatusRequisicao.PedidoEmitido;
        db.Pedidos.Add(p);
        await db.SaveChangesAsync(ct);
        return p.Id;
    }

    /// <summary>Monta a minuta do contrato (serviço) a partir da cotação aprovada.</summary>
    public async Task<Guid> GerarContratoAsync(Guid cotacaoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var c = await db.Cotacoes.Include(x => x.Itens).ThenInclude(i => i.RequisicaoItem!).ThenInclude(ri => ri.Locais).Include(x => x.Requisicao).FirstAsync(x => x.Id == cotacaoId, ct);
        if (c.Status != StatusCotacao.Aprovado) throw new RegraDeNegocioException("Cotação ainda não aprovada.");
        var p = await ParametrosServico.CarregarAsync(db, ct);
        var n = await Numerador.ProximoAsync(db, $"Contrato-{Hoje.Year}", ct);
        var k = new Contrato
        {
            ObraId = c.ObraId, Numero = $"CT-{Hoje.Year}-{n:000}", FornecedorId = c.FornecedorVencedorId!.Value, RequisicaoId = c.RequisicaoId, CotacaoId = c.Id,
            SpeId = c.Requisicao?.SpeId, EstabelecimentoFaturamentoId = c.Requisicao?.EstabelecimentoFaturamentoId,
            Categoria = c.Requisicao?.CategoriaServico ?? "Serviço de obra", Objeto = c.Nome, RetencaoPerc = p[Parametros.RetencaoPadraoPerc],
            VigenciaInicio = Hoje, VigenciaFim = Hoje.AddMonths(6)
        };
        var seq = 0;
        foreach (var i in c.Itens.Where(i => i.FornecedorId == c.FornecedorVencedorId))
        {
            seq++;
            var ki = new ContratoItem
            {
                Item = $"1.{seq}", Descricao = i.Descricao, ServicoControladoId = i.RequisicaoItem?.ServicoControladoId, Unidade = i.Unidade,
                Quantidade = i.Quantidade, PrecoUnitario = i.ValorUnitarioFinal, ComposicaoId = i.RequisicaoItem?.ComposicaoId
            };
            // Quantidade por local: proporcional à quebra da requisição (soma = quantidade contratada).
            var ls = i.RequisicaoItem?.Locais.ToList() ?? [];
            var totalLs = ls.Sum(l => l.Quantidade);
            foreach (var l in ls)
                ki.Locais.Add(new ContratoItemLocal { ContratoItemId = ki.Id, LocalId = l.LocalId, Quantidade = totalLs == 0 ? 0 : Math.Round(i.Quantidade * l.Quantidade / totalLs, 4) });
            if (ki.Locais.Count > 0) ki.Locais.Last().Quantidade += i.Quantidade - ki.Locais.Sum(l => l.Quantidade);
            k.Itens.Add(ki);
        }
        foreach (var sid in k.Itens.Where(i => i.ServicoControladoId is not null).Select(i => i.ServicoControladoId!.Value).Distinct())
            k.ServicosVinculados.Add(new ContratoServico { ServicoControladoId = sid });
        c.Status = StatusCotacao.PedidoGerado; c.Concluido = true;
        if (c.Requisicao is { } r) r.Status = StatusRequisicao.PedidoEmitido;
        db.Contratos.Add(k);
        await db.SaveChangesAsync(ct);
        return k.Id;
    }
}
