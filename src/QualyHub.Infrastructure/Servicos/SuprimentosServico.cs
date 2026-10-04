using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Regras;
using QualyHub.Domain.Suprimentos;
using QualyHub.Domain.Qualidade;
using QualyHub.Infrastructure.Contexto;
using QualyHub.Infrastructure.Dados;

namespace QualyHub.Infrastructure.Servicos;

/// <summary>Casos de uso do fluxo de compras (P.SUP): requisição → cotação → pedido/contrato → recebimento/medição → pagamento → avaliação.</summary>
public sealed class SuprimentosServico(FabricaDb fabrica, AprovacaoServico aprovacoes, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── T01–T03 Requisição ─────────

    public async Task<Guid> SalvarRequisicaoAsync(Requisicao r, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (r.Numero == 0)
        {
            r.Numero = await Numerador.ProximoAsync(db, "Requisicao", ct);
            r.SolicitanteUsuarioId = usuario.UsuarioId ?? Guid.Empty;
            r.SolicitanteNome = usuario.Nome;
        }
        if (r.Status is not (StatusRequisicao.Rascunho or StatusRequisicao.DevolvidaAjuste))
            throw new RegraDeNegocioException("Requisição já enviada: não pode ser alterada.");
        r.RecalcularPrazo();
        if (r.EstabelecimentoFaturamentoId is null && r.Tipo == TipoRequisicao.Material)
            r.EstabelecimentoFaturamentoId = await EstabelecimentosServico.PadraoFaturamentoAsync(db, ct);
        // SPE beneficiária sai dos locais escolhidos (todos do mesmo condomínio).
        var locIds = r.Itens.SelectMany(i => i.Locais).Select(l => l.LocalId).Distinct().ToList();
        if (locIds.Count > 0)
        {
            var spes = await db.Locais.Where(l => locIds.Contains(l.Id)).Select(l => l.SpeId).Distinct().ToListAsync(ct);
            if (spes.Count(x => x != null) > 1) throw new RegraDeNegocioException("Os itens têm locais de SPEs (condomínios) diferentes: faça uma requisição por SPE.");
            if (spes.FirstOrDefault(x => x != null) is { } sp) r.SpeId = sp;
        }
        // Netos (quebra por local) não são cobertos pelo Grafo: grava à parte.
        var locais = r.Itens.ToDictionary(i => i.Id, i => i.Locais.Select(l => new RequisicaoItemLocal { RequisicaoItemId = i.Id, LocalId = l.LocalId, ComposicaoId = l.ComposicaoId, Quantidade = l.Quantidade }).ToList());
        foreach (var i in r.Itens) i.Locais = [];
        await Grafo.SalvarAsync(db, r, nameof(Requisicao.Itens));
        var ids = locais.Keys.ToList();
        db.RequisicaoItemLocais.RemoveRange(await db.RequisicaoItemLocais.Where(x => ids.Contains(x.RequisicaoItemId)).ToListAsync(ct));
        db.RequisicaoItemLocais.AddRange(locais.Values.SelectMany(x => x));
        await db.SaveChangesAsync(ct);
        foreach (var i in r.Itens) i.Locais = locais[i.Id];
        return r.Id;
    }

    public async Task EnviarRequisicaoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var r = await db.Requisicoes.Include(x => x.Itens).ThenInclude(i => i.Locais).FirstAsync(x => x.Id == id, ct);
        var temEscopo = await db.Anexos.AnyAsync(a => a.Entidade == nameof(Requisicao) && a.EntidadeId == id && a.Categoria == "Escopo", ct);
        // Serviço vindo do orçamento: a planilha serviço × local é o escopo (projetos/ITS seguem como anexo opcional).
        if (r.Tipo == TipoRequisicao.Servico && r.Itens.Any(i => i.Locais.Count > 0)) temEscopo = true;
        var erros = r.ValidarEnvio(temEscopo).ToList();
        if (r.SpeId is null && await db.Spes.AnyAsync(s => s.ObraId == r.ObraId && s.Ativa, ct))
            erros.Add("Informe a SPE beneficiária (destino do custo) — ou puxe os itens do orçamento pelos locais.");
        if (r.EstabelecimentoFaturamentoId is null && await db.Estabelecimentos.AnyAsync(e => e.Ativo, ct))
            erros.Add("Informe o \"Faturar para\" (CNPJ que compra).");
        foreach (var i in r.Itens.Where(i => i.Status != StatusItemRequisicao.Cancelado && i.Locais.Count > 0 && i.Quantidade > i.Locais.Sum(l => l.Quantidade) + 0.001m))
            if (string.IsNullOrWhiteSpace(r.JustificativaAcimaOrcado))
                erros.Add($"{i.Descricao}: quantidade acima do saldo orçado para os locais ({i.Locais.Sum(l => l.Quantidade):0.###}) — justifique.");
        if (r.SpeId is { } sid && !await db.Spes.AnyAsync(s => s.Id == sid && s.ObraId == r.ObraId && s.Ativa, ct))
            erros.Add("A SPE informada não pertence a esta obra ou está inativa.");
        ValidacaoException.Se(erros);
        await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Requisicao, r.Id, r.ObraId, $"Requisição {r.Numero} – {r.Atividade}", r.ValorEstimado, ct);
        r.Status = StatusRequisicao.AguardandoEngenheiro;
        r.MotivoReprovacao = null;
        await db.SaveChangesAsync(ct);
    }

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
    public static async Task PrecosReferenciaAsync(QualyHubDbContext db, Cotacao c, CancellationToken ct = default)
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

    // ───────── T07 Pedido ─────────

    public async Task EnviarPedidoAprovacaoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var p = await db.Pedidos.Include(x => x.Itens)
            .Include(x => x.Fornecedor).ThenInclude(f => f.Qualificacoes)
            .Include(x => x.Fornecedor).ThenInclude(f => f.Documentos).ThenInclude(d => d.TipoDocumento)
            .FirstAsync(x => x.Id == id, ct);
        var trava = TravasQualidade.VerificarPedido(p, p.Fornecedor, Hoje);
        p.Bloqueios = trava.Resumo;
        if (!trava.Liberado) { await db.SaveChangesAsync(ct); ValidacaoException.Se(trava.Bloqueios); }
        await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Pedido, p.Id, p.ObraId, $"Pedido {p.Numero} – {p.Fornecedor.NomeExibicao}", p.Valor, ct);
        p.Status = StatusPedido.AguardandoAprovacaoDiretor;
        await db.SaveChangesAsync(ct);
    }

    public async Task RegistrarEnvioPedidoAsync(Guid id, string emailsCopia, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var p = await db.Pedidos.FirstAsync(x => x.Id == id, ct);
        if (p.Status != StatusPedido.Aprovado) throw new RegraDeNegocioException("Pedido precisa estar aprovado pelo Diretor antes do envio.");
        p.EnviadoFornecedorEm = relogio.GetUtcNow();
        p.EmailsCopia = emailsCopia;
        p.Status = StatusPedido.EnviadoFornecedor;
        await db.SaveChangesAsync(ct);
    }

    // ───────── T11/T12 Contrato ─────────

    public async Task<Guid> SalvarContratoAsync(Contrato k, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (string.IsNullOrWhiteSpace(k.Numero))
            k.Numero = $"CT-{Hoje.Year}-{await Numerador.ProximoAsync(db, $"Contrato-{Hoje.Year}", ct):000}";
        if (k.Status is not (StatusContrato.EmElaboracao)) throw new RegraDeNegocioException("Contrato fora de elaboração: use aditivo.");
        await Grafo.SalvarAsync(db, k, nameof(Contrato.Itens), nameof(Contrato.ServicosVinculados));
        await db.SaveChangesAsync(ct);
        return k.Id;
    }

    public async Task EnviarContratoAprovacaoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var k = await db.Contratos.Include(x => x.Itens).Include(x => x.ServicosVinculados)
            .Include(x => x.Fornecedor).ThenInclude(f => f.Qualificacoes)
            .Include(x => x.Fornecedor).ThenInclude(f => f.Documentos).ThenInclude(d => d.TipoDocumento)
            .FirstAsync(x => x.Id == id, ct);
        ValidacaoException.Se(TravasQualidade.VerificarContrato(k, Hoje).Bloqueios);
        await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Contrato, k.Id, k.ObraId, $"{k.Numero} – {k.Fornecedor.NomeExibicao}", k.ValorTotal, ct);
        k.Status = StatusContrato.AguardandoAprovacaoDiretoria;
        await db.SaveChangesAsync(ct);
    }

    public async Task AtivarContratoAsync(Guid id, DateOnly assinaturaEmpresa, DateOnly assinaturaFornecedor, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var k = await db.Contratos.FirstAsync(x => x.Id == id, ct);
        k.AssinaturaEmpresa = assinaturaEmpresa;
        k.AssinaturaFornecedor = assinaturaFornecedor;
        k.Ativar();
        await db.SaveChangesAsync(ct);
    }

    public async Task RegistrarEnvioContratoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var k = await db.Contratos.FirstAsync(x => x.Id == id, ct);
        if (k.Status != StatusContrato.Ativo) throw new RegraDeNegocioException("Ative o contrato (assinaturas) antes de enviar ao fornecedor.");
        k.EnviadoFornecedorEm = relogio.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    // ───────── T13 Recebimento + FVM ─────────

    public async Task<Guid> RegistrarRecebimentoAsync(Recebimento r, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (r.PedidoId is { } pid)
        {
            var ped = await db.Pedidos.Include(p => p.Itens).FirstAsync(p => p.Id == pid, ct);
            r.FornecedorId = ped.FornecedorId;
            r.ObraId = ped.ObraId;
            if (ped.EntregaAlmoxarifadoCentral)
            {
                r.AlmoxarifadoCentral = true;
                r.EstabelecimentoId ??= await db.Estabelecimentos.Where(e => e.AlmoxarifadoCentral).Select(e => (Guid?)e.Id).FirstOrDefaultAsync(ct);
            }
        }
        r.AplicarChecklist();
        ValidacaoException.Se(r.Validar());
        r.Numero = await Numerador.ProximoAsync(db, "Recebimento", ct);
        // Modelo de FVM vigente do material controlado (Q01): quarentena, rastreabilidade e ensaios previstos (Q12 → Q14).
        var matId = r.Itens.Select(i => i.MaterialControladoId).FirstOrDefault(i => i is not null);
        if (matId is not null)
        {
            var modelo = await db.ModelosInspecao.AsNoTracking().Include(m => m.Ensaios)
                .FirstOrDefaultAsync(m => m.Tipo == TipoModelo.FVM && m.MaterialControladoId == matId && m.Situacao == SituacaoModelo.Vigente, ct);
            if (modelo is not null)
            {
                r.ModeloFvmId = modelo.Id; r.QuarentenaAteEnsaio = modelo.QuarentenaAteEnsaio; r.RastrearAplicacao = modelo.RastrearAplicacao;
                if (r.Destino != DestinoRecebimento.Devolvido)
                    foreach (var x in modelo.Ensaios)
                        r.Ensaios.Add(new Domain.Qualidade.EnsaioRecebimento
                        {
                            RecebimentoId = r.Id, EnsaioModeloId = x.Id, Ensaio = x.Ensaio, Criterio = x.Criterio, AcaoReprovacao = x.AcaoReprovacao,
                            Amostra = $"{r.Numero}-{x.Ensaio[..Math.Min(3, x.Ensaio.Length)].ToUpperInvariant()}", DataColeta = Hoje,
                            DataPrevista = Hoje.AddDays(x.Momento == MomentoEnsaio.Posterior ? x.MaiorPrazo() : 0)
                        });
            }
        }
        foreach (var i in r.FvmItens) i.RecebimentoId = r.Id;
        r.StatusLote = Domain.Qualidade.RegrasQualidadeObra.StatusDoLote(r.FvmAprovada == true, r.Destino == DestinoRecebimento.Devolvido,
            r.Destino == DestinoRecebimento.AceitoComRessalva, r.QuarentenaAteEnsaio, [.. r.Ensaios]);
        if (r.FvmAprovada == false)
        {
            var rnc = new Domain.Qualidade.Rnc
            {
                Numero = $"RNC-{await Numerador.ProximoAsync(db, "Rnc", ct):0000}", Origem = OrigemRnc.FvmRecebimento, Referencia = $"Recebimento {r.Numero} · NF {r.NumeroNotaFiscal ?? r.ChaveNFe}",
                ReferenciaId = r.Id, ObraId = r.ObraId, FornecedorId = r.FornecedorId, Classificacao = ClassificacaoRnc.Material, Gravidade = Gravidade.Media,
                Descricao = r.Ocorrencia ?? "FVM reprovada", Disposicao = r.Destino == DestinoRecebimento.Devolvido ? "Devolução ao fornecedor" : "Aceite sob concessão",
                Concessao = r.Destino == DestinoRecebimento.AceitoComRessalva ? "Aceito com ressalva no recebimento — confirmar com engenharia" : null,
                ResponsavelId = usuario.UsuarioId, ResponsavelNome = usuario.Nome, Prazo = Hoje.AddDays(7), Status = StatusRnc.EmTratamento
            };
            db.Rncs.Add(rnc); r.RncId = rnc.Id;
        }
        r.RecebidoPorUsuarioId = usuario.UsuarioId;
        r.RecebidoPorNome = usuario.Nome;
        if (r.FvmAprovada == false || r.TemDivergencia)
        {
            r.ComunicadoA ??= "Engenheiro da obra; Suprimentos; Qualidade" + (r.Destino == DestinoRecebimento.Devolvido ? "; Comprador; Financeiro" : "");
            r.ComunicadoEm = relogio.GetUtcNow();
        }
        db.Recebimentos.Add(r);

        if (r.PedidoId is { } pedidoId && r.Destino != DestinoRecebimento.Devolvido)
        {
            var ped = await db.Pedidos.Include(p => p.Itens).FirstAsync(p => p.Id == pedidoId, ct);
            foreach (var ri in r.Itens.Where(i => i.PedidoItemId is not null))
            {
                var pi = ped.Itens.First(i => i.Id == ri.PedidoItemId);
                pi.QuantidadeRecebidaAceita += ri.QtdRecebida;
            }
            ped.Status = ped.Itens.All(i => i.QuantidadeRecebidaAceita >= i.Quantidade) ? StatusPedido.Entregue : StatusPedido.EntregueParcial;
        }
        if (RegrasAvaliacao.PlanoPorFvm(r, Hoje, await NumeroPlanoAsync(db, ct)) is { } plano) db.PlanosAcao.Add(plano);
        await db.SaveChangesAsync(ct);
        return r.Id;
    }

    // ───────── Transferência: almoxarifado central → obra/SPE ─────────

    /// <summary>Monta a transferência a partir do recebimento central (itens aceitos, custo do pedido, SPE do pedido).</summary>
    public async Task<Guid> GerarTransferenciaAsync(Guid recebimentoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var r = await db.Recebimentos.Include(x => x.Itens).Include(x => x.Pedido!).ThenInclude(p => p.Itens).FirstAsync(x => x.Id == recebimentoId, ct);
        if (r.Destino == DestinoRecebimento.Devolvido) throw new RegraDeNegocioException("Material devolvido não é transferido.");
        if (r.FvmAprovada is null) throw new RegraDeNegocioException("Registre a FVM antes de transferir.");
        if (await db.Transferencias.AnyAsync(t => t.RecebimentoId == r.Id && t.Status != StatusTransferencia.Cancelada, ct)) throw new RegraDeNegocioException("Este recebimento já tem transferência.");
        var t = new TransferenciaMaterial
        {
            ObraId = r.ObraId, RecebimentoId = r.Id, PedidoId = r.PedidoId, SpeId = r.Pedido?.SpeId, OrigemEstabelecimentoId = r.EstabelecimentoId ?? r.Pedido?.EstabelecimentoFaturamentoId,
            Data = Hoje, Numero = await Numerador.ProximoAsync(db, "Transferencia", ct)
        };
        foreach (var i in r.Itens.Where(i => i.QtdRecebida > 0))
        {
            var pi = r.Pedido?.Itens.FirstOrDefault(x => x.Id == i.PedidoItemId);
            t.Itens.Add(new TransferenciaItem
            {
                TransferenciaId = t.Id, RecebimentoItemId = i.Id, ProdutoId = pi?.ProdutoId, Descricao = i.Descricao, Unidade = i.Unidade, Quantidade = i.QtdRecebida,
                CustoUnitario = pi?.ValorUnitario ?? 0, Lote = i.Lote
            });
        }
        db.Transferencias.Add(t);
        await db.SaveChangesAsync(ct);
        return t.Id;
    }

    public async Task SalvarTransferenciaAsync(TransferenciaMaterial t, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (t.Numero == 0) t.Numero = await Numerador.ProximoAsync(db, "Transferencia", ct);
        if (t.Status != StatusTransferencia.Rascunho) throw new RegraDeNegocioException("Transferência já enviada.");
        foreach (var i in t.Itens) i.TransferenciaId = t.Id;
        ValidacaoException.Se(t.Validar());
        await Grafo.SalvarAsync(db, t, nameof(TransferenciaMaterial.Itens));
        await db.SaveChangesAsync(ct);
    }

    public async Task EnviarTransferenciaAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var t = await db.Transferencias.Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
        t.Enviar();
        await db.SaveChangesAsync(ct);
    }

    public async Task ConferirTransferenciaAsync(Guid id, IReadOnlyDictionary<Guid, decimal> recebidas, string? observacao, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var t = await db.Transferencias.Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
        if (!string.IsNullOrWhiteSpace(observacao)) t.Observacao = observacao;
        t.Conferir(usuario.Nome, relogio.GetUtcNow(), recebidas);
        await db.SaveChangesAsync(ct);
    }

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
        foreach (var i in m.Itens)
            if (quantidades.TryGetValue(i.Id, out var q))
            {
                i.QtdPeriodo = q.Qtd; i.FvsId = q.FvsId; i.LocalId = q.LocalId;
                i.FvsRef = q.FvsId is { } fid ? await db.Fvss.Where(f => f.Id == fid).Select(f => f.Numero).FirstOrDefaultAsync(ct) : null;
                if (q.FvsId is { } fv && q.LocalId is null) i.LocalId = await db.Fvss.Where(f => f.Id == fv).Select(f => (Guid?)f.LocalId).FirstOrDefaultAsync(ct);
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
        var cels = await db.ServicosLocais.AsNoTracking().Where(s => s.ObraId == m.ObraId && compIds.Contains(s.ComposicaoId)).Select(s => new { s.ComposicaoId, s.LocalId }).ToListAsync(ct);
        IReadOnlyCollection<Guid> Exigidos(MedicaoItem i)
        {
            if (i.LocalId is not { } lid || i.ContratoItem.ComposicaoId is not { } cid) return [];
            var sub = MotorOrcamento.Subarvore(lid, filhos);
            return cels.Where(c => c.ComposicaoId == cid && sub.Contains(c.LocalId)).Select(c => c.LocalId).ToList();
        }
        var tipos = await db.TiposDocumento.AsNoTracking().Where(t => t.AplicaA == AplicaA.Fornecedor).ToListAsync(ct);
        var trava = TravasQualidade.VerificarMedicao(m, fvs, Hoje, locais, Exigidos, tipos);
        m.Bloqueios = trava.Resumo;
        if (!trava.Liberado) { await db.SaveChangesAsync(ct); ValidacaoException.Se(trava.Bloqueios); }
        if (m.PlanoAcaoId is null && RegrasAvaliacao.PlanoPorMedicao(m, m.Contrato.FornecedorId, Hoje, await NumeroPlanoAsync(db, ct)) is { } plano)
        {
            db.PlanosAcao.Add(plano);
            m.PlanoAcaoId = plano.Id;
        }
        await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Medicao, m.Id, m.ObraId, $"Medição {m.NumeroMedicao:00} – {m.Contrato.Numero}", m.ValorPeriodo, ct);
        m.Status = StatusMedicao.AguardandoAprovacao;
        await db.SaveChangesAsync(ct);
    }

    public static async Task<Medicao> CarregarMedicaoAsync(QualyHubDbContext db, Guid id, CancellationToken ct) =>
        await db.Medicoes
            .Include(x => x.Itens).ThenInclude(i => i.ContratoItem).ThenInclude(ci => ci.ServicoControlado)
            .Include(x => x.Contrato).ThenInclude(k => k.Itens).ThenInclude(i => i.ServicoControlado)
            .Include(x => x.Contrato).ThenInclude(k => k.Itens).ThenInclude(i => i.Locais)
            .Include(x => x.Contrato).ThenInclude(k => k.Fornecedor).ThenInclude(f => f.Documentos).ThenInclude(d => d.TipoDocumento)
            .Include(x => x.Contrato).ThenInclude(k => k.Fornecedor).ThenInclude(f => f.Exigencias)
            .FirstAsync(x => x.Id == id, ct);

    // ───────── T18 Pagamento ─────────

    public async Task<Guid> EncaminharPagamentoAsync(LiberacaoPagamento l, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        Pedido? ped = l.PedidoId is { } pid ? await db.Pedidos.FirstAsync(p => p.Id == pid, ct) : null;
        Medicao? med = l.MedicaoId is { } mid ? await db.Medicoes.Include(m => m.Contrato).FirstAsync(m => m.Id == mid, ct) : null;
        var recs = l.PedidoId is null ? [] : await db.Recebimentos.AsNoTracking().Where(r => r.PedidoId == l.PedidoId).ToListAsync(ct);
        l.FornecedorId = ped?.FornecedorId ?? med?.Contrato.FornecedorId ?? l.FornecedorId;
        l.ObraId = ped?.ObraId ?? med?.ObraId;
        l.ContratoId ??= med?.ContratoId;
        var trava = TravasQualidade.VerificarPagamento(l, ped, recs, med);
        l.Bloqueios = trava.Resumo;
        ValidacaoException.Se(trava.Bloqueios);
        l.Numero = await Numerador.ProximoAsync(db, "Pagamento", ct);
        l.EncaminhadoPor = usuario.Nome; l.EncaminhadoEm = relogio.GetUtcNow();
        db.Pagamentos.Add(l);
        if (med is not null) { med.ChaveNFe = l.ChaveNFe; med.Status = StatusMedicao.NfRecebida; }
        await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Pagamento, l.Id, l.ObraId, $"Pagamento NF {l.ChaveNFe[^9..]}", l.ValorLiberado, ct);
        await db.SaveChangesAsync(ct);
        return l.Id;
    }

    public async Task MarcarPagoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var l = await db.Pagamentos.FirstAsync(x => x.Id == id, ct);
        if (l.Status != StatusPagamento.Liberado) throw new RegraDeNegocioException("Pagamento ainda não liberado pelo Diretor Técnico.");
        l.Status = StatusPagamento.Pago;
        if (l.MedicaoId is { } mid) (await db.Medicoes.FirstAsync(m => m.Id == mid, ct)).Status = StatusMedicao.Paga;
        await db.SaveChangesAsync(ct);
    }

    // ───────── T08–T10 Fornecedor / qualificação ─────────

    public async Task SalvarQualificacaoAsync(FornecedorQualificacao q, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        ValidacaoException.Se(q.Validar());
        q.CalcularValidade(await ParametrosServico.CarregarAsync(db, ct));
        q.ResponsavelUsuarioId ??= usuario.UsuarioId;
        if (await db.Qualificacoes.AnyAsync(x => x.Id == q.Id, ct)) db.Update(q); else db.Qualificacoes.Add(q);
        await db.SaveChangesAsync(ct);
        await RecalcularFornecedoresAsync([q.FornecedorId], ct);
    }

    /// <summary>Recalcula status de qualificação e nota média (6 meses) dos fornecedores.</summary>
    public async Task RecalcularFornecedoresAsync(IReadOnlyCollection<Guid>? ids = null, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var q = db.Fornecedores.Include(f => f.Qualificacoes).AsQueryable();
        if (ids is not null) q = q.Where(f => ids.Contains(f.Id));
        var fs = await q.ToListAsync(ct);
        var desde = Hoje.AddMonths(-6);
        var fids = fs.Select(f => f.Id).ToList();
        var notasMed = await db.Medicoes.AsNoTracking().Where(m => m.NotaFinal != null && m.PeriodoFim >= desde && fids.Contains(m.Contrato.FornecedorId))
            .Select(m => new { m.Contrato.FornecedorId, Nota = m.NotaFinal!.Value }).ToListAsync(ct);
        var desdeDt = new DateTimeOffset(desde.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var recs = await db.Recebimentos.AsNoTracking().Where(r => r.DataRecebimento >= desdeDt && fids.Contains(r.FornecedorId)).ToListAsync(ct);
        foreach (var f in fs)
        {
            f.StatusQualificacao = f.CalcularStatus(Hoje);
            var notas = notasMed.Where(n => n.FornecedorId == f.Id).Select(n => n.Nota)
                .Concat(recs.Where(r => r.FornecedorId == f.Id && r.NotaEntrega is not null).Select(r => r.NotaEntrega!.Value)).ToList();
            f.NotaMedia = notas.Count == 0 ? null : Math.Round(notas.Average(), 2);
        }
        await db.SaveChangesAsync(ct);
    }

    // ───────── T17 Avaliação semestral ─────────

    public async Task<int> ConsolidarSemestreAsync(string periodo, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var (ini, fim) = AvaliacaoSemestral.Intervalo(periodo);
        var iniDt = new DateTimeOffset(ini.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var fimDt = new DateTimeOffset(fim.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        var recs = await db.Recebimentos.AsNoTracking().Where(r => r.DataRecebimento >= iniDt && r.DataRecebimento <= fimDt).ToListAsync(ct);
        var meds = await db.Medicoes.AsNoTracking().Include(m => m.Contrato).Where(m => m.PeriodoFim >= ini && m.PeriodoFim <= fim).ToListAsync(ct);
        var planos = await db.PlanosAcao.AsNoTracking().ToListAsync(ct);
        var fornecedores = recs.Select(r => r.FornecedorId).Concat(meds.Select(m => m.Contrato.FornecedorId)).Distinct().ToList();
        var existentes = await db.AvaliacoesSemestrais.Where(a => a.Periodo == periodo).ToListAsync(ct);
        foreach (var fid in fornecedores)
        {
            var a = existentes.FirstOrDefault(x => x.FornecedorId == fid);
            if (a is null) { a = new AvaliacaoSemestral { Periodo = periodo, FornecedorId = fid }; db.AvaliacoesSemestrais.Add(a); }
            if (a.Decisao is null)
            {
                a.Consolidar(recs, meds, planos);
                a.QtdRnc = await db.Rncs.CountAsync(r => r.FornecedorId == fid && r.CriadoEm >= iniDt && r.CriadoEm <= fimDt, ct);
            }
        }
        await db.SaveChangesAsync(ct);
        return fornecedores.Count;
    }

    public async Task DecidirSemestreAsync(AvaliacaoSemestral a, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        ValidacaoException.Se(a.ValidarDecisao());
        db.Update(a);
        var f = await db.Fornecedores.FirstAsync(x => x.Id == a.FornecedorId, ct);
        switch (a.Decisao)
        {
            case DecisaoSemestral.Desqualificar:
                f.StatusQualificacao = StatusQualificacao.Desqualificado; f.Ativo = false; break;
            case DecisaoSemestral.ManterComPlanoAcao:
                db.PlanosAcao.Add(new PlanoAcaoFornecedor
                {
                    Numero = await NumeroPlanoAsync(db, ct), FornecedorId = f.Id, Origem = OrigemPlanoAcao.AvaliacaoSemestral, OrigemId = a.Id,
                    TipoPlano = TipoPlanoAcao.Completo, Problema = $"Avaliação {a.Periodo}: nota média {a.NotaMedia:0.00}, FVM aprovadas {a.PercFvmAprovadas:0.#}%.",
                    Acao = "A definir com o fornecedor.", Responsavel = "Suprimentos", Prazo = Hoje.AddDays(30)
                });
                break;
        }
        await db.SaveChangesAsync(ct);
    }

    // ───────── T28 Aditivo ─────────

    public async Task<Aditivo> NovoAditivoAsync(Guid contratoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var k = await db.Contratos.AsNoTracking().FirstAsync(x => x.Id == contratoId, ct);
        if (k.Status != StatusContrato.Ativo) throw new RegraDeNegocioException("Aditivo só em contrato ativo.");
        if (await db.Aditivos.AnyAsync(a => a.ContratoId == contratoId && (a.Status == StatusAditivo.EmElaboracao || a.Status == StatusAditivo.AguardandoAprovacao || a.Status == StatusAditivo.AguardandoAssinaturas), ct))
            throw new RegraDeNegocioException("Já existe aditivo em andamento neste contrato.");
        var n = await db.Aditivos.CountAsync(a => a.ContratoId == contratoId, ct) + 1;
        return new Aditivo { ContratoId = k.Id, ObraId = k.ObraId, Numero = $"AD-{n:00}" };
    }

    public async Task<Guid> SalvarAditivoAsync(Aditivo a, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var k = await db.Contratos.Include(x => x.Itens).AsNoTracking().FirstAsync(x => x.Id == a.ContratoId, ct);
        if (a.Status != StatusAditivo.EmElaboracao) throw new RegraDeNegocioException("Aditivo fora de elaboração não pode ser alterado.");
        foreach (var i in a.Itens)
        {
            i.AditivoId = a.Id;
            if (k.Itens.FirstOrDefault(x => x.Id == i.ContratoItemId) is { } ci) { i.QtdOriginal = ci.Quantidade; i.PrecoOriginal = ci.PrecoUnitario; }
        }
        ValidacaoException.Se(a.Validar(k));
        var original = await OriginalAsync(db, k, ct);
        var anteriores = await db.Aditivos.Where(x => x.ContratoId == k.Id && x.Status == StatusAditivo.Vigente && x.Id != a.Id).SumAsync(x => x.Valor, ct);
        a.Calcular(original, anteriores);
        a.Contrato = null;
        await Grafo.SalvarAsync(db, a, nameof(Aditivo.Itens));
        await db.SaveChangesAsync(ct);
        return a.Id;
    }

    /// <summary>Valor original = valor atual − aditivos vigentes (os itens do contrato já refletem os aditivos aplicados).</summary>
    private static async Task<decimal> OriginalAsync(QualyHubDbContext db, Contrato k, CancellationToken ct) =>
        k.ValorTotal - await db.Aditivos.Where(x => x.ContratoId == k.Id && x.Status == StatusAditivo.Vigente).SumAsync(x => x.Valor, ct);

    public async Task EnviarAditivoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var a = await db.Aditivos.Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
        var k = await db.Contratos.Include(x => x.Fornecedor).FirstAsync(x => x.Id == a.ContratoId, ct);
        await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Aditivo, a.Id, a.ObraId, $"{k.Numero} · {a.Numero} – {k.Fornecedor.NomeExibicao}", Math.Abs(a.Valor), ct);
        a.Status = StatusAditivo.AguardandoAprovacao;
        await db.SaveChangesAsync(ct);
    }

    public async Task AssinarAditivoAsync(Guid id, DateOnly empresa, DateOnly fornecedor, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var a = await db.Aditivos.Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
        if (a.Status != StatusAditivo.AguardandoAssinaturas) throw new RegraDeNegocioException("Aditivo não está aguardando assinaturas.");
        var k = await db.Contratos.Include(x => x.Itens).FirstAsync(x => x.Id == a.ContratoId, ct);
        a.AssinaturaEmpresa = empresa; a.AssinaturaFornecedor = fornecedor;
        a.Aplicar(k);
        await db.SaveChangesAsync(ct);
    }

    public async Task<string> NumeroPlanoAsync(QualyHubDbContext db, CancellationToken ct) => $"PA-{await Numerador.ProximoAsync(db, "PlanoAcao", ct):0000}";
}
