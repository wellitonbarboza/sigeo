using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;

namespace QualyHub.Infrastructure.Importacao;

/// <summary>Lê a planilha de modelos de FVS/FVM (abas Modelos, Itens e Ensaios; cabeçalho na linha 1).</summary>
public static class LeitorKitModelos
{
    public static KitModelos Ler(Stream arquivo)
    {
        using var wb = Planilha.Abrir(arquivo);
        var erros = new List<string>();
        var modelos = AbasPlanilha.Linhas(wb, erros, "Modelos", true).Select(l => new KitModelo(l.N,
            l.Enum("tipo", Tipos), l.Txt("codigo"), l.Txt("titulo"), l.Int("revisao"), l.TxtOpc("its"), l.TxtOpc("nomedaits"),
            l.TxtOpc("material"), l.TxtOpc("nomedomaterial"), l.TxtOpc("normaespecificacao") ?? l.TxtOpc("norma"), l.TxtOpc("amostragem"),
            l.TxtOpc("momentodainspecao") ?? l.TxtOpc("momento"), l.TxtOpc("unidadedelocal") ?? l.TxtOpc("nivel"),
            l.Bool("liberamedicao", true), l.Bool("quarentenaateensaio", false), l.Bool("rastrearaplicacao", false),
            l.TxtOpc("prerequisitosorientacoes") ?? l.TxtOpc("orientacoes"), l.Bool("publicar", true))).ToList();
        var itens = AbasPlanilha.Linhas(wb, erros, "Itens", true).Select(l => new KitItem(l.N,
            l.Txt("modelo"), l.Int("ordem"), l.Txt("itemdeverificacao") is { Length: > 0 } d ? d : l.Txt("item"), l.Txt("metodo"), l.Txt("criterio"),
            l.Bool("exigeequipamento", false), l.Enum("foto", Fotos), l.Bool("bloqueia", true), l.Dec("valorminimo"), l.Dec("valormaximo"))).ToList();
        var ensaios = AbasPlanilha.Linhas(wb, erros, "Ensaios", false).Select(l => new KitEnsaio(l.N,
            l.Txt("modelo"), l.Txt("ensaio"), l.TxtOpc("norma"), l.Enum("momento", Momentos), l.TxtOpc("prazodias"), l.Txt("frequencia"),
            l.Enum("executante", Executantes), l.Txt("criterio"), l.Enum("acaonareprovacao", Acoes))).ToList();
        ValidacaoException.Se(erros);
        return new KitModelos(modelos, itens, ensaios);
    }

    /// <summary>
    /// Lê vários arquivos de uma vez: planilhas de kit (abas Modelos/Itens/Ensaios) e fichas FVS da empresa (aba BANCO DE DADOS),
    /// uma ficha por arquivo. Tudo vira um só kit para análise e importação.
    /// </summary>
    public static KitModelos LerVarios(IReadOnlyList<(string Nome, byte[] Conteudo)> arquivos)
    {
        var modelos = new List<KitModelo>(); var itens = new List<KitItem>(); var ensaios = new List<KitEnsaio>();
        var erros = new List<string>();
        foreach (var (nome, conteudo) in arquivos)
        {
            try
            {
                if (LeitorDocumentoSgq.LerFichaFvs(new MemoryStream(conteudo), nome, modelos.Count + 1) is { } ficha)
                {
                    if (ficha.Itens.Count == 0) erros.Add($"{nome}: ficha {ficha.Modelo.Codigo} sem itens na tabela (Nº | Item | Método | Critério).");
                    modelos.Add(ficha.Modelo); itens.AddRange(ficha.Itens);
                    continue;
                }
                var kit = Ler(new MemoryStream(conteudo));
                modelos.AddRange(kit.Modelos); itens.AddRange(kit.Itens); ensaios.AddRange(kit.Ensaios);
            }
            catch (ValidacaoException e) { erros.AddRange(e.Erros.Select(x => $"{nome}: {x}")); }
            catch (RegraDeNegocioException e) { erros.Add($"{nome}: {e.Message}"); }
        }
        ValidacaoException.Se(erros);
        return new KitModelos(modelos, itens, ensaios);
    }

    private static readonly Dictionary<string, TipoModelo> Tipos = new() { ["fvs"] = TipoModelo.FVS, ["fvm"] = TipoModelo.FVM };
    private static readonly Dictionary<string, FotoItem> Fotos = new()
    { ["opcional"] = FotoItem.Opcional, ["obrigatoria"] = FotoItem.Obrigatoria, ["sempre"] = FotoItem.Obrigatoria, ["senaoconforme"] = FotoItem.SoEmNaoConformidade, ["soemnaoconformidade"] = FotoItem.SoEmNaoConformidade, [""] = FotoItem.SoEmNaoConformidade };
    private static readonly Dictionary<string, MomentoEnsaio> Momentos = new() { ["nachegada"] = MomentoEnsaio.NaChegada, ["posterior"] = MomentoEnsaio.Posterior, [""] = MomentoEnsaio.NaChegada };
    private static readonly Dictionary<string, ExecutanteEnsaio> Executantes = new() { ["obra"] = ExecutanteEnsaio.Obra, ["laboratorioexterno"] = ExecutanteEnsaio.LaboratorioExterno, ["laboratorio"] = ExecutanteEnsaio.LaboratorioExterno, [""] = ExecutanteEnsaio.Obra };
    private static readonly Dictionary<string, AcaoReprovacaoEnsaio> Acoes = new() { ["recusarcarga"] = AcaoReprovacaoEnsaio.RecusarCarga, ["abrirrnc"] = AcaoReprovacaoEnsaio.AbrirRnc, ["rnccomlocais"] = AcaoReprovacaoEnsaio.RncComLocais, [""] = AcaoReprovacaoEnsaio.AbrirRnc };
}
