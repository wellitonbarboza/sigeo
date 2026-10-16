namespace Sigeo.Tests.Integracao;

[Collection(ColecaoBanco.Nome)]
public sealed class MaoObraTests(BancoFixture f)
{
    /// <summary>Cenário 6: custos do mês, réplica, apropriação rateada e fechamento.</summary>
    [FatoComBanco]
    public async Task Custos_replica_apropriacao_e_fechamento()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var (rev, _, _, _) = await Dados.MontarOrcamentoAsync(f, e);
        var mo = f.MaoObra;
        var equipe = new Equipe { ObraId = e.ObraId, Codigo = "ALV1", Nome = "Equipe de alvenaria 1" };
        await mo.SalvarEquipeAsync(equipe);
        var mes = new DateOnly(2026, 8, 1);

        // Linhas repetidas (mesma equipe/descrição) no mesmo mês: recusado antes de gravar.
        CustoMaoObraMes Linha() => new() { EquipeId = equipe.Id, Descricao = "Pedreiros", Pessoas = 2, Salario = 3000, EncargosPerc = 80, Beneficios = 600, Outros = 250 };
        await Assert.ThrowsAsync<ValidacaoException>(() => mo.SalvarCustosMesAsync(e.ObraId, mes, [Linha(), Linha()], []));
        await using (var db = f.Fabrica.CriarLeitura())
            Assert.False(await db.CustosMaoObra.AnyAsync(c => c.ObraId == e.ObraId));

        var linha = Linha();
        await mo.SalvarCustosMesAsync(e.ObraId, mes.AddDays(14), [linha], []);
        // 2 × (3000 × 1,8 + 600) + 250 = 12.250,00
        Assert.Equal(12250m, linha.Total);

        // Réplica: a primeira copia; a segunda não duplica.
        Assert.Equal(1, await mo.ReplicarAsync(e.ObraId, mes, mes.AddMonths(1)));
        Assert.Equal(0, await mo.ReplicarAsync(e.ObraId, mes, mes.AddMonths(1)));
        await using (var db = f.Fabrica.CriarLeitura())
            Assert.Equal(1, await db.CustosMaoObra.CountAsync(c => c.ObraId == e.ObraId && c.Competencia == mes.AddMonths(1)));

        // Apropriação: 7 h em 3 apartamentos AP2D (mesma quantidade orçada → pesos iguais) — soma exata, sem centavo perdido.
        var locais = await Dados.LocaisAsync(f, e.ObraId);
        var aptos = locais.Where(l => l.Tipo == GeradorEmpreendimento.TipoApartamento && l.TipologiaId == e.Ap2d.Id).OrderBy(l => l.Codigo).Take(3).Select(l => l.Id).ToList();
        Assert.Equal(3, await mo.ApropriarAsync(e.ObraId, mes.AddDays(9), equipe.Id, null, rev.Id, aptos, 7m, null, null));
        // 10 h num AP2D (20 m² de sala) e num AP3D (25 m²) → proporcional à quantidade orçada.
        var ap3 = locais.First(l => l.Tipo == GeradorEmpreendimento.TipoApartamento && l.TipologiaId == e.Ap3d.Id).Id;
        Assert.Equal(2, await mo.ApropriarAsync(e.ObraId, mes.AddDays(10), equipe.Id, null, rev.Id, [aptos[0], ap3], 9m, null, null));

        await using (var db = f.Fabrica.CriarLeitura())
        {
            var aps = await db.Apropriacoes.Where(a => a.ObraId == e.ObraId).ToListAsync();
            Assert.Equal(5, aps.Count);
            var primeiro = aps.Where(a => a.Data == mes.AddDays(9)).Select(a => a.Horas).Order().ToArray();
            Assert.Equal([2.33m, 2.33m, 2.34m], primeiro);
            var segundo = aps.Where(a => a.Data == mes.AddDays(10)).ToDictionary(a => a.LocalId, a => a.Horas);
            Assert.Equal(4m, segundo[aptos[0]]); // 9 × 20/45
            Assert.Equal(5m, segundo[ap3]);      // 9 × 25/45
            Assert.Equal(16m, aps.Sum(a => a.Horas));
        }

        // Fechamento: todo o custo do mês vai para as células (a equipe teve horas).
        var fech = await mo.FechamentoAsync(e.ObraId, mes);
        Assert.Equal(12250m, fech.CustoTotal);
        Assert.Equal(16m, fech.HorasApropriadas);
        Assert.Equal(0m, fech.CustoSemApropriacao);
        Assert.Equal(12250m, fech.Celulas.Sum(c => c.Custo));
        var eq = Assert.Single(fech.Equipes);
        Assert.Equal(Math.Round(12250m / 16m, 4), eq.CustoHoraReal);
    }
}
