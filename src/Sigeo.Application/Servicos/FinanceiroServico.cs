using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Financeiro;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Servicos;

public sealed record LinhaParcelaFin(Guid TituloId, Guid ParcelaId, TipoTitulo Tipo, int Numero, string Parcela, string? Documento, string Pessoa, string Descricao,
    string? Obra, Guid? ObraId, string? Conta, DateOnly Vencimento, decimal Valor, StatusParcelaTitulo Status, DateOnly? DataPagamento, decimal ValorPago,
    string? ContaBancaria, OrigemTitulo Origem, bool Historico, TipoParcelaVenda TipoVenda, int DiasAtraso);

public sealed record BaixaParcela(Guid ParcelaId, DateOnly Data, Guid? ContaBancariaId, FormaPagamento Forma, decimal Correcao, decimal Juros, decimal Multa, decimal Desconto, string? Comprovante);

public sealed record LinhaFluxo(DateOnly Mes, decimal EntradasPrevistas, decimal SaidasPrevistas, decimal EntradasRealizadas, decimal SaidasRealizadas)
{
    public decimal SaldoPrevisto => EntradasPrevistas - SaidasPrevistas;
    public decimal SaldoRealizado => EntradasRealizadas - SaidasRealizadas;
}

public sealed record LinhaDre(string Codigo, string Conta, int Nivel, NaturezaConta Natureza, decimal Valor);
public sealed record LinhaAging(TipoTitulo Tipo, string Faixa, int Parcelas, decimal Valor);
public sealed record SaldoConta(Guid Id, string Conta, decimal Saldo, int NaoConciliados);
public sealed record LinhaMovimento(Guid Id, DateOnly Data, string Historico, decimal Valor, bool Conciliado, bool DeBaixa);

/// <summary>
/// Módulo Financeiro (adicional): títulos a pagar e a receber com parcelas, baixa com conta e forma, movimentos bancários,
/// transferências, fluxo de caixa previsto × realizado, aging e DRE gerencial por regime de caixa.
/// </summary>
public sealed class FinanceiroServico(IFabricaDb fabrica, TimeProvider relogio)
{
    public const string RotaPagar = "financeiro/pagar";
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    /// <summary>O módulo está contratado (liberado pela plataforma) para a construtora?</summary>
    public static async Task<bool> AtivoAsync(ISigeoDb db, CancellationToken ct = default)
    {
        var bloq = await db.Empresas.AsNoTracking().Where(e => e.Id == db.EmpresaAtualId).Select(e => e.TelasBloqueadas).FirstOrDefaultAsync(ct);
        return !PlataformaServico.Bloqueadas(bloq).Contains(RotaPagar, StringComparer.OrdinalIgnoreCase);
    }

    // ───────────── Cadastros ─────────────

    public async Task<List<ContaGerencial>> ContasGerenciaisAsync(bool soAtivas = false, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var l = await db.ContasGerenciais.Where(c => !soAtivas || c.Ativa).ToListAsync(ct);
        return l.OrderBy(c => c.Codigo, ComparadorCodigo.Instancia).ToList();
    }

