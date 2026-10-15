using Groundkey.Domain.Engenharia;
using Groundkey.Domain.Estrutura;
using Groundkey.Infrastructure.Importacao;

namespace Groundkey.Tests.Integracao;

/// <summary>
/// Kit do Residencial Nest Cancelli (docs/kits): orçamento revisado R01 com insumos, apartamento 2 dormitórios e edifício
/// térreo + 9 pavimentos. Importa, gera a torre (38 aptos) e o orçamento por local tem de bater com o total do R01.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public sealed class KitNestCancelliTests(BancoFixture f)
{
    private static string Arquivo()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Groundkey.slnx"))) d = d.Parent;
        return Path.Combine(d!.FullName, "docs", "kits", "Kit_Tipologia_Nest_Cancelli_R01.xlsx");
    }

    [FatoComBanco]
    public async Task Kit_Nest_gera_38_aptos_e_orcamento_igual_ao_R01()
    {
        KitTipologia kit;
        await using (var s = File.OpenRead(Arquivo())) kit = LeitorKitTipologia.Ler(s);
        Assert.Equal(128, kit.Servicos.Count);
        Assert.True(kit.InsumosOuVazio.Count > 600);
        // cômodos medidos no DWG V7: perímetro, pé-direito livre 2,82 m e vãos
        Assert.All(kit.Ambientes.Where(a => a.Escopo == EscopoAmbiente.Comodo), a =>
        {
            Assert.True(a.Perimetro > 0 && a.AreaVaos > 0);
            Assert.Equal(2.82m, a.PeDireito);
        });
        Assert.Equal(10.50m, kit.Ambientes.Single(a => a.Sigla == "DOR").Perimetro);

        var obraId = await Dados.CriarObraAsync(f);
        var eng = f.Engenharia;
        var servico = new KitTipologiaServico(f.Fabrica, eng, f.Orcamento);
        var previa = await servico.AnalisarAsync(obraId, kit);
        Assert.Equal(2, previa.TipologiasNovas);
        Assert.Equal(kit.InsumosOuVazio.Count, previa.Insumos);
        Assert.True(previa.ProdutosNovos > 100);
        await servico.AplicarAsync(obraId, kit);
        var segunda = await servico.AplicarAsync(obraId, kit); // idempotente: não duplica produtos nem serviços
        Assert.Equal(0, segunda.ProdutosNovos);
        Assert.Equal(0, segunda.ServicosNovos);

        Guid torreId;
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var tips = await db.Tipologias.Where(t => t.ObraId == obraId).ToListAsync();
            torreId = tips.Single(t => t.Codigo == "T-NC").Id;
            var alv = await db.Composicoes.Include(c => c.Itens).SingleAsync(c => c.Codigo == "NC-4.03.01.01");
            Assert.Equal(33, alv.Itens.Count);
            Assert.All(alv.Itens.Where(i => i.Tipo == TipoInsumo.Material), i => Assert.NotNull(i.ProdutoId));
        }

        var q = new Quadra { ObraId = obraId, Codigo = "NC" + Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(), Nome = "Residencial ficticio", Tipo = "Condomínio" };
        await eng.SalvarCondominioAsync(q, new Spe { Codigo = "S" + Guid.NewGuid().ToString("N")[..4], RazaoSocial = "SPE Ficticia Ltda", Cnpj = Dados.Cnpj() });
        await eng.GerarAsync(obraId, q.Id, [("Torre única", torreId)]);
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var aptos = await db.Locais.Where(l => l.ObraId == obraId && l.Tipo == GeradorEmpreendimento.TipoApartamento).Select(l => l.Nome).ToListAsync();
            Assert.Equal(38, aptos.Count);
            Assert.Contains(aptos, a => a.EndsWith(" 101"));
            Assert.Contains(aptos, a => a.EndsWith(" 1004"));
        }

        var r = await f.Orcamento.GerarAsync(obraId);
        Assert.True(r.Criadas > 0);
        Assert.InRange(r.Total, 7_116_810.64m, 7_116_811.64m);
    }

    [FatoComBanco]
    public async Task Cronograma_R08_importa_com_custo_vinculos_e_feriados()
    {
        var obraId = await Dados.CriarObraAsync(f);
        var plan = new PlanejamentoServico(f.Fabrica, f.Usuario, f.Aprovacoes, f.Relogio);
        var lb0 = (await plan.VersaoEmElaboracaoAsync(obraId, criarSeNaoHouver: true))!.Value;
        var arq = Path.Combine(Path.GetDirectoryName(Arquivo())!, "Nest_Cancelli_Cronograma_R08_importacao_Groundkey.xml");
        await using (var s = File.OpenRead(arq)) Assert.Equal(555, await plan.ImportarMsProjectAsync(lb0, s));
        await using var db = f.Fabrica.CriarLeitura();
        var v = await db.VersoesCronograma.Include(x => x.Atividades).Include(x => x.Vinculos).AsSplitQuery().SingleAsync(x => x.Id == lb0);
        Assert.InRange(v.CustoTotal, 7_116_810m, 7_116_812m);
        Assert.Equal(14, v.Atividades.Count(a => a.PaiId == null && a.Resumo));       // macroetapas = 1º nível
        Assert.Contains(v.Atividades, a => a.PaiId == null && a.Nome == "APARTAMENTOS");
        Assert.DoesNotContain(v.Atividades, a => System.Text.RegularExpressions.Regex.IsMatch(a.Nome, @"^\d+(\.\d+)*\.?\s"));
        Assert.True(v.Vinculos.Count > 800);
        Assert.True(await db.Feriados.CountAsync(x => x.ObraId == obraId) >= 40);
        Assert.NotNull(v.TerminoPrevisto);
        Assert.InRange(v.TerminoPrevisto!.Value, new DateOnly(2028, 6, 1), new DateOnly(2028, 10, 31));
    }
}
