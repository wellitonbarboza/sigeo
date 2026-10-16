using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Financeiro;
using Sigeo.Domain.Incorporacao;

namespace Sigeo.Application.Servicos;

public sealed record LinhaUnidade(Guid Id, Guid LocalId, string Unidade, string? Torre, string? Pavimento, int Ordem, string? Tipologia, decimal? Area,
    decimal PrecoTabela, SituacaoUnidade Situacao, string? Comprador, Guid? ContratoId, decimal? ValorVenda, int OrdemPavimento = 0);

public sealed record LinhaContratoVenda(Guid Id, int Numero, string Unidade, string Comprador, DateOnly Data, decimal Valor, StatusContratoVenda Status,
    decimal Recebido, decimal AReceber, int Atrasadas);

public sealed record LinhaCarteira(Guid ParcelaId, Guid TituloId, Guid ContratoId, int Contrato, string Unidade, string Comprador, TipoParcelaVenda Tipo,
    int Numero, DateOnly Vencimento, decimal ValorBase, decimal ValorCorrigido, StatusParcelaTitulo Status, DateOnly? DataPagamento, decimal ValorPago, int DiasAtraso);

public sealed record PainelVendas(decimal Vgv, decimal VgvVendido, decimal VgvEstoque, int Unidades, int Vendidas, int Disponiveis,
    decimal Recebido, decimal AReceber, decimal Inadimplente, IReadOnlyList<(DateOnly Mes, int Vendas, decimal Valor)> Vso,
    IReadOnlyList<(DateOnly Mes, decimal Valor)> ProjecaoRecebimentos, IReadOnlyList<DateOnly> MesesSemIndice);

public sealed record CalculoParcela(decimal ValorBase, decimal Correcao, decimal Juros, decimal Total, IReadOnlyList<DateOnly> MesesSemIndice);