    public async Task SalvarContaGerencialAsync(ContaGerencial c, CancellationToken ct = default)
    {
        c.Codigo = (c.Codigo ?? "").Trim().Trim('.'); c.Nome = (c.Nome ?? "").Trim();
        var e = new List<string>();
        if (!System.Text.RegularExpressions.Regex.IsMatch(c.Codigo, @"^\d+(\.\d+)*$")) e.Add("Código no formato 2.1.3.");
        if (c.Nome.Length == 0) e.Add("Informe o nome da conta.");
        ValidacaoException.Se(e);
        await using var db = fabrica.Criar();
        if (ContaGerencial.CodigoPai(c.Codigo) is { } pai && !await db.ContasGerenciais.AnyAsync(x => x.Codigo == pai, ct))
            throw new RegraDeNegocioException($"Cadastre antes a conta-pai {pai}.");
        if (await db.ContasGerenciais.AnyAsync(x => x.Id != c.Id && x.Codigo == c.Codigo, ct)) throw new RegraDeNegocioException("Código já usado.");
        await Grafo.SalvarAsync(db, c);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<ContaBancaria>> ContasBancariasAsync(bool soAtivas = false, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.ContasBancarias.Where(c => !soAtivas || c.Ativa).OrderBy(c => c.Nome).ToListAsync(ct);
    }

    public async Task SalvarContaBancariaAsync(ContaBancaria c, CancellationToken ct = default)
    {
        c.Nome = (c.Nome ?? "").Trim();
        if (c.Nome.Length == 0) throw new ValidacaoException(["Informe o nome da conta (ex.: Banco X — obra)."]);
        if (c.DataSaldoInicial == default) c.DataSaldoInicial = Hoje;
        await using var db = fabrica.Criar();
        await Grafo.SalvarAsync(db, c);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<CondicaoPagamento>> CondicoesAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.CondicoesPagamento.OrderBy(c => c.Nome).ToListAsync(ct);
    }

    public async Task SalvarCondicaoAsync(CondicaoPagamento c, CancellationToken ct = default)
    {
        c.Nome = (c.Nome ?? "").Trim();
        ValidacaoException.Se(c.Validar());
        await using var db = fabrica.Criar();
        if (await db.CondicoesPagamento.AnyAsync(x => x.Id != c.Id && x.Nome == c.Nome, ct)) throw new RegraDeNegocioException("Já existe condição com esse nome.");
        await Grafo.SalvarAsync(db, c);
        await db.SaveChangesAsync(ct);
    }

    // ───────────── Títulos ─────────────

    public async Task<List<LinhaParcelaFin>> ParcelasAsync(TipoTitulo tipo, Guid? obraId, DateOnly? de, DateOnly? ate, bool incluirPagas, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var q = db.TituloParcelas.Include(p => p.Titulo).Where(p => p.Titulo.Tipo == tipo && !p.Titulo.Cancelado && p.Status != StatusParcelaTitulo.Cancelada);
        if (obraId is { } o) q = q.Where(p => p.Titulo.ObraId == o);
        if (!incluirPagas) q = q.Where(p => p.Status == StatusParcelaTitulo.Aberta);
        if (de is { } d) q = q.Where(p => (p.Status == StatusParcelaTitulo.Paga ? p.DataPagamento : p.Vencimento) >= d);
        if (ate is { } a) q = q.Where(p => (p.Status == StatusParcelaTitulo.Paga ? p.DataPagamento : p.Vencimento) <= a);
        var lista = await q.OrderBy(p => p.Vencimento).Take(5000).ToListAsync(ct);
        return await MontarAsync(db, lista, ct);
    }

    public async Task<List<LinhaParcelaFin>> ParcelasDoTituloAsync(Guid tituloId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var lista = await db.TituloParcelas.Include(p => p.Titulo).Where(p => p.TituloId == tituloId).OrderBy(p => p.Numero).ToListAsync(ct);
        return await MontarAsync(db, lista, ct);
    }

    private async Task<List<LinhaParcelaFin>> MontarAsync(ISigeoDb db, List<TituloParcela> lista, CancellationToken ct)
    {
        var fIds = lista.Select(p => p.Titulo.FornecedorId).Where(x => x != null).Select(x => x!.Value).Distinct().ToList();
        var forn = await db.Fornecedores.IgnoreQueryFilters().Where(f => fIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.NomeFantasia ?? f.RazaoSocial, ct);
        var cIds = lista.Select(p => p.Titulo.CompradorId).Where(x => x != null).Select(x => x!.Value).Distinct().ToList();
        var comp = await db.Compradores.IgnoreQueryFilters().Where(f => cIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.Nome, ct);
        var obras = await db.Obras.IgnoreQueryFilters().ToDictionaryAsync(o => o.Id, o => o.Nome, ct);
        var contas = await db.ContasGerenciais.ToDictionaryAsync(c => c.Id, c => c.Codigo + " " + c.Nome, ct);
        var bancos = (await db.ContasBancarias.ToListAsync(ct)).ToDictionary(c => c.Id, c => c.Rotulo);
        var porTitulo = lista.GroupBy(p => p.TituloId).ToDictionary(g => g.Key, g => g.Count());
        var hoje = Hoje;
        return lista.Select(p =>
        {
            var t = p.Titulo;
            var pessoa = t.FornecedorId is { } f ? forn.GetValueOrDefault(f) ?? "" : t.CompradorId is { } c ? comp.GetValueOrDefault(c) ?? "" : t.Pessoa ?? "";
            var doc = string.Join(" ", new[] { t.DocumentoTipo, t.DocumentoNumero }.Where(x => !string.IsNullOrWhiteSpace(x)));
            return new LinhaParcelaFin(t.Id, p.Id, t.Tipo, t.Numero, $"{p.Numero}", doc.Length == 0 ? null : doc, pessoa, t.Descricao,
                t.ObraId is { } o ? obras.GetValueOrDefault(o) : null, t.ObraId, t.ContaGerencialId is { } cg ? contas.GetValueOrDefault(cg) : null,
                p.Vencimento, p.Valor, p.Status, p.DataPagamento, p.ValorPago, p.ContaBancariaId is { } b ? bancos.GetValueOrDefault(b) : null,
                t.Origem, t.Historico, p.TipoVenda, p.DiasAtraso(hoje));
        }).ToList();
    }

    public async Task<Titulo?> ObterTituloAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Titulos.Include(t => t.Parcelas.OrderBy(p => p.Numero)).FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    /// <summary>Gera as parcelas pela condição de pagamento (substitui as abertas na tela).</summary>
    public static List<TituloParcela> ParcelasPorCondicao(CondicaoPagamento c, decimal valorLiquido, DateOnly dataBase) =>
        c.Gerar(valorLiquido, dataBase).Select((x, i) => new TituloParcela { Numero = i + 1, Vencimento = x.Vencimento, Valor = x.Valor }).ToList();

    /// <summary>Título manual (novo ou edição). Com parcela paga só muda descrição, conta, obra e observação.</summary>
    public async Task<Guid> SalvarTituloAsync(Titulo t, CancellationToken ct = default)
    {
        t.Descricao = (t.Descricao ?? "").Trim();
        if (t.Competencia == default) t.Competencia = new DateOnly(t.Emissao.Year, t.Emissao.Month, 1);
        await using var db = fabrica.Criar();
        var atual = await db.Titulos.Include(x => x.Parcelas).FirstOrDefaultAsync(x => x.Id == t.Id, ct);
        if (atual is not null && atual.Parcelas.Any(p => p.Status == StatusParcelaTitulo.Paga))
        {
            atual.Descricao = t.Descricao; atual.ContaGerencialId = t.ContaGerencialId; atual.ObraId = t.ObraId; atual.CentroCustoId = t.CentroCustoId; atual.Observacao = t.Observacao;
            await db.SaveChangesAsync(ct);
            return atual.Id;
        }
        var n = 0;
        foreach (var p in t.Parcelas.OrderBy(p => p.Vencimento)) { p.TituloId = t.Id; p.Numero = ++n; }
        ValidacaoException.Se(t.Validar());
        if (atual is null)
        {
            t.Numero = await Numerador.ProximoAsync(db, t.Tipo == TipoTitulo.Pagar ? "TituloPagar" : "TituloReceber", ct);
            foreach (var p in t.Parcelas) p.Titulo = null!;
            db.Titulos.Add(t);
        }
        else
        {
            db.Entry(atual).CurrentValues.SetValues(t);
            atual.Numero = atual.Numero; // mantém
            var ids = t.Parcelas.Select(p => p.Id).ToHashSet();
            foreach (var velha in atual.Parcelas.Where(p => !ids.Contains(p.Id)).ToList()) db.TituloParcelas.Remove(velha);
            foreach (var p in t.Parcelas)
            {
                var ex = atual.Parcelas.FirstOrDefault(x => x.Id == p.Id);
                if (ex is null) { p.Titulo = null!; db.TituloParcelas.Add(p); }
                else { ex.Numero = p.Numero; ex.Vencimento = p.Vencimento; ex.Valor = p.Valor; }
            }
        }
        await db.SaveChangesAsync(ct);
        return t.Id;
    }

    public async Task CancelarTituloAsync(Guid id, string motivo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new ValidacaoException(["Informe o motivo do cancelamento."]);
        await using var db = fabrica.Criar();
        var t = await db.Titulos.Include(x => x.Parcelas).FirstAsync(x => x.Id == id, ct);
        if (t.Parcelas.Any(p => p.Status == StatusParcelaTitulo.Paga)) throw new RegraDeNegocioException("Título com parcela paga: estorne as baixas antes de cancelar.");
        t.Cancelado = true; t.MotivoCancelamento = motivo.Trim();
        foreach (var p in t.Parcelas) p.Status = StatusParcelaTitulo.Cancelada;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Baixa da parcela: grava valores, gera o movimento na conta (exceto permuta/compensação) e dispara os efeitos da origem.</summary>
    public async Task BaixarAsync(BaixaParcela b, CancellationToken ct = default)
    {
        var e = new List<string>();
        if (b.Data == default) e.Add("Informe a data do pagamento.");
        if (b.ContaBancariaId is null && !RegrasFinanceiro.SemMovimento(b.Forma)) e.Add("Escolha a conta bancária/caixa.");
        if (b.Juros < 0 || b.Multa < 0 || b.Desconto < 0) e.Add("Juros, multa e desconto não podem ser negativos.");
        ValidacaoException.Se(e);
        await fabrica.EmTransacaoAsync(async db =>
        {
            var p = await db.TituloParcelas.Include(x => x.Titulo).FirstAsync(x => x.Id == b.ParcelaId, ct);
            if (p.Status != StatusParcelaTitulo.Aberta) throw new RegraDeNegocioException("Parcela já baixada ou cancelada.");
            p.DataPagamento = b.Data; p.ContaBancariaId = RegrasFinanceiro.SemMovimento(b.Forma) ? null : b.ContaBancariaId; p.Forma = b.Forma;
            p.Correcao = b.Correcao; p.Juros = b.Juros; p.Multa = b.Multa; p.Desconto = b.Desconto; p.Comprovante = b.Comprovante?.Trim();
            p.ValorPago = p.TotalBaixa; p.Status = StatusParcelaTitulo.Paga;
            if (p.ValorPago <= 0) throw new RegraDeNegocioException("Valor pago deve ser maior que zero.");
            if (p.ContaBancariaId is { } conta)
                db.MovimentosBancarios.Add(new MovimentoBancario
                {
                    ContaBancariaId = conta, Data = b.Data, Valor = p.Titulo.Tipo == TipoTitulo.Receber ? p.ValorPago : -p.ValorPago,
                    Historico = $"{(p.Titulo.Tipo == TipoTitulo.Receber ? "Recebimento" : "Pagamento")} título {p.Titulo.Numero}/{p.Numero} · {p.Titulo.Descricao}".Trim(),
                    TituloParcelaId = p.Id, ContaGerencialId = p.Titulo.ContaGerencialId, ObraId = p.Titulo.ObraId
                });
            await db.SaveChangesAsync(ct);
            await EfeitosDaBaixaAsync(db, p.Titulo, ct);
            await db.SaveChangesAsync(ct);
        }, ct);
    }

    public async Task EstornarBaixaAsync(Guid parcelaId, CancellationToken ct = default)
    {
        await fabrica.EmTransacaoAsync(async db =>
        {
            var p = await db.TituloParcelas.Include(x => x.Titulo).FirstAsync(x => x.Id == parcelaId, ct);
            if (p.Status != StatusParcelaTitulo.Paga) throw new RegraDeNegocioException("Parcela não está paga.");
            foreach (var m in await db.MovimentosBancarios.Where(m => m.TituloParcelaId == p.Id).ToListAsync(ct))
            {
                if (m.Conciliado) throw new RegraDeNegocioException("Movimento já conciliado com o extrato: desfaça a conciliação antes.");
                db.MovimentosBancarios.Remove(m);
            }
            p.Status = StatusParcelaTitulo.Aberta; p.DataPagamento = null; p.ValorPago = 0; p.Correcao = p.Juros = p.Multa = p.Desconto = 0; p.ContaBancariaId = null; p.Forma = null;
            if (p.Titulo.Origem == OrigemTitulo.LiberacaoPagamento && p.Titulo.OrigemId is { } lid
                && await db.Pagamentos.FirstOrDefaultAsync(x => x.Id == lid, ct) is { Status: StatusPagamento.Pago } lib)
                lib.Status = StatusPagamento.Liberado;
            await db.SaveChangesAsync(ct);
        }, ct);
    }

    /// <summary>Pagamento liberado quitado → liberação "Paga" (e medição "Paga"); venda recebida → comissões condicionadas e quitação.</summary>
    private static async Task EfeitosDaBaixaAsync(ISigeoDb db, Titulo t, CancellationToken ct)
    {
        var parcelas = await db.TituloParcelas.Where(p => p.TituloId == t.Id).ToListAsync(ct);
        var quitado = parcelas.Where(p => p.Status != StatusParcelaTitulo.Cancelada).All(p => p.Status == StatusParcelaTitulo.Paga);
        if (t.Origem == OrigemTitulo.LiberacaoPagamento && t.OrigemId is { } lid && quitado)
        {
            var l = await db.Pagamentos.FirstOrDefaultAsync(x => x.Id == lid, ct);
            if (l is not null && l.Status == StatusPagamento.Liberado)
            {
                l.Status = StatusPagamento.Pago;
                if (l.MedicaoId is { } mid && await db.Medicoes.FirstOrDefaultAsync(m => m.Id == mid, ct) is { } med) med.Status = StatusMedicao.Paga;
            }
        }
        if (t.Origem == OrigemTitulo.VendaUnidade && t.OrigemId is { } cid)
            await IncorporacaoServico.AposRecebimentoAsync(db, cid, ct);
    }

    /// <summary>Liberação de pagamento aprovada vira título a pagar (se o módulo estiver contratado). Idempotente.</summary>
    public static async Task GerarDaLiberacaoAsync(ISigeoDb db, LiberacaoPagamento l, CancellationToken ct = default)
    {
        if (!await AtivoAsync(db, ct)) return;
        var chave = $"LIB:{l.Id:N}";
        if (db.Titulos.Local.Any(t => t.ChaveOrigem == chave) || await db.Titulos.AnyAsync(t => t.ChaveOrigem == chave, ct)) return;
        var conta = await db.ContasGerenciais.Where(c => c.Codigo == (l.Origem == OrigemPagamento.MedicaoServico ? "2.4" : "2.3")).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct);
        var t = new Titulo
        {
            Tipo = TipoTitulo.Pagar, Numero = await Numerador.ProximoAsync(db, "TituloPagar", ct), DocumentoTipo = "NF",
            DocumentoNumero = l.ChaveNFe.Length >= 34 ? int.TryParse(l.ChaveNFe.Substring(25, 9), out var nf) ? nf.ToString() : null : l.ChaveNFe,
            FornecedorId = l.FornecedorId, ObraId = l.ObraId, ContaGerencialId = conta, Descricao = $"Liberação de pagamento {l.Numero}",
            Emissao = DateOnly.FromDateTime((l.LiberadoEm ?? DateTimeOffset.UtcNow).Date), ValorBruto = l.ValorLiberado,
            Origem = OrigemTitulo.LiberacaoPagamento, OrigemId = l.Id, ChaveOrigem = chave
        };
        t.Competencia = new DateOnly(t.Emissao.Year, t.Emissao.Month, 1);
        t.Parcelas.Add(new TituloParcela { TituloId = t.Id, Numero = 1, Vencimento = l.Vencimento == default ? t.Emissao : l.Vencimento, Valor = l.ValorLiberado });
        db.Titulos.Add(t);
    }

    // ───────────── Bancos ─────────────

    public async Task<List<SaldoConta>> SaldosAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var contas = await db.ContasBancarias.Where(c => c.Ativa).ToListAsync(ct);
        var movs = await db.MovimentosBancarios.GroupBy(m => m.ContaBancariaId)
            .Select(g => new { g.Key, Soma = g.Sum(x => x.Valor), Pend = g.Count(x => !x.Conciliado) }).ToListAsync(ct);
        return contas.Select(c => new SaldoConta(c.Id, c.Rotulo, c.SaldoInicial + (movs.FirstOrDefault(m => m.Key == c.Id)?.Soma ?? 0), movs.FirstOrDefault(m => m.Key == c.Id)?.Pend ?? 0))
            .OrderBy(s => s.Conta).ToList();
    }

