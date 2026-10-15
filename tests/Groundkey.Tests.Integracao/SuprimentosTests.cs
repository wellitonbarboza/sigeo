using Groundkey.Domain.Suprimentos;

namespace Groundkey.Tests.Integracao;

[Collection(ColecaoBanco.Nome)]
public sealed class SuprimentosTests(BancoFixture f)
{
    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Cenário 3: excluir um item com quebra por local e salvar de novo não esbarra na FK da quebra.</summary>
    [FatoComBanco]
    public async Task Requisicao_excluir_item_com_quebra_por_local_nao_da_erro_de_fk()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var (rev, _, prod, _) = await Dados.MontarOrcamentoAsync(f, e);
        var salas = (await Dados.LocaisAsync(f, e.ObraId)).Where(l => l.AmbienteModeloId == e.SalaAp2d).Take(2).ToList();

        var r = new Requisicao { ObraId = e.ObraId, Tipo = TipoRequisicao.Material, Atividade = "Revestimento de piso", DataEntrega = Hoje.AddDays(30) };
        foreach (var s in salas)
        {
            var i = new RequisicaoItem { RequisicaoId = r.Id, ProdutoId = prod.Id, Descricao = prod.Nome, Unidade = "KG", Quantidade = 100 };
            i.Locais.Add(new RequisicaoItemLocal { RequisicaoItemId = i.Id, LocalId = s.Id, ComposicaoId = rev.Id, Quantidade = 100 });
            r.Itens.Add(i);
        }
        await f.Requisicoes.SalvarRequisicaoAsync(r);

        // Como a tela: recarrega, tira o primeiro item e salva.
        Requisicao tela;
        await using (var db = f.Fabrica.CriarLeitura())
            tela = await db.Requisicoes.Include(x => x.Itens).ThenInclude(i => i.Locais).SingleAsync(x => x.Id == r.Id);
        var removido = tela.Itens.OrderBy(i => i.Locais.First().LocalId == salas[0].Id ? 0 : 1).First();
        tela.Itens.Remove(removido);
        await f.Requisicoes.SalvarRequisicaoAsync(tela);

