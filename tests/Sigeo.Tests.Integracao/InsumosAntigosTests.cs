using System.Security.Cryptography;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Importacao;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Tests.Integracao;

/// <summary>Insumos antigos × padronizados: filtro "Cadastro", inativação dos sem uso e substituição pelo equivalente. Dados fictícios.</summary>
[Collection(ColecaoBanco.Nome)]
public sealed class InsumosAntigosTests(BancoFixture f)
{
    private sealed record Cenario(Guid Obra, Produto Padrao, Produto Fora, Produto AClassificar, Produto ComUso, Produto Similar);

    private async Task<Cenario> MontarAsync()
    {
        var suf = Dados.Sufixo();
        var obra = await Dados.CriarObraAsync(f);
        var grupo = new CategoriaProduto { Nome = $"Hidraulica {suf}", Sigla = suf[..3] };
        var sub = new CategoriaProduto { Nome = "Conexoes", Sigla = "CNX", PaiId = grupo.Id };
        Produto P(string cod, string nome, string? g, string? s, string un = "UN") => new() { Codigo = $"{cod}-{suf}", Nome = nome, Unidade = un, Grupo = g, SubGrupo = s };
        var padrao = P("HID", $"Joelho PVC soldavel 25 mm 90 graus {suf}", grupo.Nome, "Conexoes");
        var similar = P("HID2", $"Luva PVC soldavel 25 mm {suf}", grupo.Nome, "Conexoes");
        var fora = P("ANT1", $"JOELHO PVC SOLD 25MM 90G {suf}", "Hidraulica antiga", "Diversos");
        var aClass = P("ANT2", $"Prego 17x27 cabeca {suf}", "A Cadastrar", null, "KG");
        var comUso = P("ANT3", $"Arame recozido 18 {suf}", null, null, "KG");
        await using var db = f.Fabrica.Criar();
        db.CategoriasProduto.AddRange(grupo, sub);
        db.Produtos.AddRange(padrao, similar, fora, aClass, comUso);
        await db.SaveChangesAsync();
        return new Cenario(obra, padrao, fora, aClass, comUso, similar);
    }

    [FatoComBanco]
    public async Task Filtro_de_cadastro_separa_padronizados_fora_do_catalogo_e_a_classificar()
    {
        var c = await MontarAsync();
        var cad = new CadastrosServico(f.Fabrica);
        async Task<List<LinhaProduto>> Lista(FiltroCadastroProduto filtro) =>
            (await cad.ListarProdutosAsync(new FiltroProdutos(Texto: c.Padrao.Nome[^6..], Cadastro: filtro), 0, 50, default)).Itens.ToList();

        var todos = await Lista(FiltroCadastroProduto.Todos);
        Assert.Equal(5, todos.Count);
        Assert.Equal(SituacaoCadastroProduto.Padronizado, todos.Single(l => l.Id == c.Padrao.Id).Situacao);
        Assert.Equal(SituacaoCadastroProduto.ForaDoCatalogo, todos.Single(l => l.Id == c.Fora.Id).Situacao);
        Assert.Equal(SituacaoCadastroProduto.AClassificar, todos.Single(l => l.Id == c.AClassificar.Id).Situacao);

        Assert.Equal(new[] { c.Padrao.Id, c.Similar.Id }.Order(), (await Lista(FiltroCadastroProduto.Padronizados)).Select(l => l.Id).Order());
        Assert.Equal(new[] { c.Fora.Id }, (await Lista(FiltroCadastroProduto.ForaDoCatalogo)).Select(l => l.Id));
        Assert.Equal(new[] { c.AClassificar.Id, c.ComUso.Id }.Order(), (await Lista(FiltroCadastroProduto.AClassificar)).Select(l => l.Id).Order());
        Assert.Equal(3, (await Lista(FiltroCadastroProduto.Antigos)).Count);
    }

