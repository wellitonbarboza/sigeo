using Sigeo.Contratos;
using Sigeo.Domain.Common;

namespace Sigeo.Tests;

public sealed class AcessoTelasTests
{
    [Theory]
    [InlineData("sistema/perfis", "sistema/perfis")]
    [InlineData("/sistema/permissoes?x=1", "sistema/permissoes")]
    [InlineData("obra/modelos/fvm", "obra/modelos/fvm")]
    [InlineData("obra/mapa-torre", "obra/mapa-torre")]
    [InlineData("obra/mapa", "obra/mapa")]
    [InlineData("engenharia/diario/imprimir", "engenharia/diario")]
    [InlineData("engenharia/empreendimento", "engenharia/obras")]
    [InlineData("campo/fvs/0198f3a2-0000-7000-8000-000000000000", "obra/fvs")]
    [InlineData("cotacoes/mapa", "cotacoes")]
    [InlineData("engenharia/planejamento/imprimir", "engenharia/planejamento")]
    [InlineData("engenharia/cronograma", "engenharia/cronograma")]
    [InlineData("engenharia/last-planner?aba=medio", "engenharia/last-planner")]
    public void Endereco_cai_na_tela_certa(string caminho, string rota) => Assert.Equal(rota, CatalogoTelas.TelaDe(caminho)?.Rota);

    [Theory]
    [InlineData("")]
    [InlineData("mensagens")]
    [InlineData("tarefas")]
    [InlineData("painel")]
    [InlineData("plataforma/construtoras")]
    [InlineData("pedidosx")]
    public void Telas_livres_ficam_fora_do_catalogo(string caminho) => Assert.Null(CatalogoTelas.TelaDe(caminho));

    [Fact]
    public void Situacao_considera_contratado_perfil_e_impressao()
    {
        var m = new MapaAcesso(false, new() { ["engenharia/diario"] = "VI", ["pedidos"] = "VX" }, ["sst/pt"]);
        Assert.Equal(SituacaoTela.Liberada, CatalogoTelas.Situacao(m, "engenharia/diario", out _));
        Assert.Equal(SituacaoTela.SemPermissao, CatalogoTelas.Situacao(m, "engenharia/diario/imprimir", out _));
        Assert.Equal(SituacaoTela.NaoContratada, CatalogoTelas.Situacao(m, "sst/pt", out _));
        Assert.Equal(SituacaoTela.SemPermissao, CatalogoTelas.Situacao(m, "contratos", out var t));
        Assert.Equal("Contratos", t!.Nome);
        Assert.Equal(SituacaoTela.Livre, CatalogoTelas.Situacao(m, "mensagens", out _));
    }

    [Fact]
    public void Excecoes_ida_e_volta_e_aplicacao()
    {
        var exc = CatalogoTelas.LerExcecoes("pedidos=+A-E;requisicoes=-V");
        Assert.True(exc["pedidos"]['A']);
        Assert.False(exc["pedidos"]['E']);
        Assert.Equal("pedidos=-E+A;requisicoes=-V", CatalogoTelas.EscreverExcecoes(exc));
        Assert.Equal("VIAX", CatalogoTelas.Aplicar("VIEX", exc["pedidos"], "VIEAX"));
        Assert.Equal("", CatalogoTelas.Aplicar("VIEX", exc["requisicoes"], "VIEAX")); // sem Ver, nada vale
        Assert.Equal("VX", AcaoTela.Normalizar("XAV", "VX")); // ação que não existe na tela cai
        Assert.Equal("", AcaoTela.Normalizar("IE", "VIEX"));
    }

    [Fact]
    public void Perfis_padrao_espelham_as_areas()
    {
        var adm = PerfisPadrao.Regras(PerfilUsuario.Administrador);
        Assert.All(CatalogoTelas.Telas.Values, t => Assert.Equal(t.Acoes, adm[t.Rota]));
        var eng = PerfisPadrao.Regras(PerfilUsuario.Engenharia);
        Assert.Equal("VIEAX", eng["engenharia/diario"]);
        Assert.Equal("VAX", eng["pedidos"]);
        Assert.False(eng.ContainsKey("sistema/perfis"));
        var ter = PerfisPadrao.Regras(PerfilUsuario.Terceiro);
        Assert.Equal(2, ter.Count);
        Assert.All(Enum.GetValues<PerfilUsuario>(), p => Assert.All(PerfisPadrao.Regras(p), r => Assert.Contains('V', r.Value)));
    }

    [Fact]
    public void Planejamento_dividido_indicadores_so_leitura_edicao_e_last_planner_separados()
    {
        Assert.Equal("VX", CatalogoTelas.Telas["engenharia/planejamento"].Acoes);
        var dir = PerfisPadrao.Regras(PerfilUsuario.Diretoria);
        Assert.Contains('I', dir["engenharia/cronograma"]);
        Assert.Contains('A', dir["engenharia/cronograma"]);
        var campo = PerfisPadrao.Regras(PerfilUsuario.Campo);
        Assert.Contains('I', campo["engenharia/last-planner"]);
        Assert.DoesNotContain('I', campo["engenharia/cronograma"]);
        Assert.Equal("VX", campo["engenharia/planejamento"]);
    }
}
