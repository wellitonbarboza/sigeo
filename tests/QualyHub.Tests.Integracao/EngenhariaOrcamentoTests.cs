using QualyHub.Domain.Suprimentos;

namespace QualyHub.Tests.Integracao;

[Collection(ColecaoBanco.Nome)]
public sealed class EngenhariaOrcamentoTests(BancoFixture f)
{
    /// <summary>Cenário 1: tipologias + condomínio/SPE + geração de 1 torre.</summary>
    [FatoComBanco]
    public async Task Gerar_torre_cria_arvore_completa_da_spe_com_fracao_ideal_exata()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);

        // Condomínio 1 + torre 1 + escada 1 + 4 pavimentos + 4 halls + 12 aptos + 8×3 cômodos (AP2D) + 4×4 cômodos (AP3D) = 63.
        Assert.Equal(63, e.LocaisGerados);
        var locais = await Dados.LocaisAsync(f, e.ObraId);
        Assert.Equal(63, locais.Count);
        Assert.Equal(1, locais.Count(l => l.Tipo == GeradorEmpreendimento.TipoCondominio));
        Assert.Equal(1, locais.Count(l => l.Tipo == GeradorEmpreendimento.TipoTorre));
        Assert.Equal(4, locais.Count(l => l.Tipo == GeradorEmpreendimento.TipoPavimento));
        Assert.Equal(12, locais.Count(l => l.Tipo == GeradorEmpreendimento.TipoApartamento));
        Assert.Equal(40, locais.Count(l => l.Tipo == GeradorEmpreendimento.TipoComodo));
        Assert.Equal(5, locais.Count(l => l.Tipo == GeradorEmpreendimento.TipoAreaComum));
        Assert.All(locais, l => Assert.Equal(e.SpeId, l.SpeId));

        var aptos = locais.Where(l => l.Tipo == GeradorEmpreendimento.TipoApartamento).ToList();
        Assert.Equal(1m, aptos.Sum(a => a.FracaoIdeal ?? 0));
        Assert.Empty(GeradorEmpreendimento.ConferirFracao(locais));

        await using var db = f.Fabrica.CriarLeitura();
        var quadra = await db.Quadras.SingleAsync(q => q.Id == e.QuadraId);
        Assert.Equal(locais.Single(l => l.Tipo == GeradorEmpreendimento.TipoCondominio).Id, quadra.LocalId);
        var resumo = Assert.Single(await f.Engenharia.ResumoAsync(e.ObraId));
        Assert.Equal(1m, resumo.FracaoSoma);
        Assert.Equal(8 * 50m + 4 * 65m, resumo.AreaPrivativa);
    }

    /// <summary>Cenário 2: orçamento gera células; demanda de material abate o requisitado (sem composição conta uma vez só).</summary>
    [FatoComBanco]
    public async Task Orcamento_gera_celulas_e_demanda_abate_requisitado_sem_contar_em_dobro()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var (rev, cp, prod, res) = await Dados.MontarOrcamentoAsync(f, e);

        Assert.Equal(24, res.Criadas); // 12 salas × 2 composições
        await using (var db = f.Fabrica.CriarLeitura())
            Assert.Equal(24, await db.ServicosLocais.CountAsync(s => s.ObraId == e.ObraId));

        var locais = await Dados.LocaisAsync(f, e.ObraId);
        var torre = locais.Single(l => l.Tipo == GeradorEmpreendimento.TipoTorre);
        var comps = new[] { rev.Id, cp.Id };
        var antes = Assert.Single(await f.Orcamento.DemandaAsync(e.ObraId, comps, [torre.Id], TipoRequisicao.Material));
        Assert.Equal(prod.Id, antes.ProdutoId);
        Assert.Equal(1820m, antes.Orcado);
        Assert.Equal(0m, antes.Requisitado);
        Assert.Equal(antes.Orcado, antes.Saldo);

        // Requisição de parte dos locais: 100 kg na sala de um apto pela composição de revestimento + 10 kg "manuais" (sem composição).
        var sala = locais.First(l => l.Tipo == GeradorEmpreendimento.TipoComodo && l.AmbienteModeloId == e.SalaAp2d);
        var r = new Requisicao { ObraId = e.ObraId, Tipo = TipoRequisicao.Material, Atividade = "Revestimento de piso", DataEntrega = DateOnly.FromDateTime(DateTime.Today.AddDays(30)) };
        var item = new RequisicaoItem { RequisicaoId = r.Id, ProdutoId = prod.Id, Descricao = prod.Nome, Unidade = "KG", Quantidade = 110 };
        item.Locais.Add(new RequisicaoItemLocal { RequisicaoItemId = item.Id, LocalId = sala.Id, ComposicaoId = rev.Id, Quantidade = 100 });
        item.Locais.Add(new RequisicaoItemLocal { RequisicaoItemId = item.Id, LocalId = sala.Id, ComposicaoId = null, Quantidade = 10 });
        r.Itens.Add(item);
        await f.Requisicoes.SalvarRequisicaoAsync(r);

        var depois = Assert.Single(await f.Orcamento.DemandaAsync(e.ObraId, comps, [torre.Id], TipoRequisicao.Material));
        Assert.Equal(1820m, depois.Orcado);
        // As duas composições usam o produto naquela sala: o requisitado sem composição (10) abate uma vez só → 110, não 120.
        Assert.Equal(110m, depois.Requisitado);
        Assert.Equal(1710m, depois.Saldo);
        Assert.Equal(110m, depois.Locais.Where(x => x.LocalId == sala.Id).Sum(x => x.Requisitado));
        // A SPE beneficiária sai dos locais.
        await using var db2 = f.Fabrica.CriarLeitura();
        Assert.Equal(e.SpeId, await db2.Requisicoes.Where(x => x.Id == r.Id).Select(x => x.SpeId).SingleAsync());
    }
}
