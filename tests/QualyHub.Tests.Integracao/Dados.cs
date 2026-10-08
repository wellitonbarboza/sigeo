using System.Security.Cryptography;
using QualyHub.Domain.Cadastros;

namespace QualyHub.Tests.Integracao;

/// <summary>Empreendimento fictício gerado pelo serviço real (1 condomínio, 1 torre de 4 pavimentos, 2 tipologias de apartamento).</summary>
public sealed record Empreendimento(Guid ObraId, Guid QuadraId, Guid SpeId, Tipologia Ap2d, Tipologia Ap3d, Tipologia Torre, int LocaisGerados)
{
    public Guid SalaAp2d => Ap2d.Ambientes.Single(a => a.Nome == "Sala de estar").Id;
    public Guid SalaAp3d => Ap3d.Ambientes.Single(a => a.Nome == "Sala de estar").Id;
}

/// <summary>Dados fictícios (nomes inventados, CNPJs aleatórios com dígito verificador válido).</summary>
public static class Dados
{
    /// <summary>CNPJ aleatório válido (14 dígitos, sem máscara). Gerado a cada chamada — nunca um CNPJ real fixo no código.</summary>
    public static string Cnpj()
    {
        var d = new int[14];
        for (var i = 0; i < 8; i++) d[i] = RandomNumberGenerator.GetInt32(10);
        d[8] = 0; d[9] = 0; d[10] = 0; d[11] = 1; // "0001" = matriz
        if (d.Take(8).Distinct().Count() == 1) d[0] = (d[0] + 1) % 10; // evita sequência repetida (rejeitada pelo validador)
        d[12] = Dv(d, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        d[13] = Dv(d, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        return string.Concat(d);
    }

    private static int Dv(int[] d, int[] pesos)
    {
        var resto = pesos.Select((p, i) => p * d[i]).Sum() % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    private static string Sufixo() => Convert.ToHexString(RandomNumberGenerator.GetBytes(3));

    public static async Task<Guid> CriarObraAsync(BancoFixture f)
    {
        var o = new Obra { Codigo = $"OB{Sufixo()}", Nome = "Residencial Ficticio", Cidade = "Cidade Teste", Uf = "PR", EnderecoEntrega = "Rua Teste, 100" };
        await using var db = f.Fabrica.Criar();
        db.Obras.Add(o);
        await db.SaveChangesAsync();
        return o.Id;
    }

    public static async Task<Produto> CriarProdutoAsync(BancoFixture f, string nome, decimal preco)
    {
        var p = new Produto { Codigo = $"P{Sufixo()}", Nome = nome, Unidade = "KG", Grupo = "Argamassas", PrecoReferencia = preco };
        await using var db = f.Fabrica.Criar();
        db.Produtos.Add(p);
        await db.SaveChangesAsync();
        return p;
    }

    public static async Task<Guid> CriarFornecedorAsync(BancoFixture f)
    {
        var x = new Fornecedor { CpfCnpj = Cnpj(), RazaoSocial = $"Empreiteira Ficticia {Sufixo()} Ltda", TipoFornecimento = TipoFornecimento.ServicoObra };
        await using var db = f.Fabrica.Criar();
        db.Fornecedores.Add(x);
        await db.SaveChangesAsync();
        return x.Id;
    }

    /// <summary>
    /// Tipologias AP2D (50 m², 3 cômodos) e AP3D (65 m², 4 cômodos), torre de 4 pavimentos (2×AP2D + 1×AP3D por pavimento,
    /// hall por pavimento e escada na torre), condomínio + SPE e geração de 1 torre — tudo pelos serviços reais.
    /// </summary>
    public static async Task<Empreendimento> MontarEmpreendimentoAsync(BancoFixture f)
    {
        var obraId = await CriarObraAsync(f);
        var eng = f.Engenharia;
        var ap2 = new Tipologia
        {
            ObraId = obraId, Tipo = TipoTipologia.Apartamento, Codigo = "AP2D", Nome = "Apto 2 dormitórios", Dormitorios = 2, AreaPrivativa = 50, PeDireitoPadrao = 2.6m,
            Ambientes =
            [
                new() { Ordem = 1, Nome = "Sala de estar", Categoria = "Sala de estar", AreaPiso = 20, Perimetro = 18 },
                new() { Ordem = 2, Nome = "Dormitório 1", Categoria = "Dormitório", AreaPiso = 10, Perimetro = 13 },
                new() { Ordem = 3, Nome = "BWC", Categoria = "BWC", AreaPiso = 4, Perimetro = 8 },
            ]
        };
        var ap3 = new Tipologia
        {
            ObraId = obraId, Tipo = TipoTipologia.Apartamento, Codigo = "AP3D", Nome = "Apto 3 dormitórios", Dormitorios = 3, AreaPrivativa = 65, PeDireitoPadrao = 2.6m,
            Ambientes =
            [
                new() { Ordem = 1, Nome = "Sala de estar", Categoria = "Sala de estar", AreaPiso = 25, Perimetro = 20 },
                new() { Ordem = 2, Nome = "Dormitório 1", Categoria = "Dormitório", AreaPiso = 11, Perimetro = 13.5m },
                new() { Ordem = 3, Nome = "Dormitório 2", Categoria = "Dormitório", AreaPiso = 9, Perimetro = 12 },
                new() { Ordem = 4, Nome = "BWC", Categoria = "BWC", AreaPiso = 4, Perimetro = 8 },
            ]
        };
        await eng.SalvarTipologiaAsync(ap2);
        await eng.SalvarTipologiaAsync(ap3);
        var torre = new Tipologia
        {
            ObraId = obraId, Tipo = TipoTipologia.Torre, Codigo = "TR4", Nome = "Torre 4 pavimentos", Pavimentos = 4, PeDireitoPadrao = 2.6m,
            Ambientes =
            [
                new() { Ordem = 1, Escopo = EscopoAmbiente.AreaComumPavimento, Nome = "Hall", Categoria = "Hall / circulação comum", AreaPiso = 8, Perimetro = 12 },
                new() { Ordem = 2, Escopo = EscopoAmbiente.AreaComumTorre, Nome = "Escada", Categoria = "Escada", AreaPiso = 12, Perimetro = 14 },
            ],
            Mix =
            [
                new() { Ordem = 1, PavimentoDe = 1, PavimentoAte = 4, ApartamentoTipologiaId = ap2.Id, Quantidade = 2 },
                new() { Ordem = 2, PavimentoDe = 1, PavimentoAte = 4, ApartamentoTipologiaId = ap3.Id, Quantidade = 1 },
            ]
        };
        await eng.SalvarTipologiaAsync(torre);

        var sigla = "Q" + Sufixo()[..4];
        var q = new Quadra { ObraId = obraId, Codigo = sigla, Nome = $"Condomínio {sigla}", Tipo = "Condomínio" };
        var spe = new Spe { Codigo = "S" + Sufixo()[..4], RazaoSocial = $"SPE {sigla} Empreendimentos Ltda", Cnpj = Cnpj() };
        await eng.SalvarCondominioAsync(q, spe);
        var n = await eng.GerarAsync(obraId, q.Id, [("Torre 1", torre.Id)]);
        return new Empreendimento(obraId, q.Id, spe.Id, ap2, ap3, torre, n);
    }

    /// <summary>
    /// Duas composições que usam o mesmo produto (revestimento 5 kg/m² e contrapiso 2 kg/m²), marcadas na sala das duas tipologias.
    /// Orçado do produto na torre: (8×20 + 4×25 m²) × (5 + 2) = 1820 kg.
    /// </summary>
    public static async Task<(Composicao Rev, Composicao Cp, Produto Produto, ResultadoOrcamento Resultado)> MontarOrcamentoAsync(BancoFixture f, Empreendimento e)
    {
        var p = await CriarProdutoAsync(f, "Argamassa ficticia", 2m);
        var rev = new Composicao
        {
            Codigo = "REV" + Sufixo(), Descricao = "Revestimento de piso", Unidade = "m²", Regra = RegraQuantidade.AreaPiso,
            Itens = [new() { Ordem = 1, Tipo = TipoInsumo.Material, ProdutoId = p.Id, Descricao = p.Nome, Unidade = "KG", Coeficiente = 5 }]
        };
        var cp = new Composicao
        {
            Codigo = "CPI" + Sufixo(), Descricao = "Contrapiso", Unidade = "m²", Regra = RegraQuantidade.AreaPiso,
            Itens = [new() { Ordem = 1, Tipo = TipoInsumo.Material, ProdutoId = p.Id, Descricao = p.Nome, Unidade = "KG", Coeficiente = 2 }]
        };
        var orc = f.Orcamento;
        await orc.SalvarComposicaoAsync(rev);
        await orc.SalvarComposicaoAsync(cp);
        foreach (var c in new[] { rev, cp })
        {
            await orc.MarcarAplicacaoAsync(e.Ap2d.Id, c.Id, AlvoServico.Ambiente, e.SalaAp2d, true);
            await orc.MarcarAplicacaoAsync(e.Ap3d.Id, c.Id, AlvoServico.Ambiente, e.SalaAp3d, true);
        }
        var r = await orc.GerarAsync(e.ObraId);
        return (rev, cp, p, r);
    }

    public static async Task<List<Local>> LocaisAsync(BancoFixture f, Guid obraId)
    {
        await using var db = f.Fabrica.CriarLeitura();
        return await db.Locais.Where(l => l.ObraId == obraId).ToListAsync();
    }
}
