using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Engenharia;
using Groundkey.Domain.Qualidade;

namespace Groundkey.Tests.Integracao;

/// <summary>Torre: renomear (código fica) e excluir com a subárvore — bloqueado quando há registro de obra.</summary>
[Collection(ColecaoBanco.Nome)]
public sealed class TorreTests(BancoFixture f)
{
    [FatoComBanco]
    public async Task Torre_renomeia_e_exclui_com_pavimentos_aptos_e_orcamento()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        await f.Engenharia.GerarAsync(e.ObraId, e.QuadraId, [("Torre 2", e.Torre.Id)]);
        await Dados.MontarOrcamentoAsync(f, e);
        Guid t1, t2; string codigo;
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var torres = await db.Locais.Where(l => l.ObraId == e.ObraId && l.Tipo == GeradorEmpreendimento.TipoTorre).OrderBy(l => l.Codigo).ToListAsync();
            Assert.Equal(2, torres.Count);
            (t1, t2, codigo) = (torres[0].Id, torres[1].Id, torres[1].Codigo);
        }

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => f.Engenharia.RenomearTorreAsync(t2, "Torre 1"));
        await f.Engenharia.RenomearTorreAsync(t2, "Torre B");
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var t = await db.Locais.SingleAsync(l => l.Id == t2);
            Assert.Equal(("Torre B", codigo), (t.Nome, t.Codigo));
        }

        // Registro de obra num apartamento da torre bloqueia a exclusão.
        var apto = (await Dados.LocaisAsync(f, e.ObraId)).First(l => l.Tipo == GeradorEmpreendimento.TipoApartamento && l.Codigo.StartsWith(codigo));
        var rnc = new Rnc { Numero = "RNC-T" + Random.Shared.Next(99999), ObraId = e.ObraId, LocalId = apto.Id, Descricao = "Fissura ficticia", Prazo = DateOnly.FromDateTime(DateTime.Today.AddDays(5)) };
        await using (var db = f.Fabrica.Criar()) { db.Rncs.Add(rnc); await db.SaveChangesAsync(); }
        Assert.Contains("1 RNC", await f.Engenharia.UsoDaTorreAsync(t2));
        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(() => f.Engenharia.ExcluirTorreAsync(t2));
        Assert.Contains("RNC", erro.Message);
        await using (var db = f.Fabrica.Criar()) { db.Rncs.Remove(await db.Rncs.SingleAsync(x => x.Id == rnc.Id)); await db.SaveChangesAsync(); }

        int celulasAntes;
        await using (var db = f.Fabrica.CriarLeitura()) celulasAntes = await db.ServicosLocais.CountAsync(x => x.ObraId == e.ObraId);
        Assert.Empty(await f.Engenharia.UsoDaTorreAsync(t2));
        var n = await f.Engenharia.ExcluirTorreAsync(t2);
        Assert.True(n > 12);

        await using (var db = f.Fabrica.CriarLeitura())
        {
            var locais = await db.Locais.Where(l => l.ObraId == e.ObraId).ToListAsync();
            Assert.DoesNotContain(locais, l => l.Id == t2 || l.Codigo.StartsWith(codigo));
            Assert.Contains(locais, l => l.Id == t1);
            var aptos = locais.Where(l => l.Tipo == GeradorEmpreendimento.TipoApartamento).ToList();
            Assert.Equal(1m, Math.Round(aptos.Sum(a => a.FracaoIdeal ?? 0), 6));           // fração redistribuída na torre que ficou
            var ids = locais.Select(l => l.Id).ToHashSet();
            var celulas = await db.ServicosLocais.Where(x => x.ObraId == e.ObraId).ToListAsync();
            Assert.True(celulas.Count < celulasAntes);
            Assert.All(celulas, c => Assert.Contains(c.LocalId, ids));
        }
    }
}
