using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;

namespace QualyHub.Infrastructure.Importacao;

/// <summary>
/// Lê a planilha do padrão do catálogo (cabeçalho na linha 1; abas opcionais, ao menos uma):
/// <list type="bullet">
/// <item><b>Grupos</b>: Grupo, Sigla, Tipo de custo, Fase padrão, Material controlado (código).</item>
/// <item><b>Reclassificar</b>: Grupo atual, Subgrupo atual (vazio ou * = todos), Nome contém (palavras separadas por ;), Grupo novo, Subgrupo novo, Motivo.</item>
/// <item><b>Produtos</b>: Nome, Unidade, Grupo, Subgrupo, Tipo de custo, Código ERP, Preço referência.</item>
/// </list>
/// </summary>
public static class LeitorPadraoCatalogo
{
    private static readonly Dictionary<string, TipoCusto> Tipos = new()
    {
        ["material"] = TipoCusto.Material, ["materiais"] = TipoCusto.Material, ["equipamento"] = TipoCusto.Equipamento, ["equipamentos"] = TipoCusto.Equipamento,
        ["maodeobra"] = TipoCusto.MaoObra, ["mo"] = TipoCusto.MaoObra, ["servico"] = TipoCusto.Servico, ["servicos"] = TipoCusto.Servico,
        ["frete"] = TipoCusto.Frete, ["fretes"] = TipoCusto.Frete, ["outros"] = TipoCusto.Outros, ["outro"] = TipoCusto.Outros
    };

    private static TipoCusto? Tipo(AbasPlanilha.Linha l) => l.TxtOpc("tipodecusto") is null ? null : l.Enum("tipodecusto", Tipos);

    public static PadraoCatalogo Ler(Stream arquivo)
    {
        using var wb = Planilha.Abrir(arquivo);
        var erros = new List<string>();
        var grupos = AbasPlanilha.Linhas(wb, erros, "Grupos", false).Select(l => new PadraoGrupo(l.N, l.Txt("grupo"), l.TxtOpc("sigla"),
            Tipo(l), l.TxtOpc("fasepadrao") ?? l.TxtOpc("fasedoorcamento") ?? l.TxtOpc("fase"), l.TxtOpc("materialcontrolado"))).ToList();
        var regras = AbasPlanilha.Linhas(wb, erros, "Reclassificar", false).Select(l => new RegraReclassificacao(l.N, l.Txt("grupoatual"), l.TxtOpc("subgrupoatual"),
            l.TxtOpc("nomecontem") ?? l.TxtOpc("contem"), l.Txt("gruponovo"), l.Txt("subgruponovo"), l.TxtOpc("motivo"))).ToList();
        var produtos = AbasPlanilha.Linhas(wb, erros, "Produtos", false).Select(l => new ProdutoPadrao(l.N, l.Txt("nome"), l.TxtOpc("unidade") ?? "UN",
            l.Txt("grupo"), l.Txt("subgrupo"), Tipo(l), l.TxtOpc("codigoerp"), l.Dec("precoreferencia") ?? l.Dec("preco"))).ToList();
        if (grupos.Count + regras.Count + produtos.Count == 0 && erros.Count == 0)
            erros.Add("Planilha sem as abas Grupos, Reclassificar ou Produtos (cabeçalho na linha 1).");
        ValidacaoException.Se(erros);
        return new PadraoCatalogo(grupos, regras, produtos);
    }
}