    public async Task<List<LinhaMovimento>> MovimentosAsync(Guid contaId, DateOnly de, DateOnly ate, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.MovimentosBancarios.Where(m => m.ContaBancariaId == contaId && m.Data >= de && m.Data <= ate)
            .OrderBy(m => m.Data).ThenBy(m => m.CriadoEm)
            .Select(m => new LinhaMovimento(m.Id, m.Data, m.Historico, m.Valor, m.Conciliado, m.TituloParcelaId != null)).ToListAsync(ct);
    }

    /// <summary>Lançamento avulso na conta (tarifa, rendimento, ajuste). Positivo = entrada.</summary>
    public async Task LancarMovimentoAsync(Guid contaId, DateOnly data, decimal valor, string historico, Guid? contaGerencialId, Guid? obraId, CancellationToken ct = default)
    {
        if (valor == 0 || string.IsNullOrWhiteSpace(historico)) throw new ValidacaoException(["Informe valor e histórico."]);
        await using var db = fabrica.Criar();
        db.MovimentosBancarios.Add(new MovimentoBancario { ContaBancariaId = contaId, Data = data, Valor = valor, Historico = historico.Trim(), ContaGerencialId = contaGerencialId, ObraId = obraId });
        await db.SaveChangesAsync(ct);
    }

