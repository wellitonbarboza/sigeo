using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Engenharia;

namespace QualyHub.Infrastructure.Servicos;

public sealed record FechamentoMes(DateOnly Competencia, decimal CustoTotal, decimal HorasApropriadas, decimal CustoSemApropriacao,
    IReadOnlyList<FechamentoMaoObra.CustoEquipe> Equipes, IReadOnlyList<FechamentoMaoObra.CustoCelula> Celulas);

/// <summary>Mão de obra própria: equipes, custo mensal (com réplica do mês anterior), apropriação de horas por serviço × local e fechamento.</summary>
public sealed class MaoObraServico(FabricaDb fabrica)
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

    public async Task SalvarCustoAsync(CustoMaoObraMes c, CancellationToken ct = default)
    {
        c.Competencia = CustoMaoObraMes.Mes(c.Competencia);
        ValidacaoException.Se(c.Validar());
        await using var db = fabrica.Criar();
        await Grafo.SalvarAsync(db, c);
        await db.SaveChangesAsync(ct);
    }

    public async Task ExcluirCustoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        db.Remove(await db.CustosMaoObra.FirstAsync(x => x.Id == id, ct));
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
        ValidacaoException.Se(e);
        await using var db = fabrica.Criar();
        var todos = await db.Locais.AsNoTracking().Where(l => l.ObraId == obraId).ToListAsync(ct);
        var filhos = todos.ToLookup(l => l.LocalPaiId);
        var cels = await db.ServicosLocais.AsNoTracking().Where(s => s.ObraId == obraId && s.ComposicaoId == composicaoId).ToListAsync(ct);
        var pesos = locais.Select(l => (l, cels.Where(c => MotorOrcamento.Subarvore(l, filhos).Contains(c.LocalId)).Sum(c => c.Quantidade))).ToList();
        var rateio = FechamentoMaoObra.Ratear(horas, pesos);
        var lanc = Guid.CreateVersion7();
        var totalPeso = pesos.Sum(p => p.Item2);
        foreach (var (localId, h) in rateio)
            db.Apropriacoes.Add(new Apropriacao
            {
                ObraId = obraId, Data = data, EquipeId = equipeId, ColaboradorId = colaboradorId, ComposicaoId = composicaoId, LocalId = localId, Horas = h, Observacao = observacao, Lancamento = lanc,
                QuantidadeExecutada = quantidadeExecutada is { } q ? Math.Round(totalPeso > 0 ? q * pesos.First(p => p.l == localId).Item2 / totalPeso : q / rateio.Count, 4) : null
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
