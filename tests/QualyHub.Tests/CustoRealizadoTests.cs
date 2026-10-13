using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Importacao;
using Xunit;

namespace QualyHub.Tests;

/// <summary>Unidades padrão, regras do de-para de importação e rateio do custo realizado.</summary>
public class CustoRealizadoTests
{
    [Theory]
    [InlineData("und", "UN")] [InlineData("UND", "UN")] [InlineData(" un ", "UN")] [InlineData("", "UN")]
    [InlineData("m²", "M2")] [InlineData("M²", "M2")] [InlineData("M³", "M3")] [InlineData("pç", "PC")] [InlineData("PC", "PC")]
    [InlineData("MÊS", "MES")] [InlineData("vb", "VB")] [InlineData("Kg", "KG")] [InlineData("barra", "BR")] [InlineData("PCT", "PCT")]
    public void Unidades_equivalentes_viram_a_padrao(string origem, string esperado) => Assert.Equal(esperado, UnidadesPadrao.Normalizar(origem));

    [Theory]
    [InlineData("# Lapis Carpinteiro", "Lapis Carpinteiro")]
    [InlineData("1° PAVIMENTO [Instalações Hidrossanitárias - Prumadas] - Locação de mini escavadeira", "Locação de mini escavadeira")]
    [InlineData("Aço CA 50 - 6,3mm [SUPRAESTRUTURA] - Locação de munck", "Locação de munck")]
    [InlineData("AÇO  CA-60 5MM", "AÇO CA-60 5MM")]
    public void Limpa_nome_de_insumo_do_erp(string origem, string esperado) => Assert.Equal(esperado, SugestoesImportacao.LimparNome(origem));

    private static readonly string[] Fases =
        ["Infraestrutura / fundações", "Estrutura / pré-moldados", "Instalações hidrossanitárias", "Impermeabilização", "Revestimentos de piso", "Vedações",
         "Áreas comuns e urbanização", .. SugestoesImportacao.FasesIndiretas];

    [Theory]
    [InlineData("Fundações - Estacas, Blocos, Vigas", "Infraestrutura / fundações")]
    [InlineData("Transição-Baldrame", "Infraestrutura / fundações")]
    [InlineData("Supraestrutura - Alvenaria Estrutural, Lajes e Escadas", "Estrutura / pré-moldados")]
    [InlineData("Projetos, ARTs / RRTs, Alvará e Licenças", "Projetos, ARTs e licenças")]
    [InlineData("Instalações Provisórias (Água, Energia, Internet, Tapume, Placa, Edificação)", "Canteiro e instalações provisórias")]
    [InlineData("(Mat) Instalações Hidrossanitárias - Prumadas, Ramais e Conexões", "Instalações hidrossanitárias")]
    [InlineData("Medicina e Segurança do Trabalho (PGR, EPI, Exames, NR's, TST)", "Segurança e medicina do trabalho")]
    [InlineData("Salários e Encargos da Equipe Técnica e Administrativa (Incluso Equipe de Apoio)", "Equipe técnica e administrativa")]
    [InlineData("(Mat+MO) Prevenção e Combate a Incêndio (Sprinkler, Hidrantes, Sinalização)", "Prevenção e combate a incêndio")]
    [InlineData("Contrapiso e Regularização de Superfície", "Revestimentos de piso")]
    [InlineData("Impermeabilização de Lajes, Vigas Baldrame e Áreas Molhadas", "Impermeabilização")]
    [InlineData("Portões e Fechamento Perimetral", "Áreas comuns e urbanização")]
    [InlineData("Muro de Arrimo de Divisa", "Infraestrutura / fundações")]
    [InlineData("Equipamentos de Proteção Coletiva", "Segurança e medicina do trabalho")]
    [InlineData("-", "A apropriar")]
    [InlineData("Algo sem regra", "A apropriar")]
    public void Sugere_fase_da_eap_de_custo(string origem, string esperado) => Assert.Equal(esperado, SugestoesImportacao.SugerirFase(origem, Fases));