    public async Task TransferirAsync(Guid origem, Guid destino, DateOnly data, decimal valor, string? historico, CancellationToken ct = default)
    {
        if (origem == destino) throw new ValidacaoException(["Escolha contas diferentes."]);
        if (valor <= 0) throw new ValidacaoException(["Valor deve ser maior que zero."]);
        await using var db = fabrica.Criar();
        var tid = Guid.CreateVersion7();
        var h = string.IsNullOrWhiteSpace(historico) ? "Transferência entre contas" : historico.Trim();
        db.MovimentosBancarios.Add(new MovimentoBancario { ContaBancariaId = origem, Data = data, Valor = -valor, Historico = h, TransferenciaId = tid });
        db.MovimentosBancarios.Add(new MovimentoBancario { ContaBancariaId = destino, Data = data, Valor = valor, Historico = h, TransferenciaId = tid });
        await db.SaveChangesAsync(ct);
    }

    public async Task ConciliarAsync(IReadOnlyCollection<Guid> movimentos, bool conciliado, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        foreach (var m in await db.MovimentosBancarios.Where(m => movimentos.Contains(m.Id)).ToListAsync(ct)) m.Conciliado = conciliado;
        await db.SaveChangesAsync(ct);
    }

    // ───────────── Relatórios ─────────────

