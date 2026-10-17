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

/// <summary>Cotação (T04–T06): convite, mapa de preços, aprovação e fechamento — a cotação aprovada gera o pedido (material) ou a minuta de contrato (serviço), por isso a geração fica aqui.</summary>
public sealed class CotacoesServico(IFabricaDb fabrica, AprovacaoServico aprovacoes, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── T04–T06 Cotação ─────────

    public Task<Guid> GerarCotacaoAsync(Guid requisicaoId, IReadOnlyCollection<Guid> fornecedores, CancellationToken ct = default) =>
        GerarCotacaoAsync(requisicaoId, fornecedores, null, null, null, ct);

    /// <summary>
    /// Cotação com o saldo da requisição: todos os itens com saldo (ou só os do lote / os escolhidos, com a quantidade escolhida).
    /// Nunca passa do saldo a pedir menos o que já está em cotação aberta. Fornecedor bloqueado ou em qualificação pode ser
    /// convidado: a pendência fica registrada no convite e aparece no mapa.
    /// </summary>
    public async Task<Guid> GerarCotacaoAsync(Guid requisicaoId, IReadOnlyCollection<Guid> fornecedores, Guid? loteId, IReadOnlyDictionary<Guid, decimal>? quantidades,
        DateTimeOffset? prazoResposta, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var r = await db.Requisicoes.Include(x => x.Itens).FirstAsync(x => x.Id == requisicaoId, ct);
        if (r.Status is not (StatusRequisicao.Autorizada or StatusRequisicao.EmCotacao or StatusRequisicao.PedidoEmitido or StatusRequisicao.ParcialmenteAtendida))
            throw new RegraDeNegocioException("A requisição precisa estar autorizada (Engenheiro + Diretor Técnico) e com saldo a pedir.");
        if (fornecedores.Count == 0) throw new RegraDeNegocioException("Convide ao menos um fornecedor.");
        if (prazoResposta is { } pr && pr < relogio.GetUtcNow()) throw new RegraDeNegocioException("Prazo de resposta no passado.");
        var saldos = (await AtendimentoServico.SaldosAsync(db, r.Id, ct)).ToDictionary(s => s.ItemId);
        var itens = r.Itens.Where(i => saldos.TryGetValue(i.Id, out var s) && s.DisponivelCotacao > 0)
            .Where(i => loteId is null || i.LoteCompraId == loteId)
            .Where(i => quantidades is null || quantidades.Count == 0 || quantidades.ContainsKey(i.Id)).ToList();
        if (itens.Count == 0) throw new RegraDeNegocioException("Não há saldo a cotar para os itens escolhidos (já pedidos, em cotação aberta ou encerrados).");
        var erros = new List<string>();
        var qtd = new Dictionary<Guid, decimal>();
        foreach (var i in itens)
        {
            var disp = saldos[i.Id].DisponivelCotacao;
            var q = quantidades is not null && quantidades.TryGetValue(i.Id, out var x) ? x : disp;
            if (q <= 0 || q > disp) erros.Add($"{i.Descricao}: quantidade entre 0 e {disp:0.###} (saldo disponível).");
            qtd[i.Id] = q;
        }
        ValidacaoException.Se(erros);
        var fs = await db.Fornecedores.Include(f => f.Qualificacoes).Include(f => f.Documentos).ThenInclude(d => d.TipoDocumento).Where(f => fornecedores.Contains(f.Id)).ToListAsync(ct);
        var lote = loteId ?? (itens.Select(i => i.LoteCompraId).Distinct().Count() == 1 ? itens[0].LoteCompraId : null);
        var c = new Cotacao
        {
            ObraId = r.ObraId, RequisicaoId = r.Id, LoteCompraId = lote, PrazoResposta = prazoResposta, Numero = await Numerador.ProximoAsync(db, "Cotacao", ct), Tipo = r.Tipo,
            Nome = $"SC {r.Numero} - {r.Atividade}".ToUpperInvariant(), EnderecoEntrega = r.EnderecoEntrega, Status = StatusCotacao.AguardandoPropostas
        };
        foreach (var f in fs)
        {
            c.Fornecedores.Add(new CotacaoFornecedor { FornecedorId = f.Id, QualificacaoNaData = f.CalcularStatus(Hoje), PendenciaNaData = PendenciaFornecedor(f, Hoje) });
            foreach (var i in itens)
                c.Itens.Add(new CotacaoItem
                {
                    FornecedorId = f.Id, RequisicaoItemId = i.Id, Descricao = i.Descricao, Unidade = i.Unidade, Quantidade = qtd[i.Id],
                    Entrega = i.DataNecessidade ?? r.DataEntrega, MaterialControlado = i.MaterialControlado, Marca = i.Marca
                });
        }
        db.Cotacoes.Add(c);
        await db.SaveChangesAsync(ct);
        await AtendimentoServico.AtualizarAsync(db, r.Id, ct);
        await db.SaveChangesAsync(ct);
        return c.Id;
    }

    /// <summary>Pendência do fornecedor no convite (não impede convidar): qualificação e documentos vencidos/pendentes.</summary>
    public static string? PendenciaFornecedor(Fornecedor f, DateOnly hoje)
    {
        var l = new List<string>();
        var st = f.CalcularStatus(hoje);
        if (!f.Ativo) l.Add("Inativo");
        if (st != StatusQualificacao.Qualificado) l.Add(st switch
        {
            StatusQualificacao.EmQualificacao => "Em qualificação", StatusQualificacao.Vencida => "Qualificação vencida",
            StatusQualificacao.NaoQualificado => "Não qualificado", StatusQualificacao.Desqualificado => "Bloqueado (desqualificado)", _ => st.ToString()
        });
        l.AddRange(TravasQualidade.DocumentosBloqueantes(f, hoje).Select(d => d.Replace("Documento do fornecedor ", "Documento ")));
        return l.Count == 0 ? null : string.Join("; ", l.Distinct());
    }

    public async Task SalvarCotacaoAsync(Cotacao c, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var p = await ParametrosServico.CarregarAsync(db, ct);
        if (c.Frete == TipoFrete.NaoSeAplica) foreach (var f in c.Fornecedores) f.ValorFrete = null; // frete não se aplica: nada de valor de frete nas propostas
        foreach (var f in c.Fornecedores)
            if (f.Status == StatusConvite.Convidado && c.Itens.Any(i => i.FornecedorId == f.FornecedorId && i.ValorOfertado > 0))
            { f.Status = StatusConvite.Respondido; f.DataResposta ??= Hoje; }
        // Marca: obrigatória em item de material com preço (da cotação em diante); mesma grafia do cadastro de marcas.
        var marcas = await MarcasServico.NormalizarAsync(db, c.Itens.Select(i => i.Marca), ct);
        foreach (var i in c.Itens) i.Marca = MarcasServico.Oficial(marcas, i.Marca);
        var reqIds = c.Itens.Where(i => i.RequisicaoItemId != null).Select(i => i.RequisicaoItemId!.Value).Distinct().ToList();
        var pedidas = await db.Set<RequisicaoItem>().AsNoTracking().Where(r => reqIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => (r.Marca, r.MesmaMarca), ct);
        ValidacaoException.Se(c.Itens.Select(i => i.ErroMarca(c.Tipo == TipoRequisicao.Material,
            i.RequisicaoItemId is { } rq && pedidas.TryGetValue(rq, out var m) ? m.Marca : null, i.RequisicaoItemId is { } rq2 && pedidas.TryGetValue(rq2, out var m2) && m2.MesmaMarca))
            .OfType<string>().Distinct().ToList());
        c.Recalcular(p);
        c.AtualizarVencedorGeral();
        await PrecosReferenciaAsync(db, c, ct);
        await Grafo.SalvarAsync(db, c, nameof(Cotacao.Fornecedores), nameof(Cotacao.Itens));
        await db.SaveChangesAsync(ct);
        if (await SincronizacaoCadastros.QualificacaoNasCotacoesAsync(db, c.Fornecedores.Select(f => f.FornecedorId).ToList(), Hoje, ct) > 0) await db.SaveChangesAsync(ct);
    }

    /// <summary>Grava a qualificação de hoje dos fornecedores da cotação (a tela chama ao abrir: o cadastro pode ter mudado).</summary>
    public async Task AtualizarQualificacoesAsync(Guid cotacaoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var fs = await db.CotacaoFornecedores.Where(c => c.CotacaoId == cotacaoId).Select(c => c.FornecedorId).ToListAsync(ct);
        if (await SincronizacaoCadastros.QualificacaoNasCotacoesAsync(db, fs, Hoje, ct) > 0) await db.SaveChangesAsync(ct);
    }

    private static string Texto(Enum e) => System.Text.RegularExpressions.Regex.Replace(e.ToString(), "(?<!^)([A-Z])", " $1").ToLowerInvariant()
        .Replace("qualificacao", "qualificação").Replace("nao ", "não ");

    private static bool EmCotacao(Cotacao c) => SincronizacaoCadastros.CotacoesAbertas.Contains(c.Status);

    /// <summary>
    /// Inclui fornecedores numa cotação em andamento: cada um recebe os mesmos itens (sem preço) e a qualificação de hoje.
    /// Depois é só convidar pela Área do Fornecedor.
    /// </summary>
    public async Task<int> AdicionarFornecedoresAsync(Guid cotacaoId, IReadOnlyCollection<Guid> fornecedores, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var c = await db.Cotacoes.Include(x => x.Fornecedores).Include(x => x.Itens).FirstAsync(x => x.Id == cotacaoId, ct);
        if (!EmCotacao(c)) throw new RegraDeNegocioException("Só dá para incluir fornecedor enquanto a cotação está aberta (antes de enviar para aprovação).");
        var ja = c.Fornecedores.Select(f => f.FornecedorId).ToHashSet();
        var ids = fornecedores.Where(id => !ja.Contains(id)).Distinct().ToList();
        if (ids.Count == 0) throw new RegraDeNegocioException("Escolha um fornecedor que ainda não está na cotação.");
        var fs = await db.Fornecedores.Include(f => f.Qualificacoes).Where(f => ids.Contains(f.Id) && f.Ativo).ToListAsync(ct);
        if (fs.Count == 0) throw new RegraDeNegocioException("Fornecedor inativo ou não encontrado.");
        // Modelo dos itens: uma linha por item da cotação (o mesmo item aparece uma vez por fornecedor).
        var modelos = c.Itens.GroupBy(i => i.RequisicaoItemId?.ToString() ?? $"{i.Descricao}|{i.Unidade}").Select(g => g.First()).ToList();
        foreach (var f in fs)
        {
            var conv = new CotacaoFornecedor { CotacaoId = c.Id, FornecedorId = f.Id, QualificacaoNaData = f.CalcularStatus(Hoje) };
            c.Fornecedores.Add(conv); db.Add(conv);
            foreach (var m in modelos)
            {
                var it = new CotacaoItem
                {
                    CotacaoId = c.Id, FornecedorId = f.Id, RequisicaoItemId = m.RequisicaoItemId, Descricao = m.Descricao, Unidade = m.Unidade,
                    Quantidade = m.Quantidade, Entrega = m.Entrega, MaterialControlado = m.MaterialControlado, PrecoUltimo = m.PrecoUltimo, PrecoReferencia = m.PrecoReferencia
                };
                c.Itens.Add(it); db.Add(it);
            }
            db.Notas.Add(Domain.Colaboracao.Nota.Criar(nameof(Cotacao), c.Id, $"Fornecedor incluído na cotação: {f.NomeExibicao} (qualificação: {Texto(f.CalcularStatus(Hoje))})."));
        }
        c.Recalcular(await ParametrosServico.CarregarAsync(db, ct));
        await db.SaveChangesAsync(ct);
        return fs.Count;
    }

    /// <summary>Retira um fornecedor (convite e preços dele) de uma cotação em andamento — fica registrado nas notas da cotação.</summary>
    public async Task RemoverFornecedorAsync(Guid cotacaoId, Guid fornecedorId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var c = await db.Cotacoes.Include(x => x.Fornecedores).Include(x => x.Itens).FirstAsync(x => x.Id == cotacaoId, ct);
        if (!EmCotacao(c)) throw new RegraDeNegocioException("Só dá para retirar fornecedor enquanto a cotação está aberta (antes de enviar para aprovação).");
        var conv = c.Fornecedores.FirstOrDefault(f => f.FornecedorId == fornecedorId) ?? throw new RegraDeNegocioException("Fornecedor não está nesta cotação.");
        if (c.Fornecedores.Count == 1) throw new RegraDeNegocioException("A cotação precisa de ao menos um fornecedor. Inclua outro antes de retirar este.");
        var itens = c.Itens.Where(i => i.FornecedorId == fornecedorId).ToList();
        var nome = await db.Fornecedores.Where(f => f.Id == fornecedorId).Select(f => f.NomeFantasia ?? f.RazaoSocial).FirstAsync(ct);
        var total = itens.Sum(i => i.ValorTotal);
        foreach (var i in itens) { c.Itens.Remove(i); db.Remove(i); }
        c.Fornecedores.Remove(conv); db.Remove(conv);
        if (c.FornecedorVencedorId == fornecedorId) { c.FornecedorVencedorId = null; c.CriterioSelecao = null; }
        db.Notas.Add(Domain.Colaboracao.Nota.Criar(nameof(Cotacao), c.Id,
            $"Fornecedor retirado da cotação: {nome} (convite {Texto(conv.Status)}{(total > 0 ? $", proposta de {total.ToString("C2", new System.Globalization.CultureInfo("pt-BR"))}" : "")})."));
        c.Recalcular(await ParametrosServico.CarregarAsync(db, ct));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Fotografa, por item, o último preço pago pelo mesmo produto (pedido de outra cotação) e o preço de referência do cadastro.
    /// Base das 3 economias do mapa (vs. ofertado, vs. último, vs. referência).
    /// </summary>
    public static async Task PrecosReferenciaAsync(ISigeoDb db, Cotacao c, CancellationToken ct = default)
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
        await SincronizacaoCadastros.QualificacaoNasCotacoesAsync(db, c.Fornecedores.Select(f => f.FornecedorId).ToList(), Hoje, ct);
        c.AtualizarVencedorGeral();
        var ids = c.FornecedoresVencedores().ToList();
        var vencedores = await db.Fornecedores.Include(f => f.Qualificacoes).Include(f => f.Documentos).ThenInclude(d => d.TipoDocumento)
            .Where(f => ids.Contains(f.Id)).ToDictionaryAsync(f => f.Id, ct);
        var trava = TravasQualidade.VerificarCotacao(c, p, Hoje, vencedores);
        ValidacaoException.Se(trava.Bloqueios);
        foreach (var f in c.Fornecedores)
            if (ids.Contains(f.FornecedorId)) f.Status = StatusConvite.Vencedor;
            else if (f.Status == StatusConvite.Vencedor) f.Status = StatusConvite.Respondido;
        var speCotacao = c.RequisicaoId is { } rq ? await db.Requisicoes.Where(x => x.Id == rq).Select(x => x.SpeId).FirstOrDefaultAsync(ct) : null;
        await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Cotacao, c.Id, c.ObraId, c.Nome, c.ValorEstimado, ct, speCotacao);
        c.Status = StatusCotacao.AguardandoAprovacao;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Compatível com a tela antiga: gera os pedidos e devolve o primeiro.</summary>
    public async Task<Guid> GerarPedidoAsync(Guid cotacaoId, CancellationToken ct = default) => (await GerarPedidosAsync(cotacaoId, ct))[0];

    /// <summary>
    /// Pedidos (material) da cotação aprovada: um por fornecedor vencedor (vencedor item a item ou geral), com a quantidade vencida.
    /// Leva especificação e documentos exigidos dos itens controlados. O que sobrar volta para "a pedir" na requisição.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> GerarPedidosAsync(Guid cotacaoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var c = await db.Cotacoes.Include(x => x.Fornecedores).Include(x => x.Itens).ThenInclude(i => i.RequisicaoItem).Include(x => x.Requisicao).FirstAsync(x => x.Id == cotacaoId, ct);
        if (c.Status != StatusCotacao.Aprovado) throw new RegraDeNegocioException("Cotação ainda não aprovada.");
        var vencidos = c.ItensVencedores();
        if (vencidos.Count == 0) throw new RegraDeNegocioException("Nenhum item com vencedor.");
        var central = c.Requisicao?.Tipo == TipoRequisicao.Material && await db.Estabelecimentos.AnyAsync(e => e.AlmoxarifadoCentral && e.Ativo, ct);
        var ids = new List<Guid>();
        foreach (var g in vencidos.GroupBy(i => i.FornecedorId))
        {
            var conv = c.Fornecedores.FirstOrDefault(f => f.FornecedorId == g.Key);
            var itens = g.ToList();
            var p = new Pedido
            {
                ObraId = c.ObraId, Numero = await Numerador.ProximoAsync(db, "Pedido", ct), CotacaoId = c.Id, RequisicaoId = c.RequisicaoId,
                SpeId = c.Requisicao?.SpeId, EstabelecimentoFaturamentoId = c.Requisicao?.EstabelecimentoFaturamentoId, EntregaAlmoxarifadoCentral = central,
                FornecedorId = g.Key, Frete = c.Frete, EnderecoEntrega = c.EnderecoEntrega,
                Entrega = itens.Select(i => i.Entrega).Max() ?? conv?.PrazoEntregaOfertado ?? Hoje.AddDays(7), SemOrcamento = c.SemOrcamento,
                CondicaoPagamento = conv?.CondicaoPagamento, ValorFrete = c.Frete == TipoFrete.NaoSeAplica ? null : conv?.ValorFrete
            };
            foreach (var i in itens)
                p.Itens.Add(new PedidoItem
                {
                    RequisicaoItemId = i.RequisicaoItemId, ProdutoId = i.RequisicaoItem?.ProdutoId, Descricao = i.Descricao, Unidade = i.Unidade,
                    Quantidade = i.QuantidadePedido, ValorUnitario = i.ValorUnitarioFinal, ValorTotal = i.ValorTotalVencido,
                    MaterialControladoId = i.RequisicaoItem?.MaterialControladoId, EspecificacaoTecnica = i.RequisicaoItem?.EspecificacaoTecnica,
                    DocumentosExigidos = i.RequisicaoItem?.DocumentosExigidos, Marca = i.Marca
                });
            p.EspecificacoesIncluidas = p.Itens.Where(i => i.MaterialControladoId is not null).All(i => !string.IsNullOrWhiteSpace(i.EspecificacaoTecnica));
            db.Pedidos.Add(p);
            ids.Add(p.Id);
        }
        c.Status = StatusCotacao.PedidoGerado; c.Concluido = true;
        await db.SaveChangesAsync(ct);
        await AtendimentoServico.AtualizarAsync(db, c.RequisicaoId, ct);
        await db.SaveChangesAsync(ct);
        return ids;
    }

    /// <summary>Monta a minuta do contrato (serviço) a partir da cotação aprovada.</summary>
    public async Task<Guid> GerarContratoAsync(Guid cotacaoId, CancellationToken ct = default) => (await GerarContratosAsync(cotacaoId, ct))[0];

    /// <summary>Minutas de contrato (serviço): uma por fornecedor vencedor (item a item ou geral).</summary>
    public async Task<IReadOnlyList<Guid>> GerarContratosAsync(Guid cotacaoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var c = await db.Cotacoes.Include(x => x.Itens).ThenInclude(i => i.RequisicaoItem!).ThenInclude(ri => ri.Locais).Include(x => x.Requisicao).FirstAsync(x => x.Id == cotacaoId, ct);
        if (c.Status != StatusCotacao.Aprovado) throw new RegraDeNegocioException("Cotação ainda não aprovada.");
        var p = await ParametrosServico.CarregarAsync(db, ct);
        var vencidos = c.ItensVencedores();
        if (vencidos.Count == 0) throw new RegraDeNegocioException("Nenhum item com vencedor.");
        var ids = new List<Guid>();
        foreach (var g in vencidos.GroupBy(i => i.FornecedorId))
        {
            var n = await Numerador.ProximoAsync(db, $"Contrato-{Hoje.Year}", ct);
            var k = new Contrato
            {
                ObraId = c.ObraId, Numero = $"CT-{Hoje.Year}-{n:000}", FornecedorId = g.Key, RequisicaoId = c.RequisicaoId, CotacaoId = c.Id,
                SpeId = c.Requisicao?.SpeId, EstabelecimentoFaturamentoId = c.Requisicao?.EstabelecimentoFaturamentoId,
                Categoria = c.Requisicao?.CategoriaServico ?? "Serviço de obra", Objeto = c.Nome, RetencaoPerc = p[Parametros.RetencaoPadraoPerc],
                VigenciaInicio = Hoje, VigenciaFim = Hoje.AddMonths(6)
            };
            var seq = 0;
            foreach (var i in g)
            {
                seq++;
                var qtd = i.QuantidadePedido;
                var ki = new ContratoItem
                {
                    Item = $"1.{seq}", Descricao = i.Descricao, ServicoControladoId = i.RequisicaoItem?.ServicoControladoId, Unidade = i.Unidade,
                    Quantidade = qtd, PrecoUnitario = i.ValorUnitarioFinal, ComposicaoId = i.RequisicaoItem?.ComposicaoId
                };
                // Quantidade por local: proporcional à quebra da requisição (soma = quantidade contratada).
                var ls = i.RequisicaoItem?.Locais.ToList() ?? [];
                var totalLs = ls.Sum(l => l.Quantidade);
                foreach (var l in ls)
                    ki.Locais.Add(new ContratoItemLocal { ContratoItemId = ki.Id, LocalId = l.LocalId, Quantidade = totalLs == 0 ? 0 : Math.Round(qtd * l.Quantidade / totalLs, 4) });
                if (ki.Locais.Count > 0) ki.Locais.Last().Quantidade += qtd - ki.Locais.Sum(l => l.Quantidade);
                k.Itens.Add(ki);
            }
            foreach (var sid in k.Itens.Where(i => i.ServicoControladoId is not null).Select(i => i.ServicoControladoId!.Value).Distinct())
                k.ServicosVinculados.Add(new ContratoServico { ServicoControladoId = sid });
            db.Contratos.Add(k);
            ids.Add(k.Id);
        }
        c.Status = StatusCotacao.PedidoGerado; c.Concluido = true;
        await db.SaveChangesAsync(ct);
        await AtendimentoServico.AtualizarAsync(db, c.RequisicaoId, ct);
        await db.SaveChangesAsync(ct);
        return ids;
    }
}
