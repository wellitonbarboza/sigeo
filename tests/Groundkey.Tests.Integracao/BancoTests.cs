namespace Groundkey.Tests.Integracao;

[Collection(ColecaoBanco.Nome)]
public sealed class BancoTests(BancoFixture f)
{
    /// <summary>Cenário 7: dois usuários editam os itens da mesma composição a partir da mesma versão — o segundo não sobrescreve em silêncio.</summary>
    [FatoComBanco]
    public async Task Duas_edicoes_da_mesma_versao_a_segunda_da_conflito_de_concorrencia()
    {
        var p = await Dados.CriarProdutoAsync(f, "Cimento ficticio", 1.5m);
        var c = new Composicao
        {
            Codigo = "CON" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(), Descricao = "Chapisco", Unidade = "m²",
            Itens =
            [
                new() { Ordem = 1, Tipo = TipoInsumo.Material, ProdutoId = p.Id, Descricao = p.Nome, Unidade = "KG", Coeficiente = 1 },
                new() { Ordem = 2, Tipo = TipoInsumo.MaoObraEmpreitada, Descricao = "Empreitada", Unidade = "m²", Coeficiente = 1, CustoUnitario = 8 },
            ]
        };
        await f.Orcamento.SalvarComposicaoAsync(c);

        async Task<Composicao> AbrirTela()
        {
            await using var db = f.Fabrica.CriarLeitura();
            return await db.Composicoes.Include(x => x.Itens).SingleAsync(x => x.Id == c.Id);
        }
        var telaA = await AbrirTela();
        var telaB = await AbrirTela();
        Assert.Equal(telaA.RowVersion, telaB.RowVersion);

        telaA.Itens.Single(i => i.Ordem == 1).Coeficiente = 1.2m;
        await f.Orcamento.SalvarComposicaoAsync(telaA);

        telaB.Itens.Single(i => i.Ordem == 2).Coeficiente = 1.1m; // só os itens mudaram: o agregado ainda confere a versão
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => f.Orcamento.SalvarComposicaoAsync(telaB));

        var final = await AbrirTela();
        Assert.Equal(1.2m, final.Itens.Single(i => i.Ordem == 1).Coeficiente);
        Assert.Equal(1m, final.Itens.Single(i => i.Ordem == 2).Coeficiente);
    }

    /// <summary>Cenário 8: as migrations criam os índices únicos que protegem contra linha duplicada.</summary>
    [FatoComBanco]
    public async Task Migracao_cria_indices_unicos_esperados()
    {
        string[] esperados = ["UX_CustosMaoObra_Linha", "UX_Transferencias_Recebimento", "UX_FornecedorDocumentos_Competencia"];
        await using var db = f.Fabrica.CriarLeitura();
        var achados = await db.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sys.indexes WHERE is_unique = 1 AND name IN ('UX_CustosMaoObra_Linha', 'UX_Transferencias_Recebimento', 'UX_FornecedorDocumentos_Competencia')")
            .ToListAsync();
        Assert.Equal(esperados.Order(), achados.Order());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }
}
