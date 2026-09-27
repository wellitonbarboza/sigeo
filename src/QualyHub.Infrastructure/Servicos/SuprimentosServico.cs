using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Regras;
using QualyHub.Domain.Suprimentos;
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
        await Grafo.SalvarAsync(db, r, nameof(Requisicao.Itens));
        await db.SaveChangesAsync(ct);
        return r.Id;
    }

    public async Task EnviarRequisicaoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var r = await db.Requisicoes.Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
        var temEscopo = await db.Anexos.AnyAsync(a => a.Entidade == nameof(Requisicao) && a.EntidadeId == id && a.Categoria == "Escopo", ct);
        ValidacaoException.Se(r.ValidarEnvio(temEscopo));
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
        await Grafo.SalvarAsync(db, c, nameof(Cotacao.Fornecedores), nameof(Cotacao.Itens));
        await db.SaveChangesAsync(ct);
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
        var c = await db.Cotacoes.Include(x => x.Itens).ThenInclude(i => i.RequisicaoItem).Include(x => x.Requisicao).FirstAsync(x => x.Id == cotacaoId, ct);
        if (c.Status != StatusCotacao.Aprovado) throw new RegraDeNegocioException("Cotação ainda não aprovada.");
        var p = await ParametrosServico.CarregarAsync(db, ct);
        var n = await Numerador.ProximoAsync(db, $"Contrato-{Hoje.Year}", ct);
        var k = new Contrato
        {
            ObraId = c.ObraId, Numero = $"CT-{Hoje.Year}-{n:000}", FornecedorId = c.FornecedorVencedorId!.Value, RequisicaoId = c.RequisicaoId, CotacaoId = c.Id,
            Categoria = c.Requisicao?.CategoriaServico ?? "Serviço de obra", Objeto = c.Nome, RetencaoPerc = p[Parametros.RetencaoPadraoPerc],
            VigenciaInicio = Hoje, VigenciaFim = Hoje.AddMonths(6)
        };
        var seq = 0;
        foreach (var i in c.Itens.Where(i => i.FornecedorId == c.FornecedorVencedorId))
        {
            seq++;
            k.Itens.Add(new ContratoItem
            {
                Item = $"1.{seq}", Descricao = i.Descricao, ServicoControladoId = i.RequisicaoItem?.ServicoControladoId, Unidade = i.Unidade,
                Quantidade = i.Quantidade, PrecoUnitario = i.ValorUnitarioFinal
            });
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
        }
        ValidacaoException.Se(r.Validar());
        r.Numero = await Numerador.ProximoAsync(db, "Recebimento", ct);
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

    // ───────── T14–T16 Medição ─────────

    public async Task<Guid> NovaMedicaoAsync(Guid contratoId, DateOnly inicio, DateOnly fim, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var k = await db.Contratos.Include(x => x.Itens).FirstAsync(x => x.Id == contratoId, ct);
        if (k.Status != StatusContrato.Ativo) throw new RegraDeNegocioException("Contrato não está ativo.");
        if (await db.Medicoes.AnyAsync(m => m.ContratoId == contratoId && m.Status == StatusMedicao.EmElaboracao, ct))
            throw new RegraDeNegocioException("Já existe medição em elaboração para este contrato.");
        var numero = (await db.Medicoes.Where(m => m.ContratoId == contratoId).MaxAsync(m => (int?)m.NumeroMedicao, ct) ?? 0) + 1;
        var m = new Medicao { ContratoId = k.Id, ObraId = k.ObraId, NumeroMedicao = numero, PeriodoInicio = inicio, PeriodoFim = fim };
        foreach (var i in k.Itens) m.Itens.Add(new MedicaoItem { ContratoItemId = i.Id });
        db.Medicoes.Add(m);
        await db.SaveChangesAsync(ct);
        return m.Id;
    }

    public async Task SalvarMedicaoAsync(Guid id, IReadOnlyDictionary<Guid, (decimal Qtd, string? Fvs)> quantidades,
        decimal? escopo, decimal? material, decimal? servico, decimal? limpeza, decimal? docSeg, DateOnly? reuniao, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var m = await CarregarMedicaoAsync(db, id, ct);
        if (m.Status is not (StatusMedicao.EmElaboracao or StatusMedicao.Reprovada)) throw new RegraDeNegocioException("Medição já enviada.");
        foreach (var i in m.Itens)
            if (quantidades.TryGetValue(i.Id, out var q)) { i.QtdPeriodo = q.Qtd; i.FvsRef = q.Fvs; }
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
        var fvs = await db.Fichas.AsNoTracking().Where(f => f.ObraId == m.ObraId && f.Tipo == TipoFicha.FVS).ToListAsync(ct);
        var trava = TravasQualidade.VerificarMedicao(m, fvs, Hoje);
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
            .Include(x => x.Contrato).ThenInclude(k => k.Fornecedor).ThenInclude(f => f.Documentos).ThenInclude(d => d.TipoDocumento)
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
            if (a.Decisao is null) a.Consolidar(recs, meds, planos);
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

    public async Task<string> NumeroPlanoAsync(QualyHubDbContext db, CancellationToken ct) => $"PA-{await Numerador.ProximoAsync(db, "PlanoAcao", ct):0000}";
}
