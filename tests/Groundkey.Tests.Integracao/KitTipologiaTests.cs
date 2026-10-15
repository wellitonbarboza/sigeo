using Groundkey.Domain.Engenharia;
using Groundkey.Domain.Estrutura;
using Groundkey.Infrastructure.Importacao;

namespace Groundkey.Tests.Integracao;

/// <summary>Kit de tipologia (docs/kits): importa, gera as torres e o orçamento — e importar de novo não duplica.</summary>
[Collection(ColecaoBanco.Nome)]
public sealed class KitTipologiaTests(BancoFixture f)
{
    private static string Arquivo()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Groundkey.slnx"))) d = d.Parent;
        return Path.Combine(d!.FullName, "docs", "kits", "Kit_Tipologia_2Q-8_Ecoparque_QuadraC.xlsx");
    }

    [FatoComBanco]
    public async Task Kit_2Q8_gera_duas_torres_com_120_aptos_e_orcamento()
    {
        KitTipologia kit;
        await using (var s = File.OpenRead(Arquivo())) kit = LeitorKitTipologia.Ler(s);
        Assert.Equal(3, kit.Tipologias.Count);

        var obraId = await Dados.CriarObraAsync(f);
        var eng = f.Engenharia;
        var servico = new KitTipologiaServico(f.Fabrica, eng, f.Orcamento);
        var previa = await servico.AnalisarAsync(obraId, kit);
        Assert.Equal(3, previa.TipologiasNovas);
        await servico.AplicarAsync(obraId, kit);
        var segunda = await servico.AplicarAsync(obraId, kit); // idempotente
        Assert.Equal(0, segunda.TipologiasNovas);
        Assert.Equal(0, segunda.ServicosNovos);

        Guid torreId;
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var tips = await db.Tipologias.Include(t => t.Ambientes).Where(t => t.ObraId == obraId).ToListAsync();
            Assert.Equal(3, tips.Count);
            Assert.Equal(9, tips.Single(t => t.Codigo == "AP-2Q").Ambientes.Count);
            torreId = tips.Single(t => t.Codigo == "T-2Q-8").Id;
            var ids = tips.Select(t => t.Id).ToList();
            Assert.Equal(kit.Aplicacoes.Count, await db.ServicoAplicacoes.CountAsync(a => ids.Contains(a.TipologiaId)));
        }

        var q = new Quadra { ObraId = obraId, Codigo = "QC" + Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(), Nome = "Condomínio ficticio", Tipo = "Condomínio" };
        await eng.SalvarCondominioAsync(q, new Spe { Codigo = "S" + Guid.NewGuid().ToString("N")[..4], RazaoSocial = "SPE Ficticia Ltda", Cnpj = Dados.Cnpj() });
        await eng.GerarAsync(obraId, q.Id, [("Torre A", torreId), ("Torre B", torreId)]);

        await using (var db = f.Fabrica.CriarLeitura())
        {
            var locais = await db.Locais.Where(l => l.ObraId == obraId).ToListAsync();
            var aptos = locais.Where(l => l.Tipo == GeradorEmpreendimento.TipoApartamento).ToList();
            Assert.Equal(240, aptos.Count);
            Assert.Equal(2160, locais.Count(l => l.Tipo == GeradorEmpreendimento.TipoComodo));
            Assert.Equal(30, locais.Count(l => l.Tipo == GeradorEmpreendimento.TipoPavimento));
            Assert.Contains(aptos, a => a.Nome == "Apto 01");
            Assert.Contains(aptos, a => a.Nome == "Apto 1408");
            Assert.DoesNotContain(aptos, a => a.Nome == "Apto 1508");
        }

        var r = await f.Orcamento.GerarAsync(obraId);
        Assert.True(r.Criadas > 0);
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var vinilico = await db.Composicoes.Where(c => c.Codigo == "SV.05.03").Select(c => c.Id).SingleAsync();
            var total = await db.ServicosLocais.Where(s => s.ObraId == obraId && s.ComposicaoId == vinilico).SumAsync(s => s.Quantidade);
            Assert.Equal(7725.6m, total); // 32,19 m² × 240 aptos
        }
    }
}