    [Fact]
    public void Sugere_local_pelo_pavimento()
    {
        var t = Guid.NewGuid(); var p1 = Guid.NewGuid(); var p2 = Guid.NewGuid(); var cob = Guid.NewGuid();
        List<SugestoesImportacao.LocalCandidato> locais =
        [
            new(Guid.NewGuid(), "Torre A", "Torre", 1), new(t, "Térreo", "Pavimento", 1), new(p1, "1º Pavimento", "Pavimento", 2),
            new(p2, "Pavimento 2", "Pavimento", 3), new(cob, "Cobertura", "Pavimento", 9)
        ];
        Assert.Equal(t, SugestoesImportacao.SugerirLocal("TÉRREO", locais));
        Assert.Equal(p1, SugestoesImportacao.SugerirLocal("1° PAVIMENTO", locais));
        Assert.Equal(p2, SugestoesImportacao.SugerirLocal("2° PAVIMENTO - APTOS", locais));
        Assert.Equal(cob, SugestoesImportacao.SugerirLocal("COBERTURA", locais));
        Assert.Null(SugestoesImportacao.SugerirLocal("Despesas Administrativas", locais));
        Assert.Null(SugestoesImportacao.SugerirLocal("5° PAVIMENTO", locais));
    }

    [Fact]
    public void Similaridade_respeita_bitola_e_unidade()
    {
        Assert.Equal(1m, SugestoesImportacao.Similaridade("Aço CA-50 10 mm", "AÇO CA 50 10MM") >= 0.6m ? 1m : 0m);
        Assert.True(SugestoesImportacao.Similaridade("Aço CA-50 10mm", "Aço CA-50 12,5mm") < 0.4m); // bitola diferente = outro produto
        var cat = new[] { new SugestoesImportacao.Candidato(Guid.NewGuid(), "CIMENTO CP II 50KG", "SC"), new SugestoesImportacao.Candidato(Guid.NewGuid(), "AREIA MEDIA", "M3") };
        var s = SugestoesImportacao.Produtos("Cimento CP-II 50kg", "sc", cat);
        Assert.Equal("CIMENTO CP II 50KG", s[0].Nome);
        Assert.True(s[0].Nota >= SugestoesImportacao.NotaVinculoAutomatico);
    }

    [Theory]
    [InlineData("Materiais", TipoCusto.Material)] [InlineData("Mão de obra", TipoCusto.MaoObra)] [InlineData("Serviço", TipoCusto.Servico)]
    [InlineData("Equipamentos", TipoCusto.Equipamento)] [InlineData("Fretes", TipoCusto.Frete)] [InlineData("Outros", TipoCusto.Outros)]
    public void Tipo_de_custo_do_erp(string origem, TipoCusto esperado) => Assert.Equal(esperado, SugestoesImportacao.TipoCustoDe(origem));

    [Fact]
    public void Categoria_so_vira_grupo_existente()
    {
        Assert.Equal("Hidráulica", SugestoesImportacao.GrupoDaCategoria("Hidráulico", ["Hidráulica", "EPI"]));
        Assert.Equal("A Cadastrar", SugestoesImportacao.GrupoDaCategoria("Hidráulico", ["EPI"]));
        Assert.Equal("A Cadastrar", SugestoesImportacao.GrupoDaCategoria("Outros", ["EPI"]));
    }

    [Fact]
    public void Rateio_por_local_fecha_o_centavo()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid(); var c = Guid.NewGuid();
        var partes = RateioCusto.Ratear(10, 100m, [(a, 1), (b, 1), (c, 1)]);
        Assert.Equal(3, partes.Count);
        Assert.Equal(100m, partes.Sum(p => p.Valor));
        Assert.Equal(10m, partes.Sum(p => p.Quantidade));
        Assert.Equal(33.34m, partes[^1].Valor);
        Assert.Single(RateioCusto.Ratear(5, 50m, []));
        Assert.Null(RateioCusto.Ratear(5, 50m, [])[0].LocalId);
    }

    [Fact]
    public void Lancamento_valida_e_trava_valor_de_origem()
    {
        var c = new CustoRealizado { ObraId = Guid.NewGuid(), Data = new DateOnly(2026, 10, 1), Descricao = "Teste", Valor = 0 };
        Assert.Contains(c.Validar(), e => e.Contains("valor"));
        Assert.False(c.ValorTravado);
        c.Origem = OrigemCusto.Recebimento;
        Assert.True(c.ValorTravado);
    }
}