    /// <summary>
    /// Fluxo de caixa por mês: previsto = parcelas em aberto pelo vencimento (vencidas entram no mês atual);
    /// realizado = parcelas pagas pela data do pagamento + lançamentos avulsos (transferências não contam).
    /// </summary>
    public async Task<List<LinhaFluxo>> FluxoAsync(Guid? obraId, DateOnly de, DateOnly ate, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var q = db.TituloParcelas.Where(p => !p.Titulo.Cancelado && p.Status != StatusParcelaTitulo.Cancelada);
        if (obraId is { } o) q = q.Where(p => p.Titulo.ObraId == o);
        var ps = await q.Select(p => new { p.Titulo.Tipo, p.Status, p.Vencimento, p.DataPagamento, p.Valor, p.ValorPago }).ToListAsync(ct);
        var mq = db.MovimentosBancarios.Where(m => m.TituloParcelaId == null && m.TransferenciaId == null);
        if (obraId is { } o2) mq = mq.Where(m => m.ObraId == o2);
        var avulsos = await mq.Select(m => new { m.Data, m.Valor }).ToListAsync(ct);
        var mesAtual = new DateOnly(Hoje.Year, Hoje.Month, 1);
        static DateOnly Mes(DateOnly d) => new(d.Year, d.Month, 1);
        var r = new List<LinhaFluxo>();
        for (var m = Mes(de); m <= Mes(ate); m = m.AddMonths(1))
        {
            var mm = m;
            var abertas = ps.Where(p => p.Status == StatusParcelaTitulo.Aberta && (Mes(p.Vencimento) == mm || (mm == mesAtual && p.Vencimento < mesAtual))).ToList();
            var pagas = ps.Where(p => p.Status == StatusParcelaTitulo.Paga && p.DataPagamento is { } dp && Mes(dp) == mm).ToList();
            var av = avulsos.Where(a => Mes(a.Data) == mm).ToList();
            r.Add(new LinhaFluxo(mm,
                abertas.Where(p => p.Tipo == TipoTitulo.Receber).Sum(p => p.Valor), abertas.Where(p => p.Tipo == TipoTitulo.Pagar).Sum(p => p.Valor),
                pagas.Where(p => p.Tipo == TipoTitulo.Receber).Sum(p => p.ValorPago) + av.Where(a => a.Valor > 0).Sum(a => a.Valor),
                pagas.Where(p => p.Tipo == TipoTitulo.Pagar).Sum(p => p.ValorPago) - av.Where(a => a.Valor < 0).Sum(a => a.Valor)));
        }
        return r;
    }

