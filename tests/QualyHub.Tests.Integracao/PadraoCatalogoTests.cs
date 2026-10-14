using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Tests.Integracao;

/// <summary>Padrão do catálogo: reclassificação por regra, grupo × material controlado × tipo de custo × fase, insumos novos sem duplicar. Dados fictícios.</summary>
[Collection(ColecaoBanco.Nome)]
public sealed class PadraoCatalogoTests(BancoFixture f)
{
    [FatoComBanco]
    public async Task Aplica_padrao_reclassifica_liga_material_e_fase_cai_no_custo_realizado()
    {
        var s = Dados.Sufixo();
        string gAtual = $"Argamassas Ficticias {s}", gColante = $"Colante Ficticio {s}", gArg = $"Argamassa Padrao {s}";
        var mat = new MaterialControlado { Codigo = $"M{s}", Nome = $"Argamassa colante ficticia {s}" };
        await using (var db = f.Fabrica.Criar())
        {
            db.MateriaisControlados.Add(mat);
            var gA = new CategoriaProduto { Nome = gAtual, Sigla = "Q" + s[..2] };
            db.CategoriasProduto.AddRange(gA, new CategoriaProduto { PaiId = gA.Id, Nome = "Geral", Sigla = "GER" });
            db.Produtos.AddRange(
                new Produto { Codigo = $"X{s}1", Nome = $"Argamassa ACIII ficticia {s}", Unidade = "SC", Grupo = gAtual, SubGrupo = "Geral" },
                new Produto { Codigo = $"X{s}2", Nome = $"Rejunte ficticio {s}", Unidade = "KG", Grupo = gAtual, SubGrupo = "Geral" },
                new Produto { Codigo = $"X{s}3", Nome = $"Argamassa de reboco {s}", Unidade = "SC", Grupo = gAtual, SubGrupo = "Geral" });
            await db.SaveChangesAsync();
        }
        var padrao = new PadraoCatalogo(
            [new PadraoGrupo(2, gColante, null, TipoCusto.Material, "Revestimentos de parede", mat.Codigo), new PadraoGrupo(3, gArg, null, TipoCusto.Material, "Vedações", null)],
            [new RegraReclassificacao(2, gAtual, "*", "ACIII;REJUNTE", gColante, "Colante e Rejunte", null), new RegraReclassificacao(3, gAtual, "*", null, gArg, "Argamassa", null)],
            [new ProdutoPadrao(2, $"Rejunte ficticio {s}", "kg", gColante, "Colante e Rejunte", null, null, 9m),
             new ProdutoPadrao(3, $"Argamassa nova {s}", "UND", gArg, "Argamassa", TipoCusto.Material, $"ERP{s}", 30m)])
        { ListaOficialMateriais = false };
        var svc = new PadraoCatalogoServico(f.Fabrica, new CodigosProdutoServico(f.Fabrica));
        var previa = await svc.AnalisarAsync(padrao);
        Assert.Equal(3, previa.ProdutosMovidos);
        Assert.Equal((1, 1), (previa.ProdutosNovos, previa.ProdutosCompletados));
        Assert.Contains(previa.CategoriasDesativadas, c => c == gAtual);
        await svc.AplicarAsync(padrao);

        await using (var db = f.Fabrica.CriarLeitura())
        {
            var ps = await db.Produtos.Where(p => p.Nome.EndsWith(s)).ToListAsync();
            Assert.Equal(4, ps.Count);
            Assert.All(ps.Where(p => p.Grupo == gColante), p => Assert.Equal(mat.Id, p.MaterialControladoId));
            Assert.Equal(2, ps.Count(p => p.Grupo == gColante));
            Assert.All(ps, p => Assert.Equal(TipoCusto.Material, p.TipoCusto)); // herdado do grupo
            var novo = ps.Single(p => p.CodigoErp == $"ERP{s}");
            Assert.Equal("UN", novo.Unidade);
            Assert.Matches(@"^[A-Z0-9]{3}-[A-Z0-9]{3}-\d{4}$", novo.Codigo);
            var g = await db.CategoriasProduto.SingleAsync(c => c.PaiId == null && c.Nome == gColante);
            Assert.Equal("Revestimentos de parede", g.FasePadrao);
            Assert.False((await db.CategoriasProduto.SingleAsync(c => c.PaiId == null && c.Nome == gAtual)).Ativo);
        }
        // Reaplicar não duplica nem move de novo.
        var segunda = await svc.AplicarAsync(padrao);
        Assert.Equal((0, 0, 2), (segunda.ProdutosMovidos, segunda.ProdutosNovos, segunda.ProdutosCompletados));

        // Compra sem composição: custo realizado cai na fase padrão do grupo do produto.
        var obra = await Dados.CriarObraAsync(f);
        var forn = await Dados.CriarFornecedorAsync(f);
        Guid prodId;
        await using (var db = f.Fabrica.CriarLeitura()) prodId = (await db.Produtos.SingleAsync(p => p.CodigoErp == $"ERP{s}")).Id;
        var pedido = new Pedido { ObraId = obra, Numero = Random.Shared.Next(100_000, 900_000), FornecedorId = forn, Entrega = DateOnly.FromDateTime(DateTime.Today), Status = StatusPedido.EnviadoFornecedor };
        var pi = new PedidoItem { PedidoId = pedido.Id, ProdutoId = prodId, Descricao = "Argamassa nova", Unidade = "UN", Quantidade = 10, ValorUnitario = 30 };
        pedido.Itens.Add(pi);
        await using (var db = f.Fabrica.Criar()) { db.Pedidos.Add(pedido); await db.SaveChangesAsync(); }
        await new RecebimentosServico(f.Fabrica, f.Usuario, f.Relogio).RegistrarRecebimentoAsync(new Recebimento
        {
            PedidoId = pedido.Id, NumeroNotaFiscal = "4001", DataRecebimento = DateTimeOffset.Now,
            FvmPrazo = ResultadoCriterio.Aprovado, FvmQualidade = ResultadoCriterio.Aprovado, FvmAtendimento = ResultadoCriterio.Aprovado,
            Itens = [new RecebimentoItem { PedidoItemId = pi.Id, Descricao = pi.Descricao, Unidade = "UN", QtdPedida = 10, QtdRecebida = 10 }]
        });
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var c = await db.CustosRealizados.SingleAsync(x => x.PedidoId == pedido.Id);
            Assert.Equal("Vedações", c.Fase);
            Assert.Equal(TipoCusto.Material, c.TipoCusto);
        }
    }
}