    [FatoComBanco]
    public async Task Inativa_so_antigos_sem_uso_e_substitui_levando_rascunho_kit_e_vinculo_para_o_novo()
    {
        var c = await MontarAsync();
        var svc = new InsumosAntigosServico(f.Fabrica, f.Relogio);

        // Uso recente do "ComUso" (requisição) e rascunho + kit + vínculo do ERP apontando para o "Fora".
        var req = new Requisicao { ObraId = c.Obra, Numero = RandomNumberGenerator.GetInt32(700_000, 900_000), Tipo = TipoRequisicao.Material, Atividade = "Teste", DataEntrega = DateOnly.FromDateTime(DateTime.Today.AddDays(10)) };
        req.Itens.Add(new RequisicaoItem { RequisicaoId = req.Id, ProdutoId = c.ComUso.Id, Descricao = c.ComUso.Nome, Unidade = "KG", Quantidade = 5 });
        req.Itens.Add(new RequisicaoItem { RequisicaoId = req.Id, ProdutoId = c.Fora.Id, Descricao = c.Fora.Nome, Unidade = "UN", Quantidade = 40 });
        var kit = new KitMaterial { Codigo = $"K{Dados.Sufixo()}", Nome = "Kit teste" };
        kit.Itens.Add(new KitMaterialItem { KitId = kit.Id, ProdutoId = c.Fora.Id, Quantidade = 12 });
        kit.Itens.Add(new KitMaterialItem { KitId = kit.Id, ProdutoId = c.Padrao.Id, Quantidade = 3 });
        await using (var db = f.Fabrica.Criar())
        {
            db.Requisicoes.Add(req);
            db.KitsMaterial.Add(kit);
            db.VinculosOrigem.Add(new VinculoOrigem { Tipo = TipoVinculoOrigem.Produto, Chave = $"joelho {c.Fora.Codigo}", TextoOrigem = c.Fora.Nome, DestinoId = c.Fora.Id });
            await db.SaveChangesAsync();
        }

        var meus = new[] { c.Fora.Id, c.AClassificar.Id, c.ComUso.Id };
        var previa = await svc.PreverInativacaoAsync(meus);
        Assert.Equal(3, previa.Antigos);
        Assert.Equal(2, previa.ComUso); // ComUso (requisição) e Fora (requisição + kit)
        Assert.Equal(new[] { c.AClassificar.Id }, previa.Amostra.Select(a => a.Id));
        Assert.Equal(1, await svc.InativarSemUsoAsync(meus));

        var sugestoes = await svc.SugerirEquivalentesAsync(c.Fora.Id, null);
        Assert.Equal(c.Padrao.Id, sugestoes.First().Id);
        Assert.DoesNotContain(sugestoes, s => s.Id == c.Fora.Id);

        var r = await svc.SubstituirAsync(c.Fora.Id, c.Padrao.Id);
        Assert.Equal(1, r.RequisicoesAtualizadas);
        Assert.Equal(1, r.KitsAtualizados);
        Assert.Equal(1, r.VinculosImportacao);

        await using var dbl = f.Fabrica.CriarLeitura();
        var fora = await dbl.Produtos.SingleAsync(p => p.Id == c.Fora.Id);
        Assert.False(fora.Ativo);
        Assert.Equal(c.Padrao.Id, fora.SubstituidoPorId);
        Assert.False((await dbl.Produtos.SingleAsync(p => p.Id == c.AClassificar.Id)).Ativo);
        Assert.True((await dbl.Produtos.SingleAsync(p => p.Id == c.ComUso.Id)).Ativo);
        var item = await dbl.RequisicaoItens.SingleAsync(i => i.RequisicaoId == req.Id && i.Quantidade == 40);
        Assert.Equal(c.Padrao.Id, item.ProdutoId);
        Assert.Equal(c.Padrao.Nome, item.Descricao);
        var kitItem = await dbl.KitMaterialItens.SingleAsync(k => k.KitId == kit.Id); // fundiu com o item que já existia
        Assert.Equal(c.Padrao.Id, kitItem.ProdutoId);
        Assert.Equal(15, kitItem.Quantidade);
        Assert.Equal(c.Padrao.Id, (await dbl.VinculosOrigem.SingleAsync(v => v.TextoOrigem == c.Fora.Nome)).DestinoId);

        await Assert.ThrowsAsync<Sigeo.Domain.Aprovacoes.RegraDeNegocioException>(() => svc.SubstituirAsync(c.Similar.Id, c.Fora.Id));
    }
}