/// <summary>
/// Módulo Incorporação (adicional): dados da incorporação e RET, unidades e espelho de vendas, compradores, contratos com condição
/// em blocos (entrada, mensais, balões, chaves, financiamento, permuta), comissões, carteira corrigida e apuração mensal do RET.
/// As parcelas e as comissões são títulos do Financeiro (um razão só).
/// </summary>
public sealed class IncorporacaoServico(IFabricaDb fabrica, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────────── Incorporação, alíquotas e índices ─────────────

    public async Task<IncorporacaoObra> ObterAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Incorporacoes.FirstOrDefaultAsync(i => i.ObraId == obraId, ct)
               ?? new IncorporacaoObra { ObraId = obraId, Nome = await db.Obras.Where(o => o.Id == obraId).Select(o => o.Nome).FirstAsync(ct) };
    }

    public async Task SalvarAsync(IncorporacaoObra i, CancellationToken ct = default)
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(i.Nome)) e.Add("Informe o nome do empreendimento.");
        if (i.Regime is RegimeTributario.RetGeral or RegimeTributario.RetSocial && !i.PatrimonioAfetacao) e.Add("O RET exige patrimônio de afetação: marque e informe a averbação.");
        if (i.DiaVencimentoRet is < 1 or > 28) e.Add("Dia de vencimento do DARF entre 1 e 28.");
        if (i.JurosAposChavesMes is < 0 or > 5) e.Add("Juros após as chaves entre 0 e 5% ao mês.");
        ValidacaoException.Se(e);
        i.CnpjIncorporacao = string.IsNullOrWhiteSpace(i.CnpjIncorporacao) ? null : Comum.Conversor.SomenteDigitos(i.CnpjIncorporacao) is { Length: 14 } d ? Comum.Conversor.FormatarCnpj(d)
            : throw new ValidacaoException(["CNPJ da incorporação inválido."]);
        await using var db = fabrica.Criar();
        await Grafo.SalvarAsync(db, i);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<AliquotaRegime>> AliquotasAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.AliquotasRegime.OrderBy(a => a.Regime).ThenByDescending(a => a.VigenciaInicio).ToListAsync(ct);
    }

    public async Task SalvarAliquotaAsync(AliquotaRegime a, CancellationToken ct = default)
    {
        if (a.Total <= 0 || a.Total > 30) throw new ValidacaoException(["Alíquota total entre 0 e 30%."]);
        await using var db = fabrica.Criar();
        if (await db.AliquotasRegime.AnyAsync(x => x.Id != a.Id && x.Regime == a.Regime && x.VigenciaInicio == a.VigenciaInicio, ct))
            throw new RegraDeNegocioException("Já existe vigência com essa data para o regime.");
        await Grafo.SalvarAsync(db, a);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<IndiceEconomico>> IndicesAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.IndicesEconomicos.OrderByDescending(i => i.Competencia).ThenBy(i => i.Indice).Take(600).ToListAsync(ct);
    }

    public async Task SalvarIndiceAsync(string indice, DateOnly competencia, decimal variacao, CancellationToken ct = default)
    {
        indice = (indice ?? "").Trim().ToUpperInvariant();
        if (indice.Length == 0) throw new ValidacaoException(["Informe o índice (INCC, IPCA…)."]);
        if (variacao is < -20 or > 20) throw new ValidacaoException(["Variação mensal fora do esperado (−20% a 20%)."]);
        var mes = new DateOnly(competencia.Year, competencia.Month, 1);
        await using var db = fabrica.Criar();
        var x = await db.IndicesEconomicos.FirstOrDefaultAsync(i => i.Indice == indice && i.Competencia == mes, ct);
        if (x is null) db.IndicesEconomicos.Add(new IndiceEconomico { Indice = indice, Competencia = mes, VariacaoPerc = variacao });
        else x.VariacaoPerc = variacao;
        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<(string, DateOnly), decimal>> MapaIndicesAsync(ISigeoDb db, CancellationToken ct) =>
        (await db.IndicesEconomicos.AsNoTracking().ToListAsync(ct)).ToDictionary(i => (i.Indice, i.Competencia), i => i.VariacaoPerc);

    // ───────────── Unidades e espelho ─────────────

    /// <summary>Cria a unidade comercial para cada apartamento/casa da obra que ainda não tem. Retorna quantas criou.</summary>
    public async Task<int> GerarUnidadesAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var aptos = await db.Locais.Where(l => l.ObraId == obraId && l.Tipo == GeradorEmpreendimento.TipoApartamento).Select(l => l.Id).ToListAsync(ct);
        var ja = await db.UnidadesComerciais.Where(u => u.ObraId == obraId).Select(u => u.LocalId).ToListAsync(ct);
        var novos = aptos.Except(ja).ToList();
        foreach (var l in novos) db.UnidadesComerciais.Add(new UnidadeComercial { ObraId = obraId, LocalId = l });
        await db.SaveChangesAsync(ct);
        return novos.Count;
    }

    public async Task<List<LinhaUnidade>> EspelhoAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var unidades = await db.UnidadesComerciais.Where(u => u.ObraId == obraId).ToListAsync(ct);
        var locais = await db.Locais.Where(l => l.ObraId == obraId).ToDictionaryAsync(l => l.Id, ct);
        var tipologias = await db.Tipologias.Where(t => t.ObraId == obraId).ToDictionaryAsync(t => t.Id, t => t.Nome, ct);
        var contratos = await db.ContratosVenda.Where(c => c.ObraId == obraId && c.Status != StatusContratoVenda.Distratado).ToListAsync(ct);
        var compIds = contratos.Select(c => c.CompradorId).Distinct().ToList();
        var comps = await db.Compradores.Where(c => compIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Nome, ct);
        Domain.Estrutura.Local? Ancestral(Guid? id, string tipo)
        {
            for (var x = id; x is { } i && locais.TryGetValue(i, out var l); x = l.LocalPaiId) if (l.Tipo == tipo) return l;
            return null;
        }
        return unidades.Select(u =>
        {
            var l = locais.GetValueOrDefault(u.LocalId);
            var c = contratos.FirstOrDefault(k => k.UnidadeComercialId == u.Id);
            var pav = Ancestral(l?.LocalPaiId, GeradorEmpreendimento.TipoPavimento);
            return new LinhaUnidade(u.Id, u.LocalId, l?.Nome ?? "?", Ancestral(l?.LocalPaiId, GeradorEmpreendimento.TipoTorre)?.Nome, pav?.Nome,
                l?.Ordem ?? 0, l?.TipologiaId is { } t ? tipologias.GetValueOrDefault(t) : null, l?.AreaPrivativa, u.PrecoTabela, u.Situacao,
                c is null ? null : comps.GetValueOrDefault(c.CompradorId), c?.Id, c?.ValorVenda, pav?.Ordem ?? 0);
        }).OrderBy(x => x.Torre).ThenBy(x => x.OrdemPavimento).ThenBy(x => x.Ordem).ThenBy(x => x.Unidade).ToList();
    }

    public async Task SalvarUnidadeAsync(Guid id, decimal preco, SituacaoUnidade situacao, string? vagas, string? obs, CancellationToken ct = default)
    {
        if (preco < 0) throw new ValidacaoException(["Preço não pode ser negativo."]);
        await using var db = fabrica.Criar();
        var u = await db.UnidadesComerciais.FirstAsync(x => x.Id == id, ct);
        var vendida = await db.ContratosVenda.AnyAsync(c => c.UnidadeComercialId == id && c.Status != StatusContratoVenda.Distratado, ct);
        if (vendida && situacao is not (SituacaoUnidade.Vendida or SituacaoUnidade.Permuta)) throw new RegraDeNegocioException("Unidade com contrato ativo: a situação só muda pelo distrato.");
        if (!vendida && situacao is SituacaoUnidade.Vendida or SituacaoUnidade.Permuta) throw new RegraDeNegocioException("Vendida/permuta vem do contrato de venda.");
        u.PrecoTabela = preco; u.Situacao = situacao; u.Vagas = vagas?.Trim(); u.Observacao = obs?.Trim();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Reajusta a tabela das unidades disponíveis (e reservadas) em %.</summary>
    public async Task<int> ReajustarTabelaAsync(Guid obraId, decimal perc, CancellationToken ct = default)
    {
        if (perc is < -50 or > 100 or 0) throw new ValidacaoException(["Percentual entre −50% e 100% (diferente de zero)."]);
        await using var db = fabrica.Criar();
        var us = await db.UnidadesComerciais.Where(u => u.ObraId == obraId && (u.Situacao == SituacaoUnidade.Disponivel || u.Situacao == SituacaoUnidade.Reservada)).ToListAsync(ct);
        foreach (var u in us) u.PrecoTabela = Math.Round(u.PrecoTabela * (1 + perc / 100m), 2);
        await db.SaveChangesAsync(ct);
        return us.Count;
    }

    // ───────────── Compradores ─────────────

    public async Task<List<Comprador>> CompradoresAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Compradores.OrderBy(c => c.Nome).ToListAsync(ct);
    }

    public async Task SalvarCompradorAsync(Comprador c, CancellationToken ct = default)
    {
        c.Nome = (c.Nome ?? "").Trim();
        var e = new List<string>();
        if (c.Nome.Length == 0) e.Add("Informe o nome do comprador.");
        var d = Comum.Conversor.SomenteDigitos(c.CpfCnpj);
        if (d.Length > 0 && !(Comum.Conversor.CpfValido(d) || Comum.Conversor.CnpjValido(d))) e.Add("CPF/CNPJ inválido.");
        ValidacaoException.Se(e);
        c.CpfCnpj = d.Length == 0 ? null : d.Length == 14 ? Comum.Conversor.FormatarCnpj(d) : Convert.ToUInt64(d).ToString(@"000\.000\.000\-00");
        await using var db = fabrica.Criar();
        await Grafo.SalvarAsync(db, c);
        await db.SaveChangesAsync(ct);
    }

    // ───────────── Contratos de venda ─────────────

    public async Task<List<LinhaContratoVenda>> ContratosAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var cs = await db.ContratosVenda.Where(c => c.ObraId == obraId).OrderByDescending(c => c.Numero).ToListAsync(ct);
        var esp = (await EspelhoAsync(obraId, ct)).ToDictionary(u => u.Id);
        var comps = await db.Compradores.ToDictionaryAsync(c => c.Id, c => c.Nome, ct);
        var titIds = cs.Where(c => c.TituloId != null).Select(c => c.TituloId!.Value).ToList();
        var parcelas = await db.TituloParcelas.Where(p => titIds.Contains(p.TituloId) && p.Status != StatusParcelaTitulo.Cancelada).ToListAsync(ct);
        var hoje = Hoje;
        return cs.Select(c =>
        {
            var ps = parcelas.Where(p => p.TituloId == c.TituloId).ToList();
            var u = esp.GetValueOrDefault(c.UnidadeComercialId);
            return new LinhaContratoVenda(c.Id, c.Numero, u is null ? "?" : $"{u.Torre} {u.Unidade}".Trim(), comps.GetValueOrDefault(c.CompradorId) ?? "?",
                c.DataContrato, c.ValorVenda, c.Status, ps.Where(p => p.Status == StatusParcelaTitulo.Paga).Sum(p => p.ValorPago),
                ps.Where(p => p.Status == StatusParcelaTitulo.Aberta).Sum(p => p.Valor), ps.Count(p => p.Vencida(hoje)));
        }).ToList();
    }

    public async Task<ContratoVenda?> ObterContratoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.ContratosVenda.Include(c => c.Blocos).Include(c => c.Comissoes).FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    /// <summary>
    /// Grava o contrato e gera (ou refaz, se nada foi recebido) o título a receber com as parcelas dos blocos e as comissões a pagar.
    /// A unidade passa a Vendida (Permuta se toda a condição for permuta).
    /// </summary>
    public async Task<Guid> SalvarContratoAsync(ContratoVenda c, CancellationToken ct = default)
    {
        if (c.DataBase == default) c.DataBase = c.DataContrato;
        foreach (var k in c.Comissoes) if (k.Valor <= 0 && k.Percentual > 0) k.Valor = Math.Round(c.ValorVenda * k.Percentual / 100m, 2);
        ValidacaoException.Se(c.Validar());
        Guid id = c.Id;
        await fabrica.EmTransacaoAsync(async db =>
        {
            var inc = await db.Incorporacoes.FirstOrDefaultAsync(i => i.ObraId == c.ObraId, ct);
            var unidade = await db.UnidadesComerciais.FirstAsync(u => u.Id == c.UnidadeComercialId, ct);
            if (unidade.ObraId != c.ObraId) throw new RegraDeNegocioException("A unidade é de outra obra.");
            if (await db.ContratosVenda.AnyAsync(x => x.Id != c.Id && x.UnidadeComercialId == c.UnidadeComercialId && x.Status != StatusContratoVenda.Distratado, ct))
                throw new RegraDeNegocioException("Unidade já vendida em outro contrato ativo.");
            var atual = await db.ContratosVenda.Include(x => x.Blocos).Include(x => x.Comissoes).FirstOrDefaultAsync(x => x.Id == c.Id, ct);
            if (atual is not null)
            {
                if (atual.Status != StatusContratoVenda.Ativo) throw new RegraDeNegocioException("Contrato quitado ou distratado não é alterado.");
                if (atual.TituloId is { } tid && await db.TituloParcelas.AnyAsync(p => p.TituloId == tid && p.Status == StatusParcelaTitulo.Paga, ct))
                    throw new RegraDeNegocioException("Contrato já tem parcela recebida: renegociações entram como novo aditivo no financeiro (ajuste as parcelas em Contas a Receber).");
                if (atual.UnidadeComercialId != c.UnidadeComercialId && await db.UnidadesComerciais.FirstOrDefaultAsync(u => u.Id == atual.UnidadeComercialId, ct) is { } velha)
                    velha.Situacao = SituacaoUnidade.Disponivel;
                await DesfazerTitulosAsync(db, atual, ct);
                db.Set<BlocoCondicaoVenda>().RemoveRange(atual.Blocos);
                db.Set<ComissaoVenda>().RemoveRange(atual.Comissoes);
                db.Entry(atual).CurrentValues.SetValues(c);
                foreach (var b in c.Blocos) { b.Id = Guid.CreateVersion7(); b.ContratoVendaId = atual.Id; db.Set<BlocoCondicaoVenda>().Add(b); }
                foreach (var k in c.Comissoes) { k.Id = Guid.CreateVersion7(); k.ContratoVendaId = atual.Id; k.TituloId = null; db.Set<ComissaoVenda>().Add(k); }
                c = atual;
            }
            else
            {
                c.Numero = await Numerador.ProximoAsync(db, "ContratoVenda", ct);
                if (inc is not null)
                {
                    if (string.IsNullOrWhiteSpace(c.IndiceAteChaves)) c.IndiceAteChaves = inc.IndiceAteChaves;
                    if (string.IsNullOrWhiteSpace(c.IndiceAposChaves)) c.IndiceAposChaves = inc.IndiceAposChaves;
                    c.DataChaves ??= inc.PrevisaoChaves;
                }
                foreach (var b in c.Blocos) b.ContratoVendaId = c.Id;
                foreach (var k in c.Comissoes) k.ContratoVendaId = c.Id;
                db.ContratosVenda.Add(c);
            }
            unidade.Situacao = c.Blocos.All(b => b.Tipo == TipoParcelaVenda.Permuta) ? SituacaoUnidade.Permuta : SituacaoUnidade.Vendida;
            await GerarTitulosAsync(db, c, unidade, ct);
            await db.SaveChangesAsync(ct);
            id = c.Id;
        }, ct);
        return id;
    }

    private static async Task<Guid?> ContaAsync(ISigeoDb db, string codigo, CancellationToken ct) =>
        await db.ContasGerenciais.Where(x => x.Codigo == codigo).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);

    private static async Task GerarTitulosAsync(ISigeoDb db, ContratoVenda c, UnidadeComercial u, CancellationToken ct)
    {
        var local = await db.Locais.AsNoTracking().Where(l => l.Id == u.LocalId).Select(l => l.Nome).FirstOrDefaultAsync(ct) ?? "unidade";
        var t = new Titulo
        {
            Tipo = TipoTitulo.Receber, Numero = await Numerador.ProximoAsync(db, "TituloReceber", ct), DocumentoTipo = "Contrato", DocumentoNumero = c.Numero.ToString(),
            CompradorId = c.CompradorId, ObraId = c.ObraId, ContaGerencialId = await ContaAsync(db, "1.1", ct), Descricao = $"Venda {local} — contrato {c.Numero}",
            Emissao = c.DataContrato, Competencia = new DateOnly(c.DataContrato.Year, c.DataContrato.Month, 1), ValorBruto = c.ValorVenda,
            Origem = OrigemTitulo.VendaUnidade, OrigemId = c.Id, ChaveOrigem = $"VENDA:{c.Id:N}:{Guid.CreateVersion7():N}"
        };
        var n = 0;
        foreach (var b in c.Blocos.OrderBy(b => b.PrimeiroVencimento))
            foreach (var (venc, valor) in b.Parcelas())
                t.Parcelas.Add(new TituloParcela { TituloId = t.Id, Vencimento = venc, Valor = valor, TipoVenda = b.Tipo, FornecedorPermutaId = b.FornecedorPermutaId });
        foreach (var p in t.Parcelas.OrderBy(p => p.Vencimento)) p.Numero = ++n;
        db.Titulos.Add(t);
        c.TituloId = t.Id;

        var contaComissao = await ContaAsync(db, "4.1", ct);
        foreach (var k in c.Comissoes.Where(k => k.Forma != FormaComissao.AposPercentualPago))
            k.TituloId = await TituloComissaoAsync(db, c, k, local, contaComissao, ct);
    }

    private static async Task<Guid> TituloComissaoAsync(ISigeoDb db, ContratoVenda c, ComissaoVenda k, string local, Guid? conta, CancellationToken ct)
    {
        var tc = new Titulo
        {
            Tipo = TipoTitulo.Pagar, Numero = await Numerador.ProximoAsync(db, "TituloPagar", ct), DocumentoTipo = "Comissão", DocumentoNumero = c.Numero.ToString(),
            FornecedorId = k.FornecedorId, ObraId = c.ObraId, ContaGerencialId = conta, Descricao = $"Comissão ({k.Papel}) venda {local} — contrato {c.Numero}",
            Emissao = c.DataContrato, Competencia = new DateOnly(c.DataContrato.Year, c.DataContrato.Month, 1), ValorBruto = k.Valor,
            Origem = OrigemTitulo.Comissao, OrigemId = c.Id, ChaveOrigem = $"COM:{k.Id:N}"
        };
        var parcelas = k.Forma == FormaComissao.Parcelada ? Math.Max(k.Parcelas, 2) : 1;
        var cada = Math.Round(k.Valor / parcelas, 2);
        var inicio = k.Forma == FormaComissao.AposPercentualPago ? DateOnly.FromDateTime(DateTime.Today) : c.DataContrato;
        for (var i = 0; i < parcelas; i++)
            tc.Parcelas.Add(new TituloParcela { TituloId = tc.Id, Numero = i + 1, Vencimento = inicio.AddMonths(i), Valor = i == parcelas - 1 ? k.Valor - cada * (parcelas - 1) : cada });
        db.Titulos.Add(tc);
        return tc.Id;
    }

    /// <summary>Cancela o título a receber e as comissões ainda não pagas do contrato (para refazer ou distratar).</summary>
    private static async Task DesfazerTitulosAsync(ISigeoDb db, ContratoVenda c, CancellationToken ct)
    {
        var ids = c.Comissoes.Where(k => k.TituloId != null).Select(k => k.TituloId!.Value).Append(c.TituloId ?? Guid.Empty).ToList();
        foreach (var t in await db.Titulos.Include(t => t.Parcelas).Where(t => ids.Contains(t.Id)).ToListAsync(ct))
        {
            var pagas = t.Parcelas.Any(p => p.Status == StatusParcelaTitulo.Paga);
            foreach (var p in t.Parcelas.Where(p => p.Status == StatusParcelaTitulo.Aberta)) p.Status = StatusParcelaTitulo.Cancelada;
            if (!pagas) { t.Cancelado = true; t.MotivoCancelamento = "Contrato de venda alterado/distratado"; t.ChaveOrigem = null; }
        }
    }

    /// <summary>
    /// Distrato: cancela as parcelas a receber em aberto e as comissões não pagas, devolve a unidade ao estoque e, se houver devolução
    /// ao comprador, gera o título a pagar (conta 8.1). Retenção conforme contrato/Lei 13.786/2018 (definida pelo jurídico).
    /// </summary>
    public async Task DistratarAsync(Guid id, DateOnly data, decimal valorDevolvido, string motivo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new ValidacaoException(["Informe o motivo do distrato."]);
        if (valorDevolvido < 0) throw new ValidacaoException(["Valor devolvido não pode ser negativo."]);
        await fabrica.EmTransacaoAsync(async db =>
        {
            var c = await db.ContratosVenda.Include(x => x.Comissoes).FirstAsync(x => x.Id == id, ct);
            if (c.Status != StatusContratoVenda.Ativo) throw new RegraDeNegocioException("Só contrato ativo é distratado.");
            var recebido = c.TituloId is { } tid ? await db.TituloParcelas.Where(p => p.TituloId == tid && p.Status == StatusParcelaTitulo.Paga).SumAsync(p => p.ValorPago, ct) : 0;
            if (valorDevolvido > recebido) throw new RegraDeNegocioException($"Devolução maior que o recebido ({recebido:N2}).");
            await DesfazerTitulosAsync(db, c, ct);
            foreach (var k in c.Comissoes) k.Estornada = k.TituloId is null || !await db.TituloParcelas.AnyAsync(p => p.TituloId == k.TituloId && p.Status == StatusParcelaTitulo.Paga, ct);
            c.Status = StatusContratoVenda.Distratado; c.DataDistrato = data; c.ValorDevolvido = valorDevolvido; c.Observacao = $"{c.Observacao}\nDistrato: {motivo.Trim()}".Trim();
            var u = await db.UnidadesComerciais.FirstAsync(x => x.Id == c.UnidadeComercialId, ct);
            u.Situacao = SituacaoUnidade.Disponivel;
            if (valorDevolvido > 0)
            {
                var comprador = await db.Compradores.Where(x => x.Id == c.CompradorId).Select(x => x.Nome).FirstAsync(ct);
                var t = new Titulo
                {
                    Tipo = TipoTitulo.Pagar, Numero = await Numerador.ProximoAsync(db, "TituloPagar", ct), DocumentoTipo = "Distrato", DocumentoNumero = c.Numero.ToString(),
                    Pessoa = comprador, CompradorId = c.CompradorId, ObraId = c.ObraId, ContaGerencialId = await ContaAsync(db, "8.1", ct),
                    Descricao = $"Devolução distrato contrato {c.Numero}", Emissao = data, Competencia = new DateOnly(data.Year, data.Month, 1), ValorBruto = valorDevolvido,
                    Origem = OrigemTitulo.Distrato, OrigemId = c.Id, ChaveOrigem = $"DIS:{c.Id:N}"
                };
                t.Parcelas.Add(new TituloParcela { TituloId = t.Id, Numero = 1, Vencimento = data.AddDays(30), Valor = valorDevolvido });
                db.Titulos.Add(t);
            }
            await db.SaveChangesAsync(ct);
        }, ct);
    }

    /// <summary>Depois de cada recebimento da venda: libera comissões "após X% pago" e marca o contrato quitado.</summary>
    public static async Task AposRecebimentoAsync(ISigeoDb db, Guid contratoId, CancellationToken ct)
    {
        var c = await db.ContratosVenda.Include(x => x.Comissoes).FirstOrDefaultAsync(x => x.Id == contratoId, ct);
        if (c?.TituloId is not { } tid || c.Status != StatusContratoVenda.Ativo) return;
        var ps = await db.TituloParcelas.Where(p => p.TituloId == tid && p.Status != StatusParcelaTitulo.Cancelada).ToListAsync(ct);
        var pagoBase = ps.Where(p => p.Status == StatusParcelaTitulo.Paga).Sum(p => p.Valor);
        var perc = c.ValorVenda == 0 ? 0 : pagoBase / c.ValorVenda * 100m;
        var local = await (from u in db.UnidadesComerciais join l in db.Locais on u.LocalId equals l.Id where u.Id == c.UnidadeComercialId select l.Nome).FirstOrDefaultAsync(ct) ?? "unidade";
        foreach (var k in c.Comissoes.Where(k => k.Forma == FormaComissao.AposPercentualPago && k.TituloId is null && !k.Estornada && perc >= (k.PercentualPagoMinimo ?? 100)))
            k.TituloId = await TituloComissaoAsync(db, c, k, local, await ContaAsync(db, "4.1", ct), ct);
        if (ps.Count > 0 && ps.All(p => p.Status == StatusParcelaTitulo.Paga)) c.Status = StatusContratoVenda.Quitado;
    }

    // ───────────── Carteira ─────────────

    /// <summary>Valor da parcela na data (base + correção pelo índice do contrato + juros após as chaves).</summary>
    public async Task<CalculoParcela> CalcularParcelaAsync(Guid parcelaId, DateOnly data, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var p = await db.TituloParcelas.Include(x => x.Titulo).FirstAsync(x => x.Id == parcelaId, ct);
        if (p.Titulo.Origem != OrigemTitulo.VendaUnidade || p.Titulo.OrigemId is not { } cid) return new(p.Valor, 0, 0, p.Valor, []);
        var c = await db.ContratosVenda.FirstAsync(x => x.Id == cid, ct);
        var r = Corrigir(p, c, data, await MapaIndicesAsync(db, ct));
        return new CalculoParcela(p.Valor, r.Correcao, r.Juros, r.ValorCorrigido, r.MesesSemIndice);
    }

    private static MotorCorrecao.Resultado Corrigir(TituloParcela p, ContratoVenda c, DateOnly data, IReadOnlyDictionary<(string, DateOnly), decimal> idx) =>
        p.TipoVenda is TipoParcelaVenda.Financiamento or TipoParcelaVenda.Permuta
            ? new MotorCorrecao.Resultado(1, p.Valor, 0, 0, []) // repasse bancário e permuta não corrigem
            : MotorCorrecao.Corrigir(p.Valor, c.DataBase, data > p.Vencimento ? data : p.Vencimento, c.DataChaves, c.IndiceAteChaves, c.IndiceAposChaves, c.JurosAposChavesMes, idx);

    public async Task<List<LinhaCarteira>> CarteiraAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var cs = await db.ContratosVenda.Where(c => c.ObraId == obraId && c.TituloId != null).ToListAsync(ct);
        var tIds = cs.Select(c => c.TituloId!.Value).ToList();
        var ps = await db.TituloParcelas.Where(p => tIds.Contains(p.TituloId) && p.Status != StatusParcelaTitulo.Cancelada).OrderBy(p => p.Vencimento).ToListAsync(ct);
        var esp = (await EspelhoAsync(obraId, ct)).ToDictionary(u => u.Id);
        var comps = await db.Compradores.ToDictionaryAsync(c => c.Id, c => c.Nome, ct);
        var idx = await MapaIndicesAsync(db, ct);
        var hoje = Hoje;
        return ps.Select(p =>
        {
            var c = cs.First(x => x.TituloId == p.TituloId);
            var u = esp.GetValueOrDefault(c.UnidadeComercialId);
            var corr = p.Status == StatusParcelaTitulo.Paga ? p.ValorPago : Corrigir(p, c, hoje, idx).ValorCorrigido;
            return new LinhaCarteira(p.Id, p.TituloId, c.Id, c.Numero, u is null ? "?" : $"{u.Torre} {u.Unidade}".Trim(), comps.GetValueOrDefault(c.CompradorId) ?? "?",
                p.TipoVenda, p.Numero, p.Vencimento, p.Valor, corr, p.Status, p.DataPagamento, p.ValorPago, p.DiasAtraso(hoje));
        }).ToList();
    }

    public async Task<PainelVendas> PainelAsync(Guid obraId, CancellationToken ct = default)
    {
        var esp = await EspelhoAsync(obraId, ct);
        var cart = await CarteiraAsync(obraId, ct);
        await using var db = fabrica.CriarLeitura();
        var vendas = await db.ContratosVenda.Where(c => c.ObraId == obraId && c.Status != StatusContratoVenda.Distratado).Select(c => new { c.DataContrato, c.ValorVenda }).ToListAsync(ct);
        var vendido = esp.Where(u => u.ContratoId != null).Sum(u => u.ValorVenda ?? 0);
        var estoque = esp.Where(u => u.ContratoId == null && u.Situacao != SituacaoUnidade.Bloqueada).Sum(u => u.PrecoTabela);
        var vso = vendas.GroupBy(v => new DateOnly(v.DataContrato.Year, v.DataContrato.Month, 1)).OrderBy(g => g.Key)
            .Select(g => (g.Key, g.Count(), g.Sum(x => x.ValorVenda))).ToList();
        var proj = cart.Where(p => p.Status == StatusParcelaTitulo.Aberta).GroupBy(p => new DateOnly(p.Vencimento.Year, p.Vencimento.Month, 1)).OrderBy(g => g.Key)
            .Select(g => (g.Key, g.Sum(x => x.ValorCorrigido))).ToList();
        // Meses sem índice informado que afetam a correção (do mês mais antigo de data-base até o mês passado).
        var idx = await MapaIndicesAsync(db, ct);
        var falta = new SortedSet<DateOnly>();
        var cs = await db.ContratosVenda.Where(c => c.ObraId == obraId && c.Status == StatusContratoVenda.Ativo).ToListAsync(ct);
        var ultimo = new DateOnly(Hoje.Year, Hoje.Month, 1).AddMonths(-1);
        foreach (var c in cs)
            for (var m = new DateOnly(c.DataBase.Year, c.DataBase.Month, 1); m <= ultimo; m = m.AddMonths(1))
            {
                var ind = c.DataChaves is { } ch && m >= new DateOnly(ch.Year, ch.Month, 1) ? c.IndiceAposChaves : c.IndiceAteChaves;
                if (!idx.ContainsKey((ind, m))) falta.Add(m);
            }
        return new PainelVendas(esp.Sum(u => u.ContratoId != null ? u.ValorVenda ?? 0 : u.PrecoTabela), vendido, estoque, esp.Count, esp.Count(u => u.ContratoId != null),
            esp.Count(u => u.Situacao == SituacaoUnidade.Disponivel), cart.Where(p => p.Status == StatusParcelaTitulo.Paga).Sum(p => p.ValorPago),
            cart.Where(p => p.Status == StatusParcelaTitulo.Aberta).Sum(p => p.ValorCorrigido), cart.Where(p => p.DiasAtraso > 0).Sum(p => p.ValorCorrigido),
            vso, proj, falta.ToList());
    }

    // ───────────── RET ─────────────

    public async Task<List<ApuracaoRet>> ApuracoesAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.ApuracoesRet.Where(a => a.ObraId == obraId).OrderByDescending(a => a.Competencia).ToListAsync(ct);
    }

    /// <summary>
    /// Apura o RET do mês (receita da venda recebida no mês, inclusive correção, juros e permuta) pela alíquota vigente do regime
    /// e gera o título do DARF (conta 6.1) com vencimento no dia configurado do mês seguinte. Reapurar só enquanto o DARF não foi pago.
    /// </summary>
    public async Task<ApuracaoRet> ApurarAsync(Guid obraId, DateOnly competencia, CancellationToken ct = default)
    {
        var mes = new DateOnly(competencia.Year, competencia.Month, 1);
        ApuracaoRet? res = null;
        await fabrica.EmTransacaoAsync(async db =>
        {
            var inc = await db.Incorporacoes.FirstOrDefaultAsync(i => i.ObraId == obraId, ct)
                      ?? throw new RegraDeNegocioException("Cadastre os dados da incorporação (regime tributário) antes de apurar.");
            var aliq = await db.AliquotasRegime.Where(a => a.Regime == inc.Regime && a.VigenciaInicio <= mes).OrderByDescending(a => a.VigenciaInicio).FirstOrDefaultAsync(ct)
                       ?? throw new RegraDeNegocioException($"Sem alíquota vigente para {NomesVenda.Regime(inc.Regime)} em {mes:MM/yyyy}.");
            var fim = mes.AddMonths(1).AddDays(-1);
            var receita = await db.TituloParcelas.Where(p => p.Titulo.ObraId == obraId && p.Titulo.Tipo == TipoTitulo.Receber && p.Titulo.Origem == OrigemTitulo.VendaUnidade
                    && p.Status == StatusParcelaTitulo.Paga && p.DataPagamento >= mes && p.DataPagamento <= fim).SumAsync(p => p.ValorPago, ct);
            var a = await db.ApuracoesRet.FirstOrDefaultAsync(x => x.IncorporacaoId == inc.Id && x.Competencia == mes, ct);
            if (a?.TituloId is { } tid0 && await db.TituloParcelas.AnyAsync(p => p.TituloId == tid0 && p.Status == StatusParcelaTitulo.Paga, ct))
                throw new RegraDeNegocioException("DARF desta competência já pago: a apuração não muda.");
            if (a is null) db.ApuracoesRet.Add(a = new ApuracaoRet { IncorporacaoId = inc.Id, ObraId = obraId, Competencia = mes });
            decimal V(decimal perc) => Math.Round(receita * perc / 100m, 2);
            a.Regime = inc.Regime; a.ReceitaRecebida = receita; a.AliquotaTotal = aliq.Total;
            a.Irpj = V(aliq.Irpj); a.Csll = V(aliq.Csll); a.Pis = V(aliq.Pis); a.Cofins = V(aliq.Cofins); a.Cbs = V(aliq.Cbs); a.Ibs = V(aliq.Ibs);
            if (a.TituloId is { } tid && await db.Titulos.Include(t => t.Parcelas).FirstOrDefaultAsync(t => t.Id == tid, ct) is { } velho)
            {
                velho.Cancelado = true; velho.MotivoCancelamento = "Reapuração do RET"; velho.ChaveOrigem = null;
                foreach (var p in velho.Parcelas) p.Status = StatusParcelaTitulo.Cancelada;
                a.TituloId = null;
            }
            if (a.Total > 0)
            {
                var venc = new DateOnly(mes.AddMonths(1).Year, mes.AddMonths(1).Month, Math.Clamp(inc.DiaVencimentoRet, 1, 28));
                var t = new Titulo
                {
                    Tipo = TipoTitulo.Pagar, Numero = await Numerador.ProximoAsync(db, "TituloPagar", ct), DocumentoTipo = "DARF", DocumentoNumero = aliq.CodigoReceita,
                    Pessoa = "Receita Federal (DARF RET)", ObraId = obraId, ContaGerencialId = await ContaAsync(db, "6.1", ct),
                    Descricao = $"RET {mes:MM/yyyy} — {inc.Nome}", Emissao = fim, Competencia = mes, ValorBruto = a.Total,
                    Origem = OrigemTitulo.ApuracaoRet, OrigemId = a.Id, ChaveOrigem = $"RET:{inc.Id:N}:{mes:yyyyMM}:{Guid.CreateVersion7():N}"
                };
                t.Parcelas.Add(new TituloParcela { TituloId = t.Id, Numero = 1, Vencimento = venc, Valor = a.Total });
                db.Titulos.Add(t);
                a.TituloId = t.Id;
            }
            await db.SaveChangesAsync(ct);
            res = a;
        }, ct);
        return res!;
    }
}
