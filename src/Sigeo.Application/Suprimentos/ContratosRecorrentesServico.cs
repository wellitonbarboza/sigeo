using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Contrato recorrente: gera as parcelas do período, recalcula o previsto, recebe a nota e segue o fluxo até o pagamento.</summary>
public sealed class ContratosRecorrentesServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);
    private DateTimeOffset Agora => relogio.GetUtcNow();

    /// <summary>Períodos de locação dos equipamentos ligados ao contrato (chegada → devolução).</summary>
    public static async Task<List<PeriodoLocacao>> LocacoesAsync(ISigeoDb db, Guid contratoId, CancellationToken ct = default)
    {
        var eqs = await db.Ativos.AsNoTracking().Where(e => e.ContratoId == contratoId && e.ContratoItemId != null).ToListAsync(ct);
        var ids = eqs.Select(e => e.Id).ToList();
        var movs = await db.MovimentosEquipamento.AsNoTracking().Where(m => ids.Contains(m.EquipamentoId)).OrderBy(m => m.Data).ToListAsync(ct);
        var r = new List<PeriodoLocacao>();
        foreach (var e in eqs)
        {
            var chegada = movs.FirstOrDefault(m => m.EquipamentoId == e.Id && m.Tipo == TipoMovimentoEquipamento.Chegada)?.Data ?? DateOnly.FromDateTime(e.CriadoEm.LocalDateTime);
            var dev = movs.LastOrDefault(m => m.EquipamentoId == e.Id && m.Tipo == TipoMovimentoEquipamento.Devolucao)?.Data;
            r.Add(new PeriodoLocacao(e.ContratoItemId!.Value, $"{e.Patrimonio} {e.Nome}", e.Quantidade, chegada, dev));
        }
        return r;
    }

    /// <summary>
    /// Gera (ou atualiza) as parcelas do contrato: cria as que faltam e recalcula o previsto das que ainda não têm nota.
    /// Parcela com nota lançada não muda (o histórico fica).
    /// </summary>
    public async Task<int> GerarParcelasAsync(Guid contratoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var c = await db.Contratos.Include(x => x.Itens).FirstAsync(x => x.Id == contratoId, ct);
        if (!c.Recorrente || c.FormaCobranca == FormaCobranca.Medicao) throw new RegraDeNegocioException("O contrato não tem cobrança recorrente.");
        var existentes = await db.ParcelasContrato.Where(p => p.ContratoId == c.Id).ToListAsync(ct);
        var locacoes = c.FormaCobranca == FormaCobranca.Diaria ? await LocacoesAsync(db, c.Id, ct) : [];
        var usos = c.FormaCobranca == FormaCobranca.Uso ? await db.UsosContrato.AsNoTracking().Where(u => u.ContratoId == c.Id).ToListAsync(ct) : [];
        var periodos = RegrasRecorrencia.Periodos(c);
        var n = 0;
        for (var k = 0; k < periodos.Count; k++)
        {
            var (ini, fim) = periodos[k];
            var linhas = RegrasRecorrencia.Previsto(c, ini, fim, locacoes, usos);
            var p = existentes.FirstOrDefault(x => x.Numero == k + 1);
            if (p is null)
            {
                p = new ParcelaContrato { ContratoId = c.Id, ObraId = c.ObraId, Numero = k + 1 };
                db.ParcelasContrato.Add(p); n++;
            }
            else if (p.Status != StatusParcela.Prevista) continue;
            p.Inicio = ini; p.Fim = fim; p.Vencimento = RegrasRecorrencia.Vencimento(fim, c.DiaVencimento);
            p.ValorPrevisto = linhas.Sum(l => l.Total); p.Composicao = RegrasRecorrencia.Texto(linhas);
        }
        // Parcelas além do novo fim da vigência (sem nota) somem.
        foreach (var p in existentes.Where(p => p.Numero > periodos.Count && p.Status == StatusParcela.Prevista)) p.Status = StatusParcela.Cancelada;
        await db.SaveChangesAsync(ct);
        return n;
    }

    public async Task LancarUsoAsync(UsoContrato u, CancellationToken ct = default)
    {
        if (u.Quantidade <= 0) throw new RegraDeNegocioException("Informe a quantidade.");
        await using var db = fabrica.Criar();
        var c = await db.Contratos.AsNoTracking().Include(x => x.Itens).FirstAsync(x => x.Id == u.ContratoId, ct);
        if (c.FormaCobranca != FormaCobranca.Uso) throw new RegraDeNegocioException("O contrato não é cobrado por uso.");
        if (c.Itens.All(i => i.Id != u.ContratoItemId)) throw new RegraDeNegocioException("Item não pertence ao contrato.");
        u.ObraId = c.ObraId;
        db.UsosContrato.Add(u);
        await db.SaveChangesAsync(ct);
        await GerarParcelasAsync(c.Id, ct);
    }

    /// <summary>Exclui um uso lançado por engano e recalcula o previsto (só enquanto a parcela do período não tem nota).</summary>
    public async Task ExcluirUsoAsync(Guid usoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var u = await db.UsosContrato.FirstAsync(x => x.Id == usoId, ct);
        if (await db.ParcelasContrato.AnyAsync(p => p.ContratoId == u.ContratoId && p.Inicio <= u.Data && p.Fim >= u.Data && p.Status != StatusParcela.Prevista && p.Status != StatusParcela.Cancelada, ct))
            throw new RegraDeNegocioException("A parcela deste período já tem nota: devolva a parcela antes de excluir o uso.");
        db.UsosContrato.Remove(u);
        await db.SaveChangesAsync(ct);
        await GerarParcelasAsync(u.ContratoId, ct);
    }

    /// <summary>Cancela uma parcela prevista (ex.: mês sem cobrança combinado com o fornecedor).</summary>
    public async Task CancelarParcelaAsync(Guid parcelaId, string motivo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new RegraDeNegocioException("Informe o motivo do cancelamento.");
        await using var db = fabrica.Criar();
        var p = await db.ParcelasContrato.FirstAsync(x => x.Id == parcelaId, ct);
        if (p.Status != StatusParcela.Prevista) throw new RegraDeNegocioException("Só parcela prevista (sem nota) é cancelada. Com nota: devolva antes.");
        p.Status = StatusParcela.Cancelada; p.Observacao = string.IsNullOrWhiteSpace(p.Observacao) ? $"Cancelada: {motivo.Trim()}" : $"{p.Observacao} · Cancelada: {motivo.Trim()}";
        await db.SaveChangesAsync(ct);
    }

    public async Task LancarNotaAsync(Guid parcelaId, string numero, decimal valor, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var p = await db.ParcelasContrato.FirstAsync(x => x.Id == parcelaId, ct);
        p.LancarNota(numero, valor, "Almoxarife / obra", null, usuario.Nome, Agora);
        await db.SaveChangesAsync(ct);
    }

    public async Task AprovarAsync(Guid parcelaId, string? justificativa, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var p = await db.ParcelasContrato.FirstAsync(x => x.Id == parcelaId, ct);
        var c = await db.Contratos.AsNoTracking().FirstAsync(x => x.Id == p.ContratoId, ct);
        if (!string.IsNullOrWhiteSpace(justificativa)) p.Justificativa = justificativa.Trim();
        p.Aprovar(c.ToleranciaPerc, c.ExigeOsPorVisita, usuario.Nome, Agora);
        await db.SaveChangesAsync(ct);
    }

    public async Task LiberarAsync(Guid parcelaId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var p = await db.ParcelasContrato.FirstAsync(x => x.Id == parcelaId, ct);
        p.Liberar(usuario.Nome, Agora);
        await db.SaveChangesAsync(ct);
    }

    public async Task MarcarPagaAsync(Guid parcelaId, DateOnly data, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var p = await db.ParcelasContrato.FirstAsync(x => x.Id == parcelaId, ct);
        p.MarcarPaga(data);
        await db.SaveChangesAsync(ct);
    }

    public async Task DevolverAsync(Guid parcelaId, string motivo, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var p = await db.ParcelasContrato.FirstAsync(x => x.Id == parcelaId, ct);
        p.Devolver(motivo);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Notas importadas (SIEG/XML) do fornecedor do contrato ainda sem vínculo: ligam-se à parcela prevista do período
    /// (emissão até 20 dias depois do fim do período).
    /// </summary>
    public async Task<int> VincularNotasAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var abertas = await db.ParcelasContrato.Where(p => p.Status == StatusParcela.Prevista && p.NotaFiscalId == null).ToListAsync(ct);
        if (abertas.Count == 0) return 0;
        var contratos = abertas.Select(p => p.ContratoId).Distinct().ToList();
        var fornecedores = await db.Contratos.AsNoTracking().Where(c => contratos.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.FornecedorId, ct);
        var forns = fornecedores.Values.Distinct().ToList();
        var usadas = await db.ParcelasContrato.AsNoTracking().Where(p => p.NotaFiscalId != null).Select(p => p.NotaFiscalId!.Value).ToListAsync(ct);
        var notas = await db.NotasFiscais.AsNoTracking()
            .Where(n => n.FornecedorId != null && forns.Contains(n.FornecedorId.Value) && n.PedidoId == null && n.RecebimentoId == null && !usadas.Contains(n.Id))
            .ToListAsync(ct);
        var n = 0;
        foreach (var p in abertas.OrderBy(p => p.Fim))
        {
            var forn = fornecedores[p.ContratoId];
            var nota = notas.FirstOrDefault(x => x.FornecedorId == forn && DateOnly.FromDateTime(x.Emissao.LocalDateTime) >= p.Inicio && DateOnly.FromDateTime(x.Emissao.LocalDateTime) <= p.Fim.AddDays(20));
            if (nota is null) continue;
            notas.Remove(nota);
            p.LancarNota(nota.Numero.ToString(), nota.ValorTotal, "SIEG (vinculada pelo CNPJ)", nota.Id, "Sistema", Agora);
            n++;
        }
        await db.SaveChangesAsync(ct);
        return n;
    }
}
