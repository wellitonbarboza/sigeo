using Microsoft.EntityFrameworkCore;
using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Engenharia;

namespace Groundkey.Application.Servicos;

public sealed record FechamentoMes(DateOnly Competencia, decimal CustoTotal, decimal HorasApropriadas, decimal CustoSemApropriacao,
    IReadOnlyList<FechamentoMaoObra.CustoEquipe> Equipes, IReadOnlyList<FechamentoMaoObra.CustoCelula> Celulas);

/// <summary>Mão de obra própria: equipes, custo mensal (com réplica do mês anterior), apropriação de horas por serviço × local e fechamento.</summary>
public sealed class MaoObraServico(IFabricaDb fabrica)
{
    public async Task SalvarEquipeAsync(Equipe e, CancellationToken ct = default)
    {
        var erros = new List<string>();
        e.Codigo = (e.Codigo ?? "").Trim().ToUpperInvariant();
        if (e.Codigo.Length == 0) erros.Add("Informe o código da equipe.");
        if (string.IsNullOrWhiteSpace(e.Nome)) erros.Add("Informe o nome da equipe.");
        if (e.ObraId == Guid.Empty) erros.Add("Selecione a obra.");
        if (e.Membros.GroupBy(m => m.ColaboradorId).Any(g => g.Count() > 1)) erros.Add("Colaborador repetido na equipe.");
        ValidacaoException.Se(erros);
        foreach (var m in e.Membros) m.EquipeId = e.Id;
        await using var db = fabrica.Criar();
        if (await db.Equipes.AnyAsync(x => x.Id != e.Id && x.ObraId == e.ObraId && x.Codigo == e.Codigo, ct)) throw new RegraDeNegocioException("Código de equipe já usado nesta obra.");
        await Grafo.SalvarAsync(db, e, nameof(Equipe.Membros));
        await db.SaveChangesAsync(ct);
    }

    public async Task SalvarCustoAsync(CustoMaoObraMes c, CancellationToken ct = default) =>
        await SalvarCustosMesAsync(c.ObraId, c.Competencia, [c], [], ct);

    /// <summary>
    /// Grava os custos de um mês de uma vez (linhas novas/alteradas + excluídas) numa única transação:
    /// ou tudo é gravado, ou nada (a tela não fica com metade salva).
    /// </summary>
    public async Task SalvarCustosMesAsync(Guid obraId, DateOnly competencia, IReadOnlyList<CustoMaoObraMes> linhas, IReadOnlyCollection<Guid> removidos,
        CancellationToken ct = default)
    {
        var mes = CustoMaoObraMes.Mes(competencia);
        var erros = new List<string>();
        if (obraId == Guid.Empty) erros.Add("Selecione a obra.");
        foreach (var c in linhas)
        {
            c.ObraId = obraId; c.Competencia = mes; c.Descricao = (c.Descricao ?? "").Trim();
            erros.AddRange(c.Validar().Select(x => string.IsNullOrEmpty(c.Descricao) ? x : $"{c.Descricao}: {x}"));
        }
        if (linhas.GroupBy(c => (c.EquipeId, c.ColaboradorId, c.FuncaoObraId, c.Descricao)).Any(g => g.Count() > 1))
            erros.Add("Há linhas repetidas (mesma equipe/colaborador/função/descrição) no mês.");
        ValidacaoException.Se(erros);
        await fabrica.EmTransacaoAsync(async db =>
        {
            foreach (var x in await db.CustosMaoObra.Where(x => removidos.Contains(x.Id) && x.ObraId == obraId).ToListAsync(ct)) db.Remove(x);
            await db.SaveChangesAsync(ct);
            var ids = linhas.Select(l => l.Id).ToList();
            var outras = await db.CustosMaoObra.AsNoTracking().Where(x => x.ObraId == obraId && x.Competencia == mes && !ids.Contains(x.Id)).ToListAsync(ct);
            if (linhas.Any(c => outras.Any(o => o.EquipeId == c.EquipeId && o.ColaboradorId == c.ColaboradorId && o.FuncaoObraId == c.FuncaoObraId && o.Descricao == c.Descricao)))
                throw new RegraDeNegocioException("Já existe custo lançado no mês para a mesma equipe/colaborador/função/descrição.");
            foreach (var c in linhas) await Grafo.SalvarAsync(db, c);
            await db.SaveChangesAsync(ct);
        }, ct);
    }

    public async Task ExcluirCustoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (await db.CustosMaoObra.FirstOrDefaultAsync(x => x.Id == id, ct) is not { } c) return; // já excluído
        db.Remove(c);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Copia os custos de um mês para outro (o que já existe no destino para a mesma equipe/colaborador/função não é duplicado).</summary>
    public async Task<int> ReplicarAsync(Guid obraId, DateOnly de, DateOnly para, CancellationToken ct = default)
    {
        de = CustoMaoObraMes.Mes(de); para = CustoMaoObraMes.Mes(para);
        if (de == para) throw new RegraDeNegocioException("Escolha meses diferentes.");
        await using var db = fabrica.Criar();
        var origem = await db.CustosMaoObra.AsNoTracking().Where(c => c.ObraId == obraId && c.Competencia == de).ToListAsync(ct);
        if (origem.Count == 0) throw new RegraDeNegocioException($"Não há custos lançados em {de:MM/yyyy}.");
        var destino = await db.CustosMaoObra.AsNoTracking().Where(c => c.ObraId == obraId && c.Competencia == para).ToListAsync(ct);
        var n = 0;
        foreach (var c in origem.Where(o => !destino.Any(d => d.EquipeId == o.EquipeId && d.ColaboradorId == o.ColaboradorId && d.FuncaoObraId == o.FuncaoObraId && d.Descricao == o.Descricao)))
        { db.CustosMaoObra.Add(c.CopiarPara(para)); n++; }
        await db.SaveChangesAsync(ct);
        return n;
    }

