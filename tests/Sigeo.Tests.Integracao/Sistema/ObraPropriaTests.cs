using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Tests.Integracao;

/// <summary>
/// Obra própria (dono do custo = construtora, sem SPE): gera torres sem SPE, a requisição fatura no CNPJ da construtora e,
/// quando a SPE é constituída no condomínio, locais e documentos em aberto passam para ela e a obra deixa de ser própria.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public sealed class ObraPropriaTests(BancoFixture f)
{
    [FatoComBanco]
    public async Task Obra_propria_gera_sem_spe_e_constituir_spe_leva_locais_e_documentos()
    {
        var matriz = new Estabelecimento { Codigo = "MZ" + Dados.Sufixo()[..4], Tipo = TipoEstabelecimento.Matriz, RazaoSocial = "Construtora Ficticia Ltda", Cnpj = Dados.Cnpj() };
        var obra = new Obra
        {
            Codigo = $"OP{Dados.Sufixo()}", Nome = "Obra Propria Ficticia", EnderecoEntrega = "Rua Teste, 1",
            DonoCusto = DonoCustoObra.Construtora, EstabelecimentoDonoId = matriz.Id
        };
        await using (var db = f.Fabrica.Criar()) { db.Estabelecimentos.Add(matriz); db.Obras.Add(obra); await db.SaveChangesAsync(); }

        // Sem o CNPJ da construtora, a obra própria não salva.
        Assert.Contains(EngenhariaServico.PrepararObra(new Obra { Codigo = "X", Nome = "X", EnderecoEntrega = "X", DonoCusto = DonoCustoObra.Construtora }), e => e.Contains("CNPJ da construtora"));

        var ap = new Tipologia
        {
            ObraId = obra.Id, Tipo = TipoTipologia.Apartamento, Codigo = "AP2", Nome = "Apto 2D", Dormitorios = 2, AreaPrivativa = 50, PeDireitoPadrao = 2.6m,
            Ambientes = [new() { Ordem = 1, Nome = "Sala de estar", Categoria = "Sala de estar", AreaPiso = 20, Perimetro = 18 }]
        };
        await f.Engenharia.SalvarTipologiaAsync(ap);
        var torre = new Tipologia
        {
            ObraId = obra.Id, Tipo = TipoTipologia.Torre, Codigo = "TR2", Nome = "Torre 2 pav", Pavimentos = 2, PeDireitoPadrao = 2.6m,
            Mix = [new() { Ordem = 1, PavimentoDe = 1, PavimentoAte = 2, ApartamentoTipologiaId = ap.Id, Quantidade = 2 }]
        };
        await f.Engenharia.SalvarTipologiaAsync(torre);
        var q = new Quadra { ObraId = obra.Id, Codigo = "P" + Dados.Sufixo()[..4], Nome = "Condomínio Próprio", Tipo = "Condomínio" };
        await f.Engenharia.SalvarCondominioAsync(q, null);

        // Gera sem SPE.
        var n = await f.Engenharia.GerarAsync(obra.Id, q.Id, [("Torre 1", torre.Id)]);
        Assert.True(n > 0);
        List<Local> locais;
        await using (var db = f.Fabrica.CriarLeitura()) locais = await db.Locais.Where(l => l.ObraId == obra.Id).ToListAsync();
        Assert.All(locais, l => Assert.Null(l.SpeId));

        // Requisição sem "Faturar para": fica no CNPJ da construtora; envio não pede SPE.
        var apto = locais.First(l => l.Tipo == GeradorEmpreendimento.TipoApartamento);
        var r = new Requisicao
        {
            ObraId = obra.Id, Tipo = TipoRequisicao.Material, Atividade = "Teste", DataEntrega = DateOnly.FromDateTime(DateTime.Today.AddDays(20)),
            Itens = [new RequisicaoItem { Descricao = "Cimento ficticio", Unidade = "SC", Quantidade = 10, Locais = [new RequisicaoItemLocal { LocalId = apto.Id, Quantidade = 10 }] }]
        };
        await f.Requisicoes.SalvarRequisicaoAsync(r);
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var salva = await db.Requisicoes.SingleAsync(x => x.Id == r.Id);
            Assert.Equal(matriz.Id, salva.EstabelecimentoFaturamentoId);
            Assert.Null(salva.SpeId);
        }

        // Constitui a SPE no condomínio: locais e a requisição em aberto passam para ela; a obra deixa de ser própria.
        var spe = new Spe { Codigo = "S" + Dados.Sufixo()[..4], RazaoSocial = "SPE Constituida Ficticia Ltda", Cnpj = Dados.Cnpj() };
        await using (var db = f.Fabrica.CriarLeitura()) q = await db.Quadras.AsNoTracking().SingleAsync(x => x.Id == q.Id); // como a tela: relê o condomínio
        await f.Engenharia.SalvarCondominioAsync(q, spe);
        await using (var db = f.Fabrica.CriarLeitura())
        {
            Assert.All(await db.Locais.Where(l => l.ObraId == obra.Id).ToListAsync(), l => Assert.Equal(spe.Id, l.SpeId));
            Assert.Equal(spe.Id, (await db.Requisicoes.SingleAsync(x => x.Id == r.Id)).SpeId);
            var o = await db.Obras.SingleAsync(x => x.Id == obra.Id);
            Assert.Equal(DonoCustoObra.Spe, o.DonoCusto);
        }
    }
}