        await using var db2 = f.Fabrica.CriarLeitura();
        var itens = await db2.RequisicaoItens.Where(i => i.RequisicaoId == r.Id).Select(i => i.Id).ToListAsync();
        var restante = Assert.Single(itens);
        Assert.NotEqual(removido.Id, restante);
        Assert.False(await db2.RequisicaoItemLocais.AnyAsync(x => x.RequisicaoItemId == removido.Id));
        Assert.Equal(1, await db2.RequisicaoItemLocais.CountAsync(x => x.RequisicaoItemId == restante));
    }

    private Contrato NovoContrato(Guid obraId, Guid fornecedorId, decimal qtd, params (Guid Local, decimal Qtd)[] locais)
    {
        var k = new Contrato
        {
            ObraId = obraId, FornecedorId = fornecedorId, Categoria = "Consultoria", Objeto = "Serviço fictício de teste",
            VigenciaInicio = Hoje, VigenciaFim = Hoje.AddMonths(6)
        };
        var i = new ContratoItem { ContratoId = k.Id, Item = "1.1", Descricao = "Revestimento de piso", Unidade = "m²", Quantidade = qtd, PrecoUnitario = 50 };
        foreach (var (l, q) in locais) i.Locais.Add(new ContratoItemLocal { ContratoItemId = i.Id, LocalId = l, Quantidade = q });
        k.Itens.Add(i);
        return k;
    }

    private async Task<Contrato> RecarregarContratoAsync(Guid id)
    {
        await using var db = f.Fabrica.CriarLeitura();
        return await db.Contratos.Include(x => x.Itens).ThenInclude(i => i.Locais).Include(x => x.ServicosVinculados).SingleAsync(x => x.Id == id);
    }

    /// <summary>Cenário 4: quebra por local reescalona com a quantidade do item; soma maior que o item é recusada.</summary>
    [FatoComBanco]
    public async Task Contrato_reescalona_locais_e_recusa_soma_maior_que_o_item()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var fornecedor = await Dados.CriarFornecedorAsync(f);
        var aptos = (await Dados.LocaisAsync(f, e.ObraId)).Where(l => l.Tipo == GeradorEmpreendimento.TipoApartamento).OrderBy(l => l.Codigo).Take(2).ToList();
        var k = NovoContrato(e.ObraId, fornecedor, 10, (aptos[0].Id, 6), (aptos[1].Id, 4));
        await f.Contratos.SalvarContratoAsync(k);

        var tela = await RecarregarContratoAsync(k.Id);
        Assert.Equal(10m, tela.Itens.Single().Locais.Sum(l => l.Quantidade));
        tela.Itens.Single().Quantidade = 20; // locais continuam 6 + 4 (somam a quantidade antiga) → reescalona
        await f.Contratos.SalvarContratoAsync(tela);

        var salvo = await RecarregarContratoAsync(k.Id);
        var ls = salvo.Itens.Single().Locais.ToDictionary(l => l.LocalId, l => l.Quantidade);
        Assert.Equal(12m, ls[aptos[0].Id]);
        Assert.Equal(8m, ls[aptos[1].Id]);
        Assert.Equal(20m, ls.Values.Sum());

        var invalido = await RecarregarContratoAsync(k.Id);
        foreach (var l in invalido.Itens.Single().Locais) l.Quantidade = 15; // 30 > 20
        await Assert.ThrowsAsync<ValidacaoException>(() => f.Contratos.SalvarContratoAsync(invalido));
    }

    /// <summary>Cenário 5: duas linhas da medição no mesmo local somam no acumulado do local e a trava pede aditivo.</summary>
    [FatoComBanco]
    public async Task Medicao_duas_linhas_do_mesmo_local_somam_no_acumulado_e_trava_pede_aditivo()
    {
        var e = await Dados.MontarEmpreendimentoAsync(f);
        var fornecedor = await Dados.CriarFornecedorAsync(f);
        var aptos = (await Dados.LocaisAsync(f, e.ObraId)).Where(l => l.Tipo == GeradorEmpreendimento.TipoApartamento).OrderBy(l => l.Codigo).Take(2).ToList();
        var k = NovoContrato(e.ObraId, fornecedor, 10, (aptos[0].Id, 6), (aptos[1].Id, 4));
        await f.Contratos.SalvarContratoAsync(k);
        // Pré-condição montada direto no banco: ativar pelo fluxo exige aprovação da Diretoria + assinaturas (fora do escopo deste cenário).
        await using (var db = f.Fabrica.Criar())
        {
            var c = await db.Contratos.SingleAsync(x => x.Id == k.Id);
            c.Status = StatusContrato.Ativo;
            await db.SaveChangesAsync();
        }

        var medId = await f.Medicoes.NovaMedicaoAsync(k.Id, Hoje.AddDays(-30), Hoje);
        Guid itemId;
        await using (var db = f.Fabrica.Criar())
        {
            var linhas = await db.MedicaoItens.Where(i => i.MedicaoId == medId).ToListAsync();
            Assert.Equal(2, linhas.Count); // uma linha por local contratado
            itemId = linhas[0].ContratoItemId;
            // Segunda linha no mesmo local (ex.: duas frentes no mesmo apartamento).
            db.MedicaoItens.Add(new MedicaoItem { MedicaoId = medId, ContratoItemId = itemId, LocalId = aptos[0].Id });
            await db.SaveChangesAsync();
        }
        List<MedicaoItem> doLocal;
        await using (var db = f.Fabrica.CriarLeitura())
            doLocal = await db.MedicaoItens.Where(i => i.MedicaoId == medId && i.LocalId == aptos[0].Id).ToListAsync();
        Assert.Equal(2, doLocal.Count);

        var qtds = doLocal.ToDictionary(i => i.Id, i => (4m, (Guid?)aptos[0].Id, (Guid?)null));
        await f.Medicoes.SalvarMedicaoAsync(medId, qtds, 10, 10, 10, 10, 10, null);

        await using (var db = f.Fabrica.CriarLeitura())
        {
            var salvas = await db.MedicaoItens.Where(i => i.MedicaoId == medId && i.LocalId == aptos[0].Id).ToListAsync();
            Assert.Equal([4m, 8m], salvas.Select(i => i.QtdAcumulada).Order().ToArray());
            Assert.Equal([0m, 4m], salvas.Select(i => i.QtdAnterior).Order().ToArray());
        }
        // 8 medidos num local contratado com 6 → trava (o restante dos bloqueios — documentos do fornecedor etc. — não importa aqui).
        var ex = await Assert.ThrowsAsync<ValidacaoException>(() => f.Medicoes.EnviarMedicaoAsync(medId));
        Assert.Contains(ex.Erros, x => x.Contains("acumulado no local maior que o contratado") && x.Contains("aditivo"));
    }
}