    /// <summary>
    /// Lança horas num serviço para um ou mais locais (torre, pavimentos, apartamentos…). As horas são rateadas pelas quantidades
    /// orçadas do serviço abaixo de cada local (sem orçamento: em partes iguais).
    /// </summary>
    public async Task<int> ApropriarAsync(Guid obraId, DateOnly data, Guid? equipeId, Guid? colaboradorId, Guid composicaoId, IReadOnlyCollection<Guid> locais,
        decimal horas, decimal? quantidadeExecutada, string? observacao, CancellationToken ct = default)
    {
        var e = new List<string>();
        if (composicaoId == Guid.Empty) e.Add("Informe o serviço.");
        if (locais.Count == 0) e.Add("Escolha ao menos um local.");
        if (horas is <= 0 or > 10000) e.Add("Horas devem ser maiores que zero.");
        if (quantidadeExecutada < 0) e.Add("Quantidade executada não pode ser negativa.");
        ValidacaoException.Se(e);
        await using var db = fabrica.Criar();
        var todos = await db.Locais.AsNoTracking().Where(l => l.ObraId == obraId).ToDictionaryAsync(l => l.Id, ct);
        if (locais.Any(l => !todos.ContainsKey(l))) throw new RegraDeNegocioException("Há locais que não pertencem a esta obra.");
        // Local escolhido junto com um ancestral dele contaria as horas duas vezes: fica só o ancestral.
        bool TemAncestral(Guid id) { var x = todos[id].LocalPaiId; while (x is { } p && todos.TryGetValue(p, out var pai)) { if (locais.Contains(p)) return true; x = pai.LocalPaiId; } return false; }
        var escolhidos = locais.Distinct().Where(l => !TemAncestral(l)).ToList();
        if (!await db.Composicoes.AnyAsync(c => c.Id == composicaoId, ct)) throw new RegraDeNegocioException("Serviço não encontrado.");
        if (equipeId is { } eqId && !await db.Equipes.AnyAsync(x => x.Id == eqId && x.ObraId == obraId, ct)) throw new RegraDeNegocioException("Equipe não pertence a esta obra.");
        if (colaboradorId is { } colId && !await db.Colaboradores.AnyAsync(x => x.Id == colId, ct)) throw new RegraDeNegocioException("Colaborador não encontrado.");
        var filhos = todos.Values.ToLookup(l => l.LocalPaiId);
        var qtdPorLocal = (await db.ServicosLocais.AsNoTracking().Where(s => s.ObraId == obraId && s.ComposicaoId == composicaoId)
                                  .Select(s => new { s.LocalId, s.Quantidade }).ToListAsync(ct))
                          .GroupBy(c => c.LocalId).ToDictionary(g => g.Key, g => g.Sum(c => c.Quantidade));
        var pesos = escolhidos.Select(l => (l, MotorOrcamento.Subarvore(l, filhos).Sum(x => qtdPorLocal.GetValueOrDefault(x)))).ToList();
        var rateio = FechamentoMaoObra.Ratear(horas, pesos);
        var qtds = quantidadeExecutada is { } q ? Rateio.Proporcional(q * 100m, pesos.Select(p => p.Item2).ToList()).Select(x => (decimal?)(x / 100m)).ToArray()
                                                : new decimal?[pesos.Count];
        var lanc = Guid.CreateVersion7();
        for (var k = 0; k < rateio.Count; k++)
            db.Apropriacoes.Add(new Apropriacao
            {
                ObraId = obraId, Data = data, EquipeId = equipeId, ColaboradorId = colaboradorId, ComposicaoId = composicaoId, LocalId = rateio[k].LocalId,
                Horas = rateio[k].Horas, Observacao = observacao, Lancamento = lanc, QuantidadeExecutada = qtds[k]
            });
        await db.SaveChangesAsync(ct);
        return rateio.Count;
    }

    public async Task ExcluirLancamentoAsync(Guid lancamento, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        foreach (var a in await db.Apropriacoes.Where(a => a.Lancamento == lancamento).ToListAsync(ct)) db.Remove(a);
        await db.SaveChangesAsync(ct);
    }

    public async Task<FechamentoMes> FechamentoAsync(Guid obraId, DateOnly competencia, CancellationToken ct = default)
    {
        var mes = CustoMaoObraMes.Mes(competencia);
        await using var db = fabrica.Criar();
        var custos = await db.CustosMaoObra.AsNoTracking().Where(c => c.ObraId == obraId && c.Competencia == mes).ToListAsync(ct);
        var fim = mes.AddMonths(1);
        var aps = await db.Apropriacoes.AsNoTracking().Where(a => a.ObraId == obraId && a.Data >= mes && a.Data < fim).ToListAsync(ct);
        var (eq, cel, sem) = FechamentoMaoObra.Calcular(custos, aps);
        return new FechamentoMes(mes, custos.Sum(c => c.Total), aps.Sum(a => a.Horas), sem, eq, cel);
    }
}
