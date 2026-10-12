using System.Globalization;
using ClosedXML.Excel;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;

namespace QualyHub.Infrastructure.Importacao;

/// <summary>
/// Lê a planilha do kit de tipologia (abas Tipologias, Ambientes, Mix, Serviços, Aplicação e, opcional, Insumos; cabeçalho na linha 1).
/// Cabeçalhos e listas (tipo, escopo, regra, nível) são comparados sem acento e sem caixa.
/// </summary>
public static class LeitorKitTipologia
{
    public static KitTipologia Ler(Stream arquivo)
    {
        using var wb = Planilha.Abrir(arquivo);
        var erros = new List<string>();
        var tips = AbasPlanilha.Linhas(wb, erros, "Tipologias", true).Select(l => new KitTip(l.N,
            l.Txt("codigo"), l.Txt("nome"), l.Enum("tipo", TiposTipologia), l.Int("dormitorios"), l.Int("suites"), l.Dec("areaprivativam2") ?? l.Dec("areaprivativa") ?? 0,
            l.Dec("areaequivalentem2") ?? l.Dec("areaequivalente"), l.Int("pavimentos"), l.Bool("primeiroterreo", true), l.Bool("numeracaoporandar", false),
            l.Dec("pedireitopadraom") ?? l.Dec("pedireitopadrao"), l.TxtOpc("observacoes"))).ToList();
        var ambs = AbasPlanilha.Linhas(wb, erros, "Ambientes", true).Select(l => new KitAmbiente(l.N,
            l.Txt("tipologia"), l.Int("ordem"), l.Enum("escopo", Escopos), l.Txt("nome"), l.Txt("sigla"), l.TxtOpc("categoria"),
            l.Dec("areapisom2") ?? l.Dec("areapiso") ?? 0, l.Dec("perimetrom") ?? l.Dec("perimetro") ?? 0, l.Dec("pedireitom") ?? l.Dec("pedireito"),
            l.Dec("areavaosm2") ?? l.Dec("areavaos") ?? 0, l.IntOpc("pavde"), l.IntOpc("pavate"))).ToList();
        var mix = AbasPlanilha.Linhas(wb, erros, "Mix", false).Select(l => new KitMix(l.N,
            l.Txt("torre"), l.Int("pavde"), l.Int("pavate"), l.Txt("apartamento"), l.Int("aptosporpavimento") is var q and > 0 ? q : l.Int("quantidade"))).ToList();
        var servs = AbasPlanilha.Linhas(wb, erros, "Servicos", true).Select(l => new KitServico(l.N,
            l.Txt("codigo"), l.Txt("descricao"), l.Txt("unidade"), l.TxtOpc("grupo") ?? l.TxtOpc("grupoeap"), l.Enum("regra", Regras),
            l.Dec("fator") ?? 1, l.TxtOpc("its"), l.TxtOpc("observacao"))).ToList();
        var aps = AbasPlanilha.Linhas(wb, erros, "Aplicacao", true).Select(l => new KitAplicacao(l.N,
            l.Txt("tipologia"), l.Txt("servico"), l.Enum("nivel", Alvos), l.TxtOpc("ambiente"), l.Dec("quantidadefixa"))).ToList();
        var ins = AbasPlanilha.Linhas(wb, erros, "Insumos", false).Select(l => new KitInsumo(l.N,
            l.Txt("servico"), l.Enum("tipo", TiposInsumo), l.Txt("descricao"), l.Txt("unidade"),
            l.Dec("quantidadeporunidadedoservico") ?? l.Dec("coeficiente") ?? 0, l.Dec("perda") ?? 0,
            l.Dec("custounitarior") ?? l.Dec("custounitario") ?? 0, l.TxtOpc("produtocodigo") ?? l.TxtOpc("produto"),
            l.TxtOpc("grupodoproduto"), l.TxtOpc("subgrupodoproduto"))).ToList();
        ValidacaoException.Se(erros);
        return new KitTipologia(tips, ambs, mix, servs, aps, ins);
    }

    private static readonly Dictionary<string, TipoInsumo> TiposInsumo = new()
    {
        ["material"] = TipoInsumo.Material, ["maodeobra"] = TipoInsumo.MaoObraEmpreitada, ["maodeobraempreitada"] = TipoInsumo.MaoObraEmpreitada,
        ["empreitada"] = TipoInsumo.MaoObraEmpreitada, ["maodeobrapropria"] = TipoInsumo.MaoObraPropria, ["equipamento"] = TipoInsumo.Equipamento,
        ["servicodeterceiro"] = TipoInsumo.ServicoTerceiro, ["servicoterceiro"] = TipoInsumo.ServicoTerceiro, ["terceiro"] = TipoInsumo.ServicoTerceiro,
        ["materialmaodeobra"] = TipoInsumo.ServicoTerceiro, ["verba"] = TipoInsumo.ServicoTerceiro
    };
    private static readonly Dictionary<string, TipoTipologia> TiposTipologia = new()
    { ["apartamento"] = TipoTipologia.Apartamento, ["torre"] = TipoTipologia.Torre, ["implantacao"] = TipoTipologia.Implantacao };
    private static readonly Dictionary<string, EscopoAmbiente> Escopos = new()
    {
        ["comodo"] = EscopoAmbiente.Comodo, ["pavimento"] = EscopoAmbiente.AreaComumPavimento, ["areacomumdopavimento"] = EscopoAmbiente.AreaComumPavimento,
        ["torre"] = EscopoAmbiente.AreaComumTorre, ["areacomumdatorre"] = EscopoAmbiente.AreaComumTorre,
        ["condominio"] = EscopoAmbiente.AreaComumCondominio, ["areacomumdocondominio"] = EscopoAmbiente.AreaComumCondominio
    };
    private static readonly Dictionary<string, RegraQuantidade> Regras = new()
    {
        ["areadepiso"] = RegraQuantidade.AreaPiso, ["areapiso"] = RegraQuantidade.AreaPiso, ["areadeparede"] = RegraQuantidade.AreaParede, ["areaparede"] = RegraQuantidade.AreaParede,
        ["perimetro"] = RegraQuantidade.Perimetro, ["areadeteto"] = RegraQuantidade.AreaTeto, ["areateto"] = RegraQuantidade.AreaTeto,
        ["unidade"] = RegraQuantidade.Unidade, ["fixa"] = RegraQuantidade.Fixa, ["quantidadefixa"] = RegraQuantidade.Fixa
    };
    private static readonly Dictionary<string, AlvoServico> Alvos = new()
    {
        ["ambiente"] = AlvoServico.Ambiente, ["apartamento"] = AlvoServico.Apartamento, ["pavimento"] = AlvoServico.Pavimento,
        ["torre"] = AlvoServico.Torre, ["condominio"] = AlvoServico.Condominio
    };

}