    public async Task<List<LinhaAging>> AgingAsync(Guid? obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var q = db.TituloParcelas.Where(p => !p.Titulo.Cancelado && p.Status == StatusParcelaTitulo.Aberta);
        if (obraId is { } o) q = q.Where(p => p.Titulo.ObraId == o);
        var ps = await q.Select(p => new { p.Titulo.Tipo, p.Vencimento, p.Valor }).ToListAsync(ct);
        var hoje = Hoje;
        return ps.GroupBy(p => (p.Tipo, Faixa: RegrasFinanceiro.Faixa(hoje.DayNumber - p.Vencimento.DayNumber)))
            .Select(g => new LinhaAging(g.Key.Tipo, g.Key.Faixa, g.Count(), g.Sum(x => x.Valor)))
            .OrderBy(a => a.Tipo).ThenBy(a => Array.IndexOf(RegrasFinanceiro.Faixas, a.Faixa)).ToList();
    }

    /// <summary>DRE gerencial por regime de caixa (pagamentos e recebimentos do período por conta gerencial, somando os níveis).</summary>
    public async Task<List<LinhaDre>> DreAsync(Guid? obraId, DateOnly de, DateOnly ate, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var q = db.TituloParcelas.Where(p => p.Status == StatusParcelaTitulo.Paga && p.DataPagamento >= de && p.DataPagamento <= ate && !p.Titulo.Cancelado);
        if (obraId is { } o) q = q.Where(p => p.Titulo.ObraId == o);
        var pagos = await q.Select(p => new { p.Titulo.ContaGerencialId, p.Titulo.Tipo, p.ValorPago }).ToListAsync(ct);
        var mq = db.MovimentosBancarios.Where(m => m.TituloParcelaId == null && m.TransferenciaId == null && m.Data >= de && m.Data <= ate);
        if (obraId is { } o2) mq = mq.Where(m => m.ObraId == o2);
        var avulsos = await mq.Select(m => new { m.ContaGerencialId, m.Valor }).ToListAsync(ct);
        var contas = await db.ContasGerenciais.ToListAsync(ct);
        var porConta = new Dictionary<string, decimal>();
        void Somar(Guid? contaId, decimal valor)
        {
            var cod = contaId is { } id ? contas.FirstOrDefault(c => c.Id == id)?.Codigo : null;
            if (cod is null) { porConta["?"] = porConta.GetValueOrDefault("?") + valor; return; }
            for (var c = cod; c is not null; c = ContaGerencial.CodigoPai(c)) porConta[c] = porConta.GetValueOrDefault(c) + valor;
        }
        foreach (var p in pagos) Somar(p.ContaGerencialId, p.ValorPago);
        foreach (var a in avulsos) Somar(a.ContaGerencialId, Math.Abs(a.Valor));
        var linhas = contas.Where(c => porConta.ContainsKey(c.Codigo))
            .Select(c => new LinhaDre(c.Codigo, c.Nome, c.Nivel, c.Natureza, porConta[c.Codigo]))
            .OrderBy(l => l.Codigo, ComparadorCodigo.Instancia).ToList();
        if (porConta.TryGetValue("?", out var semConta)) linhas.Add(new LinhaDre("—", "Sem conta gerencial", 1, NaturezaConta.Despesa, semConta));
        return linhas;
    }
}

/// <summary>Ordena códigos hierárquicos numericamente ("2.10" depois de "2.9").</summary>
public sealed class ComparadorCodigo : IComparer<string>
{
    public static readonly ComparadorCodigo Instancia = new();
    public int Compare(string? x, string? y)
    {
        var a = (x ?? "").Split('.'); var b = (y ?? "").Split('.');
        for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            var c = int.TryParse(a[i], out var ia) && int.TryParse(b[i], out var ib) ? ia.CompareTo(ib) : string.CompareOrdinal(a[i], b[i]);
            if (c != 0) return c;
        }
        return a.Length.CompareTo(b.Length);
    }
}
